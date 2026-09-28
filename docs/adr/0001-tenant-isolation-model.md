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

- **The filter fails open.** A query that forgets the filter, or a raw SQL path, or a new `DbContext`
  that omits `ApplyTenantConfiguration`, leaks across tenants silently. There is no database-level
  backstop until option 4.
- `IgnoreQueryFilters()` is a footgun reachable from anywhere in the codebase.
- **An authenticated principal with no `tenant_id` claim is treated as Host** (`Guid.Empty`), which
  *disables* the filter. The default for "no tenant context" is maximum visibility, not minimum. See the
  rule below — this is the single most important thing to get right.
- Every tenant-scoped table carries the discriminator and needs the `TenantId` index to stay fast.
- Tenant-scoped uniqueness has to be maintained per entity by removing Identity's global indexes. It is
  easy to add a new entity and forget.

**Neutral**

- Host-level tables are deliberately **not** `ITenantEntity`: `PermissionCatalogEntry` (system-wide) and
  Tenancy's `Tenant` (the tenant registry itself). The absence of a filter on these is intentional, but
  must be stated rather than assumed.

## The host-scope escape-hatch rule

This is the part that determines whether the chosen model holds up. All seven `IgnoreQueryFilters()`
call sites in the codebase were audited when writing this ADR; they fall into exactly two categories.

**`IgnoreQueryFilters()` is legitimate only when all four of these hold:**

1. The code is **not serving a tenant-scoped request** — it runs at startup/seeding, or it is a
   deliberately cross-tenant administrative operation.
2. Bypassing the filter is **required for correctness** — the query works on every tenant by design, or
   looks up a genuinely global key.
3. The query **re-applies the tenant predicate explicitly** — filter on `TenantId` in the predicate, or be
   demonstrably host-scoped by design.
4. There is a **comment on the call site** saying why.

**Never** use `IgnoreQueryFilters()` to make a failing query work. If a query returns nothing and the fix
is to bypass the filter, that is either a missing `Change()` or a genuine cross-tenant read that must
satisfy the four conditions above.

### Which tool for which job

These two are not interchangeable, and confusing them is the most likely source of bugs:

| Intent | Use | Why |
|---|---|---|
| **Act as** another tenant — background work, seeding, a specific tenant's job | `_currentTenant.Change(new TenantInfo(id, name))` | Changes the *ambient* tenant, so it affects the query filter **and** `TenantId` stamping on new rows, consistently |
| **See across** tenants for one query while staying in the current scope | `IgnoreQueryFilters()` + an explicit `TenantId` predicate | One query, no ambient change, no effect on what gets written |

Using `IgnoreQueryFilters()` where `Change()` was meant produces rows stamped with the wrong `TenantId`;
using `Change()` where a one-off cross-tenant read was meant silently widens everything written in that
scope.

### Where the hatches currently are

| Call site | Category | Justification |
|---|---|---|
| `Identity.API/Program.cs:392` | Startup seeding | Resolves the seeded role *within* the tenant; predicate includes `r.TenantId == tenantId` |
| `Identity.API/Program.cs:403-404` | Startup seeding | Finds stale cross-tenant role joins to clean up |
| `Identity.API/Program.cs:417` | Startup seeding | Idempotent default-role assignment across pre-existing data |
| `PermissionCatalogSynchronizer.cs:92` | Cross-tenant admin | Admin roles must be found in **every** tenant |
| `PermissionCatalogSynchronizer.cs:126,140` | Cross-tenant admin | Grants a new permission to Admin in every tenant |

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
- [ ] New `DbContext` calls the equivalent of `ApplyTenantConfiguration`.
- [ ] Any new `IgnoreQueryFilters()` satisfies all four conditions, with an explanatory comment.
- [ ] Any new composite unique index includes `TenantId` where uniqueness should be per-tenant, and
      Identity's global default index has been removed.
- [ ] Background work wraps its scope in `Change(...)`, and takes the tenant id from the payload.
- [ ] A test asserts that a query in tenant A cannot see tenant B's rows. *(No test project exists yet —
      this becomes enforceable when one does.)*

## Follow-ups

| Follow-up | Trigger |
|---|---|
| Adopt PostgreSQL RLS as a database-level backstop (option 4) | When a second developer joins, or when tenant data is exposed to end customers rather than only to staff |
| Move the tenancy abstractions into the framework so all services can use them | [#16](https://github.com/Agent-6/shopit/issues/16) — already planned |
| Make "no tenant context" explicit instead of overloading `Guid.Empty` | When a second service acquires tenant-scoped data |
| Automated cross-tenant leak test in CI | When a test project exists |
