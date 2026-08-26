# 03 — Theme and Plugins

**Confidence:** CONFIRMED (filesystem listing + database references)

## Active theme

| Theme | Version | Type | Notes |
|---|---|---|---|
| **Woodmart** | 7.0.4 | Commercial (XTemos / ThemeForest) | Primary storefront UX |

### Woodmart configuration highlights (from `xts_backups_auto` options snapshot)

| Setting | Value | UX impact |
|---|---|---|
| AJAX shop | enabled | Filter/sort without full reload |
| AJAX scroll | enabled | Infinite/paged shop loading |
| Products hover | `base` | Hover interaction on product cards |
| Bordered product grid | enabled | Visual grid styling |
| Product quantity on grid | enabled | Qty input in archive |
| Stock progress bar | enabled | Scarcity UI |
| Shop countdown | enabled | Promo timers |
| Category description position | `before` | SEO text above product grid |
| Shop breadcrumbs | enabled | Navigation + schema support |

## Plugin inventory (filesystem)

| Plugin | Purpose | Migration relevance |
|---|---|---|
| **woocommerce** | Ecommerce core | **CRITICAL** — data model source |
| **woodmart-core** | Theme companion | Low — theme-specific; don't port |
| **wordpress-seo** + **wordpress-seo-premium** | Yoast SEO | **HIGH** — metadata source |
| **elementor** | Page builder | MEDIUM — layout reference only |
| **fancy-elementor-gallery-box** | Gallery widget | LOW |
| **special-offer-woodmart** | Offer carousel widget | LOW — reimplement in new UI |
| **wordfence** | Security WAF | N/A — do not migrate config |
| **flying-press** | Performance/cache | N/A — replace with modern stack |
| **duplicator** | Backups/migration | N/A |
| **mailchimp-for-wp** | Newsletter | OPTIONAL |
| **site-mailer** | Email | OPTIONAL |
| **safe-svg** | SVG uploads | OPTIONAL |
| **classic-editor** / **classic-widgets** | Editor compatibility | N/A |
| **wp-file-manager** | File manager | **REMOVE** — security risk |
| **HelloDollyV2_ixwz** | Disguised malware | **MALWARE** — never deploy |

## Plugins referenced but not in active directory scan

Revolution Slider data exists in DB (`wp_revslider_*` tables, `rs-templates` option) but plugin directory not in current filesystem listing — may have been removed while DB tables remain.

## Page builder usage

| Builder | Evidence |
|---|---|
| Elementor | `_elementor_edit_mode`, `_elementor_template_type` postmeta; Elementor library type |
| Gutenberg | Some default WP block markup in sample page |

Product pages show Elementor widget markup mixed with WooCommerce product title classes.

## TGMPA / bundled plugins

Woodmart historically bundles required plugins via TGMPA (`tgmpa_dismissed_notice_tgmpa` in usermeta) — standard ThemeForest pattern.

## Theme-dependent features to reimplement

| Legacy feature | New platform approach |
|---|---|
| AJAX shop filters | Native Next.js catalog + URL-driven filters |
| Woodmart wishlist | Custom or lightweight wishlist |
| Special offer carousel | Design system component |
| Woodmart category icons/images | CMS media fields on categories |
| Mini-cart / sticky mobile checkout | First-class cart UX requirement |

## Plugin versions (sample)

| Plugin | Version | Source |
|---|---|---|
| WooCommerce | 9.4.2 | plugin header |
| WordPress | 6.8.3 | core |

Other plugin versions: **NOT EXTRACTED** in this pass — verify if needed before migration tooling.

## Recommendations for new stack

- **Do not** install Woodmart, Elementor, or legacy plugin stack on new platform
- **Do** extract: product data, category tree, Yoast meta, media, legitimate page content
- **Do not** migrate Wordfence/file-manager/malware artifacts
