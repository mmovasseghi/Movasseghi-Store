# COMPONENT-INVENTORY — Legacy → New Platform

**Status:** COMPLETE (filesystem + theme audit)
**Theme:** Woodmart 7.0.4 + WooCommerce + Elementor

---

## Commerce components (P0 — must reimplement)

| Legacy component | Source | Purpose | New platform | Action |
|---|---|---|---|---|
| Product grid card | `content-product-base.php` | Shop/category listing | `ProductCard` custom | **REDESIGN** — preserve bordered grid DNA |
| Product gallery | Woodmart single product | Image zoom/swipe | `ProductGallery` | **KEEP+IMPROVE** |
| Product long content | `post_content` HTML | SEO body | `LegacyProductContent` | **KEEP verbatim** |
| Breadcrumbs | `global/breadcrumb.php` | Navigation + SEO | Next.js breadcrumb | **KEEP** |
| Add to cart | WooCommerce loop/cart | Conversion | Cart service + UI | **REDESIGN** |
| Mini-cart | `cart/mini-cart.php` | Quick cart view | Cart drawer | **REDESIGN** |
| Cart page | `cart/cart.php` | Full cart | `/cart` page | **REDESIGN** |
| Checkout | `checkout/form-*.php` | Payment flow | `/checkout` multi-path | **REDESIGN** |
| Price display | WooCommerce templates | IRT formatting | `formatIrt()` | **KEEP** |
| Stock badge | Theme option | Urgency | Stock component | **OPTIONAL** |
| Sale flash | `loop/sale-flash.php` | Promotions | Badge component | **KEEP if used** |
| Orderby / filters | AJAX shop | Catalog refinement | Search + filters | **REDESIGN** |
| Pagination | `loop/pagination.php` | Archive navigation | Server pagination | **KEEP** |
| Category description | `taxonomy-product_cat` | SEO intro | Category page header | **KEEP** |

---

## Product card variants (Woodmart — 15 templates)

Only **`base`** hover active (CONFIRMED). Others available but unused:

`tiled`, `icons`, `quick`, `button`, `standard`, `alt`, `info`, `info-alt`, `buttons-on-hover`, `fw-button`, `buttons`, `small`, `add-to-cart`, `full-width`

**Decision:** Do not port variants — design one premium card informed by `base`.

---

## Header / navigation (P0)

| Component | Classes / files | Action |
|---|---|---|
| Header builder | `whb-*`, DB-driven | **REDESIGN** — mobile sheet |
| Main nav | `wd-header-nav` | **REDESIGN** |
| Mobile nav | `wd-header-mobile-nav` | **REDESIGN** — priority |
| Search | `wd-header-search` | **REDESIGN** — Persian search |
| Cart icon | `wd-dropdown-cart` | **REDESIGN** |
| Top bar | Optional WHB row | **EVALUATE** — phone number |

---

## Marketing / homepage (P1)

| Component | Source | Action |
|---|---|---|
| Elementor sections | Page ID 13 | **REBUILD natively** — do not port Elementor |
| Special offer carousel | `special-offer-woodmart` plugin | **REDESIGN** — product spotlight |
| Hero banner | Elementor / RevSlider | **UNKNOWN layout** — rebuild from brand assets |
| CMS blocks | Woodmart CMS blocks | **ARCHIVE** — extract content only |
| Infobox / banner widgets | Elementor elements | **REDESIGN selectively** |

---

## Content / blog (P2)

| Component | Action |
|---|---|
| Blog archive `/mag/` | **KEEP URL** — sanitize spam posts |
| Single post template | **REDESIGN** |
| Related posts | **OPTIONAL** |

---

## Trust / footer (P1)

| Component | Legacy evidence | Action |
|---|---|---|
| Enamad badge | `2022/01/enamad.png` | **KEEP** if still valid |
| Samandehi | `2022/01/samandehi.png` | **KEEP** if still valid |
| Phone CTA | Project spec 09125199105 | **ADD** — not in legacy DB |
| Instagram | movasseghiStore | **ADD** |

---

## Woodmart features (evaluate)

| Feature | Enabled | Action |
|---|---|---|
| Wishlist | Yes | **P2** — optional |
| Compare | Theme support | **REMOVE** — low B2B value |
| Quick view | Yes | **P2** — mobile gallery preferred |
| AJAX shop | Yes | **KEEP behavior** |
| Countdown on products | Yes | **OPTIONAL** |
| Stock progress bar | Yes | **OPTIONAL** |
| Quantity on grid | Yes | **REMOVE on mobile** |

---

## Elementor widgets (~40)

Do **not** port Elementor runtime. Extract content from:

- Homepage sections
- Landing pages (pricing page)
- Product Elementor blocks embedded in descriptions

---

## Admin (new — not WordPress)

| Area | Legacy | New |
|---|---|---|
| Products | WooCommerce admin | Payload Products collection |
| Orders | WooCommerce (minimal orders in backup) | Custom Orders collection (future) |
| SEO | Yoast | Product SEO fields + docs/seo |
| Media | WP Media Library | Payload Media + media-master |

---

## Component priority matrix

| Priority | Components |
|---|---|
| **P0** | Product card, gallery, content, cart, checkout, nav, search, category page |
| **P1** | Homepage hero, B2B page, contact, pricing landing, footer trust |
| **P2** | Wishlist, quick view, blog, compare |
| **P3** | Countdown, stock bar, RevSlider |

---

## Related docs

- `docs/audit/VISUAL-DESIGN-AUDIT.md`
- `docs/audit/03-theme-and-plugins.md`
- `docs/DESIGN-SYSTEM.md`
