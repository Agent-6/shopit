# GitHub Issues Workflow — Plan

## Context

`shopit` is an ASP.NET Core 10 + Aspire microservices backend with an Angular 21 portal. The goal is to
run **spec-driven development on GitHub Issues** — issues are the specs, and development happens against
them.

Decisions already made by the user:

1. **Scope:** workflow mechanics first, then issue drafting.
2. **Priority order:** harden the existing **Identity / Tenancy / Notifications** services **and the
   permissions subsystem** first. Permissions is explicitly in scope for M0, because downstream
   services must be able to declare their own permission names — the product catalog will need
   `catalog.view` and `catalog.write`. Then start e-commerce with the **product catalog** module.
3. **Granularity:** parent/sub-issue hierarchy, Azure DevOps style (epics desired).
4. **Team:** solo developer, but a second machine pushes to `main` — so **`main` becomes PR-only**.
5. **Time-boxing:** needs explanation (covered below).
6. **Tests and CI are explicitly deferred** — e-commerce features take priority. The issue template's
   definition-of-done must be corrected, since it currently demands `dotnet test` which cannot run.

---

## GitHub capability findings

Verified against the live repo (CLI 2.101.0).

### Hierarchy — yes, epics work

| Capability | Status |
|---|---|
| Sub-issue nesting | ✅ **8 levels deep**, up to **100 sub-issues per parent** |
| Projects **Hierarchy view** | ✅ available (public preview, Jan 2026) — expand/collapse nesting in table views |
| `gh issue create --parent <n>` | ✅ create directly as a sub-issue |
| `gh issue edit --add-sub-issue / --parent / --remove-parent` | ✅ |
| Project fields `Parent issue` + `Sub-issues progress` | ✅ already present on Project #1 |
| **Issue dependencies** | ✅ `--blocked-by` / `--blocking` on both `create` and `edit` |
| `gh issue list --type <name>` | ✅ |

### Issue types — exposed by REST, but NOT usable here ⚠️

`GET /repos/Agent-6/shopit/issue-types` returns three enabled types, and `gh issue create --help`
advertises `--type`. **They cannot actually be applied.** Verified six ways:

| Probe | Result |
|---|---|
| `GET /repos/.../issue-types` | returns Task / Bug / Feature, `is_enabled: true` |
| GraphQL `repository.issueTypes(first:10)` | **`null`** |
| REST issue object's `type` field | **absent** |
| GraphQL `issue.issueType` | **`null`** |
| `PATCH /issues/11 -f type=Feature` | silently no-ops |
| `gh issue edit 11 --type Feature` | `type "Feature" not found; available types:` (empty) |

Issue types are **organisation-scoped** — the docs say as much, and the REST endpoint merely echoes the
global defaults. `Agent-6` is a user account, so there is no org in which to define them.

**Consequence — this changed the design.** Types were originally the plan's KIND axis. They cannot be,
so KIND is carried by **labels** instead: `feature`, `task`, `bug`. `spec` stays retired, since
`feature` now covers it; `epic` stays a label for the same reason.

### Branch protection

`main` is currently **unprotected** (404), and the repo is **public** — so branch protection rules are
**free** (no Pro required). Rulesets are also available and currently empty.

---

## Azure DevOps → GitHub mapping

Since the user's mental model is Azure DevOps:

| Azure DevOps | GitHub equivalent | Notes |
|---|---|---|
| Epic | issue + labels `feature` + `epic` | No usable native Epic type on a user repo |
| Feature | issue + label `feature` | |
| User Story | issue + label `feature` (or `task` if small) | |
| Task | issue + label `task`, sub-issue of the story | |
| Area Path | label `svc:*` | Same idea: which part of the system |
| Iteration Path | Project **`Iteration`** field | Not yet created on Project #1 |
| Board columns | Project **`Status`** field | Already exists: Todo / In Progress / Done |
| Predecessor/Successor | `blocked by` / `blocking` | Native, and scriptable |

**Recommended depth: 3 levels max.** `Epic → Feature → Task`. GitHub allows 8, but for a solo developer
depth past three is bookkeeping rather than information. Only create an Epic when it genuinely groups
several features (e.g. "Multi-tenancy foundation").

---

## Milestones vs. iterations (question 5)

These are two different axes, and GitHub separates them cleanly:

| | **Milestone** | **Iteration** (Project field) |
|---|---|---|
| What it is | A named bucket with an optional due date | A repeating time window (e.g. 2 weeks) that auto-advances |
| Answers | "Which release/phase does this belong to?" | "When am I working on it?" |
| How many per issue | Exactly one | Exactly one |
| In ADO terms | Closest to a *release* or delivery phase | Literally *Iteration Path* / sprint |
| Progress UI | Built-in progress bar (N of M closed) | Burnup chart in Projects |

