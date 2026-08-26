# Theme Rendering Pipeline — Legacy

**Purpose:** Trace how legacy pages become HTML. Use for parity validation.  
**Theme:** Woodmart 7.0.4 + WooCommerce + Elementor

---

## Global pipeline

```
HTTP request
  → WordPress bootstrap (wp-load.php)
  → Rewrite / query vars
  → template_loader
  → Woodmart template hierarchy
  → get_header() [WHB header builder]
  → Main template (page.php / archive-product.php / single-product.php)
  → WooCommerce hooks + Woodmart partials
  → Elementor render (if _elementor_edit_mode)
  → the_content / WC loops
  → get_footer()
  → wp_enqueue_style/script (woodmart, wc, elementor)
  → HTML response
```

---

## Homepage `/`

| Stage | Component |
|---|---|
| Route | Front page → `page_id=13` |
| Template | `page.php` or Elementor canvas |
| Builder | Elementor sections + Woodmart widgets |
| Plugins | special-offer-woodmart carousel |
| Assets | `picMain-1.png` hero, category blocks |
| CSS | Woodmart + Elementor inline + custom CSS |
| JS | Elementor frontend, Woodmart core |

**New:** `src/app/(frontend)/page.tsx` — native React sections (no Elementor port).

---

## Shop archive `/shop/` (legacy: product archive)

| Stage | Component |
|---|---|
| Route | `post_type=product` archive |
| Template | `archive-product.php` |
| Loop | `content-product-base.php` (hover: base) |
| Options | bordered grid, qty, stock bar, countdown |
| AJAX | Woodmart shop AJAX filters/sort |
| CSS | `.products-bordered-grid`, `.wd-product` |

**New:** `/shop` → Payload query → bordered `ProductCard` grid.

---

## Category `/product-category/{slug}/`

| Stage | Component |
|---|---|
| Route | `product_cat` taxonomy |
| Template | `taxonomy-product_cat.php` |
| Header | Term name + description (`cat_desc_position: before`) |
| Loop | Same product card template |
| SEO | Yoast term meta |

**New:** `/shop/[category]` — description block + child chips + grid.

---

## Product single `/product/{slug}/`

| Stage | Component |
|---|---|
| Route | `post_type=product` |
| Template | `single-product.php` → `content-single-product.php` |
| Gallery | Woodmart gallery + zoom |
| Content | `post_content` HTML (3600+ chars avg) |
| Meta | Yoast title/desc, schema via Yoast |
| Related | Theme upsells |

**New:** `/product/[slug]` — Gallery, Actions, Specs, `LegacyProductContent`, JSON-LD.

---

## Cart `/cart/`

| Stage | Component |
|---|---|
| Template | `woocommerce/cart/cart.php` |
| Mini-cart | AJAX fragment `wd-dropdown-cart` |

**New:** `CartSheet` + `/cart` + client cart + server validation.

---

## Checkout `/checkout/`

| Stage | Component |
|---|---|
| Template | Woodmart checkout overrides |
| Payment | Legacy: no live gateway evidenced |

**New:** Multi-path checkout (phone, card-to-card, online stub).

---

## Search

| Stage | Component |
|---|---|
| Route | `?s=` or Woodmart AJAX search |
| Template | search product results |

**New:** `/shop?q=` + `persianSearchMatch()`.

---

## Static pages

| Legacy slug | New route |
|---|---|
| تماس-باما | `/contact` |
| درباره-ما | `/about` |
| قیمت-آنلاین-محصولات-آملون | `/pricing` |
| Blog `/mag/` | `/mag` |

---

## Assets pipeline

```
uploads/YYYY/MM/file.jpg
  → attachment post
  → product _thumbnail_id / _product_image_gallery
  → Woodmart responsive srcset
  → browser
```

**New:** Payload media → `/api/media/file/*` → Next.js `Image` (AVIF/WebP).

---

## New platform pipeline (target)

```
HTTP request
  → Next.js App Router
  → RSC page component
  → getPayloadClient() (PostgreSQL)
  → React components (Design System)
  → JSON-LD / metadata API
  → HTML + minimal client JS
```

Parity check: compare output structure to sections in [`VISUAL-BASELINE.md`](VISUAL-BASELINE.md).
