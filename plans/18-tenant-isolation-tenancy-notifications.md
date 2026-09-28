# #18 — Tenant isolation for Tenancy and Notifications

**Issue:** [#18](https://github.com/Agent-6/shopit/issues/18) · **Epic:** [#11](https://github.com/Agent-6/shopit/issues/11) · **Milestone:** M0

---

## Context

`#18` is the last piece of Epic 1. Its goal is that tenant isolation stops being an Identity-only
concern, so the catalog service in M1 starts on a foundation that already works.

Its acceptance criteria are written as observable behaviour:

> Given two tenants A and B each have data, when a query runs under tenant A, then no row belonging to
> tenant B is returned.
> Given an entity implements `ITenantEntity`, when a row is created, then `TenantId` is stamped from
> `ICurrentTenant` rather than by hand.

## Finding: as written, #18 is not satisfiable

I went looking for the entities to isolate. There are none.

**Every `ITenantEntity` implementor in the repository is in Identity** — 7 of them (`User`, `Role`,
`UserClaim`, `RoleClaim`, `UserLogin`, `UserRole`, `UserToken`). Zero in Tenancy, zero in Notifications.

| Service | Entities | DbSets |
|---|---|---|
| **Tenancy** | `Tenant` — **and that is host-level by design** | `Tenants` |
| **Notifications** | **none** — no `Domain` project | **none** — no `DbSet` at all |

`Tenant` is the tenant *registry*. ADR-0001 is explicit that host-level tables must **not** implement
`ITenantEntity` — their absence of a filter is intentional. So Tenancy has nothing to scope, and
Notifications has no data whatsoever.

`NotificationsDbContext` in full:

```csharp
public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // The Notifications service only consumes integration events, so its schema
        // consists solely of the Kafka inbox/outbox tables (idempotent delivery).
        modelBuilder.ApplyInboxOutboxConfigurations();
    }
}
```

The behavioural criteria will stay untestable **until M1's catalog service introduces the first
non-Identity tenant-scoped entity**. Writing "two tenants each have data" against a service with no
data means the criterion can only be satisfied by inventing a production entity purely to test it —
which would be worse than deferring.

## The real constraint: EF Core only re-evaluates filters rooted at the context instance

The mechanism is trapped in Identity, but **not** for the reason it first appears. Extracting it into a
framework extension method does not merely fail to help — it **silently breaks tenant isolation**,
because of how EF Core compiles query filters.

EF Core caches the model **per `DbContext` type**. A filter is re-evaluated on every query **only if its
expression is rooted at the context instance**. Anything else — a captured local, a method parameter, a
field on another object — is evaluated once, when `OnModelCreating` runs, and **baked into the cached
model**. The first tenant evaluated wins, and every later query filters by that tenant. No exception, no
warning, just wrong data.

Documented, in EF Core's own words:

> For EF's purposes, the tenant ID must be available **on the context instance**, so that the global
> query filter can refer to it and use it when querying.
> — [Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)

> By using local variable in filter you effectively broke the linkage with context. So we no longer know
> which part comes from DbContext.
> — [efcore#10322](https://github.com/dotnet/efcore/issues/10322)

> …the one in the filter is a member of the **DbContext class**. This one is captured into the query
> expression such that it can be changed on each context instance…
> — [efcore#12375](https://github.com/dotnet/efcore/issues/12375)

> …all variables which are not **rooted to the target db context** are evaluated and computed once.
> — [SO 50868531](https://stackoverflow.com/questions/50868531/)

### Why Identity works today, and why an extension breaks it

In `OnModelCreating` — an instance method — `_currentTenant` is `this._currentTenant`. The lambda
captures **only `this`**, so C# emits `Expression.Constant(this)` and the reference **is** rooted at the
context. EF's model-cache rewrite swaps the model-building context for the executing one, so the value
is re-read per query. That is why Identity is correct today.

In an extension `ApplyTenantConfigurations(modelBuilder, currentTenant)`, `currentTenant` is a
**parameter**. C# must generate a closure **display class**, and the lambda becomes
`Expression.Constant(displayClass).currentTenant` — the constant is the display class, not the context.
Nothing is rooted, so EF evaluates it once at model build and bakes it in.

**The refactor in the previous revision of this plan would have quietly pinned Identity's tenant filter
to whichever tenant was seen first. A single-tenant test would have passed.**

## Approach

**Take the reusable base class directly. No dummy-field mechanism, no spike.**

An earlier revision proposed sharing the filter through an `IEntityTypeConfiguration` with a `null!`
context field, which EF Core documents as a workaround. That is rejected outright. Tenant isolation is
the one thing in this system that must not rest on an undocumented rewrite of a null field, tested or
not. The mechanism used is the one that is rooted **by construction**.

### The deliverable: an opt-in base context

```csharp
// Framework/ShopIt.Framework.Persistence/Tenancy/TenantAwareDbContext.cs
public abstract class TenantAwareDbContext<TContext> : DbContext
    where TContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    protected TenantAwareDbContext(DbContextOptions<TContext> options, ICurrentTenant currentTenant)
        : base(options) => _currentTenant = currentTenant;

    /// Applies the tenant query filter to every ITenantEntity in the model.
    /// Call from OnModelCreating.
    protected void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        // reflection over modelBuilder.Model.GetEntityTypes(), invoking ApplyTenantFilter<TEntity>
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _currentTenant.Id == Guid.Empty || e.TenantId == _currentTenant.Id);
    }
}
```

The lambda is written **inside the context class** and captures only `this`, so C# emits
`Expression.Constant(this)` and the reference is rooted at the context instance. That is the same shape
as Identity's working code — which is the point: a known-good pattern, not a new one.

If the base-class form were ever to fail to root (`this` *is* the context, so it should not), the
deterministic fallback is to build the predicate explicitly with
`Expression.Field(Expression.Constant(this), …)`, which cannot be mis-compiled. Noted, not used.

### Who uses it: nobody yet, and that is the deliverable

`#18` stops being *"give Tenancy and Notifications isolation"* and becomes **"make it possible for other
services to opt in to multi-tenancy"**. The base class is the opt-in. The first real consumer is M1's
catalog service, which is where the behavioural proof belongs.

**Identity keeps its own inline implementation** and is a documented exception, because
`ApplicationDbContext` already inherits `IdentityDbContext<User, Role, …>` and C# has no multiple
inheritance. The isolation rule therefore exists in two places, and ADR-0001 must say why.

**No migrations.** Query filters are model-level, not relational schema.

## Host services are exempt by design

**Neither Tenancy nor Notifications opts in. Both are host services**, and that is now a recorded
decision rather than an omission:

| Service | Why it is host-level |
|---|---|
| **Tenancy** | It is the tenant *registry*. Tenant rows do not belong to a tenant — they define them. `Tenant` is the canonical host-level table |
| **Notifications** | It sends email. Its only tables are the inbox/outbox infrastructure tables; it owns no tenant data at all |

This matters because "this service has no tenant filter" currently looks identical to the omission
ADR-0001 warns about. It is not — it is the correct state, and the ADR should list these exemptions
explicitly so nobody later "fixes" them.

## Files to modify

```
Framework/ShopIt.Framework.Persistence/Tenancy/TenantAwareDbContext.cs   new — the opt-in base class
docs/adr/0001-tenant-isolation-model.md    rooting constraint, shared mechanism, host exemptions,
                                           Identity's documented exception
```

**Deliberately unchanged:**

```
Services/Identity/.../Data/ApplicationDbContext.cs          works today; cannot inherit (IdentityDbContext)
Services/Tenancy/.../Data/TenancyDbContext.cs               host service — no tenant data
Services/Notifications/.../Data/NotificationsDbContext.cs   host service — no tenant data
```

No service component changes at all. `Framework.Persistence` already references `Framework.Domain`
(`ICurrentTenant`) and EF Core, so no project reference is added. No migrations: query filters are
model-level, not schema.

## Reuse

| Existing | Where | Role |
|---|---|---|
| `ApplyTenantConfiguration` / `ApplyTenantFilter<TEntity>` | `Identity.Persistence/Data/ApplicationDbContext.cs` | The **working reference implementation** — the base class must match its behaviour, line for line |
| `ApplyInboxOutboxConfigurations` | `Framework.Persistence/ModelBuilderExtensions.cs` | Precedent that the framework owns model-building concerns |
| `ICurrentTenant` | `Framework.Domain/Tenancy` | The scoping abstraction; already registered for every service by `AddInfrastructureServices` since #17 |
| `null!` design-time pattern | `Identity.Persistence/Data/DesignTimeDbContextFactory.cs` | Reference for how a consumer's design-time factory supplies a dummy tenant — generating a migration must not require an HTTP context |

## Steps

- [x] Add `TenantAwareDbContext<TContext>` to `Framework.Persistence/Tenancy/`, mirroring Identity's reflection + `HasQueryFilter` shape exactly
- [x] `dotnet build ShopIt.slnx` — 0 errors, no new warnings
- [x] Prove it with a harness context — **two tenants**, not one (see Verification)
- [x] Amend ADR-0001: the EF Core rooting constraint; the base class as the opt-in; Tenancy and Notifications as host-service exemptions; Identity as a documented exception
- [x] Retitle and rescope #18 to *"make it possible for other services to opt in to multi-tenancy"*, noting the behavioural proof lands with M1's catalog
- [x] Record in #18 that Tenancy and Notifications are host services and deliberately excluded

## Verification

There is no test project, so this is a harness plus a build — the approach used for #16, #17 and #29.

**The central requirement: every check uses two different tenants.** A single-tenant test passes even
when the filter is baked to the first tenant, so it is worthless as evidence here.

1. **The base class filters per tenant.** A throwaway harness context inheriting
   `TenantAwareDbContext<T>` with one tenant-scoped entity. Tenant A inserts a row; a **second context
   instance** acting as tenant B queries and must see nothing; A then queries and must see its own. Then
   repeat in a fresh process with the order reversed — that is what catches a baked filter.
2. **Host scope bypasses** — under `Guid.Empty` both tenants' rows are visible, matching Identity.
3. **Identity is unaffected** — its 7 entities still receive `HasIndex`/`HasQueryFilter`. Nothing in
   Identity's persistence changes here, so this is a regression guard rather than a new check.
4. **A host entity stays unfiltered** — a context inheriting the base whose only entity does *not*
   implement `ITenantEntity` gets no filter. This is exactly how `Tenant` would behave if Tenancy ever
   adopted the base class, so it pins the exemption's mechanism.
5. **No pending migration** — query filters are not schema.
6. **`dotnet build ShopIt.slnx`** — 0 errors, no new warnings.

A single-tenant test is **not** acceptable evidence for any of this. That is the lesson of this revision:
the bug is invisible to it.

## Decisions

All three questions from the previous revision are settled:

1. **No dummy-field mechanism, no spike.** The `IEntityTypeConfiguration` + `null!` route is rejected
   outright, and option C is taken directly. Isolation must not depend on an undocumented rewrite.
2. **#18 stays in M0, rescoped** to *"make it possible for other services to opt in to multi-tenancy"*.
   The behavioural proof moves to M1's catalog, where a real tenant-scoped entity exists.
3. **Neither Tenancy nor Notifications opts in.** Both are host services — see *Host services are exempt
   by design* above.
