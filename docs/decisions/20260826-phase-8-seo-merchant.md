# Phase 8 — SEO & Merchant Feed

**Date:** 2026-08-26  
**Status:** Partial complete

## Implemented

| Item | Path |
|---|---|
| Dynamic robots (staging noindex) | `src/app/robots.ts` |
| Canonical URLs — product, category, shop, static pages | `generateMetadata` + `canonicalUrl()` |
| Cart/checkout noindex | page metadata |
| Google Merchant RSS feed | `GET /feed/products` |
| Image formats AVIF/WebP | `next.config.ts` |
| Alt sync fix | `scripts/import/sync-media-alt.ts` — less aggressive skip |

## Deferred (approval / infra)

- Domain cutover + Search Console validation
- Full redirect regression on production domain
- TLS (`scripts/infra/nginx-tls.sh`)

## Feed usage

Submit `https://{domain}/feed/products` to Google Merchant Center after domain live.
