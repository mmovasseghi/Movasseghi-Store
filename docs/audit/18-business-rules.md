# 18 — Business Rules

Evidence classification: **CONFIRMED** | **INFERRED** | **UNKNOWN** | **NOT FOUND**

## Market & customers

| Rule | Status | Evidence |
|---|---|---|
| Primary geography: Tehran | CONFIRMED | `woocommerce_default_country = IR:THR` |
| Nationwide shipping acceptable | INFERRED | Competitors ship nationwide; not in legacy config |
| B2C retail sales | CONFIRMED | WooCommerce retail catalog |
| B2B / wholesale buyers | INFERRED | "فله" category branch, competitor patterns; no WC wholesale plugin |
| Restaurants / catering / companies | INFERRED | Product copy mentions رستوران‌ها، کترینگ‌ها |

## Catalog & pricing

| Rule | Status | Evidence |
|---|---|---|
| Currency: Toman (IRT) | CONFIRMED | WooCommerce settings |
| Zero decimal prices | CONFIRMED | `woocommerce_price_num_decimals = 0` |
| Simple product pricing | CONFIRMED | Product meta |
| Sale pricing supported | CONFIRMED | WooCommerce `_sale_price` |
| Wholesale price tiers | NOT FOUND | |
| MOQ enforcement | NOT FOUND | |
| Pack vs bulk assortment | CONFIRMED | Category split: شیرینک vs فله |

## Product domain

| Rule | Status | Evidence |
|---|---|---|
| Plant-based Amorphous (آملون) disposables | CONFIRMED | Categories, descriptions |
| Corn starch base (نشاسته ذرت) | CONFIRMED | Category/product copy |
| PP/PS line mentioned | CONFIRMED | Empty category "ظروف یکبارمصرف pp-ps" |
| Environmental/decomposition claims | CONFIRMED in copy | SOURCE_REQUIRED for compliance |

## Ordering & fulfillment

| Rule | Status | Evidence |
|---|---|---|
| Online cart/checkout | CONFIRMED | WC pages |
| Phone orders | NOT FOUND in legacy | Spec: 09125199105 |
| Customer-arranged transport | NOT FOUND | Spec requirement |
| Seller-arranged transport | NOT FOUND | Spec requirement |
| Organizational invoice | NOT FOUND | Spec requirement |

## Payments

| Rule | Status | Evidence |
|---|---|---|
| Online payment gateway | NOT FOUND configured | PayPal key only |
| Card-to-card | NOT FOUND | Spec requirement |
| Invoice / pay-later for orgs | NOT FOUND | Spec requirement |

## Account & access

| Rule | Status | Evidence |
|---|---|---|
| Customer accounts | CONFIRMED | my-account page |
| Public registration | CONFIRMED enabled | **Should disable on new site** |
| Admin users | CONFIRMED | Movasseghi + others |

## Brand & contact (rebrand)

| Rule | Status | Evidence |
|---|---|---|
| Legacy brand: ایریک پلاستیک ایرانیان | CONFIRMED | |
| Target brand: فروشگاه موثقی | Spec | |
| Phone 09125199105 | NOT FOUND in legacy DB | Spec |
| Instagram movasseghiStore | NOT FOUND in legacy DB | Spec |

## Content rules

| Rule | Status |
|---|---|
| Migrate content before improving | Policy |
| Do not invent specs/certifications | Policy |
| Mark unverified claims SOURCE_REQUIRED | Policy |

## Consolidated business rules doc

Canonical living document: [`../BUSINESS-RULES.md`](../BUSINESS-RULES.md)