**Recommendation:** use **milestones for phases** now. Skip iterations until you actively want to
time-box — a solo developer usually gets more from "which phase is this in" than from a sprint calendar
that has to be maintained. Both can coexist later; adding the `Iteration` field costs nothing.

---

## Approach

**Two orthogonal axes — native types first, minimal labels.**

```
KIND    (label, exactly one)    feature | task | bug
AREA    (label, one per svc)    svc:identity  svc:tenancy  svc:notifications
                                svc:authentication  svc:portal  svc:framework
MARKERS (label, optional)       epic | adr | spike | blocked
```

Everything is a label, because issue types cannot be applied on a user-owned repo (see the findings
above). KIND answers "what kind of work", AREA answers "where", MARKERS carry the rest.

Structure per unit of work:

```
epic      Feature + label:epic        "Multi-tenancy foundation"
  feature Feature                     "Tenant primitives live in the framework"
    task  Task                        "Move ITenantEntity to ShopIt.Framework.Domain"
```

---

## Recon: why M0 is Identity / Tenancy / Notifications + Permissions

An analysis of the three services and the permission subsystem found substantial divergence. This is
the material M0 specs are written against.

### The blocker that matters most

**All multi-tenancy primitives live in the wrong assembly.** `ITenantEntity`, `ICurrentTenant`,
`TenantInfo`, and `CurrentTenant` are declared in

```
ShopIt.Identity.Domain/Tenancy/ITenantEntity.cs
ShopIt.Identity.Domain/Tenancy/ICurrentTenant.cs
ShopIt.Identity.Domain/Tenancy/TenantInfo.cs
ShopIt.Identity.Application/Tenancy/CurrentTenant.cs
```

`ShopIt.Tenancy.*` and `ShopIt.Notifications.*` cannot reuse them, because only `Tenancy.Application`
references Identity at all — and only its `Application.Contracts`. Query filtering
(`ApplicationDbContext.ApplyTenantConfiguration`) exists **only in Identity**. Tenancy has no query
filter and no `TenantId`; Notifications has no tenancy concept whatsoever.

This is directly why "permissions depend on it" is hard today.

### Permissions: the definition side is fine, the contract side is not

`ShopIt.Framework.Domain/Permissions/` already holds sound primitives — `PermissionName`,
`PermissionGroupName`, `PermissionGroupDefinition`, `IPermissionDefinitionProvider`,
`PermissionMultiTenancySide` — and Identity and Tenancy each own a provider. The problem is everything
around them:

| Problem | Location |
|---|---|
| The published-catalog **contract** lives in `ShopIt.Identity.Application.Contracts`, so a new service must reference Identity's contracts to declare its own permissions | `Identity.Application.Contracts/Events/PermissionCatalogPublishedIntegrationEvent.cs` |
| `PermissionRequirement` + `RequirePermission` are **duplicated verbatim** in Identity and Tenancy Presentation | `*/Presentation/Authorization/` |
| Publishing the catalog is **composition-root code**, not a repeatable hook | `Tenancy.API/Program.cs:99-119` |
| Two resolution pipelines: Identity reads its own DB, Tenancy calls Identity over HTTP behind a **1-minute stale cache** | `Tenancy.Infrastructure/TenantPermissionClient.cs` |
| `DatabasePermissionDefinitionProvider.Define` throws `NotSupportedException`; the persisted catalog is read-only, and there is no documented path for a service to add names | `Identity.Persistence/Permissions/` |

The acceptance test for this work is concrete: **a new service must be able to declare `catalog.view`
and `catalog.write`, publish them, and have them appear in the catalog, be granted to Admin, and be
enforceable — without referencing `ShopIt.Identity.*`.** If that does not work, M1 is blocked.

### Other findings, ranked

