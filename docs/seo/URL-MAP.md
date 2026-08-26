# URL Map (Draft)

**Domain migration:** `www.ayrik-cornstarch.com` → `{NEW_DOMAIN}` (TBD)

Legend: **KEEP** | **301** | **410** | **MAP** | **IGNORE**

## System routes

| Legacy URL | Action | New route (proposed) |
|---|---|---|
| `/` | MAP | `/` |
| `/shop/` | MAP | `/shop` or `/products` |
| `/cart/` | MAP | `/cart` |
| `/checkout/` | MAP | `/checkout` |
| `/my-account/` | MAP | `/account` |
| `/mag/` | MAP | `/blog` or omit if deprioritized |

## Category routes (preserve slug semantics)

| Legacy pattern | Action |
|---|---|
| `/product-category/ظروف-یکبار-مصرف-آملون/` | 301 → new domain same slug |
| All 22 category slugs | 301 slug-preserving |

Full slugs: `docs/audit/generated/product-categories.md`

## Product routes

| Legacy pattern | Action |
|---|---|
| `/product/{slug}/` | 301 — **95 products** |

Full list: `docs/audit/generated/product-urls.md`

## Content pages (Persian slugs)

| Page | Action |
|---|---|
| `/تماس-با-ما/` | 301 |
| `/درباره-ما/` | 301 |
| `/قیمت-آنلاین-محصولات-آملون/` | 301 — high value |
| `/برگه-نمونه/` | 410 |

## Exclude (410 Gone)

| Pattern | Reason |
|---|---|
| `/casino*` , `/bet*` , `/bonus*` | Spam |
| `/author/spam-user/` | Spam archives |
| Query duplicates `?p=`, `?post_type=product&p=` | Consolidate to canonical |

## Non-indexable (IGNORE)

| Pattern | Reason |
|---|---|
| `?cms_block=` | Woodmart preview URLs |
| `/wp-admin/*`, `/wp-json/*` | Utility |

## New domain TBD

Until final domain confirmed, use staging hostname with **noindex**.

See REDIRECT-MAP.md for redirect table format at migration time.
