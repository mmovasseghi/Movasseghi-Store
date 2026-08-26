# Redirect Map (Template)

**Status:** Template — populate when new domain confirmed  
**Rule:** Every important legacy URL must resolve.

## Format

| Old URL | Action | New URL | Notes |
|---|---|---|---|
| example | 301 | example | |

## Priority 0 — Core

| Old URL | Action | New URL | Notes |
|---|---|---|---|
| `https://www.ayrik-cornstarch.com/` | 301 | `https://{NEW}/` | Homepage |
| `https://ayrik-cornstarch.com/*` | 301 | `https://{NEW}/*` | Host normalize |
| `/shop/` | 301 | `/shop` | |
| `/product-category/ظروف-یکبار-مصرف-آملون/` | 301 | same slug | Primary SEO asset |

## Priority 1 — Products (95)

Generate from `docs/audit/generated/product-urls.md` — one 301 per slug.

## Priority 2 — Categories (22)

Generate from `docs/audit/generated/product-categories.md`

## Priority 3 — Content pages

Manual list after content sanitization.

## 410 Gone — Spam sample patterns

| Pattern | Action |
|---|---|
| `/*/casino/*` | 410 |
| `/*/bet-*` | 410 |
| `/*/bonus-*` | 410 |
| `/author/www-*-blogspot-*` | 410 |

**Do not 301 spam to new site.**

## Validation (future)

Scripts in `scripts/seo/`:

- `validate-redirects.ts` — check status + final URL
- `validate-canonical.ts` — parity with sitemap

## Release blocker

Any P0/P1 URL returning 404 without approved 410 blocks release.
