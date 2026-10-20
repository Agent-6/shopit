# ADR-0003: Permission-catalog publishing hook

- **Status:** Accepted
- **Date:** 2026-10-20
- **Issue:** [#22](https://github.com/Agent-6/shopit/issues/22)
- **Related:** [#20](https://github.com/Agent-6/shopit/issues/20) · [#12](https://github.com/Agent-6/shopit/issues/12) · ADR-0002
- **Supersedes:** —

## Context and problem statement

Every service that declares permissions must tell the Identity service about them. Before this
decision each service did that with its own startup code, in two different shapes:

| | Identity | Tenancy |
|---|---|---|
| Mechanism | `IPermissionCatalogSynchronizer.SynchronizeAsync` called from `SeedPermissionCatalog` | `IOutboxWriter.WriteAsync` + `DbContext.SaveChangesAsync`, inline in `Program.cs` |
| Where | startup scope in `Program.cs` | startup scope in `Program.cs` |
| What it knows | how to sync | how to build the event DTOs and commit them |

So a new service had to copy one of two code blocks, and the next one (the catalog service, M1) would
copy whichever it found first. The copying is not the real cost — the real cost is that the **shape of
"publish my catalog" was implicit**, so nothing could change it in one place.

## Decision drivers

- A service should say **which provider holds its permissions**, not how catalogs travel.
- The publishing mechanism must be changeable in one place (e.g. switch from outbox to a direct call).
- Startup ordering matters: a catalog must be applied before the first request authorizes against it,
  and after the database it writes to has been migrated.
- Identity is the registry, so its own catalog must not take a Kafka round-trip to become visible —
  during that window an Admin principal would authorize against an empty catalog.
- Framework must not reference a service: `ShopIt.Framework.*` may not depend on
  `ShopIt.Identity.*` (enforced by `EventContractOwnershipTests`).

## Considered options

1. **Leave it as bespoke code per service.** Cheapest now; every new service copies a block and the two
   shapes drift further apart.
2. **Move the event contract into the framework** so one hook can build the event itself. Violates the
   ownership rule settled in #20 and breaks `EventContractOwnershipTests`.
3. **A hosted service + sink abstraction, with the event factory supplied by the registrant.** The
   framework owns *when* and *once*; the service owns *what the message is*.
4. **Publish through the outbox for every service, including Identity.** One mechanism exactly, but
   Identity's catalog becomes eventually consistent with a Kafka round-trip on startup.

## Decision outcome

**Option 3.**

`AddPermissionCatalogPublishing` in `ShopIt.Framework.Persistence` registers three things:

```
IPermissionDefinitionProvider (TProvider)   → the service's catalog
IPermissionCatalogPublicationSink (TSink)   → what to do with it
PermissionCatalogPublisherHostedService     → run it once, at startup
```

`PermissionCatalogPublisher<TProvider>` is a singleton with a process-wide once-guard, so it publishes
exactly once even if it is also invoked explicitly later. It opens its own scope (the sink needs the
`DbContext`), reads `TProvider.GetGroups()`, and dies. A failure resets the guard and rethrows, so a
transient database error fails startup rather than leaving a service that believes it published.

Two overloads, differing only in the sink:

| Overload | Sink | Used by |
|---|---|---|
| `AddPermissionCatalogPublishing<TProvider, TContext>(sourceService, createEvent)` | `OutboxPermissionCatalogPublicationSink<TContext>` — writes the event and commits | services that do **not** own the registry (Tenancy today, the catalog service later) |
| `AddPermissionCatalogPublishing<TSink, TProvider>(sourceService)` | the service's own sink | Identity, whose sink calls `IPermissionCatalogSynchronizer` in place |

The event factory is a delegate, so `PermissionCatalogPublishedIntegrationEvent.Create` (a static
mapper living with the contract in `ShopIt.Identity.Events`) is passed by method group. The framework
never names the event type; the publisher that owns the message does.

The hook is a hosted service rather than a `Program.cs` call because hosted services start before the
web server accepts requests, and because it keeps the composition root to one registration line.
Migrations still run first: each service migrates before `app.Run()`, and hosted services only start
after that returns.

## Consequences

**Positive**

- A service's composition root states a fact — "this is my permission provider" — and nothing else.
- Identity's seeding ordering is preserved in effect: its sync still runs before the server listens, so
  no request can authorize against a half-populated catalog.
- The outbox path is unchanged on the wire: same topic (`PermissionCatalogPublishedIntegrationEvent`),
  same payload.
- Republishing on every start stays safe: the synchronizer upserts by name and grants only permissions
  not already held by an Admin role.

**Negative**

- One more indirection for a two-line operation; a reader must follow
  `AddPermissionCatalogPublishing` → sink → synchronizer to see what actually happens.
- The hook cannot build the event itself, so each publisher still supplies a factory delegate. That is
  deliberate: the alternative is a framework reference to a service contract.
- Identity's catalog is now applied **after** role seeding rather than before it. Verified equivalent
  in practice — `PermissionCatalogSynchronizer` grants newly added permissions to every existing Admin
  role, which is exactly what role seeding used to do from the catalog — but the two no longer share a
  single code path.

## Verification

Against the running Aspire stack, on a database that already held the catalog:

- Identity: `Published permission catalog from 'Identity' (2 group(s), 14 permission(s))` followed by
  `Permission catalog synchronized from 'Identity': 0 added, 14 updated, 0 removed (0 revoked), 0 new
  permission(s) granted`.
- Tenancy: `Queued permission catalog from 'Tenancy' in the outbox (1 group(s), 5 permission(s))`,
  consumed by Identity as `Received permission catalog from 'Tenancy' with 1 group(s)` →
  `0 added, 5 updated, 0 granted`.
- Two consecutive starts produced `Added=0, Granted=0` each time; the catalog stayed at 19 entries
  (14 Identity + 5 Tenancy) and each Admin role at 19 claims, with no duplicate
  `(RoleId, ClaimType)` rows.

**Not proven:** behaviour on a brand-new database (empty catalog) was not exercised in this run; the
grant path it depends on is the same one that ran when the catalog was first populated.
