# Cross-service integration event ownership

**Related:** [#20](https://github.com/Agent-6/shopit/issues/20) · [#35](https://github.com/Agent-6/shopit/issues/35) · **Epic:** [#13](https://github.com/Agent-6/shopit/issues/13) · **Milestone:** M0

---

## Decision: the catalog event moves to `ShopIt.Identity.Events` (option A)

**Decided.** The permission-catalog event and its DTOs move out of `Framework.Core` into
`ShopIt.Identity.Events`.

Rationale: Identity is the only consumer and the only service that can act on it — it owns the registry,
the synchronizer and the persistence. Under the ownership rule below, *"here is my catalog"* is a message
**to** Identity, so Identity owns the contract.

**Accepted consequence:** publishing a catalog now requires a **Tenancy → Identity.Events** reference.
That is direction-correct rather than an inversion, and it is deliberate — not something that should later
look like an accident. Leaving it in `Framework.Core` as a shared-kernel exception was rejected because a
rule with zero exceptions is worth more than one convenience.

**Naming: `.Events`** (per review), not `.IntegrationEvents` — so `ShopIt.Identity.Events`,
`ShopIt.Tenancy.Events`, `ShopIt.Authentication.Events`.

---

## Context

[#20](https://github.com/Agent-6/shopit/issues/20) was filed to *"move the permission-catalog contract out of Identity's contracts"*, because my earlier recon recorded `PermissionCatalogPublishedIntegrationEvent` as living in `ShopIt.Identity.Application.Contracts`. Planning it showed the premise was false, and that the real defect is different from both the issue and **my first draft of this plan**.

## Finding 1 — #20 as filed is already satisfied

The event and both DTOs are in the framework, and always have been:

```
Framework/ShopIt.Framework.Core/Events/Integration/PermissionCatalogPublishedIntegrationEvent.cs
  ├── PermissionCatalogPublishedIntegrationEvent
  ├── PermissionGroupDto
  └── PermissionDefinitionDto

git log --follow  →  d681a15  feat(identity): sync per-service permission catalogs…
git log --diff-filter=A -- "*Identity.Application.Contracts*PermissionCatalogPublished*.cs"
  →  no results. Never added there.
```

`Framework.Core` references only `Framework.Domain`. All three of #20's acceptance criteria already hold.

**The error originated in this conversation** — I recorded the event's location wrongly during early recon and propagated it into #20's body, `plans/github-issues-workflow.md` (lines 164 and 296), and Epic #12's description. Correcting those is part of the work.

## Finding 2 — the defect is narrower than my first draft claimed: **6 inverted edges, not 11**

My first draft rejected producer-owned contracts with *"consumers must still reference the publisher — the same coupling pointing the other way."* **That is wrong**, and it mislabelled two rows of its own table. Downstream depending on upstream is normal. What is broken is **upstream depending on downstream**.

| Event | Publisher | Consumed by | Declared in | Verdict |
|---|---|---|---|---|
| `TenantCreatedIntegrationEvent` | Tenancy | Identity | Identity.Contracts | ❌ **inverted** — move to Tenancy |
| `ForgotPasswordRequestedIntegrationEvent` | Authentication | Identity | Identity.Contracts | ❌ **inverted** — move to Authentication |
| `PasswordResetRequestedIntegrationEvent` | Authentication | Identity | Identity.Contracts | ❌ **inverted** — move to Authentication |
| `EmailConfirmationOtpRequestedIntegrationEvent` | Authentication | Identity | Identity.Contracts | ❌ **inverted** — move to Authentication |
| `EmailConfirmationSubmittedIntegrationEvent` | Authentication | Identity | Identity.Contracts | ❌ **inverted** — move to Authentication |
| `ResendInvitationRequestedIntegrationEvent` | Authentication | Identity | Identity.Contracts | ❌ **inverted** — move to Authentication |
| `PasswordResetCompletedIntegrationEvent` | Identity | Authentication | Identity.Contracts | ✅ correct — consumer references publisher |
| `UserEmailConfirmedIntegrationEvent` | Identity | Authentication | Identity.Contracts | ✅ correct |
| `UserActivatedIntegrationEvent` | Identity | nobody | Identity.Contracts | ✅ Identity owns it; keeping it is decided by ownership, not by whether a consumer appears |
| `SendEmailIntegrationEvent` | Identity + Authentication | Notifications | Notifications.Contracts | ✅ correct — a *directed* message |
| `PermissionCatalogPublishedIntegrationEvent` | any service | Identity | Framework.Core | ⚠️ see the decision at the top |

Verified inverted edges, by line:

```
Tenancy.Application.csproj:5         →  Identity.Application.Contracts   (for TenantCreated)
Authentication.Application.csproj:5  →  Identity.Application.Contracts   (for its 5 events)
```

`Authentication.Application` has **no `.cs` usage** of Identity's contracts at all — the reference exists only to reach those five event records, so it can be dropped outright once they move.

## The rule

> **The service that owns the meaning of the message owns the contract** — the publisher for a domain event, the receiving authority for a directed message.

This covers everything, including the case that breaks a naive "producer-owned" rule: `SendEmailIntegrationEvent` has **two producers** (Identity and Authentication) and one consumer. It is a command *to* Notifications, so Notifications owns it — and it is already in the right place. No move needed.

Under this rule, every publisher references the owner. That is fine because it always points downstream, and the matrix stays acyclic.

## Blocker: `Identity.Application.Contracts` is not thin — split it, don't reuse it

```
ShopIt.Identity.Application.Contracts.csproj
  <PackageReference Include="Refit" />
  <PackageReference Include="Refit.HttpClientFactory" />
  + Clients/IIdentityApi.cs, Services/IIdentityServiceClient.cs,
    Implementations/IdentityServiceClient.cs, Models/*
```

Adding any event to it makes **every publisher drag Refit and Identity's HTTP client into its graph**. Compare the assembly that already has the right shape:

```
ShopIt.Notifications.Application.Contracts.csproj
  →  Framework.Core only
```

So Identity must be **split**, not added to:

| New assembly | Contents | References |
|---|---|---|
| `ShopIt.Identity.IntegrationEvents` | Identity-published events, plus the catalog event + DTOs if option A | `Framework.Core` only |
| `ShopIt.Identity.Client` | The Refit surface: `Clients`, `Services`, `Implementations`, `Models` | `Refit` |

`ShopIt.Identity.Client` is the first step of the separate HTTP-client coupling issue — Authentication keeps referencing it and stays out of scope here.

Naming matters: **`.IntegrationEvents` rather than `.Application.Contracts`** makes *"this assembly contains no handlers and no transport dependency"* a checkable claim, and stops `Application.Contracts` from being a bag that hides both kinds of coupling again.

## Approach

Owner-owned contract assemblies, one per service that publishes or receives a directed message:

```
ShopIt.Tenancy.IntegrationEvents          new  — TenantCreatedIntegrationEvent
ShopIt.Authentication.IntegrationEvents   new  — its 5 events
ShopIt.Identity.IntegrationEvents         new  — split out of Identity.Application.Contracts
ShopIt.Identity.Client                    new  — the Refit surface (out of scope beyond the split)
ShopIt.Notifications.Application.Contracts     — unchanged, already correct
```

**6 events move** (1 → Tenancy, 5 → Authentication); 5 stay where they are; the catalog event depends on the decision above.

### Why it is wire-safe

`IntegrationEvent.EventType` is the **simple type name**, and the inbox resolves it the same way (`InboxProcessor.cs:157`):

```csharp
public string EventType => GetType().Name;   // used as the Kafka topic
```

Moving a namespace changes no topic, no payload and no discriminator. One event can land at a time, with no coordinated deploy and no message migration.

## The drift guard: a tiny architecture test

Per-service contract assemblies buy almost nothing at *build* time in a four-service monorepo with one solution — any event edit rebuilds everything either way. What they buy is **legible, enforceable dependency direction**, and that only holds if it is checked. There is no test project today, so this is the one piece of new infrastructure the plan needs:

- no service assembly may reference another service's assembly **other than** one ending `.IntegrationEvents` or `.Client`
- an `.IntegrationEvents` assembly may reference **only `Framework.Core`**

~20 lines with NetArchTest or ArchUnitNET, and it makes the whole plan permanent instead of a snapshot. Without it the repo drifts back to exactly where it is now — which is how `Tenancy.Application → Identity.Application.Contracts` got there in the first place.

## Files to modify

```
NEW  Services/Tenancy/ShopIt.Tenancy.IntegrationEvents/                     Framework.Core only
NEW  Services/Authentication/ShopIt.Authentication.IntegrationEvents/       Framework.Core only
NEW  Services/Identity/ShopIt.Identity.IntegrationEvents/                   Framework.Core only
NEW  Services/Identity/ShopIt.Identity.Client/                              the Refit surface
NEW  tests/.../ArchitectureTests.cs                                         the drift guard

MOVE Services/Identity/.../Application.Contracts/Events/TenantCreatedIntegrationEvent.cs           → Tenancy
MOVE Services/Identity/.../Application.Contracts/Events/{ForgotPasswordRequested,PasswordResetRequested,
       EmailConfirmationOtpRequested,EmailConfirmationSubmitted,ResendInvitationRequested}*.cs      → Authentication
     (the 4 that stay relocate into Identity.IntegrationEvents as part of the split)

EDIT Services/Tenancy/.../Application.csproj                 − drop the Identity reference
EDIT Services/Authentication/.../Application.csproj          − drop the Identity reference (no .cs usage)
EDIT every `using ShopIt.Identity.Application.Contracts.Events` across Identity, Tenancy, Authentication
EDIT Identity.API/Program.cs                                 inbox topic usings
EDIT Tenancy.API/Program.cs                                  publisher using
EDIT Notifications.API/Program.cs                            using only

DOC  plans/github-issues-workflow.md                         fix lines 164 and 296
DOC  #20 body                                                the source of the error (via `gh issue edit`)
```

## Reuse

| Existing | Where | Role |
|---|---|---|
| `Notifications.Application.Contracts.csproj` | `Services/Notifications/` | **The shape to copy** — `Framework.Core` only, no transport |
| `IntegrationEvent` base | `Framework.Core/Events/Integration/IntegrationEvent.cs` | What every moved event already derives from |
| `EventType => GetType().Name` | same file | Why the move is wire-safe |
| `SendEmailIntegrationEvent` | `Notifications.Application.Contracts/` | The working example of receiver-owns, already correct |

No new abstraction. This is relocation onto patterns that already exist.

## Steps

- [x] **Decide the catalog event's home** (the question at the top) — everything else is unaffected by it
- [x] Create `ShopIt.Tenancy.IntegrationEvents`; move `TenantCreatedIntegrationEvent`; rewire Tenancy + Identity; **drop `Tenancy.Application.csproj:5`**
- [x] Create `ShopIt.Authentication.IntegrationEvents`; move its 5 events; rewire Authentication + Identity; **drop `Authentication.Application.csproj:5`**
- [x] Split `Identity.Application.Contracts` → `ShopIt.Identity.IntegrationEvents` + `ShopIt.Identity.Client`; rewire Identity's own projects and Authentication's client usage
- [x] Apply the catalog decision
- [x] Add the architecture test
- [x] `dotnet build ShopIt.slnx` — 0 errors, no new warnings
- [x] Correct `plans/github-issues-workflow.md` (164, 296) and #20's body

## Verification

There is no test project, so this is build + targeted checks, plus the new architecture test — the approach used for #16, #17, #29, #44.

1. **The build is the primary signal.** Every moved event has a producer and a consumer in different projects; a missed `using` or `ProjectReference` fails compilation. This is a refactor the compiler genuinely covers.
2. **The inverted edges are gone** — `Tenancy.Application.csproj` and `Authentication.Application.csproj` contain no `ShopIt.Identity.*` reference.
3. **Nothing is double-declared** — no event type name exists in two projects.
4. **Topic names unchanged** — for each moved event, `nameof(X)` still appears in its consumer's inbox `Topics` list, and the simple type name is unchanged. Explicitly: **`PermissionCatalogPublishedIntegrationEvent.EventType` is still `PermissionCatalogPublishedIntegrationEvent`**, whatever the catalog decision.
5. **The drift guard fails when it should** — deliberately add a forbidden reference, watch the test fail, then remove it. A guard never seen failing is not a guard.
6. **`dotnet build ShopIt.slnx`** — 0 errors, no new warnings.

## Decisions taken from review

| Question | Decision |
|---|---|
| Widen [#35](https://github.com/Agent-6/shopit/issues/35) or file a sibling? | **Widen #35** — it is the same fix per service, and splitting invites a half-inverted matrix |
| One pass or incremental? | **One PR per owning service** — matches project boundaries: Tenancy, Authentication, Identity split, and Notifications as a no-op |
| Where does `SendEmailIntegrationEvent` go? | **Stays in Notifications** — it is a directed command to the receiving authority, not a shared record. Moving it to the framework would hide that |
| `UserActivatedIntegrationEvent` with no consumer? | **Keep it** in Identity's events assembly — ownership decides its home regardless of consumers. Whether it should exist at all is a separate, behavioural question |
| The Refit HTTP client coupling? | **Separate issue**; the `ShopIt.Identity.Client` split here is its first step |
