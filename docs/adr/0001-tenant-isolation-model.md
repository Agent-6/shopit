# ADR-0001: Tenant isolation model

- **Status:** Accepted
- **Date:** 2026-09-28
- **Issue:** [#19](https://github.com/Agent-6/shopit/issues/19)
- **Epic:** [#11 Multi-tenancy foundation](https://github.com/Agent-6/shopit/issues/11)
- **Supersedes:** —

## Context and problem statement

shopit is a multi-tenant SaaS. Every service must guarantee that a request in tenant A can never read
or write tenant B's rows. The mechanism has to be settled before more services exist, because it
determines the persistence layer, the migration story, and the shape of every aggregate.

Isolation exists today in **one service only** — Identity — where it is implemented as a shared
schema with a `TenantId` discriminator and an EF Core global query filter:

```csharp
// ShopIt.Identity.Persistence/Data/ApplicationDbContext.cs
builder.Entity<TEntity>().HasIndex(e => e.TenantId);
builder.Entity<TEntity>().HasQueryFilter(e =>
    _currentTenant.Id == Guid.Empty || e.TenantId == _currentTenant.Id);
```

`ApplyTenantConfiguration` reflects over the model and applies that filter to every `ITenantEntity`.
Tenant-scoped uniqueness is maintained by *replacing* ASP.NET Identity's global indexes with composite
ones — `(NormalizedUserName, TenantId)`, `(NormalizedName, TenantId)` — in `UserConfiguration` and
`RoleConfiguration`.

Tenancy and Notifications have **no isolation at all**; the abstractions live in
`ShopIt.Identity.Domain` and so cannot be reused. That is [#16](https://github.com/Agent-6/shopit/issues/16),
not this decision.

Nothing is in production and the catalog and order services do not exist yet. The cost of choosing is at
its lowest right now and rises sharply from here.

## Decision drivers

- **A solo developer.** Operational surface has to stay small; every mechanism added is one more thing to run and debug.
- **Aspire-orchestrated services**, each already owning its own PostgreSQL instance.
- **EF Core** is the persistence stack, with per-service migrations.
- **Tenants will be many and small**, which is the normal SaaS shape — not few and large.
- **A cross-tenant read is the worst bug class in the product.** It is a data breach, not an outage.
- The **catalog and order services are about to be built** on top of whatever is chosen.

## Considered options

1. **Shared database, shared schema, `TenantId` discriminator + EF Core query filter** — the status quo, formalised.
2. **Shared database, schema-per-tenant** — one PostgreSQL schema per tenant, same tables in each.
3. **Database-per-tenant** — one database (or instance) per tenant.
4. **Shared schema + query filter + PostgreSQL row-level security (RLS)** — option 1 with the boundary also enforced by the database.

## Decision outcome

**Option 1 is accepted**, continuing and formalising what Identity already does.

It is the only option that is already implemented and proven in this codebase, and it is proportionate
to the scale: with many small tenants, options 2 and 3 multiply migration and connection-management cost
by the tenant count while buying isolation the application layer already provides.

**Option 4 is not rejected — it is deferred, deliberately.** RLS is the future hardening path and is
recorded in Follow-ups with an explicit trigger. It can be layered onto option 1 without changing the
domain model, because both use the same `TenantId` discriminator. Deferring it costs nothing now and
avoids adding a mechanism before its value can be tested.

### Why not schema-per-tenant (option 2)

- EF Core has no first-class notion of a per-tenant schema; `search_path` switching or per-schema model
  caching is required, and the migrations pipeline has to be run once per tenant.
- The number of schemas grows with the tenant count, so migrations go from O(1) to O(tenants).
- It does not remove the need for the application-level filter anyway — an unscoped connection still
  sees whichever schema is active, so the same class of bug remains possible.
- It solves a problem (noisy-neighbour resource contention) that has not been observed.

### Why not database-per-tenant (option 3)

- Every tenant needs its own connection string, migration run, backup, and restore path. For a solo
  developer this is the dominant cost of the whole system.
- Cross-tenant operations that are legitimately required — granting a newly published permission to the
  Admin role in every tenant (`PermissionCatalogSynchronizer`) — become fan-out jobs across N databases.
- It is the right answer for a handful of large regulated customers, not for many small ones.

### Consequences

**Positive**

- One migration path, one backup, one connection string per service.
- Cross-tenant administrative operations are a single query rather than a fan-out.
- Already implemented and in use across seven entities in Identity.
- Tenant-scoped uniqueness works naturally via composite indexes.
- Option 4 can be added later without a domain-model change.

**Negative**

- **Isolation is enforced by the application, not by the database.** There is no database-level backstop
  until option 4, so the guarantee rests on the architectural requirements below being met. Those
  requirements are mandatory, not aspirational — a service that does not meet them is not multi-tenant,
  whatever its code compiles to.
- **An authenticated principal with no `tenant_id` claim is treated as Host** (`Guid.Empty`), which
  *disables* the filter. The default for "no tenant context" is maximum visibility, not minimum. Host
  scope must only ever be entered deliberately — see below.
- Every tenant-scoped table carries the discriminator and needs the `TenantId` index to stay fast.
- Tenant-scoped uniqueness has to be maintained per entity by removing Identity's global indexes. It is
  easy to add a new entity and forget.

**Neutral**

- Host-level tables are deliberately **not** `ITenantEntity`: `PermissionCatalogEntry` (system-wide) and
  Tenancy's `Tenant` (the tenant registry itself). The absence of a filter on these is intentional, but
  must be stated rather than assumed.

## Mandatory for any multi-tenant service

These are requirements, not recommendations. A service meeting all three is multi-tenant; a service
missing any one of them is not, whatever its code compiles to.

1. **The `DbContext` applies the tenant configuration** (`ApplyTenantConfiguration`, or its framework
   equivalent after [#16](https://github.com/Agent-6/shopit/issues/16)). This is the requirement that
   makes a service multi-tenant: it attaches the query filter and the `TenantId` index to every
   `ITenantEntity`.
2. **All data access goes through repositories.** No raw SQL, and no ad-hoc query that steps around the
   repository layer. That is what guarantees the filter is always in play.
3. **The tenant abstractions are reused, never reimplemented.** One implementation, in the framework.

## One mechanism: `Change()`

**`_currentTenant.Change(...)` is the only supported way to change tenant scope.**

| Intent | Call |
|---|---|
| Act as a specific tenant | `_currentTenant.Change(new TenantInfo(tenantId, name))` |
| Act as the host — every tenant visible | `_currentTenant.Change(new TenantInfo(Guid.Empty, "Host"))` |

`Change()` sets the *ambient* tenant, so it moves the query filter **and** `TenantId` stamping on new
rows together. That coupling is the point: scope and write-stamping cannot drift apart.

**`IgnoreQueryFilters()` is not a supported mechanism.** It bypasses only the filter, leaving ambient
scope and stamping untouched, so the two can disagree — a row read across tenants can be written back
under the acting tenant. Host scope is reached through `Change(new TenantInfo(Guid.Empty, "Host"))`.

### The existing call sites are debt

Seven `IgnoreQueryFilters()` call sites exist today. They are **temporary**. They are not precedent, and
new ones are not accepted.

| Call site | What it actually needs | Replacement |
|---|---|---|
| `Identity.API/Program.cs:392` | Nothing — the enclosing `Change(...)` already sets the ambient scope, and the predicate already carries `TenantId` | **Redundant.** Drop it |
| `Identity.API/Program.cs:403-404` | To find joins pointing at same-named roles in *other* tenants | Genuinely cross-tenant → `Change(new TenantInfo(Guid.Empty, "Host"))` |
| `Identity.API/Program.cs:417` | Nothing — the user and the role are both in the ambient scope | **Redundant.** Drop it |
| `PermissionCatalogSynchronizer.cs:92` | Admin roles in **every** tenant | Genuinely cross-tenant → `Change(new TenantInfo(Guid.Empty, "Host"))` |
| `PermissionCatalogSynchronizer.cs:126,140` | Grant a new permission to Admin in every tenant | Genuinely cross-tenant → `Change(new TenantInfo(Guid.Empty, "Host"))` |

One call site already uses the supported pattern and is the model to follow:
`EmailConfirmationOtpRequestedIntegrationEventHandler.cs:34-36` wraps an inbox handler in
`Change(new TenantInfo(Guid.Empty, "Host"))` and says why in a comment.

## Tenant resolution on non-HTTP paths

`CurrentTenant` resolves the tenant from the `tenant_id` and `tenant_name` claims on
`HttpContext.User`, and is registered **scoped** (`Program.cs:143`). It deliberately throws rather than
defaulting:

```
InvalidOperationException: Tenant context missing. Call Change() for background tasks.
UnauthorizedAccessException: User not authenticated.
```

That fail-loud behaviour is kept and is now an explicit decision: silently defaulting to `Guid.Empty`
on a missing `HttpContext` would turn every background scope into a host-scoped one, which is exactly
the fail-open failure this model is most exposed to.

**Rule:** any work outside an HTTP request — inbox handlers, outbox processors, `IHostedService`s,
startup seeding — must run inside

```csharp
using var tenantChange = _currentTenant.Change(new TenantInfo(tenantId, name));
```

The existing inbox handlers already do this
(`EmailConfirmationOtpRequestedIntegrationEventHandler.cs:34-36`). Note they currently pass
`Guid.Empty` / `"Host"` because they look up globally-unique keys such as email — that is legitimate
under rule 2 above, and the comment on the call site says so.

**For future tenant-scoped background work, the tenant id must come from the message payload**, not from
ambient state — an integration event crossing a service boundary has no ambient tenant. Every integration
event that carries tenant-scoped data must therefore carry its tenant id explicitly.

## Review checklist

Applied to PRs touching persistence:

- [ ] New entity that holds tenant data implements `ITenantEntity`.
- [ ] New `DbContext` applies the tenant configuration — **required for the service to be multi-tenant.**
- [ ] Data access goes through a repository. No raw SQL.
- [ ] No new `IgnoreQueryFilters()`. Host scope is reached with `Change()`.
- [ ] Any new composite unique index includes `TenantId` where uniqueness should be per-tenant, and
      Identity's global default index has been removed.
- [ ] Background work wraps its scope in `Change(...)`, and takes the tenant id from the payload.
- [ ] A test asserts that a query in tenant A cannot see tenant B's rows. *(No test project exists yet —
      this becomes enforceable when one does.)*

## Follow-ups

| Follow-up | Trigger |
|---|---|
| Replace the remaining `IgnoreQueryFilters()` call sites with `Change()` | Next time the seeding code or `PermissionCatalogSynchronizer` is touched |
| Adopt PostgreSQL RLS as a database-level backstop (option 4) | When a second developer joins, or when tenant data is exposed to end customers rather than only to staff |
| Move the tenancy abstractions into the framework so all services can use them | [#16](https://github.com/Agent-6/shopit/issues/16) — already planned |
| Make "no tenant context" explicit instead of overloading `Guid.Empty` | When a second service acquires tenant-scoped data |
| Automated cross-tenant leak test in CI | When a test project exists |
