# ADR-0004: Aggregate and audit model

- **Status:** Accepted
- **Date:** 2026-10-20
- **Issue:** [#31](https://github.com/Agent-6/shopit/issues/31)
- **Related:** ADR-0001 (tenant isolation) · #29 (`ICurrentUser`) · #50 (`Role.IsStatic`)
- **Supersedes:** —

## Context and problem statement

Two conventions coexist, and neither is finished:

| | Identity (`User`, `Role`) | Tenancy (`Tenant`) |
|---|---|---|
| Base type | `IdentityUser<Guid>`, `IdentityRole<Guid>` + hand-written `IAggregateRoot<Guid>` | framework `AuditedAggregateRoot<Guid>` |
| Domain events | private `List<DomainEvent>` per entity, hand-written `RaiseDomainEvent`/`ClearDomainEvents` | framework `AggregateRoot<TId>` |
| Created at | `DateTime CreatedAt` | `DateTimeOffset CreatedOn` |
| Created by | `string CreatedBy` (`"system"` when seeded) | `Guid CreatedBy`, `Guid.Empty` when the handler does not know the actor |
| Last modified | `DateTime LastModifiedAt` | `DateTimeOffset? LastModifiedOn` + `Guid? LastModifiedBy` |
| Who sets them | the entity's factory / each handler | `CreateTenantCommandHandler` sets both by hand |

Nothing stamps the audit fields: they are assigned wherever an entity happens to be created, so
"who did this" depends on whether the author remembered, and the two services answer the same
question in different types. `CreateTenantCommandHandler` writes `CreatedBy = Guid.Empty` with a
comment admitting it does not know the actor — while `ICurrentUser` (added in #29, reachable from
every service) does.

## Decision drivers

- **Audit fields are a cross-cutting convention, not a per-handler decision.** A handler that forgets
  produces data that silently claims "nobody".
- **One type for one concept.** An actor is a user id, and user ids are `Guid`s.
- Changing the persisted type later is a migration, so the choice is made once, now.
- Identity's entities **cannot** inherit a framework base: C# has single inheritance and they already
  inherit `IdentityUser<Guid>` / `IdentityRole<Guid>`. This is the same forced exception documented in
  ADR-0001 for `ApplicationDbContext`.
- Seeders and inbox handlers have no interactive actor at all, and must say so honestly.

## Considered options

1. **`Guid CreatedBy` with `Guid.Empty` meaning "system".** Matches Tenancy today; no type migration
   there. But it invents a sentinel that every reader must know, and `Guid.Empty` is a valid-looking
   value — a bug that writes it looks like a real actor.
2. **`Guid? CreatedBy`, `null` meaning "no interactive actor".** Honest and explicit; requires changing
   the framework interface and a nullable-column migration in Tenancy, and Identity's `"system"` string
   has no `Guid` mapping, so its column changes anyway.
3. **`string CreatedBy` everywhere.** Preserves Identity's data, but puts identity-shaped strings in
   every service, loses referential meaning, and keeps two representations alive.
4. **Stamp in each service's `SaveChanges` override.** Correct but duplicated, and Identity's context
   cannot inherit the framework's `TenantAwareDbContext` to get it.

## Decision outcome

**Option 2, plus one shared stamping step invoked by both persistence paths.**

- `IAuditedEntity.CreatedBy` becomes `Guid?`; `null` means the change was not made by an interactive
  user — seeding, migration, or a background inbox handler. `LastModifiedBy` / `DeletedBy` are already
  `Guid?`.
- Stamping is a framework operation, `AuditInfo.Apply(DbContext, Guid? actorId)`: on `Added` it sets
  `CreatedOn` and `CreatedBy`; on `Modified` it sets `LastModifiedOn` and `LastModifiedBy`. Time comes
  from `IDateProvider`, the actor from `ICurrentUser`.
- The two places that persist aggregate changes call it: `UnitOfWork.CommitAsync` (command path) and
  `InboxProcessor` (inbox path, actor `null` unless a principal exists). Handlers stop assigning audit
  fields.
- **Identity composes rather than inherits.** `User` and `Role` implement the framework contracts
  (`IAggregateRoot`, `IAuditedEntity<Guid>`) and their audit properties are stamped like everyone
  else's. What they cannot do is inherit `AuditedAggregateRoot<Guid>`; that exception is recorded here
  rather than worked around.
- Response DTOs keep their existing `createdAt` shapes, mapped from the entity in the query handlers,
  so the HTTP contract and the portal do not change for a storage-level rename.

## Consequences

**Positive**

- One audit vocabulary: `CreatedOn`/`CreatedBy`, `LastModifiedOn`/`LastModifiedBy`, `DeletedOn`/`DeletedBy`,
  with `Guid?` actors and one meaning for `null`.
- `CreateTenantCommandHandler` no longer guesses: the tenant records the authenticated caller.
- A new entity gets correct audit data without its author doing anything.

**Negative**

- A schema migration in Identity (column rename and `text` → `uuid`) and in Tenancy (`uuid` → nullable
  `uuid`). Existing Identity rows hold `"system"`, which cannot be parsed as a `Guid`; the migration
  drops and re-adds the column, so existing audit values are not preserved — consistent with this
  issue's "no retro-fitting" out-of-scope.
- Stamping depends on the two persistence paths calling it. A third path that saves directly would
  bypass it — the same shape of risk as ADR-0002's invalidation contract, and recorded for the same
  reason.

## What is not proven

- The `Deleted*` fields are set by neither path yet: nothing in the system soft-deletes. They are part
  of the interface for symmetry and are stamped only if an entity is marked deleted later.
