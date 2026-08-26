# Theme Reconstruction — Movasseghi Legacy

**Status:** COMPLETE (forensics pass 2026-08-26)  
**Source:** `Old Verison WebSite/backup` (read-only) + `.legacy-extract/` + `docs/audit/*`  
**Do not rescan raw backup** — use linked docs.

---

## Active theme identity

| Field | Value | Confidence |
|---|---|---|
| Active theme | **Woodmart** | CONFIRMED |
| Version | **7.0.4** | CONFIRMED |
| Vendor | XTemos / ThemeForest | CONFIRMED |
| Child theme | **None** | CONFIRMED (`template` = `stylesheet` = woodmart) |
| Companion | `woodmart-core` plugin | CONFIRMED |
| Locale | `fa_IR`, RTL | CONFIRMED |
| Page builder | **Elementor** (+ Gutenberg on some pages) | CONFIRMED |
| Commerce | **WooCommerce** | CONFIRMED |

---

## What defines the visual appearance

The legacy look is **not** one CSS file. It is the stack:

```
Woodmart parent theme (style.css, style-rtl.css, woodmart-font icons)
  + xts-theme_settings (theme panel options)
  + WooCommerce template overrides (woocommerce/*)
  + Woodmart template parts (whb header builder, content-product-*.php)
  + Elementor page meta on homepage (page ID 13)
  + wp_options custom CSS (Additional CSS)
  + Plugin widgets (special-offer-woodmart)
  + Yoast / product HTML content
  + Real product media from uploads/
```

---

## Signature visual DNA (must survive modernization)

| DNA element | Legacy mechanism | New platform |
|---|---|---|
| Primary green `#428D42` | `--wd-primary-color` | `brand-green` evolution `#2D6A4F` |
| Bordered product grid | `products_bordered_grid` option | `ProductCard variant="grid"` |
| Product hover `base` | `products_hover: base` | shortDescription on hover |
| Category desc before grid | `cat_desc_position: before` | `/shop/[category]` header block |
| Header builder | WHB (`whb-*` classes) | `Header` + `MobileNav` + `HeaderSearch` |
| Mini-cart dropdown | `wd-dropdown-cart` | `CartSheet` |
| Full product HTML | `post_content` | `legacyDescriptionHtml` |
| IranYekan 14px body | theme typography | Vazirmatn 16px min (mobile a11y) |
| Container ~1220px | Woodmart `.container` | `max-w-6xl` (72rem) |
| Trust badges | uploads 2022/01 | `/trust/enamad.png`, `samandehi.png` |

---

## Filesystem map (theme)

```
wp-content/themes/woodmart/
├── style.css, style-rtl.css     ← base + RTL
├── inc/                         ← theme logic
├── woocommerce/                 ← WC overrides (cart, single, loop)
├── header-elements/             ← WHB partials
├── template-parts/              ← content-product-base.php (active)
├── css/parts/                   ← component CSS
└── js/                          ← AJAX shop, mobile nav
```

**15 product card hover templates** exist; only **`base`** is active.

---

## Page builder (Elementor)

| Page | ID | Builder | Notes |
|---|---:|---|---|
| Homepage | 13 | Elementor | OG hero `2023/12/picMain-1.png` |
| Other CMS | varies | Elementor/Gutenberg | See `docs/audit/05-pages.md` |

Elementor JSON not fully exported — **layout UNKNOWN at section level**. Commercial intent and assets CONFIRMED.

---

## Custom code affecting appearance

| Location | Impact |
|---|---|
| wp_options `custom_css` | Color overrides, minor tweaks |
| Woodmart theme options | Grid, hover, AJAX shop, breadcrumbs |
| Elementor postmeta | Homepage sections |
| No child theme `functions.php` | No separate custom PHP layer |

Malware plugin `HelloDollyV2_ixwz` — **never migrate**.

---

## Reconstruction phases

| Phase | Goal | Status |
|---|---|---|
| A | Forensics + DNA docs | ✅ This pass |
| B | Token mapping Legacy → DS | ✅ `DESIGN-SYSTEM.md` + `LEGACY-TO-NEW-MAP.md` |
| C | Baseline parity (grid, content, images) | 🔄 Storefront live; polish ongoing |
| D | Modernize (mobile, performance, premium) | 🔄 In progress |

---

## Related docs

- [`THEME-SETTINGS.md`](THEME-SETTINGS.md) — option keys
- [`THEME-RENDERING-PIPELINE.md`](THEME-RENDERING-PIPELINE.md) — request → HTML chain
- [`VISUAL-BASELINE.md`](VISUAL-BASELINE.md) — page-by-page baseline
- [`LEGACY-TO-NEW-MAP.md`](LEGACY-TO-NEW-MAP.md) — component mapping
- [`VISUAL-DESIGN-AUDIT.md`](VISUAL-DESIGN-AUDIT.md) — extracted tokens
- [`COMPONENT-INVENTORY.md`](COMPONENT-INVENTORY.md) — component list
