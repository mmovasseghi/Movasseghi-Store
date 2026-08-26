# DATABASE-AUDIT — Legacy WordPress Schema

**Canonical detail:** [04-database.md](./04-database.md)

## Core facts (CONFIRMED)

| Item | Value |
|---|---|
| Database | `ayrikcor_site17896548586` |
| Tables | 102 |
| Prefix | `wp_` |
| Products | 95 publish in `wp_posts` |
| Attachments | 149 |
| Yoast indexables | 613 rows |

## Key tables for migration

- `wp_posts` / `wp_postmeta` — products, pages, media
- `wp_terms` / `wp_term_taxonomy` / `wp_termmeta` — categories
- `wp_yoast_indexable` — SEO metadata
- `wp_wc_product_meta_lookup` — prices, SKU

## Ignored database

`ayrikcor_site1235445166` — unrelated `freefarmdota2.top` site.

## Extract location

`.legacy-extract/db/site17896548586.sql` (gitignored)
