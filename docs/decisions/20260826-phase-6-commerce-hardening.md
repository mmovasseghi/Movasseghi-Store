# Phase 6 — Commerce Hardening

**Date:** 2026-08-26  
**Status:** Complete

## Decisions

### Server-side cart validation

- **Problem:** Client cart could submit stale prices, removed products, or out-of-stock quantities.
- **Decision:** `validateCartAgainstProducts()` in `src/commerce/cart-validation.ts` is the single source of truth for line prices, stock caps, and subtotals.
- **Endpoints:** `POST /api/cart/validate` (pre-checkout) and `POST /api/orders` (re-validates before persist; 409 on price mismatch).
- **Rationale:** Aligns with AGENTS.md rule — browser is not authoritative for price/stock.

### Persian-aware shop search

- **Problem:** Payload `contains` misses ی/ي, ک/ك, Persian digits, ZWNJ variants.
- **Decision:** Fetch published products (limit 200 when `q` present), filter in-app via `persianSearchMatch()` on name + SKU.
- **Trade-off:** Acceptable for ~95 SKUs; revisit with DB full-text or Meilisearch if catalog grows >500.

### Analytics (GTM dataLayer)

- **Decision:** Lightweight `src/lib/analytics.ts` pushing standard ecommerce events — no GTM container ID until production domain.
- **Events wired:** `view_item`, `add_to_cart`, `view_cart`, `begin_checkout`, `purchase`, `phone_click`.
- **Rationale:** See `docs/ANALYTICS-PLAN.md`; avoids premature third-party script on IP-only staging.

## Files added/changed

| Area | Path |
|---|---|
| Validation | `src/commerce/cart-validation.ts` |
| API | `src/app/api/cart/validate/route.ts`, `src/app/api/orders/route.ts` |
| Search | `src/lib/persian-search.ts`, `src/app/(frontend)/shop/page.tsx` |
| Analytics | `src/lib/analytics.ts`, ProductActions, CartSheet, CheckoutForm, CheckoutSuccess, ProductViewTracker |
| Docs | `docs/commerce/PRODUCT-DATA-MODEL.md`, `docs/ANALYTICS-PLAN.md`, `docs/PERFORMANCE-BUDGET.md` |

## Next phase

**Phase 7 — Payments:** NextPay/ZarinPal adapters in `src/commerce/payments/` — blocked on merchant credentials (approval gate).
