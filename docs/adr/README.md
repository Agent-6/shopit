# Architecture Decision Records

Decisions that are expensive to reverse, recorded so the reasoning survives the decision.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-tenant-isolation-model.md) | Tenant isolation model | Accepted |

## Format

[MADR](https://adr.github.io/madr/) — context, decision drivers, considered options, outcome, consequences.

## Adding one

An ADR is a `task` issue labelled `adr`, closed by a PR that adds the record here. Number sequentially,
never reuse a number, and never edit an accepted ADR in place — supersede it with a new one and set the
old one's status to `Superseded by ADR-XXXX`.

Filename: `NNNN-short-title.md`.
