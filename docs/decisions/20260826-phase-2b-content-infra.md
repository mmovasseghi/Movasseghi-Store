# Phase 2b — Content & Infrastructure Decisions

**Date:** 2026-08-26  
**Status:** IMPLEMENTED

## Blog `/mag/`

| Decision | Rationale |
|---|---|
| Migrate **1** legitimate Persian post only | Heuristic filter on 210 legacy posts: 209 casino/spam (CONFIRMED) |
| Return **410 Gone** for spam slugs on `/mag/*` | SEO audit: do not redirect gambling URLs |
| Collection `posts` in Payload | Admin-editable; legacy HTML preserved |

Spam filter: `scripts/legacy/export-blog-posts.py` — Persian content required + blocklist (casino, bet, crypto exchanges, etc.)

## Orders & Pages (prior step)

- Checkout → `POST /api/orders` + Payload `orders` collection
- Static pages: about, contact, pricing + legacy HTML sync

## Media alt text

- `npm run sync:media-alt` — maps product names → attachment alt from `product-media-map.json`
- Skips media that already have meaningful alt (>10 chars, not generic)

## CI/CD

- `.github/workflows/ci.yml` — lint, typecheck, build on PR/push
- `.github/workflows/deploy.yml` — deploy to `/var/www/movasseghi/app` via SSH secrets:
  - `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_SSH_KEY`

## Security headers

Added via `next.config.ts`: `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`

## Not done (needs credentials / Gate)

- Payment gateway (NextPay/ZarinPal)
- Domain + TLS
- Production data migration Gate 4