| # | Finding | Location |
|---|---|---|
| 1 | **Three different layer sets**: Identity 7 projects, Tenancy 6 (no `Application.Contracts`), Notifications 4 (no `Domain`/`Infrastructure`/`Presentation`) | `ShopIt.slnx` |
| 2 | **Two aggregate/audit conventions**: Identity entities hand-roll `IAggregateRoot` + `DateTime CreatedAt`; Tenancy uses `AuditedAggregateRoot`/`IAuditedEntity` with `DateTimeOffset CreatedOn` | `Identity.Domain/Entities/User.cs`, `Tenancy.Domain/Entities/Tenant.cs` |
| 3 | **`PermissionRequirement` + `RequirePermission` duplicated verbatim** in Identity **and** Tenancy Presentation, instead of living in `Framework.Presentation` | `*/Presentation/Authorization/` |
| 4 | **Two authorization pipelines**: Identity resolves permissions in-DB; Tenancy calls Identity over HTTP with a **1-minute stale cache** | `Tenancy.Infrastructure/TenantPermissionClient.cs` |
| 5 | **`Result<T>` / `ToProblem` / `ToHttpResult` unused by all three services** — every handler throws instead | `Framework.Domain/Result`, `Framework.Presentation/Results` |
| 6 | **Tenancy cannot receive events**: its `AddPersistence` takes no `handlerAssemblies`, `Program.cs` passes no `configureInbox`, so `InboxOptions.Topics` is empty and `InboxProcessor` exits | `Tenancy.Persistence/DependencyInjection.cs` |
| 7 | **All services share one Kafka consumer group** — `ConsumerGroupId` defaults to `"shopit-inbox"` and is never overridden | `Framework.Persistence/Inbox/InboxOptions.cs:16` |
| 8 | **Inbox handler discovery matches by simple type name** across all loaded assemblies — same-named events in different namespaces collide silently | `InboxProcessor.DispatchToHandlersAsync` |
| 9 | **Inbox handlers persist outside the UnitOfWork pipeline**, so domain events raised on that path are never dispatched | `Identity/Application/IntegrationEvents/EmailConfirmationSubmittedIntegrationEventHandler.cs` |
| 10 | **Wrong dependency direction**: `Tenancy.Application` → `Identity.Application.Contracts`, because `TenantCreatedIntegrationEvent` lives in Identity's contracts | `Tenancy.Application.csproj` |
| 11 | **Hardcoded secrets**: `ClientSecret = "BACKEND_SECRET"`, `SetClientSecret("SECRET")`, policy compares `subject == "shopit-backend"` | `Tenancy.Infrastructure/ClientCredentialsTokenHandler.cs`, both `Program.cs` |
| 12 | **Dead scaffolding**: 3 fake CQRS handlers (`"Test"`, `Guid.NewGuid()`), `Class1.cs` × 2, empty `ShopIt.Framework.CQRS` (0 files, not in solution), `ShopIt.Identity.Infrastructure` is a pure pass-through | `Identity.API/Features/*`, `Tenancy.{Persistence,Presentation}/Class1.cs` |
| 13 | **Notifications puts its SMTP transport in the Application layer** and has no Infrastructure project | `Notifications.Application/Emails/SmtpEmailSender.cs` |
| 14 | **Permission-catalog publishing lives in the composition root** rather than a handler | `Tenancy.API/Program.cs:99-119` |
| 15 | **`AddDomainServices()` called twice**; `Tenancy.API/appsettings.json` still says `"Application": "Identity Service"` | `Identity.API/Program.cs`, `Tenancy.API/appsettings.json` |

---

## Label set

Currently 9 defaults + 8 custom = 17. Proposed end state:

| Label | Action | Why |
|---|---|---|
| `svc:identity` `svc:tenancy` `svc:notifications` `svc:authentication` `svc:framework` `svc:portal` | **keep** | Matches real project boundaries |
| `feature` `task` | **add** | The KIND axis — stands in for the unusable native issue types |
| `epic` | **add** | The only way to express the epic level (no usable native type) |
| `adr` | **keep** | GitHub has no decision-record concept |
| `spike` | **add** | Time-boxed investigation; DoD is a written answer, not code |
| `blocked` | **add (optional)** | Board `Status` doesn't capture "waiting on something" |
| `spec` | **retire** | Redundant with the native `Feature` type |
| `bug` `documentation` `enhancement` `question` | **keep** | Defaults, still useful |
| `priority:*` | **skip** | Use the board's `Priority` field instead — don't double the axis |
| `good first issue` `help wanted` `duplicate` `invalid` `wontfix` | **optional** | Pure solo work — delete if unused |

## Issue templates

| Template | Kind label | Status |
|---|---|---|
| `spec.yml` | `feature` | done — DoD fixed, `labels: ["feature"]` |
| `bug.yml` | `bug` | done — repro steps, expected vs actual, environment |
| `spike.yml` | `task` + `spike` | done — the question, the time box, "answer recorded in:" |

### Required correction to `spec.yml`

The current definition-of-done reads *"Tests added, and `dotnet test` is green"* and
*"`dotnet build ShopIt.slnx` reports no new warnings"*. Given tests and CI are deferred (decision 6),
the test line is unsatisfiable — there is no test project and no CI to run one. Replace with:

