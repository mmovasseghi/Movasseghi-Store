# 13 — Internal Links

**Confidence:** PARTIAL — Yoast link table present; full graph not exported in Phase 1

## Data sources

| Source | Table / location | Status |
|---|---|---|
| Yoast SEO links | `wp_yoast_seo_links` | Present in DB — not fully parsed |
| Primary term | `wp_yoast_primary_term` | Present |
| Navigation menus | `nav_menu_item` posts (32) | CONFIRMED count |
| Product ↔ category | `wp_term_relationships` | Standard WC |
| CMS blocks | Woodmart `?cms_block=` URLs in indexables | Detected |

## Navigation menus (evidence)

Three nav menus in taxonomy:

| Menu ID | Items (count) |
|---|---:|
| 16 | 5 |
| 17 | 18 |
| 18 | 4 |

Exact menu structure: **NOT EXTRACTED** in Phase 1 — requires menu item post pass.

## Observed linking patterns (inferred)

| Pattern | Purpose |
|---|---|
| Category → products | Standard WooCommerce archive |
| Homepage → categories | Expected in Woodmart homepage |
| Product → related products | Theme/WooCommerce related |
| Blog → products | UNKNOWN density on legitimate posts |
| Footer links | Theme-controlled — not parsed |

## CMS blocks (Woodmart)

Internal preview URLs found for blocks named (Persian):

- درب ها، دیس و بشقاب، سطل ها، ظروف بسته بندی، ظروف درب دار، ظروف چند خانه، قاشق/چنگال/کارد، فوتر المنتوری، لیوان و فنجان، کاسه ها

These suggest **category shortcut blocks** on homepage or landing sections.

## SEO internal linking (Yoast)

Products have `link_count` and `incoming_link_count` fields in indexables — useful for prioritizing high-linked products during migration.

**Not extracted in Phase 1.**

## Spam link pollution

Casino spam posts likely contain outbound links to gambling sites. **Exclude entirely** from link graph migration.

## Recommendations for new platform

1. Build internal link map from clean entities only
2. Ensure category hub → product links for commercial SEO targets
3. Support informational → category/product journeys (not blog spam)
4. Implement breadcrumb schema on category + product (see 14-schema)
5. Phase 2 task: parse `wp_yoast_seo_links` into `docs/seo/INTERNAL-LINKING.md`

## Unknowns

- Full menu tree with URLs
- Footer/header link inventory
- Orphan product count
- Broken internal links (live site down)
