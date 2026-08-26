# 14 — Schema

**Confidence:** INFERRED from Yoast + WooCommerce defaults — raw schema output not scraped (live site 500)

## Expected structured data (legacy stack)

| Schema type | Source | Likely present |
|---|---|---|
| Organization / WebSite | Yoast | Yes |
| BreadcrumbList | Yoast + WooCommerce | Yes on product/category |
| Product | WooCommerce + Yoast | Yes on product pages |
| Offer (price) | WooCommerce | Yes if prices set |
| Article | Yoast | On blog posts |
| FAQPage | UNKNOWN | Not verified |

## Yoast indexable schema fields

Indexables include:

- `schema_page_type`
- `schema_article_type`

Per-record values not bulk-exported in Phase 1.

## Product schema inputs (migration)

From WooCommerce postmeta (standard):

- `name` ← post_title
- `description` ← post_content / excerpt
- `sku` ← `_sku`
- `price` ← `_price`
- `availability` ← stock status
- `image` ← attachment URLs

**Rule:** Do not markup claims not visible on page.

## Organization schema (rebrand impact)

Legacy organization name: **ایریک پلاستیک ایرانیان**  
Target organization name: **فروشگاه موثقی**

Migration must update Organization/WebSite schema without losing valid properties.

## Known risks

| Risk | Mitigation |
|---|---|
| Spam Article schema on casino posts | Exclude spam posts |
| Price mismatch | Server-side validation |
| Missing `@id` consistency | Single schema builder in new app |
| Duplicate schema (Yoast + WC) | Unified JSON-LD emitter on new platform |

## Validation plan (new platform)

- Automated JSON-LD validation in CI (`scripts/seo/`)
- Rich Results Test on staging before release
- SEO release blocker if Product schema breaks on product pages

## Unknowns

- Exact JSON-LD templates used on homepage
- LocalBusiness markup (address/phone) — store address fields empty in WC settings
- AggregateRating / Review schema — **NOT FOUND** (do not invent)

See `docs/seo/SCHEMA-STRATEGY.md` for target-state strategy.
