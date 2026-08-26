# Schema Strategy (Draft)

## Target schema types

| Type | Pages | Source |
|---|---|---|
| Organization | Site-wide | Brand settings |
| WebSite + SearchAction | Site-wide | If internal search implemented |
| BreadcrumbList | Category, product | Nav hierarchy |
| Product | Product pages | Product data model |
| Offer | Product pages | Price + availability (visible only) |
| ItemList | Category/shop | Product listings |

## Product schema rules

Include only fields backed by visible data:

- `name`, `description`, `image`, `sku`
- `offers.price`, `offers.priceCurrency` (IRT)
- `offers.availability` from stock
- `brand` — use verified manufacturer/brand name (SOURCE_REQUIRED: آملون vs موثقی vs Ayrik)

Do **not** include:

- aggregateRating without real reviews
- warranty claims not on page
- environmental certifications not documented

## Organization schema (rebrand)

| Field | Legacy | New |
|---|---|---|
| name | ایریک پلاستیک ایرانیان | فروشگاه موثقی |
| url | ayrik-cornstarch.com | new domain |
| logo | legacy uploads | new asset |
| contactPoint | empty in WC | phone if displayed on site |

## Implementation (new platform)

Single JSON-LD builder fed from product CMS — same source as sitemap/Merchant Center.

## Validation

- CI: schema parse + required field check per page type
- Staging: Google Rich Results Test before release
- **Release blocker:** broken Product schema on product pages

## Legacy note

Yoast + WooCommerce likely emitted duplicate/overlapping schema. New site should emit **one** authoritative graph per page.
