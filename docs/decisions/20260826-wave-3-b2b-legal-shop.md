# Wave 3 — B2B, legal, shop UX

**Date:** 2026-08-26

## Shipped

| Feature | Route / API |
|---|---|
| Shop sort + sale filter | `/shop?sort=&sale=1` + `ShopToolbar` |
| Special offers homepage | Section from sale products |
| B2B quote form | `/b2b/quote` + `POST /api/quotes` + `quotes` collection |
| Order tracking | `/track-order` + `POST /api/orders/track` |
| Legal pages | `/shipping`, `/payment-methods`, `/returns`, `/terms`, `/privacy` |
| Error UI | `error.tsx` |
| Footer links | Services + legal columns |

## Migration

`20260826_180000_quotes.ts` — run `npx payload migrate` on deploy.

## Next

Account auth (Payload users), shop attribute filters, admin ops dashboard.
