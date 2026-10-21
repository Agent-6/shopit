# ADR-0005: Money and pricing representation

- **Status:** Accepted, clarified by [ADR-0006](0006-tenant-currency-transport.md)
- **Date:** 2026-10-20
- **Issue:** [#76](https://github.com/Agent-6/shopit/issues/76)
- **Related:** Epic [#74](https://github.com/Agent-6/shopit/issues/74) (M1) · ADR-0001 (tenant isolation)
- **Supersedes:** —
- **Superseded in part by:** [ADR-0006](0006-tenant-currency-transport.md) (2026-10-21) — the *transport* that carries
  the tenant's currency to the catalog, the *storage* consequence below (the currency **is** stored with the price),
  and the statement that changing a tenant's currency is out of scope for M1. Representation, rounding and display
  stand unchanged.

## Context and problem statement

Nothing in the repository represents money today. The catalog is about to store prices, and the
choice is expensive to reverse: it decides a column type, the JSON contract, the portal's formatting,
and every future arithmetic operation (discounts, tax, order totals).

Three questions are entangled:

1. **Representation** — a decimal, an integer count of minor units, or a value object.
2. **Where the currency lives** — per tenant (one shop, one currency) or per product (marketplace).
3. **Rounding** — when it happens, and by whom.

## Decision drivers

- A price must never drift by a cent because of a binary floating-point conversion.
- The portal is JavaScript: JSON numbers are IEEE-754 doubles, and money must survive the round trip.
- This is a **multi-tenant SaaS**: each tenant is one shop with one currency, and tenant-scoped
  comparisons and reporting are the normal case. A marketplace where one catalog mixes currencies is
  not the product.
- A future order-total calculation must have exactly one rounding rule to call, not a convention
  spread across services.
- Changing the unit after orders exist is a data migration with financial consequences, so the
  decision is made once, now.

## Considered options

1. **`decimal` column, `decimal` in JSON, currency per tenant.**
   Exact in the database (`numeric`) and in C#, but `decimal` becomes a JSON *number* — JavaScript
   parses it as a double, and `19.99` does not survive that round trip in general.
2. **`long` minor units (cents), currency per tenant.**
   Exact everywhere, JSON-safe (integers), no rounding at storage. Costs readability: every layer
   converts, and a currency with a different minor-unit scale (JPY) breaks the assumption.
3. **`Money` value object over `decimal`, serialised as a string, currency per tenant.**
   Invariants in one place (scale ≤ 2, non-negative, currency matches the tenant), exact on the wire
   because it is a string, readable in the database.
4. **Currency per product.**
   Needed only for a marketplace; it makes every total a grouped, per-currency sum and every
   comparison conditional. Rejected for M1's product.

## Decision outcome

**Option 3**, with the following rules.

### Representation

```csharp
public readonly record struct Money(decimal Amount, string CurrencyCode)
```

- `Amount` is a `decimal` with **at most two decimal places**; more is rejected, not rounded.
- `CurrencyCode` is ISO 4217, **uppercase**, and must equal the tenant's currency.
- Money is **non-negative** in the catalog. Negative amounts belong to refunds, which are not a price.
- Persisted as **two columns** — `Price numeric(18,2)` and a currency taken from the tenant, not
  stored per product.
- Serialised over HTTP as an **object**: `{ "amount": "19.99", "currency": "EUR" }` — the amount is a
  **string**, so JavaScript cannot round it, and the currency travels with it instead of being
  inferred by the client.

### Currency's home: the tenant

The tenant record gains a currency (ISO 4217), defaulting to `EUR` for the seeded tenant. A product
does not choose its currency; it inherits the tenant's, and switching a tenant's currency is out of
scope for M1 (it would imply re-pricing the catalog).

> **2026-10-21 — how the catalog learns it, and what a change means** ([ADR-0006](0006-tenant-currency-transport.md), [#84](https://github.com/Agent-6/shopit/issues/84)).
> The paragraph above left the transport open, and the catalog worked around it with a platform-wide
> `Catalog:Currency` setting. ADR-0006 settles the transport (a `ShopIt.Tenancy.Client` edge plus a short-lived cache
> in the catalog) and changes two things stated here. The currency **is** kept with each price, because a quoted price
> is a historical fact: a tenant that changes currency keeps its existing prices on the old one, and the API reports
> both. Switching a tenant's currency is therefore **in** scope for M1 — as a change of what new writes use, not a
> re-pricing.

### Rounding

**Storage does not round: it rejects.** A price with three decimals is a `400` with a field-level
validation error, because silently turning `19.999` into `20.00` changes what the shop charges.

Arithmetic — discounts, tax, totals — is not in M1, but the rule for it is recorded here so the first
one does not invent its own: **round half away from zero, to the currency's minor unit, once, at the
end of the calculation.** Intermediate values keep full precision.

### Display

The portal formats with `Intl.NumberFormat` from `(amount, currency)`, so the symbol and the decimal
separator follow the reader's locale. The API never formats.

## Consequences

**Positive**

- No float anywhere on the money path: `numeric` in the database, `decimal` in C#, string in JSON.
- One place defines what a valid price is, and the money ADR is referenced by the product aggregate
  rather than re-deriving the rules.
- Currency is a property of the shop, which matches the SaaS model and keeps totals simple.

**Negative**

- A value object needs an EF Core conversion and a JSON converter; two pieces of infrastructure the
  rest of the domain does not need yet.
- Minor-unit currencies (JPY, KRW) are not supported: the scale is fixed at 2. Recorded as a
  limitation, not a bug — supporting them means the scale comes from the currency, which is a
  follow-up decision.
- Storing the currency once on the tenant means a product row cannot be interpreted without its
  tenant. Acceptable: products are already tenant-scoped, and no query crosses tenants by design
  (ADR-0001).

## What is not proven

- No arithmetic exists yet, so the rounding rule is a decision, not a tested behaviour. The first
  total calculation must add tests that exercise half-away-from-zero at the boundary.
- The `numeric(18,2)` choice assumes prices fit 16 integer digits — far beyond any realistic catalog,
  but it is an assumption rather than a measurement.
