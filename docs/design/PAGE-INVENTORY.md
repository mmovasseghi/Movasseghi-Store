# Page Inventory — Movasseghi Store

**Version:** 1.0 · 2026-08-26  
**Purpose:** Complete page system — every route class, purpose, priority, implementation status.  
**SSOT for IA:** Update when routes ship.

**Status legend:** ✅ Live · 🟡 Partial · ⏳ Planned · ⏸ Deferred · — N/A

---

## Store

| Page | Route | Purpose | Priority | Status |
|---|---|---|---|---|
| Home | `/` | Commercial entry, SEO hubs, discovery | P0 | ✅ |
| Shop | `/shop` | Full catalog | P0 | ✅ |
| Category | `/shop/[category]` | Category SEO + products | P0 | ✅ |
| Subcategory | `/shop/[parent]/[child]` | — | P2 | ⏳ use flat `/shop/[slug]` |
| Search | `/shop?q=` | Persian product search | P0 | ✅ |
| Product | `/product/[slug]` | Conversion + full content | P0 | ✅ |
| Compare | `/compare` | Side-by-side SKUs | P3 | ⏸ |
| Wishlist | `/wishlist` | Saved products | P3 | ⏸ |
| Cart | `/cart` | Review cart | P0 | ✅ |
| Checkout | `/checkout` | Order capture | P0 | ✅ |
| Payment pending | `/checkout/pending` | Gateway redirect return | P1 | ⏳ |
| Thank you | `/checkout/success` | Confirmation | P0 | ✅ |
| Track order | `/track-order` | Order lookup | P2 | ⏳ |

---

## Account

| Page | Route | Purpose | Priority | Status |
|---|---|---|---|---|
| Login | `/account/login` | Auth | P1 | ⏳ Payload auth |
| Register | `/account/register` | — | P2 | ⏸ spec: no public reg unless approved |
| Forgot password | `/account/forgot` | Recovery | P2 | ⏳ |
| Dashboard | `/account` | Order history hub | P1 | ⏳ |
| Profile | `/account/profile` | User info | P2 | ⏳ |
| Addresses | `/account/addresses` | Shipping book | P2 | ⏳ |
| Orders | `/account/orders` | List | P1 | ⏳ |
| Order detail | `/account/orders/[id]` | Single order | P1 | ⏳ |
| Quotes | `/account/quotes` | B2B quotes | P2 | ⏳ |

---

## B2B

| Page | Route | Purpose | Priority | Status |
|---|---|---|---|---|
| Wholesale hub | `/b2b` | B2B entry + phone CTA | P0 | ✅ |
| Restaurants | `/b2b/restaurants` | Use-case landing | P2 | ⏳ |
| Catering | `/b2b/catering` | Use-case landing | P2 | ⏳ |
| Companies | `/b2b/companies` | Org buyers | P2 | ⏳ |
| Quote request | `/b2b/quote` | Form → CRM/orders | P1 | ⏳ |
| Org invoice | `/b2b/invoice` | Invoice info | P2 | ⏳ |

---

## Content

| Page | Route | Purpose | Priority | Status |
|---|---|---|---|---|
| Blog archive | `/mag` | Content hub | P1 | ✅ |
| Article | `/mag/[slug]` | Single post | P1 | ✅ |
| FAQ | `/faq` | Support SEO | P2 | ⏳ |
| Buying guides | `/guides/*` | Commercial content | P2 | ⏳ |
| CMS pages | `/page/[slug]` | Legacy pages | P1 | ✅ |
| About | `/about` | Trust | P0 | ✅ |
| Contact | `/contact` | Lead / phone | P0 | ✅ |
| Pricing | `/pricing` | Commercial keyword | P0 | ✅ |

---

## Brand / legal

| Page | Route | Purpose | Priority | Status |
|---|---|---|---|---|
| Shipping | `/shipping` | Delivery policy | P1 | ⏳ |
| Payment methods | `/payment-methods` | Trust | P1 | ⏳ |
| Returns | `/returns` | Policy | P2 | ⏳ |
| Terms | `/terms` | Legal | P2 | ⏳ |
| Privacy | `/privacy` | Legal | P2 | ⏳ |

---

## System

| Page | Route | Purpose | Priority | Status |
|---|---|---|---|---|
| 404 | `not-found` | Recovery | P0 | ⏳ this commit |
| 500 | `error.tsx` | Failure | P1 | ⏳ |
| Empty search | `/shop?q=` empty | UX | P0 | ✅ inline |
| Empty cart | `/cart` | UX | P0 | ✅ |
| Maintenance | `/maintenance` | Deploy gate | P3 | ⏸ |

---

## Landing engine (commercial SEO)

| Landing | Target keyword / intent | Priority | Status |
|---|---|---|---|
| Shop hub | ظروف یکبار مصرف | P0 | ✅ |
| Root amylon category | ظروف یکبار مصرف گیاهی | P0 | ✅ category slug |
| Product pages | ظرف یکبار مصرف گیاهی | P0 | ✅ |
| Pricing | قیمت محصولات آملون | P0 | ✅ |
| Homepage chips | All three primary KWs | P0 | ✅ |

**Rule:** No thin doorway pages — each landing must have real catalog/content value.

---

## Admin (Payload + future custom)

| Module | Path | Status |
|---|---|---|
| Dashboard | `/admin` | ✅ Payload default |
| Products | `/admin/collections/products` | ✅ |
| Orders | `/admin/collections/orders` | ✅ |
| Categories | `/admin/collections/categories` | ✅ |
| Media | `/admin/collections/media` | ✅ |
| Custom ops UI | TBD | ⏳ Phase post-launch |

---

## Implementation waves

| Wave | Scope |
|---|---|
| **Wave 1 (done)** | Store core, B2B hub, content pages, cart/checkout |
| **Wave 2 (current)** | Theme DNA docs, design system, SEO feeds, 404, filters |
| **Wave 3** | Account, order tracking, B2B quote form |
| **Wave 4** | Legal pages, guides, landing expansion |
| **Wave 5** | Compare/wishlist if business confirms |

---

## Cross-references

- Legacy page DNA: `docs/audit/PAGE-DNA.md`
- Visual baseline: `docs/audit/VISUAL-BASELINE.md`
- Design direction: `docs/design/DESIGN-DIRECTION.md`
