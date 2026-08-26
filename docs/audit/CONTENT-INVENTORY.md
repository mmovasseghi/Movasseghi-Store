# CONTENT-INVENTORY — Legacy Source Material

**Status:** COMPLETE — CONFIRMED from SQL + migration bundle
**Rule:** Legacy text is production-grade — migrate verbatim before improving

## Summary

| Content type | Count | Notes |
|---|---:|---|
| Published products | 95 | All have long HTML descriptions |
| Products with SEO meta | 92 | Yoast indexables |
| Product categories | 22 | Persian descriptions on root categories |
| Published pages | 11 | Homepage, shop, contact, pricing landing, etc. |
| Published blog posts | 210 | `/mag/` — includes spam; sanitize before migrate |

## Product content (CONFIRMED)

- **95/95** products have substantial long descriptions (avg ~3604 chars HTML)
- Content includes: H1/H2/H3, specifications, heat resistance, eco claims, B2B copy
- Brand references: آیریک پلاستیک ایرانیان (legacy) — rebrand to موثقی at content-improvement phase
- Migration target: `products.legacyDescriptionHtml` in Payload

## Category content

| Category | Has description |
|---|---|
| پرفروش-ها | — |
| ظروف یکبار مصرف آملون | ✅ |
| ظروف یکبار مصرف فله آملون | ✅ |
| ظروف یکبار مصرف شیرینک آملون | ✅ |
| لیوان و فنجان ( آملون ) | — |
| کاســه ها ( آملون ) | — |
| سـطل ها ( آملون ) | — |
| دیس و بشقاب ( آملون ) | — |
| ظروف چند خانه ( آملون ) | — |
| ظروف درب دار ( آملون ) | — |
| ظروف بسته بندی ( آملون ) | — |
| قاشق،چنگال،کارد ( آملون ) | — |
| … +10 more | see migration-data.json |

## Pages (published)

| ID | Title | Content chars |
|---:|---|---:|
| 2 | برگه نمونه | 1252 |
| 7 | فروشگاه | 0 |
| 8 | سبد خرید | 61 |
| 9 | پرداخت | 65 |
| 10 | حساب کاربری | 67 |
| 13 | صفحه اصلی | 36739 |
| 14 | بلاگ | 0 |
| 238 | تماس باما | 1892 |
| 243 | درباره ما | 3774 |
| 397 | پاک شده ها | 21126 |
| 689 | قیمت آنلاین محصولات آملون | 69443 |

## Blog posts

**210** published posts. Many are casino/gambling spam (CONFIRMED contamination).
Migrate only legitimate Persian articles about آملون / food-service / environmental topics.

## CTA / trust copy (INFERRED from product pages)

- Phone order CTAs in product copy
- B2B targeting: رستوران، کترینگ، سازمان
- Eco / biodegradable claims — preserve verbatim; verify claims separately

## Machine-readable export

- `docs/audit/generated/product-content-map.json`
- `scripts/import/migration-data.json`
