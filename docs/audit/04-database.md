# 04 — Database

**Source:** `ayrikcor_site17896548586.sql` (decompressed ~23.6 MB)  
**Confidence:** CONFIRMED

## Summary

| Metric | Value |
|---|---:|
| Tables | 102 |
| Primary prefix | `wp_` |
| Yoast indexable rows | 613 parsed |
| Price meta records | Present (`_price`, `_regular_price`) |

## Core WordPress tables

| Table group | Tables | Notes |
|---|---|---|
| Posts & content | `wp_posts`, `wp_postmeta`, `wp_comments`, `wp_commentmeta` | Products stored as `post_type=product` |
| Taxonomy | `wp_terms`, `wp_term_taxonomy`, `wp_term_relationships`, `wp_termmeta` | `product_cat`, `product_tag`, `product_type` |
| Users | `wp_users`, `wp_usermeta` | **7,794 total users** — mostly spam |
| Options | `wp_options` | Site config, theme settings, WooCommerce |
| Links | `wp_links` | Legacy blogroll (likely unused) |

## WooCommerce tables (present)

Standard WooCommerce schema including:

- `wp_wc_product_meta_lookup`
- `wp_wc_order_stats` / order-related tables
- `wp_woocommerce_order_items`
- `wp_woocommerce_sessions`
- `wp_wc_admin_notes`
- `wp_wc_download_log`
- `wp_wc_tax_rate_classes`
- `wp_wc_webhooks`
- `wp_wc_customer_lookup`

**Shop orders in posts:** No `shop_order` post type rows detected in regex pass — **orders may be empty or purged** at backup time.

## SEO tables

| Table | Purpose |
|---|---|
| `wp_yoast_indexable` | Canonical SEO index (URLs, titles, focus keywords) |
| `wp_yoast_seo_links` | Internal link graph |
| `wp_yoast_primary_term` | Primary category per post |
| `wp_yoast_migrations` | Plugin migrations |

## Security / plugin tables

| Table | Plugin |
|---|---|
| `wp_wfconfig`, `wp_wfhits`, `wp_wffilemods`, `wp_wfknownfilelist`, ... | Wordfence |
| `wp_revslider_*` | Revolution Slider (residual) |

## Key options (business-relevant)

| Option | Value |
|---|---|
| `blogname` | ایریک پلاستیک ایرانیان |
| `woocommerce_currency` | IRT |
| `woocommerce_default_country` | IR:THR |
| `woocommerce_price_num_decimals` | 0 |
| `users_can_register` | 1 (enabled) |
| `template` / `stylesheet` | woodmart |
| `permalink_structure` | /%postname%/ |

## WooCommerce gateway options found

| Option key | Notes |
|---|---|
| `woocommerce_paypal_settings` | Present — likely disabled/default |
| `woocommerce_maxmind_geolocation_settings` | Present |
| `woocommerce_bacs_settings` | **NOT FOUND** |
| `woocommerce_cod_settings` | **NOT FOUND** |
| Iranian gateway settings (ZarinPal, NextPay, etc.) | **NOT FOUND** |

**Conclusion:** Payment configuration in DB is minimal. Checkout payment behavior is **UNKNOWN** — requires live-site verification or business input.

## User account breakdown

| Category | Count | Action |
|---|---:|---|
| Spam subscribers (heuristic) | 7,779 | **Exclude from migration** |
| Legitimate-looking accounts | 15 | Review individually |

Notable legitimate accounts:

- `Movasseghi` — administrator lineage
- `mizbancoadmin` — hosting/demo admin (exclude unless needed)
- Historical `admin` references in Wordfence config

## Data quality issues

1. **Spam posts** pollute `wp_posts` — must filter by author/date/content heuristics
2. **Malware file index** in Wordfence tables references backdoor files
3. **Split www/non-www** in options may cause inconsistent GUIDs/permalinks
4. **Secondary DB** on same account is unrelated — do not merge

## Extraction artifacts

| File | Contents |
|---|---|
| `.legacy-extract/audit-data-v2.json` | Structured summary |
| `.legacy-extract/url-inventory.json` | 606 unique Yoast permalinks |
| `docs/audit/generated/product-categories.md` | Category table |
| `docs/audit/generated/product-urls.md` | 95 product URLs |
| `docs/audit/generated/pages-inventory.md` | Page URLs |

## Recommended migration DB scope

**Include:**

- Published products + product meta
- Product categories + term meta
- Legitimate pages (shop system pages + content pages)
- Media attachments linked to products
- Yoast metadata for included entities
- Verified customer/order data (if any found in deeper pass)

**Exclude:**

- Spam users
- Casino/spam posts
- Wordfence/wf* tables
- RevSlider tables (unless media dependencies found)
- Unrelated secondary database