```
- [x] Every acceptance criterion verified manually (no test suite yet)
- [x] `dotnet build ShopIt.slnx` succeeds
- [x] Recorded as an ADR, if this settled an architectural choice
```

Leaving a checklist item that can never be ticked trains you to ignore the checklist.

## Milestones

```
M0  Core service hardening   Identity / Tenancy / Notifications + Permissions   ← now
M1  Product catalog          first e-commerce module, needs catalog.* perms     ← next
M2+                             (to be defined after M1)
```

Not yet scheduled, deliberately deferred:

```
?   Test infrastructure      xunit projects in src/backend/tests/
?   CI                       .github/workflows — makes the DoD enforceable
```

## Project board (Project #1)

| Element | Proposal |
|---|---|
| Existing fields to use | `Status`, `Milestone`, `Parent issue`, `Sub-issues progress` |
| Fields to add | `Priority` (P0–P3 single-select) |
| Field to add later | `Iteration` (only when time-boxing is wanted) |
| View 1 | **Hierarchy** view — group by `Parent issue`, expand/collapse the epic tree |
| View 2 | Board, group by `Status` |
| View 3 | Table, group by `Milestone` |
| View 4 | Table filtered `label:blocked` — the "what's stuck" view |

## Workflow rules

- **Definition of ready** — template completed; every acceptance criterion testable and observable.
- **`main` is PR-only** — enable a branch protection rule / ruleset on `main`: require a pull request,
  block force-pushes, block deletions. Free, since the repo is public. This matters because a second
  machine pushes to `main` today.
- **Branch naming** — `<type>/<issue#>-<slug>` (e.g. `feat/12-tenant-primitives-framework`).
- **Link on close** — PR body contains `Closes #<n>`.
- **Issue-first** — no code without an issue, so spec and change stay linked.
- **Dependencies** — use `--add-blocked-by` rather than prose, so the relationship is queryable.

---

## Candidate M0 backlog

Grouped into epics. This is a proposal for review, not a final list.

### Epic 1 — Multi-tenancy foundation

| # | Issue | Type |
|---|---|---|
| 1.1 | Move `ITenantEntity`, `ICurrentTenant`, `TenantInfo` into `ShopIt.Framework.Domain` | Task |
| 1.2 | Move `CurrentTenant` into `ShopIt.Framework.Infrastructure` | Task |
| 1.3 | Give Tenancy and Notifications tenant isolation (query filter + `TenantId`) | Feature |
| 1.4 | ADR: tenant isolation model (shared schema + query filter vs alternatives) | Task + `adr` |

### Epic 2 — Permissions as a reusable platform capability

**Epic acceptance criterion:** a new service can declare `catalog.view` / `catalog.write`, publish
them, and have them flow into the permission catalog, be granted to Admin, and be enforceable —
**without referencing `ShopIt.Identity.*`**. This is the gate that unblocks M1.

| # | Issue | Type |
|---|---|---|
| 2.1 | Move the permission-catalog contract (`PermissionCatalogPublishedIntegrationEvent` + DTOs) out of `Identity.Application.Contracts` into the framework, so a service can declare permissions without depending on Identity | Task |
| 2.2 | Move `PermissionRequirement` + `RequirePermission` into `Framework.Presentation` (currently duplicated verbatim) | Task |
| 2.3 | Turn catalog publishing into a repeatable hook instead of composition-root code in `Tenancy.API/Program.cs` | Task |
| 2.4 | Decide and implement one permission-resolution pipeline (Identity in-DB vs Tenancy HTTP + 1-minute stale cache) | Feature |
| 2.5 | Prove the recipe end to end with placeholder `catalog.view` / `catalog.write` definitions | Feature |

### Epic 3 — Service layering consistency

| # | Issue | Type |
|---|---|---|
| 3.1 | Unify the aggregate/audit model across Identity and Tenancy | Feature |
| 3.2 | Adopt `Result<T>` / `ToProblem` / `ToHttpResult` in all three services | Feature |
| 3.3 | Normalise the layer sets — decide Notifications' shape (`Domain`/`Infrastructure`/`Presentation`) | Task |
| 3.4 | Delete dead scaffolding: `Identity.API/Features/*`, `Class1.cs` × 2, empty `Framework.CQRS` | Task |
| 3.5 | Fix `Tenancy.Application` → `Identity.Application.Contracts` dependency direction | Task |

### Epic 4 — Messaging reliability

| # | Issue | Type |
|---|---|---|
| 4.1 | Give each service its own Kafka `ConsumerGroupId` | Task |
| 4.2 | Replace simple-type-name inbox handler discovery with an explicit registry | Feature |
| 4.3 | Give Tenancy `handlerAssemblies` + `configureInbox` so it can receive events | Task |
| 4.4 | Dispatch domain events raised on the inbox path (UnitOfWork gap) | Feature |

