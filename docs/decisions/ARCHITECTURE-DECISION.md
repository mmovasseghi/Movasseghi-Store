# Architecture Decision (Draft — Gate 2 Pending)

**Status:** PRELIMINARY evaluation only — **not approved**  
**Date:** 2026-08-26  
**Input:** Phase 1 forensics complete

## Requirements summary (from evidence)

| Requirement | Weight |
|---|---|
| 95-product catalog + 22 categories | Must |
| Persian RTL, SEO-first | Must |
| B2B/wholesale (spec — not in legacy) | Must |
| Custom checkout (multi payment path) | Must |
| Admin for products/orders/SEO | Must |
| Clean migration from WP/WC | Must |
| Minimal overlapping backends | Should |
| Iranian payments (NextPay etc.) | Must |

## Options evaluated (preliminary)

### A. Payload-only

CMS + custom commerce in Next.js.

| Pros | Cons |
|---|---|
| Single admin UX potential | Must build all commerce (cart, checkout, payments) |
| Strong content/model flexibility | B2B pricing rules custom |
| Good SEO with Next.js | Migration effort for 95 products |

### B. Payload + custom commerce logic

Payload as product CMS; commerce layer in app code.

| Pros | Cons |
|---|---|
| Structured product model | Still custom cart/checkout |
| Admin for content + products | Two domains of logic in one codebase |
| Fits SEO/content governance | |

### C. Medusa + CMS (Payload or similar)

Medusa for commerce; CMS for content.

| Pros | Cons |
|---|---|
| Cart/checkout/inventory primitives | Two systems + sync |
| B2B plugins possible | Persian/RTL admin may need work |
| | Overlap with Payload if both used |

### D. Custom domain architecture

PostgreSQL + Next.js + minimal headless admin.

| Pros | Cons |
|---|---|
| Maximum control | Highest build cost |
| No CMS license constraints | Admin UX must be built |

## Preliminary recommendation (for Gate 2 discussion)

**Option B: Payload CMS + custom commerce domain logic in Next.js**

**Reasoning:**

1. Legacy is **content + catalog heavy**, not complex WC order history (no orders in backup)
2. B2B/wholesale/Iranian payments need **custom workflows** anyway — no wholesale plugin in legacy
3. Single Next.js app avoids Medusa+CMS duplication
4. Payload supports structured products, SEO fields, media — maps to migration model
5. Commerce integrity rules (server-side price validation) fit custom service layer

**Not recommended now:** Medusa + Payload dual stack — unnecessary complexity for 95 SKUs unless B2B rules exceed custom scope.

## Open questions for Gate 2

- Confirm B2B pricing complexity with business
- Payload vs lighter CMS if admin scope minimal?
- Order volume / ERP integration needs?
- Who operates admin day-to-day?

## Decision record

| Field | Value |
|---|---|
| Decision | **PENDING Gate 2 approval** |
| Decided by | — |
| Review date | After business rules workshop |

**Do not implement architecture until Gate 2 approves.**
