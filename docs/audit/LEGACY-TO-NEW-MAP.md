# Legacy → New Platform Map

**Status:** Living document · 2026-08-26

---

## Theme & configuration

| Legacy | New |
|---|---|
| Woodmart 7.0.4 parent theme | Next.js 16 App Router + React |
| `xts-theme_settings` | Code + Payload CMS fields |
| `--wd-primary-color` etc. | `globals.css` `@theme` tokens |
| WHB header builder | `Header`, `HeaderSearch`, `MobileNav` |
| Woodmart footer widgets | `Footer` |
| Elementor homepage | `src/app/(frontend)/page.tsx` |
| IranYekan 14px | Vazirmatn (16px min body) |
| `style-rtl.css` | `dir=rtl` + logical properties |

---

## Templates → routes

| Legacy template / URL | New route | Component |
|---|---|---|
| Front page | `/` | `page.tsx` |
| `archive-product.php` | `/shop` | shop page |
| `taxonomy-product_cat.php` | `/shop/[category]` | category page |
| `single-product.php` | `/product/[slug]` | product page |
| `cart/cart.php` | `/cart` | `CartView` |
| Checkout | `/checkout` | `CheckoutForm` |
| Order received | `/checkout/success` | `CheckoutSuccess` |
| Blog archive | `/mag` | mag page |
| Single post | `/mag/[slug]` | post page |
| CMS pages | `/about`, `/contact`, `/pricing`, `/page/[slug]` | CMS sync |
| B2B (spec) | `/b2b` | B2B page |

---

## WooCommerce → commerce layer

| Legacy | New |
|---|---|
| WC cart session | `CartProvider` (localStorage + merge) |
| WC cart validation | `cart-validation.ts` + API |
| WC orders | Payload `orders` collection |
| WC products | Payload `products` collection |
| WC categories | Payload `categories` collection |
| `_price` meta | `regularPrice`, `salePrice` |
| `post_content` HTML | `legacyDescriptionHtml` |
| Attachments | Payload `media` + `legacyAttachmentId` |
| Yoast SEO | Payload `seo` group + metadata API |

---

## Components

| Legacy file / class | New component |
|---|---|
| `content-product-base.php` | `ProductCard` (grid variant) |
| Product gallery | `ProductGallery` |
| `wd-dropdown-cart` | `CartSheet` |
| `loop/sale-flash.php` | `Badge variant="sale"` |
| Breadcrumbs | `Breadcrumbs` |
| Add to cart loop | `ProductActions` + `ProductStickyBar` |
| Long description | `LegacyProductContent` |
| Category description | Category page header block |
| Enamad/samandehi | Footer `Image` trust |

---

## Plugins → services

| Legacy plugin | New |
|---|---|
| Yoast SEO | Metadata + JSON-LD + redirects |
| Elementor | Native React (no builder port) |
| special-offer-woodmart | ⏳ Featured/offers section (planned) |
| wordfence | Nginx + headers + env secrets |
| flying-press | Next.js SSR + image optimization |
| WooCommerce payments | `PaymentService` + provider adapters |

---

## SEO & feeds

| Legacy | New |
|---|---|
| Yoast sitemap | `app/sitemap.ts` |
| robots.txt | `app/robots.ts` (dynamic) |
| Product schema | `productJsonLd()` |
| Merchant feed | `/feed/products` RSS |
| Persian URLs | `next.config.ts` redirects |
| Spam blog slugs | `middleware.ts` → 410 |

---

## Data migration path

```
Legacy SQL / JSON extracts
  → scripts/import/*
  → Payload PostgreSQL
  → RSC pages
  → Customer-facing HTML
```

See `PRODUCT-CONTENT-MAP.md`, `PRODUCT-MEDIA-MAP.md`.