### Epic 5 — Configuration hygiene

| # | Issue | Type |
|---|---|---|
| 5.1 | Fix `AddDomainServices()` called twice in `Identity.API/Program.cs` | Task |
| 5.2 | Fix `Tenancy.API/appsettings.json` (`"Application": "Identity Service"`) | Task |
| 5.3 | Remove hardcoded client credentials (`"BACKEND_SECRET"`, `"SECRET"`, `subject == "shopit-backend"`); move to configuration | Task |

That's ~21 issues across 5 epics.

### Agreed first batch

Create all 5 epics, then the child issues for Epic 1 **and** Epic 2:

| Group | Issues | Why in the first batch |
|---|---|---|
| **Epic 1** — Multi-tenancy foundation | 1.1 – 1.4 | Blocks Epic 2 and every future service |
| **Epic 2** — Permissions as a reusable capability | 2.1 – 2.5 | The `catalog.view` / `catalog.write` gate that unblocks M1 |

Epics 3, 4, 5 are created as placeholders; their child issues are drafted in a later pass.

### Dependency wiring for the first batch

```
1.1  Move tenant primitives to Framework.Domain
       ├── blocks 1.2  Move CurrentTenant to Framework.Infrastructure
       ├── blocks 1.3  Tenant isolation in Tenancy + Notifications
       └── blocks 2.4  One permission-resolution pipeline
1.4  ADR: tenant isolation model        (start first — the decision gates 1.3)
2.1  Catalog contract to framework      (do first — unblocks 2.3 and 2.5)
2.2  Shared PermissionRequirement       (no blockers)
2.3  Repeatable catalog-publishing hook (blocked by 2.1)
2.5  Prove catalog.view / catalog.write (blocked by 2.1, 2.3)
5.3  Remove hardcoded credentials       (independent — lives in Epic 5)
```

Two real couplings, not bookkeeping:

- **Permission resolution is coupled to the tenancy primitives.** `PermissionResolver` re-queries under
  host scope and filters by `MultiTenancySide`, and the migrations
  `AddMultiTenancySideToPermissionCatalog` / `AddMultiTenancySideToRoles` show the coupling already
  exists. Hence 2.4 depends on 1.1.
- **2.5 is the gate for M1.** The product catalog cannot declare `catalog.view` / `catalog.write`
  until 2.1 and 2.3 land. If this epic slips, M1 slips.

---

## Files to modify

```
.github/ISSUE_TEMPLATE/spec.yml      fix DoD; set type
.github/ISSUE_TEMPLATE/bug.yml       new
.github/ISSUE_TEMPLATE/spike.yml     new
.github/ISSUE_TEMPLATE/config.yml    optional contact links
```

No application code changes in this plan. Labels, milestones, project fields, branch protection, and
issues are created via `gh` / the API.

## Steps

- [x] Normalise labels: retire `spec`; add `epic`, `spike`, `blocked`
- [x] Fix `spec.yml` DoD; add `type: Feature`
- [x] Add `bug.yml` and `spike.yml`
- [x] Create milestone `M0 — Core service hardening` (and `M1 — Product catalog`)
- [x] Add `Priority` field to Project #1; configure the four views incl. **Hierarchy**
- [x] Enable `main` protection: require PR, block force-push and deletion
- [x] Create the 5 M0 epics as `Feature` + `epic` label
- [x] Create Epic 1's four issues and Epic 2's five issues (9 total), wired as sub-issues
- [x] Wire the `blocked by` relationships per the dependency graph above
- [x] Record the workflow rules in `docs/CONTRIBUTING.md` (create it)
- [x] Commit via PR — not direct — since a second machine pushes to `main`

## Verification

- `gh issue list --type Feature --label epic` returns the five epics
- Epic 2's acceptance criterion holds: `catalog.view` / `catalog.write` are declared in a service that
  does **not** reference `ShopIt.Identity.*`, appear in the catalog, are granted to Admin, and are
  enforceable via `RequirePermission`
- `gh issue list --json parent,issueType` shows the hierarchy and types correctly set
- Project #1 Hierarchy view expands the epic tree and `Sub-issues progress` advances when a child closes
- A test PR to `main` is blocked until reviewed
- `gh issue create` with no template is refused; each form requires its acceptance criteria
- Close a test sub-issue and confirm the parent's progress bar moves, then delete the test issues

---

## Status

Recon complete. All questions answered — ready to execute.
