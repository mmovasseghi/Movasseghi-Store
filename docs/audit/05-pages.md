# 05 — Pages

**Published pages:** 11 (Yoast indexables)  
**Confidence:** CONFIRMED

Full table: [generated/pages-inventory.md](./generated/pages-inventory.md)

## System pages (WooCommerce / WordPress)

| Title | Slug / Path | ID | Purpose |
|---|---|---:|---|
| صفحه اصلی | `/` | 13 | Homepage (Elementor/Woodmart) |
| فروشگاه | `/shop/` | 7 | Product archive |
| سبد خرید | `/cart/` | 8 | Cart |
| پرداخت | `/checkout/` | 9 | Checkout |
| حساب کاربری | `/my-account/` | 10 | Customer account |
| بلاگ | `/mag/` | 14 | Blog/magazine archive |

## Content pages (additional)

| Title | Notes |
|---|---|
| برگه نمونه | Default WordPress sample page — likely **discard** on migration |
| تماس با ما | Contact page — verify content (Persian) |
| درباره ما | About page |
| قیمت آنلاین محصولات آملون | Commercial/pricing landing — **HIGH SEO value** |
| ظروف پلاستیکی که در زندگی روزمره ما | Informational content |
| پاک شده‌ها | Trash/admin utility — **do not migrate** |

Exact URLs for content pages use Persian slugs — see generated inventory.

## Homepage SEO (Yoast)

| Field | Value |
|---|---|
| URL | `https://www.ayrik-cornstarch.com/` |
| Breadcrumb title | صفحه اصلی |
| OG image | `/wp-content/uploads/2023/12/picMain-1.png` |
| Last modified | 2025-08-17 (indexable timestamp) |

## Migration actions

| Page type | Action |
|---|---|
| Homepage | MAP_TO_NEW_URL — redesign; preserve commercial intent |
| Shop / Cart / Checkout / Account | KEEP (functional equivalents) |
| Blog `/mag/` | KEEP structure; **sanitize posts** |
| Sample page | 410 / omit |
| Pricing landing | KEEP content — strong commercial asset |
| Contact / About | MIGRATE → parity check |

## Unknowns

- Exact Elementor layout structure per page — requires media + JSON export pass
- Mobile-specific page variants — live verification required
