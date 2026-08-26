# Zhaket & RTL Theme — Pattern Reference (Non-copyright)

**Purpose:** UX/design pattern library from Persian market themes.  
**Rule:** Patterns only — never copy assets, CSS, or HTML verbatim.

---

## Theme → pattern strength

| Theme | Best patterns for Movasseghi |
|---|---|
| **ShopKadeh** | Mobile nav, mega menu structure, filters drawer, account layout |
| **Woostify** | Complete Woo page coverage (cart/checkout/account archive) |
| **Molla** | Product page density, compare/wishlist UX, FAQ/about/contact |
| **DigiRado** | Mobile commerce, cart UX, account dashboard |
| **Ecommax** | Blog, testimonials, locations, contact blocks |
| **Sabad** | Order tracking, special offers, magazine layout, mega menu |
| **Liora** | Ajax search UI, element-rich homepage sections |
| **Web Store** | 50+ page completeness checklist, B2B-like flows |
| **Emall / Ecomall / Anvogue / StarMarket** | Category merchandising, deal badges |

---

## Patterns adopted (mapped in REFERENCE-MATRIX)

| Pattern | Source inspiration | Movasseghi implementation |
|---|---|---|
| Cart drawer | DigiRado, ShopKadeh | `CartSheet` |
| Mobile drawer nav | ShopKadeh, Sabad | `MobileNav` |
| Bordered product grid | **Legacy Woodmart** (primary) | `ProductCard grid` |
| Header search | Liora, ShopKadeh | `HeaderSearch` |
| Category chips | Woostify, Legacy | Shop + category pages |
| Trust + phone CTA | Web Store, Ecommax | Footer + checkout |
| Pricing table page | Legacy + Woostify | `/pricing` |
| B2B editorial | Cruip + Web Store | `/b2b` |

---

## Patterns deferred

| Pattern | Reason |
|---|---|
| Mega menu 3-level | Catalog depth manageable with chips; add if categories grow |
| Compare / wishlist | P3 — business confirmation |
| Multi-step checkout wizard | Phone-first simpler for current market |
| Heavy homepage sliders | Performance + LCP — use static hero + grid |

---

## Open Source architecture (reference only)

| Repo | Use | License note |
|---|---|---|
| [Vercel Commerce](https://github.com/vercel/commerce) | Storefront architecture, SEO, performance | MIT — patterns |
| [Payload](https://github.com/payloadcms/payload) | CMS/admin — **in use** | MIT |
| [shadcn/ui](https://github.com/shadcn/ui) | UI primitives | MIT — copy-paste |
| [PersianLabs/ui](https://github.com/persianlabs/ui) | RTL checkout fields | — copy-paste |
| [Saleor storefront](https://github.com/saleor/storefront) | Checkout/cart patterns | FSL — **reference only, do not embed** |
| next-shadcn-dashboard | Admin dashboard patterns | Reference for custom ops UI |

**Medusa starter:** archived — do not use per upstream.

---

See [`REFERENCE-MATRIX.md`](../design/REFERENCE-MATRIX.md) for per-component decisions.
