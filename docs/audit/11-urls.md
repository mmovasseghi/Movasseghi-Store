# 11 — URLs

**Total unique URLs (Yoast indexables):** 606  
**Confidence:** CONFIRMED

Raw inventory: `.legacy-extract/url-inventory.json`

## URL classification (heuristic pass)

| Class | Approx. count | Migration action |
|---|---:|---|
| Products (`/product/`) | 95 | KEEP → MAP_TO_NEW_URL (domain) |
| Product categories | ~22+ | KEEP slugs |
| System pages (shop, cart, checkout, account) | 5 | MAP to new routes |
| Homepage | 1 | MAP |
| Legitimate Persian content | ~50–80 | Review individually |
| Casino/gambling spam posts | ~150+ | **410** — do not redirect |
| Spam author archives | many | **410** |
| Query-string duplicates (`?post_type=product&p=`) | ~50 | Consolidate to canonical pretty URL |
| CMS block previews (`?cms_block=`) | ~10 | IGNORE / noindex |

## Canonical hostname issue

| Setting | URL |
|---|---|
| `siteurl` | `https://ayrik-cornstarch.com/` (non-www) |
| `home` | `https://www.ayrik-cornstarch.com` (www) |

Yoast permalinks predominantly use **www**. New platform must enforce single canonical host.

## Core commercial URLs

| URL | Type | Priority |
|---|---|---|
| `/` | Homepage | P0 |
| `/shop/` | Shop archive | P0 |
| `/product-category/ظروف-یکبار-مصرف-آملون/` | Category hub | P0 |
| `/product/{slug}/` | Product | P0 |
| `/قیمت-آنلاین-محصولات-آملون/` | Pricing landing | P1 |
| `/mag/` | Blog | P2 |
| `/تماس-با-ما/` | Contact | P1 |
| `/درباره-ما/` | About | P1 |

## Product URL list

See [generated/product-urls.md](./generated/product-urls.md) — 95 entries.

## Redirect strategy (preliminary)

| Old pattern | New pattern | Status |
|---|---|---|
| `ayrik-cornstarch.com/*` | `{new-domain}/*` | Pending domain decision |
| Spam post slugs | 410 Gone | Recommended |
| `?p={id}` legacy | 301 → pretty permalink | Required |

Full redirect map: `docs/seo/REDIRECT-MAP.md`

## Validation scripts (future)

Target location: `scripts/seo/` — to be implemented at migration phase.

## Live crawl status

Live site returned **HTTP 500** — no live crawl completed. Baseline built from DB/Yoast only.
