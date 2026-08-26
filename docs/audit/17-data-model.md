# 17 — Data Model

Target entity model for migration planning. Fields marked **CONFIRMED** exist in legacy; others are **TARGET** for new platform.

## Entity relationship (legacy)

```
Category (product_cat)
  └── Product (post_type=product)
        ├── ProductMeta (wp_postmeta)
        ├── Media (attachment)
        └── YoastIndexable

Page / Post (content)
User (customer/admin)
Order (shop_order) — empty/minimal in backup
```

## Product (CONFIRMED)

| Field | Legacy storage | Notes |
|---|---|---|
| id | wp_posts.ID | |
| name | post_title | Persian |
| slug | post_name | |
| status | post_status | publish/draft/trash |
| description | post_content | HTML |
| short_description | post_excerpt | |
| sku | _sku | verify coverage |
| regular_price | _regular_price | integer IRT |
| sale_price | _sale_price | optional |
| stock_quantity | _stock | |
| manage_stock | _manage_stock | |
| featured_image_id | _thumbnail_id | |
| gallery_ids | _product_image_gallery | CSV |
| categories | term_relationships | product_cat |
| seo_title | Yoast | often empty title field |
| seo_description | Yoast indexable | |
| focus_keyword | Yoast indexable | |

## Category (CONFIRMED)

| Field | Legacy storage |
|---|---|
| id | term_id |
| name | wp_terms.name |
| slug | wp_terms.slug |
| description | wp_term_taxonomy.description |
| parent_id | wp_term_taxonomy.parent |
| product_count | wp_term_taxonomy.count |
| sort_order | termmeta `order` |
| icon/image | termmeta (often empty) |

## Page (CONFIRMED)

Standard WordPress page fields + Elementor postmeta.

## User (CONFIRMED)

| Field | Storage |
|---|---|
| id | wp_users.ID |
| login | user_login |
| email | user_email |
| display_name | display_name |
| roles | wp_capabilities usermeta |

**Migration:** Only verified customers/admins — exclude spam.

## Order (UNKNOWN / minimal)

No shop_order posts detected. Either:

- Store had no orders in DB, or
- Orders purged, or
- Different storage — unlikely for WC

## YoastIndexable (CONFIRMED)

Denormalized SEO record — use as migration convenience table.

## Target extensions (NOT in legacy — new platform)

Per project spec, model should **support** (nullable until business confirms):

| Field | Status |
|---|---|
| wholesale_price | TARGET |
| moq | TARGET |
| pack_size / carton_size | TARGET — may parse from titles |
| material | CONFIRMED in content ("آملون", نشاسته ذرت) |
| capacity_ml | CONFIRMED in product names |
| heat_resistance_c | IN CONTENT — SOURCE_REQUIRED per SKU |
| applications | IN CONTENT prose |
| b2b_only flag | TARGET |

## Single source of truth (new platform)

Product record must feed: storefront, admin, JSON-LD, sitemap, merchant feed, search index.

See future: `docs/PRODUCT-DATA-MODEL.md` (post Gate 2).
