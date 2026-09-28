# Contributing to shopit

Spec-driven development on GitHub Issues. The issue is the spec; code follows it. **No code without an
issue** — so the reasoning and the change stay linked.

## Hierarchy

```
epic      issue + labels `feature` + `epic`
  feature issue + label `feature`
    task  issue + label `task`, a sub-issue of the feature
```

GitHub nests sub-issues 8 levels deep (100 per parent), but **use 3 at most**. Past three it becomes
bookkeeping rather than information. Only create an epic when it genuinely groups several features —
see #11 and #12 for examples.

Create children directly with `--parent`:

```bash
gh issue create --parent 12 --label task --milestone "M0 — Core service hardening" \
  --title "..." --body-file -
gh issue edit 100 --add-sub-issue 123,124
```

## ⚠️ Issue types are not available on this repo

`Agent-6` is a user account, and issue types are organisation-scoped. The REST endpoint
`GET /repos/Agent-6/shopit/issue-types` *does* return Task/Bug/Feature, which is misleading — they
cannot be applied:

- GraphQL `repository.issueTypes` returns `null`
- every issue's `type` field is absent
- `PATCH .../issues/N -f type=Feature` silently no-ops
- `gh issue edit N --type Feature` fails with `type "Feature" not found; available types:` (empty)

**So KIND is a label, not a type.** Do not waste time on `--type`.

## Labels

Three axes. An issue normally carries one KIND, one AREA, and any MARKERS that apply.

| Axis | Labels | Meaning |
|---|---|---|
| **KIND** | `feature` | A capability or change, written as a spec |
| | `task` | A specific piece of work |
| | `bug` | Something broken |
| **AREA** | `svc:identity` `svc:tenancy` `svc:notifications` `svc:authentication` `svc:portal` `svc:framework` | Which part of the system |
| **MARKERS** | `epic` | Groups several features toward one outcome |
| | `adr` | Settles an architectural decision |
| | `spike` | Time-boxed investigation; the deliverable is a written answer |
| | `blocked` | Waiting on something (see Dependencies) |

`enhancement` and `documentation` are GitHub defaults and still usable; `spec` was retired because
`feature` covers it.

## Templates

Blank issues are disabled — every issue goes through a form.

| Template | KIND label | Use for |
|---|---|---|
| **Spec** | `feature` | Capabilities. Requires goal, context, Given/When/Then acceptance criteria, and out-of-scope. |
| **Bug** | `bug` | Requires reproduction steps, expected vs actual, environment. |
| **Spike** | `task` + `spike` | Requires a question, a time box, and where the answer gets recorded. |

An issue is a spec only if its acceptance criteria are **observable and testable**. If you cannot write
a test for a criterion, it is not a criterion yet — it is a wish.

## Milestones

| Milestone | Scope |
|---|---|
| **M0 — Core service hardening** | Identity / Tenancy / Notifications + the Permissions subsystem |
| **M1 — Product catalog** | First e-commerce module |

M1 is blocked until #24 proves that a service can declare `catalog.view` / `catalog.write` without
referencing `ShopIt.Identity.*`.

**Iterations are deliberately not used.** A milestone answers *which phase*, an iteration answers *when* —
and a solo developer gets more from the former than from a sprint calendar to maintain. Add the
`Iteration` project field later if that changes.

## Project board

Project **#1 "shopit"** (private, linked to this repo) — https://github.com/users/Agent-6/projects/1

| View | Layout | Notes |
|---|---|---|
| All issues | Table | |
| Board | Board | **Group by `Status` manually** |
| By milestone | Table | **Group by `Milestone` manually** |
| Hierarchy | Table | **Enable hierarchy / group by `Parent issue` manually** |
| Blocked | Table | Filter `label:blocked` (already set) |

`Priority` (P0–P3) is a project field, not a label. Grouping cannot be set through the API — the
`UpdateProjectV2ViewInput` type only accepts `name`, `layout`, `filter` and `configuration` — so those
three groupings need one click each in the UI.

## Starting work on an issue

Three steps, in this order:

```bash
git checkout main && git pull                      # 1. branch first, from up-to-date main
git checkout -b feat/24-prove-permission-recipe

gh issue edit 24 --add-assignee Agent-6            # 2. then claim it

# 3. then do the work and open the PR
```

**Assignment means "I am working on this now"**, not "this is mine forever". Claiming *after* the branch
exists is deliberate:

- An **unassigned** open issue means nobody has picked it up.
- An **assigned** open issue means work is in progress — and the branch name tells you which one.
- If work is abandoned, **unassign it**, so it goes back into the unclaimed pool.

This is why the issue templates do **not** set `assignees` in their frontmatter: that would assign every
issue at creation, and the signal would mean nothing.

## Branches and pull requests

`main` is covered by the **"main protection"** ruleset:

- a pull request is **required** — no direct pushes
- force-pushes and branch deletion are blocked
- `current_user_can_bypass` is `never`, so this applies to the repo owner too
- **zero approvals required**, because GitHub prevents approving your own PR — requiring one would
  deadlock a solo developer. The PR is for the record and the diff, not for a second reviewer.

```bash
git checkout -b feat/24-prove-permission-recipe
# ... work ...
git commit -m "feat(identity): prove catalog.view/catalog.write recipe"
git push -u origin feat/24-prove-permission-recipe
gh pr create --fill --body "Closes #24"
```

Branch naming: `<kind>/<issue#>-<slug>`. Put `Closes #<n>` in the PR body.

## Dependencies

Model them explicitly rather than describing them in prose — they become queryable:

```bash
gh issue create ... --blocked-by 20
gh issue edit 24 --add-blocked-by 20,22
gh issue edit 24 --add-blocking 30
```

The `blocked` **label** is manual state for the Blocked board view; GitHub's own blocked-by
relationships do not set it. Remove the label when the blocker closes.

## Definition of ready

- The template is completed in full.
- Every acceptance criterion is observable and testable.
- Out-of-scope is filled in — writing it down is what stops scope creep.

## Definition of done

The checklist lives in the issue template. In short: criteria verified, the solution builds, and the
decision recorded as an ADR if one was made.

> **Tests and CI are deferred** (deliberate, so e-commerce features come first). The spec template
> therefore says *"verified manually (no test suite yet)"* rather than pretending `dotnet test` means
> something. There is no `src/backend/tests/` and no `.github/workflows/`. When that changes, restore the
> stronger checklist item.

## Gotcha: the build fails while the dev stack runs

`dotnet build ShopIt.slnx` fails with `MSB3027` / `MSB3021` — "file is locked by ShopIt.Identity.API" —
whenever the Aspire stack is running, because the running services hold their own output DLLs. It is not
a code error. Either stop the stack first, or build elsewhere:

```bash
cd src/backend && dotnet run --project ShopIt.AppHost --launch-profile https   # stop this first
# or
dotnet build ShopIt.slnx -p:BaseOutputPath=C:/temp/shopit-verify/
```

See also `.freebuff/run.md` for the full local run procedure.
