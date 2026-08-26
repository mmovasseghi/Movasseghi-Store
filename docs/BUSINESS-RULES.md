# Business Rules — Movasseghi Store

**Status:** Evidence-based draft from Phase 1 forensics + project spec  
**Last updated:** 2026-08-26

Legend: **CONFIRMED** (legacy evidence) | **SPEC** (project requirement) | **UNKNOWN**

---

## Identity

| Rule | Source |
|---|---|
| Target brand: **فروشگاه موثقی** | SPEC |
| Legacy brand: **ایریک پلاستیک ایرانیان** | CONFIRMED |
| Product domain: disposable plant-based food-service products | CONFIRMED + SPEC |
| Primary market: Tehran | CONFIRMED (`IR:THR`) + SPEC |
| Secondary market: all Iran | SPEC |

## Customers

| Segment | Supported |
|---|---|
| Retail buyers | CONFIRMED (WooCommerce) |
| Wholesale / bulk | INFERRED (فله categories) + SPEC |
| Restaurants, catering, companies, distributors | SPEC + INFERRED (copy) |
| Phone orders | SPEC — **not evidenced in legacy** |
| Organizational invoices | SPEC — **not evidenced in legacy** |

## Catalog

| Rule | Detail |
|---|---|
| Material focus | Amorphous (آملون) / corn starch plant-based | CONFIRMED |
| Assortment | Cups, bowls, plates, trays, lids, cutlery, packaging | CONFIRMED |
| Bulk vs retail packaging | Separate category branches (فله / شیرینک) | CONFIRMED |
| PP/PS products | Category exists but empty | CONFIRMED |
| ~95 active SKUs | CONFIRMED |

## Pricing

| Rule | Detail |
|---|---|
| Currency | Toman (IRT) | CONFIRMED |
| Integer prices (no decimals) | CONFIRMED |
| Public list prices on site | CONFIRMED (WC) |
| Wholesale price tiers | UNKNOWN — SPEC requires support |
| MOQ | UNKNOWN — SPEC requires support |

## Payments

| Method | Legacy | Target |
|---|---|---|
| Online gateway | NOT FOUND | NextPay primary, ZarinPal secondary (SPEC) |
| Card-to-card | NOT FOUND | SPEC |
| Pay on delivery / invoice | UNKNOWN | SPEC |

## Shipping

| Method | Legacy | Target |
|---|---|---|
| Customer-arranged vehicle | NOT FOUND | SPEC |
| Seller-arranged transport | NOT FOUND | SPEC |
| Rules may vary by order | UNKNOWN | SPEC |

## Contact

| Channel | Value | Source |
|---|---|---|
| Phone | 09125199105 | SPEC — **not in legacy DB** |
| Instagram | movasseghiStore | SPEC — **not in legacy DB** |

## Content & claims

| Rule | Policy |
|---|---|
| Migrate legacy content before improving | Required |
| Do not invent specs, certifications, percentages | Required |
| Environmental claims | Present in legacy — SOURCE_REQUIRED before strengthening |
| Reviews/testimonials | NOT FOUND — do not fabricate |

## Security & accounts

| Rule | Policy |
|---|---|
| Public open registration | Legacy enabled — **disable on new platform** unless B2B portal requires it |
| Spam users | Do not migrate |

## SEO commerce rules

| Query cluster | Primary destination type |
|---|---|
| ظروف یکبار مصرف گیاهی | Commercial category/shop landing |
| ظروف یکبار مصرف | Shop + category hub |
| ظرف یکبار مصرف گیاهی | Products + supporting category (no doorway duplicates) |

---

See audit detail: [audit/18-business-rules.md](./audit/18-business-rules.md)
