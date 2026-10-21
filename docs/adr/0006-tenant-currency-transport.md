# ADR-0006: Tenant currency transport

- **Status:** Accepted
- **Date:** 2026-10-21
- **Issue:** [#84](https://github.com/Agent-6/shopit/issues/84)
- **Related:** Epic [#74](https://github.com/Agent-6/shopit/issues/74) (M1) · ADR-0001 (tenant isolation) · ADR-0005 (money)
- **Supersedes:** [ADR-0005](0005-money-and-pricing.md) — **in part**: the claim that the currency is *not stored per
  price* and that changing a tenant's currency is out of scope. Its representation, rounding and display decisions
  stand unchanged.

## Context and problem statement

ADR-0005 settled that money is a `Money` value object and that **the currency belongs to the tenant**, not to the
product: one shop, one currency, totals that need no per-currency grouping.

It did not say how another service *learns* that value, and it could not have: Tenancy and Catalog are separate
services with separate databases. While implementing products ([#78](https://github.com/Agent-6/shopit/issues/78)) the
gap became concrete:

- The catalog cannot read Tenancy's `Tenants` table — different database, different service, and reading a peer's
  tables would delete the service boundary ADR-0001 draws around everything else.
- The access token carries `tenant_id` and `tenant_name`. It carries no currency, and nothing put one there.
- There was no `ShopIt.Tenancy.Client` and no internal surface to ask.

The interim answer shipped with #78 was a single configuration value, `Catalog:Currency` (default `EUR`), stamped on
every price and enforced by rejecting a request that named a different code. It was recorded as a deviation, because
it is a **platform-wide** currency in a **multi-tenant** product: two tenants could not price in different money, and
the tenant's own record was not the source of truth for anything.

The decision below is about the *transport*, and it drags one storage consequence with it: a price that was quoted in
euros has to stay in euros when the shop switches to pounds.

## Decision drivers

- **The tenant record must be the only source.** A second copy that can be edited independently is a second truth, and
  a price stamped from the wrong one is a financial error with no compile-time symptom.
- **A currency is not caller identity.** It is shop data, it changes independently of who is calling, and it must not
  be stale for the lifetime of a token.
- **Failures must be visible.** If the currency cannot be established, the write must be refused. A guessed currency
  is wrong money; a refused write is a support ticket.
- **The catalog's reads must not depend on another service being up.** Browsing a catalogue cannot stop because the
  tenant registry restarted.
- **Precedent over novelty.** The platform already has exactly one service-to-service pattern: a typed client in a
  `.Client` assembly, a client-credentials token handler, a `Result` failure at the boundary.
- **Never re-label history.** A price is a fact about a moment. Changing a shop's unit of money must not silently
  restate what it already charged.

## Considered options

1. **A `currency` claim on the access token.** The authentication server reads the tenant's currency and puts it in
   every access token; the catalog reads it from the principal.
2. **A `ShopIt.Tenancy.Client` edge: Tenancy owns the value, exposes it on its internal API, and the catalog caches
   what it gets.** The same shape as `ShopIt.Identity.Client`, which the catalog and Tenancy already use to resolve
   permissions.
3. **A catalog-local copy.** Keep `Catalog:Currency`, but per tenant — a settings row in the catalog's own database,
   maintained by whoever sets a tenant up.
4. **Per-product currency.** The request supplies the currency and the product stores it as its own choice.

## Decision outcome

**Option 2 is accepted.**

The tenant registry owns the currency. Tenancy exposes
`GET /api/internal/tenants/{tenantId}/currency`, guarded by the same `InternalOnly` policy Identity's internal API
uses, and `ShopIt.Tenancy.Client` is the typed access to it: a generated Refit client bound with the
client-credentials handler, and an `ITenantCurrencyProvider` that turns one HTTP answer into one `Result`. The catalog
resolves the currency when it writes a price, and caches the answer briefly.

### Why the token claim was rejected

- **It puts shop data in every user's token.** A currency that never changes per request would be replicated into every
  token issued, logged at every hop, and impossible to revoke.
- **A currency change would be invisible until the token expired.** With a 15-minute access-token lifetime, a tenant
  that corrects its currency would keep stamping the old one for a quarter of an hour, and the "source of truth" would
  be a cache the tenant cannot flush.
- **It couples the authentication server to the tenant registry's data.** Today the auth server decides who you are;
  making it also decide what your money is means a Tenancy outage becomes a login outage.
- The token is already the reason `tenant_id` travels: identity. Adding a second kind of fact to it starts a
  distributed monolith — the next field is the shop's time zone, then its tax rate.

### Why the catalog-local copy was rejected

- **Two editable truths.** A currency stored in the catalog can be changed in the catalog while the tenant record says
  something else, and nothing fails: the two services would simply disagree, each believing itself authoritative.
- **The failure mode is silent and financial.** There is no request that errors when a copy is wrong.
- A local copy also needs its own administrative surface — which is a second tenant settings screen for one field.

### Why per-product currency stays rejected

ADR-0005 rejected it for the product and gave the reason: it is a marketplace feature, and it turns every total into a
grouped per-currency sum. Nothing has changed. The column that *is* stored per product (`PriceCurrency`) is a
historical fact about a price, not a choice — a product cannot be created with a currency its tenant does not use, and
a request that tries is a `400`.

### Storage: the currency is stored with the price

ADR-0005 said the currency columns were unnecessary because the currency comes from the tenant. That is right for what
a *new* price means and wrong for what an *old* price meant.

`PriceCurrency` stays, and it is written from the tenant rather than from the request. The consequences:

- **A tenant's currency change re-prices nothing.** Existing rows keep the currency they were quoted in.
- **A reader is told both.** The product detail response carries the price's stored currency and a `tenantCurrency`
  field holding the currency the tenant prices in now, so a client can show "priced in EUR — this shop now prices in
  GBP" instead of displaying a number in the wrong money.
- **New writes use the tenant's currency**, including an update: re-submitting a product is quoting a new price.

This is the defined behaviour the issue asked for, rather than something left implicit: **existing prices are
preserved, new writes follow the tenant, and the API reports both.**

### Failure policy: no fallback

If the registry cannot answer — it is down, unroutable, or does not know the tenant — the write is refused with a
`Result` failure (`TenantCurrency.Unavailable`, a `500`; or `TenantCurrency.TenantNotFound`, a `404`). There is no
fallback to a default or to a configured currency.

The asymmetry with reads is deliberate: a read stamps nothing, so the detail read degrades — `tenantCurrency` is
`null`, the failure is logged, and the stored price still answers. A write cannot degrade, because the value it would
fall back to ends up on a price.

### Caching: a short-lived in-process cache, not the shared one

`Catalog:Currency` is gone; the catalog caches each tenant's resolved currency for **60 seconds in an `IMemoryCache`**,
in `CachedTenantCurrencyProvider` (the catalog's Infrastructure), and caches **only successes**.

The shared `ICache` was the obvious candidate and was rejected for one reason: the platform's `ICache` is FusionCache
with **fail-safe enabled globally** (it serves the last known value for up to two hours when the distributed cache or
the factory fails). On a write path that is exactly the silent fallback this ADR rejects — a stale currency would be
stamped on a new price with no error anywhere. `ICache` exposes no per-entry control over fail-safe, so using it would
have meant either accepting that or changing a platform-wide setting for one key.

What that buys and costs:

- An answer is reused for 60 seconds: a burst of product writes costs one round trip, not one per request.
- A tenant's currency change is observed by the catalog within 60 seconds rather than instantly, and each catalog
  instance keeps its own copy. That is bounded, documented, and small compared to a token's lifetime.
- Nothing is remembered on failure, so an outage does not leave a fixed window of refused writes behind it.

The invalidation a shared cache would have given (Tenancy evicting the key on change) is deliberately not implemented:
it would reintroduce the cross-service cache-key contract for a three-character value whose staleness is bounded at a
minute.

## Consequences

**Positive**

- One place decides what a shop's money is, and the catalog cannot disagree with it.
- Two tenants price in different currencies, and a request cannot cross them: the currency a request may name is
  exactly its own tenant's.
- The transport is the platform's existing one — `.Client` assembly, client-credentials handler, `Result` failure — so
  there is one service-to-service pattern in the codebase, not two.
- The interim `Catalog:Currency` setting is deleted rather than left as a dormant fallback.

**Negative**

- **Catalog now depends on Tenancy for writes.** A Tenancy outage refuses product creates and updates. That is the
  intended trade — the alternative is guessing — but it is a new availability coupling between two services that used
  to be independent on this path, and it is worth remembering when either is deployed.
- A currency change is visible to the catalog within a cache lifetime (60 s), not immediately, and the cache is
  per-instance.
- Two new moving parts on the path: an internal endpoint guarded by a policy, and a Refit client with a token handler.
- `PriceCurrency` is retained per row, which ADR-0005 called unnecessary. It is now justified by history rather than
  by configuration, which is a distinction that has to be maintained: the column must never be written from the
  request.

**Neutral**

- Tenancy gained a `.Client` assembly, so it is no longer the canonical seven-project service that consumes but never
  offers a client. It now offers one *and* consumes Identity's, which is why `ShopIt.Tenancy.Client` exists while
  `ShopIt.Identity.Client` is still referenced.
- The demo tenant Identity seeds its users into now has a row in the tenant registry (`TenantDataSeeder`), because the
  catalog can no longer price a product for a tenant the registry has never heard of.

## What is not proven

- **The HTTP hop itself is not under test.** The catalog's unit tests fake `ITenantCurrencyProvider`; what they prove
  is the catalog's behaviour *given an answer*. The client-credentials call, the internal policy and the new endpoint
  are verified by running the stack (recorded on the pull request), and nothing runs that in CI.
- **Refit's generated client has no compile-time check that its route matches the endpoint's.** A typo in
  `ITenancyApi` compiles and fails as a 404 at runtime, which — because a 404 is a legitimate answer — would read as
  "the registry does not know the tenant". Only the live verification distinguishes them.
- **No load or contention test exists for the cache.** Sixty seconds is a judgement about how often a tenant changes
  currency (rarely) against how often a price is written (in bursts), not a measurement.
- **Nothing enforces "the currency is never taken from the request".** It is asserted by tests on the create and update
  paths; a third write path added later would need its own.
- **A tenant's currency can only be changed through `PUT /tenants/{id}/currency`.** There is no re-pricing operation,
  and no way to move existing prices to the new currency — that is deliberate, and it is the follow-up below rather
  than a gap in this decision.

## Follow-ups

| Follow-up | Trigger |
|---|---|
| A re-pricing operation: move existing prices to the tenant's current currency, explicitly and auditable | The first tenant that switches currency and wants its catalogue converted |
| Notify the catalog of a currency change instead of waiting for the cache to expire | When a 60-second window is observed to matter (for example a change followed immediately by a bulk import) |
| Per-entry fail-safe control on `ICache` so a shared, invalidatable cache can be used here | When a second service caches a value it must not serve stale |
| Move the client-credentials handler into the framework | A third service needs it — currently Catalog and Tenancy each have a copy |
| Automated proof of the client-credentials round trip | When a CI pipeline exists that can host the stack |
