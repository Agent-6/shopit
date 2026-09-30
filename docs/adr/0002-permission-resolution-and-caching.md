# ADR-0002: Permission resolution and caching

- **Status:** Accepted
- **Date:** 2026-09-30
- **Issue:** [#23](https://github.com/Agent-6/shopit/issues/23)
- **Related:** [#49](https://github.com/Agent-6/shopit/pull/49) · [#50](https://github.com/Agent-6/shopit/issues/50) · ADR-0001
- **Supersedes:** —

## Context and problem statement

Two services authorise by permission, and before this decision they did it **differently**:

| | Identity | Tenancy |
|---|---|---|
| Resolution | in-database via `IPermissionResolver` | HTTP `GET /api/internal/users/{id}/permissions` |
| Cache | none — re-resolved on every request | private `IMemoryCache`, **60s, hard-coded** |
| Deactivated user | `!user.IsActive` → refused | endpoint checked only `user is null` → **granted** |
| Identity unreachable | n/a | threw → HTTP 500, not a clean deny |

So a revoked permission took effect immediately in one service and up to a minute later in the other, a
deactivated user was refused by one and authorised by the other **indefinitely**, and the cache TTL was a
literal nobody could tune.

## Decision drivers

- **A revocation that lingers is a security hole, not a cache miss.** Grants are tolerant of delay;
  revocations are not.
- Permission checks run on **every authorised request**, so resolution cost matters.
- "All permissions" is expressed by *granting every permission claim* (mirroring ABP's
  `PermissionDataSeedContributor`), which makes a published catalog a change to every admin's permission
  set — and therefore a potential invalidation fan-out across every tenant.
- A shared cache is only useful if invalidation reaches **every process**, not just the writer.

## Considered options

1. **Always ask Identity** — drop caching entirely. Simplest and always correct; adds an HTTP hop per
   authorised request and multiplies load on Identity.
2. **Keep a per-service cache with a configurable TTL.** Cheap; staleness bounded by the TTL, so a
   revocation is delayed by design.
3. **Event-driven invalidation of a shared cache.** Near-immediate revocation without per-request cost.
4. **Permissions embedded in the access token.** No resolution at all; staleness equals the token
   lifetime, and tokens grow with the permission set.
5. **Shared cache + explicit invalidation on write, with all-permissions holders represented by a flag.**

## Decision outcome

**Option 5.**

A single Redis-backed cache is shared by every service, layered as an in-process L1 in front of Redis as
L2, with **FusionCache** — chosen specifically because it has a **Redis backplane**, so removing a key
clears the writer's L1, the shared L2, *and* every other process's L1.

Resolution becomes: read the shared entry; on a miss, Identity resolves from its database, writes the
entry, and returns it.

### The flag, and why it removes a whole class of invalidation

An all-permissions holder is represented as:

```csharp
UserPermissionsSnapshot(bool IsAllPermissions, string[] Permissions)
```

…rather than by enumerating the catalog into their entry. A published catalog only ever grants new
permissions to all-permissions roles, so:

- those roles are covered by the **flag**, and
- every other user's **explicit set is untouched** by a publish.

**Neither field changes when a catalog is republished, so a publish invalidates nothing.** Without the
flag, a publish would have to invalidate one entry per admin in every tenant.

### Invalidation

Explicit, by the writer that knows something changed — never left to expiry:

| Change | Invalidates |
|---|---|
| direct permission grant/revoke on a user | that user |
| role assignment | that user |
| activate / deactivate | that user |
| delete (hard or soft) | that user |
| **edit a role's permissions** | **every user holding that role** |
| **publish a permission catalog** | **nothing** — see the flag above |

Editing a role's permissions is the one change that genuinely fans out. It is bounded by the role's
membership and happens only when an administrator edits a role.

### Why the other options were rejected

- **1 (always ask Identity)** — correct but pays an HTTP round-trip on every authorised request, and
  leaves Identity resolving from scratch each time. The cache costs less than the hop it removes.
- **2 (TTL only)** — makes revocation latency a design property rather than an event. That was the
  status quo's actual defect: up to 60s of stale *access*, not stale data.
- **3 (event-driven)** — the right shape, but only the *distribution*; it still needs a per-user
  invalidation fan-out on a catalog publish, which the flag removes for free.
- **4 (token-embedded)** — staleness equals the token lifetime, and the permission set grows the token.
  Worth revisiting only if resolution ever becomes the bottleneck.

## Consequences

**Positive**

- One resolution pipeline. Identity and Tenancy cannot disagree, because they read the same entry.
- Revocation is immediate in both, bounded only by invalidation reaching the process.
- A catalog publish costs one write and zero invalidations.
- TTLs are configuration (`Caching:Duration`, `Caching:MemoryCacheDuration`,
  `Caching:FailSafeMaxDuration`) rather than literals.
- Fail-safe is on: if Redis is unreachable, the last known value is served rather than failing the
  request.
- The duplicate client and its divergent 60-second view are gone.

**Negative**

- A Redis dependency on the authorization path — mitigated by fail-safe, and by degrading to L1-only
  when no `cache` connection string is configured. Note that degradation is real: each process then has
  its own view and invalidation is local only.
- **The backplane is load-bearing.** Without it, an invalidation clears only the writer's L1 and L2, and
  every other process serves its stale L1 entry until it expires.
- Editing a role fans out across its membership.
- A `DbContext`-level guarantee is gone: correctness now depends on every write path remembering to
  invalidate. The six paths above are wired; a seventh added later would silently reintroduce staleness.

## Operational note — flushing Redis does not invalidate

**Verified against a running stack:** deleting a key directly in Redis left the stale value being served,
because the reading process's **L1** still held it. Only `RemoveAsync` (which clears L1 *and* L2 and
publishes to the backplane) or expiry will clear it.

To force a clean cache, **restart the services** — flushing Redis alone is not enough.

## What is not proven

Recorded deliberately, because a green build should not be mistaken for evidence:

- **Backplane delivery across processes is unverified.** The channel (`FusionCache.Backplane:v2`) has a
  subscriber, so it is wired — but no removal has been observed propagating between two live processes.
- **The invalidation hook has not been driven end-to-end.** Every path that calls it needs a
  user-authenticated request, and there is no password grant (authorization-code needs a browser). The
  *mechanism* is verified — clearing the cache causes a fresh resolution reflecting the database — but
  not the trigger.

Both need two processes and an authenticated call.
