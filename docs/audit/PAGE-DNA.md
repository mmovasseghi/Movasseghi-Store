# PAGE-DNA — Legacy Page Reverse Engineering

**Status:** COMPLETE (structure + content) / PARTIAL (Elementor visual layout)

Each page: purpose → user → SEO → content → components → migration status.

---

## Homepage `/`

| Dimension | Finding |
|---|---|
| **Purpose** | Commercial entry — brand trust + category/product discovery |
| **Target user** | Restaurant owner, catering buyer, retail customer |
| **Business goal** | Drive to shop / phone / key categories |
| **SEO** | Site title pattern via Yoast; OG `picMain-1.png` |
| **Content** | Elementor-built — UNKNOWN section JSON |
| **Components** | Hero, category blocks, special offers, product carousels |
| **Strengths** | Clear green brand, product-forward |
| **Weaknesses** | Elementor weight, unknown mobile layout |
| **Migration** | **REDESIGN** — preserve commercial intent + assets |
| **New URL** | `/` |

---

## Shop `/shop/`

| Dimension | Finding |
|---|---|
| **Purpose** | Full product catalog |
| **SEO** | Archive index; supports commercial queries via categories |
| **Components** | Bordered grid, filters, sort, pagination, AJAX |
| **Migration** | **KEEP** functional equivalent at `/shop` |
| **Status** | ✅ Live on new platform |

---

## Product `/product/{slug}/`

| Dimension | Finding |
|---|---|
| **Purpose** | Conversion — inform + add to cart / call |
| **Content** | Rich HTML descriptions (~3600 chars avg), Yoast meta |
| **Components** | Gallery, price, add to cart, long description, specs in prose |
| **Migration** | **MIGRATE content verbatim** + redesign layout |
| **Status** | ✅ Content field + gallery; cart pending |

---

## Category `/product-category/{slug}/`

| Dimension | Finding |
|---|---|
| **Purpose** | Commercial landing for product families |
| **SEO** | High value — category descriptions before grid |
| **Content** | 22 categories, hierarchical (فله / شیرینک) |
| **Migration** | **KEEP** slugs + descriptions |
| **Status** | ✅ Categories seeded; category pages partial |

---

## Cart `/cart/`

| **Purpose** | Order review |
| **Migration** | **REDESIGN** — `/cart` stub live |
| **Status** | 🔄 In progress |

---

## Checkout `/checkout/`

| **Purpose** | Payment / order completion |
| **Legacy payment** | NOT FOUND — no Iranian gateway in backup |
| **Migration** | Multi-path: online (NextPay/ZarinPal TBD), card-to-card, phone |
| **Status** | 🔄 Architecture only |

---

## Account `/my-account/`

| **Purpose** | Registered customer orders/profile |
| **Legacy** | Open registration — **7,779 spam users** |
| **Migration** | **NO public registration** unless approved (AGENTS.md) |
| **Status** | ⏸ Blocked by policy |

---

## Blog `/mag/`

| **Purpose** | Content marketing + supporting SEO |
| **Content** | 209 posts — mixed legitimate + spam |
| **Migration** | **SANITIZE** — migrate legitimate only |
| **Status** | ⏸ Not started |

---

## Pricing landing `قیمت آنلاین محصولات آملون`

| **Purpose** | Commercial SEO landing |
| **SEO value** | **HIGH** — preserve content |
| **Migration** | **KEEP** as page or redirect to shop/category |
| **Status** | ⏸ Content not migrated |

---

## Contact / About

| Page | Status |
|---|---|
| تماس با ما | Migrate content — verify form fields |
| درباره ما | Migrate brand story |

---

## B2B (new)

| **Purpose** | Wholesale / organizational buyers |
| **Legacy evidence** | B2B copy in products; no wholesale plugin |
| **Migration** | **NEW PAGE** `/b2b` — stub exists |
| **Business rules** | Phone 09125199105, invoice flow — project spec |

---

## Page migration summary

| Status | Count |
|---|---:|
| Live (functional) | 4 (home, shop, product, b2b stub) |
| Content migrated | 95 products (HTML), 22 categories |
| Pending | Homepage, blog, contact, pricing landing, checkout |

---

## Related

- `docs/audit/05-pages.md`
- `docs/audit/generated/pages-inventory.md`
- `docs/seo/URL-MAP.md`
