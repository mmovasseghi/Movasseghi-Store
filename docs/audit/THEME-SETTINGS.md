# Theme Settings — Woodmart + WooCommerce + Site

**Status:** CONFIRMED from `xts_backups_auto` / `xts-theme_settings` snapshots + wp_options  
**Theme:** Woodmart 7.0.4

---

## WordPress core

| Option | Value | Visual impact |
|---|---|---|
| `template` / `stylesheet` | `woodmart` | Active theme |
| `WPLANG` | fa_IR | RTL |
| `blogdescription` | `\| فروشگاه ظروف یکبار مصرف` | Meta context |

---

## Woodmart — shop / catalog

| Setting | Value | Effect |
|---|---|---|
| `ajax_shop` | enabled | Filter/sort without full reload |
| `ajax_scroll` | enabled | Infinite/paged loading |
| `products_hover` | `base` | Hover excerpt on product cards |
| `products_bordered_grid` | enabled | 1px grid lines between products |
| `grid_quantity` | enabled | Qty on archive (legacy) |
| `stock_progress_bar` | enabled | Scarcity bar on grid |
| `shop_countdown` | enabled | Promo timers |
| `cat_desc_position` | `before` | Category SEO text above grid |
| `shop_breadcrumbs` | enabled | Breadcrumb trail |
| `shop_filters` | enabled | Sidebar/drawer filters |

---

## Woodmart — product single

| Setting | Effect |
|---|---|
| Gallery + zoom | Product image lightbox |
| Sticky add-to-cart | Desktop scroll behavior |
| Tabs / accordion | Description sections |

*(Exact keys in xts panel — values CONFIRMED in `03-theme-and-plugins.md`)*

---

## Woodmart — header / footer

| Element | Source | Notes |
|---|---|---|
| Header builder | WHB JSON in options | Logo, menu, search, cart rows |
| Logo assets | Media library IDs | `logo_top`, `logo_main`, mobile variant |
| Sticky header | `.whb-clone` | Duplicate header on scroll |
| Footer | Widget areas | Trust, links — widget content in DB |

**UNKNOWN:** Full WHB JSON layout without admin export.

---

## Custom CSS (wp_options)

Extracted variables — see `VISUAL-DESIGN-AUDIT.md`:

| Variable | Value |
|---|---|
| `--wd-primary-color` | `#428D42` |
| `--wd-text-color` | `#777777` |
| `--wd-title-color` | `#242424` |
| `--wd-link-color` | `#333333` |
| `--wd-main-bgcolor` | `#FFFFFF` |
| `--brdcolor-gray-300` | `rgba(0,0,0,0.105)` |

Elementor accent greens: `#459647`, `#385224`.

---

## WooCommerce (visual)

| Setting | Effect |
|---|---|
| Currency IRT | 0 decimal prices in UI |
| Placeholder image | Default WC placeholder if no image |
| Cart/checkout templates | Overridden by Woodmart `woocommerce/*` |

---

## Elementor (homepage)

| Meta | Purpose |
|---|---|
| `_elementor_edit_mode` | builder |
| `_elementor_data` | Section JSON (not fully parsed in audit) |
| `_elementor_page_settings` | Page-level colors/spacing |

---

## New platform mapping

| Legacy setting | New config |
|---|---|
| Woodmart options | Code conventions + Payload fields |
| Custom CSS vars | `globals.css` `@theme` tokens |
| WHB header | React `Header` component |
| Elementor homepage | Native `page.tsx` sections |
| Yoast product SEO | Payload `seo` group on products/categories |

Configuration lives in **code + CMS**, not a theme options panel clone.
