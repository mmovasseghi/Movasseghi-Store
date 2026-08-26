# 06 — Products

**Published products:** 95  
**Total product post rows:** 135 (includes drafts/trash)  
**Confidence:** CONFIRMED

Full URL list: [generated/product-urls.md](./generated/product-urls.md)

## Catalog summary

| Attribute | Finding |
|---|---|
| Product type | Simple products (`product_type` term count: 95 simple) |
| Material focus | Amorphous / آملون — corn starch based plant disposables |
| Brand in copy | آیریک پلاستیک / آیریک پلاستیک ایرانیان |
| Variations | Minimal — mostly simple SKUs by size/capacity naming |
| Pricing in DB | `_price` / `_regular_price` postmeta present |
| Currency display | IRT (Toman), 0 decimal places |

## Sample products (Yoast metadata)

| Product | Focus keyword | Commercial notes |
|---|---|---|
| لیوان 500 سی سی آملون | لیوان 500 سی سی آملون | Heat resistance up to 140°C claimed |
| لیوان 350/250/200/170 سی سی آملون | Size-specific keywords | Capacity ladder |
| فنجان آملون | فنجان آملون | Cup product |
| پایه فنجان آملون | پایه فنجان آملون | Accessory |
| کاسه صدفی (multiple sizes) | Size-specific | Shell bowl line |

## Product content patterns

Observed in descriptions (migrate verbatim first — do not invent):

- Plant-based from corn starch (نشاسته ذرت)
- Biodegradable / eco-compatible claims
- Heat resistance temperatures (vary by product — **SOURCE_REQUIRED** per SKU)
- Warranty/guarantee mentions (e.g., "18 month guarantee" on some Yoast descriptions)
- B2B-oriented copy ("رستوران‌ها، کترینگ‌ها")

## Product URL structure

```
https://www.ayrik-cornstarch.com/product/{persian-slug}/
```

Examples:

- `/product/لیوان-500-سی-سی-آملون/` (URL-encoded in HTTP)
- `/product/فنجان-آملون/`

**Migration rule:** Preserve slugs where possible for SEO continuity (may remap domain only).

## Product fields available (legacy)

| Field | Storage | Notes |
|---|---|---|
| Title | `post_title` | Persian |
| Slug | `post_name` | Persian transliteration |
| Long description | `post_content` | HTML + Elementor |
| Short description | `post_excerpt` | Used in grids |
| Regular price | `_regular_price` | postmeta |
| Sale price | `_sale_price` | if set |
| SKU | `_sku` | verify per product in meta pass |
| Stock | `_stock`, `_manage_stock` | verify |
| Gallery | `_product_image_gallery` | comma-separated attachment IDs |
| Featured image | `_thumbnail_id` | postmeta |
| Categories | term relationships | see 07-categories |
| Yoast meta description | Yoast indexable | Commercial SEO text |
| Focus keyword | Yoast indexable | Per-product |

## Fields NOT evidenced

| Field | Status |
|---|---|
| Wholesale tier price | NOT FOUND |
| MOQ | NOT FOUND in automated pass |
| Pack/carton size structured fields | UNKNOWN — may be in description prose |
| Custom B2B quote flag | NOT FOUND |

## Spam contamination

Product post type rows are **not** spam-contaminated. Spam is predominantly `post` type (blog). Products appear legitimate Persian commerce content.

## Migration priority

1. All 95 published products with images
2. Yoast meta descriptions + focus keywords
3. Category assignments
4. Prices (parity validation against business)
5. SKUs and stock (if actively managed)
