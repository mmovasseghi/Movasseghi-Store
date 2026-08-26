# Product Data Model — Canonical

**SSOT:** Payload `products` collection + `src/lib/products.ts` helpers  
**Rule:** One source for storefront, admin, search, JSON-LD, sitemap.

---

## Entity: Product

| Field | Type | Storefront | SEO | Commerce |
|---|---|:---:|:---:|:---:|
| `name` | text | ✅ | H1 | — |
| `slug` | text | URL | canonical | — |
| `sku` | text | specs | — | search |
| `status` | enum | filter | index | — |
| `regularPrice` | number | display | Offer schema | **server authority** |
| `salePrice` | number | display | — | **server authority** |
| `stockQuantity` | number | availability | schema | **server authority** |
| `shortDescription` | text | card/meta | meta desc | — |
| `legacyDescriptionHtml` | text | PDP body | long-tail | — |
| `featuredImage` + `gallery` | media | gallery | image SEO | — |
| `categories` | rel[] | breadcrumbs | hub links | — |
| `attributes.*` | group | specs table | — | filters (future) |
| `b2b.wholesalePrice` | number | badge/card | — | B2B |
| `b2b.moq` | number | B2B copy | — | validation |
| `seo.*` | group | metadata | **primary** | — |
| `legacyId` | number | — | redirect map | migration |

---

## Derived (never duplicated)

- `productDisplayPrice()` — sale vs regular
- `productCardProps()` — card DTO
- `productJsonLd()` — schema.org Product
- Cart line items — **validated server-side** via `/api/cart/validate`

---

## Category

| Field | SEO role |
|---|---|
| `name`, `slug` | Hub page H1, URL |
| `description` | Above-grid commercial copy (legacy DNA) |
| `seo.*` | Meta |
| `parent` | Hierarchy / breadcrumbs |

---

## Media

| Field | Rule |
|---|---|
| `alt` | Product name or legacy title — sync script |
| `legacyAttachmentId` | Migration provenance |
| Masters | `media-master/` gitignored; optimize derivatives later |

---

## Orders (Payload `orders`)

Client submits intent → server re-prices from Product → persists authoritative subtotal.

---

## Not in scope (yet)

- Variants (legacy: simple products only — CONFIRMED)
- Customer accounts (guest + phone identity first)
- Dynamic quantity pricing tiers — UNKNOWN business rules
