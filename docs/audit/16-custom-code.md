# 16 — Custom Code

**Confidence:** CONFIRMED (filesystem)

## Custom / third-party code (non-core)

### special-offer-woodmart plugin

| Item | Detail |
|---|---|
| Path | `wp-content/plugins/special-offer-woodmart/` |
| Purpose | Elementor offer carousel widget |
| Assets | Custom CSS/JS + Swiper from cdnjs |
| Main files | `special-offer-woodmart-demo.php`, `inc/functions.php`, `inc/elementor/widgets/widget-offer.php` |

**Assessment:** Presentation-only. Reimplement as native React component on new platform.

### Woodmart theme customizations

Stored primarily as:

- Theme options (`xts-theme_settings`, backups in `xts_backups_auto`)
- Elementor templates in postmeta
- CMS HTML blocks

No child theme — customizations are database-driven.

### Malicious custom code (CRITICAL)

| Path | Indicators |
|---|---|
| `wp-content/plugins/HelloDollyV2_ixwz/` | Disguised plugin name; files `kurd1.php`, `xnlzcfevhb.txt`, `error_log` |

Wordfence known file list confirms malware paths.

**Action:** Never deploy. Forensic reference only.

### wp-file-manager

Common attack vector plugin present. Treat as compromise indicator.

## WordPress core / vendor code

The majority of PHP is WordPress core, WooCommerce, Elementor, Woodmart, Yoast — **do not port**.

## Custom post types / hooks

No bespoke custom plugin defining business logic detected.

Business rules live in:

- WooCommerce configuration
- Woodmart theme options
- Product/category content

## JavaScript customs

| Source | Role |
|---|---|
| special-offer-woodmart | Offer slider |
| Woodmart theme JS | AJAX shop, mobile nav |
| Elementor frontend | Layout animations |

## Shortcodes

Revolution Slider shortcode referenced in DB: `[rev_slider alias="slider-1"]`

## Recommendations

1. **Rewrite** UX features in Next.js — do not embed legacy PHP
2. **Extract** only data and content semantics
3. **Document** offer carousel behavior from `widget-offer.php` if visual parity desired
4. **Scan** all uploaded PHP in media tree before any reuse (should be none)

## Unknowns

- Must-use plugins (`mu-plugins/`) — not scanned
- Custom code in `functions.php` overrides — Woodmart parent only in extract
