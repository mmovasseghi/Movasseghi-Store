# VISUAL-DESIGN-AUDIT — Legacy Design DNA

**Status:** COMPLETE (source CSS + DB theme options) / PARTIAL (live render — site HTTP 500)
**Theme:** Woodmart 7.0.4 (XTemos) — CONFIRMED
**Locale:** fa_IR, RTL — CONFIRMED

---

## Executive summary

The legacy storefront visual identity is **Woodmart ecommerce** customized for **ایریک پلاستیک ایرانیان** with:

- Primary green **`#428D42`** (`rgb(66,141,66)`)
- Body text **`#777777`**, titles **`#242424`**
- Typography: **IranYekan** 14px body / 16px semibold headings
- White background, light gray borders, bordered product grid
- Persian RTL layout throughout

The new Movasseghi platform must feel like a **premium evolution** of this DNA — not a generic template. See `docs/DESIGN-SYSTEM.md` for target tokens (Vazirmatn, refined greens).

---

## Color system (CONFIRMED — wp_options custom CSS)

| Token | Value | Usage |
|---|---|---|
| `--wd-primary-color` | `#428D42` | Buttons, links, accents, sale badges |
| `--wd-text-color` | `#777777` | Body copy |
| `--wd-title-color` | `#242424` | Headings, product titles |
| `--wd-link-color` | `#333333` | Default links |
| `--wd-link-color-hover` | `#242424` | Link hover |
| `--wd-main-bgcolor` | `#FFFFFF` | Page background |
| `--color-gray-100` | `#f7f7f7` | Subtle backgrounds |
| `--brdcolor-gray-300` | `rgba(0,0,0,0.105)` | Card/grid borders |

Additional greens in Elementor meta: `#459647`, `#385224` (success/accents).

---

## Typography (CONFIRMED)

| Role | Font | Size | Weight |
|---|---|---:|---:|
| Body | IranYekan, sans-serif | 14px | 400 |
| Headings | IranYekan | 16px | 600 |
| Product titles | IranYekan | — | 500 |
| Icons | woodmart-font (icon font) | — | — |

**New platform:** Vazirmatn (DESIGN-SYSTEM.md) — maintain similar hierarchy, improve scale for mobile.

---

## Layout & spacing

| Pattern | Legacy behavior | Keep / Elevate |
|---|---|---|
| Max content width | ~1220px (Woodmart container) | Keep readable width; improve mobile padding |
| Product grid | Bordered cells (`products_bordered_grid`) | **Keep pattern** — distinctive vs flat cards |
| Category description | Before product grid (`cat_desc_position: before`) | **Keep** — SEO + context |
| Header | Woodmart Header Builder (`whb-*`) | Redesign — sticky, mobile sheet |
| Footer | Theme widgets | Redesign — trust, contact, categories |

---

## Header (INFERRED from CSS classes + options)

Classes: `.whb-header`, `.whb-main-header`, `.whb-flex-row`, `.wd-header-nav`, `.wd-header-search`, `.wd-dropdown-cart`

| Element | Legacy | New target |
|---|---|---|
| Logo | DB: logo_top, logo_main, logo-mobile assets | Migrate real logos from MEDIA-INVENTORY |
| Navigation | Horizontal menu, RTL | Mobile-first drawer + desktop nav |
| Search | Header search icon/field | Persian-aware autocomplete |
| Cart | Mini-cart dropdown | Slide-over cart sheet |
| Sticky | `.whb-clone` sticky header | Keep with reduced shadow |

**UNKNOWN:** Exact header builder JSON layout — requires WP admin export or live inspect.

---

## Product card (CONFIRMED — `products_hover: base`)

Active template: `content-product-base.php`

| Visual element | Behavior |
|---|---|
| Image | Primary + hover image swap |
| Title | 2-line clamp |
| Price | IRT, 0 decimals |
| Grid extras | Stock progress bar, countdown, qty on grid (options enabled) |
| Hover | Excerpt reveal on hover |
| Border | 1px gray between grid cells |

**Redesign direction:** Preserve bordered grid + hover information density; improve typography, touch targets, and image aspect consistency.

---

## Product page (single)

| Section | Legacy | Migration |
|---|---|---|
| Gallery | Woodmart gallery + zoom | ProductGallery component — swipe, thumbs, zoom |
| Title / price | Standard WooCommerce | Preserve hierarchy |
| Add to cart | WooCommerce form | Custom cart — mobile sticky CTA |
| Long description | Full HTML in post_content | `legacyDescriptionHtml` — verbatim |
| Tabs | Description / additional (if used) | Accordion on mobile |
| Related products | Theme upsells | Related products block |

---

## Homepage (PARTIAL — Elementor)

- Page ID 13, built with Elementor + Woodmart widgets
- OG image: `2023/12/picMain-1.png` — CONFIRMED
- Special offer carousel: `special-offer-woodmart` plugin
- RevSlider reference `[rev_slider alias="slider-1"]` — active status UNKNOWN

**UNKNOWN:** Section order, hero composition, spacing — Elementor JSON not exported.

---

## Buttons & CTAs

| Type | Style (INFERRED) |
|---|---|
| Primary | Green fill `#428D42`, white text |
| Secondary | Outline / gray border |
| Phone CTA | Business-critical — prominent on product pages |

---

## Motion & interaction

| Interaction | Legacy | New |
|---|---|---|
| AJAX shop | Enabled — filter/sort without reload | TanStack Query or server actions |
| Hover transitions | Product image swap, excerpt fade | Respect `prefers-reduced-motion` |
| Mini-cart | Dropdown animation | Sheet transition |
| Scroll | AJAX infinite scroll option | Intersection observer + skeleton |

---

## Brand assets (see MEDIA-INVENTORY.md)

| Asset | File |
|---|---|
| Main logo | `2023/12/logo_main.png` |
| Header logo | `2023/12/logo_topMain.png` |
| Mobile logo | `2023/12/logo-mobile.png` |
| Homepage OG | `2023/12/picMain-1.png` |

---

## Strengths to preserve

1. Green + white clean commercial feel
2. Bordered product grid — catalog readability
3. Category SEO text before products
4. Dense product information (not minimal cards)
5. RTL-native Persian typography

## Weaknesses to fix

1. Desktop-first Woodmart patterns
2. Generic theme "marketplace" feel in places
3. Cluttered hover states on mobile (no hover)
4. Elementor performance weight
5. Inconsistent product image aspect ratios

---

## Live verification UNKNOWN

Legacy site returns **HTTP 500** at audit time. Items requiring staging/live:

- Mobile navigation UX
- Filter drawer behavior
- Checkout form layout
- Actual homepage hero composition

---

## Related docs

- `docs/DESIGN-SYSTEM.md` — new platform tokens
- `docs/audit/COMPONENT-INVENTORY.md`
- `docs/audit/MEDIA-INVENTORY.md`
- `docs/competition/ux-gaps.md`
