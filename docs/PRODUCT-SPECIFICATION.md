# Product Specification — Movasseghi Store

**Version:** 2.0 · 2026-08-26  
**Authority:** Consolidated from full conversation + `docs/audit/*` + implementation state  
**Supersedes:** scattered chat instructions; when this doc conflicts with older chat text, **this doc wins**.

---

## 1. Mission

Build a **production-grade** Persian RTL ecommerce platform for **ظروف یکبار مصرف گیاهی (آملون)** that:

- Surpasses legacy WordPress (Woodmart) in design, UX, performance, and maintainability
- Preserves **four years** of legacy business assets: content, SEO, images, commerce logic
- Serves **B2B + B2C**, wholesale + retail, Tehran-primary / nationwide
- Converts commercial Google traffic → product → cart / quote / call → purchase

**Operating loop:** UNDERSTAND → DECIDE → BUILD → TEST → FIX → DOCUMENT → CONTINUE

---

## 2. Non-negotiables

| Rule | Source |
|---|---|
| Legacy backup **read-only** — never modify `Old Verison WebSite/backup` | Conversation + AGENTS.md |
| **Migrate → parity → SEO check → release → then improve** | Conversation |
| No fabricated products, prices, reviews, certifications | Conversation |
| Real legacy product HTML + images — no stock/AI product media | Conversation |
| Client **never authoritative** for price, stock, payment success | Conversation |
| No invented payment API — official provider docs + env credentials only | Conversation |
| Mobile-first · performance budgets · prefers-reduced-motion | Conversation |
| Token efficiency — use `docs/` as memory, not repeated legacy scans | Conversation |
| Autonomous execution except **approval gates** | Conversation |

---

## 3. Approval gates (STOP only here)

| Gate | Trigger |
|---|---|
| Gate 4 | Destructive irreversible production data migration |
| Payment | Live merchant credentials (NextPay / ZarinPal / BalePay) |
| SEO risk | Irreversible URL changes without redirect map |
| Legacy | Any modification/deletion of legacy backup |
| Security | Production secrets requiring human input |

Everything else: agent decides and implements.

---

## 4. Legacy source

| Source | Path | Role |
|---|---|---|
| Raw backup (local) | `Old Verison WebSite/backup` | READ-ONLY forensic source |
| Extracted workspace | `.legacy-extract/` | Gitignored derived JSON |
| Sanitized export | `mmovasseghi/Movasseghi-Store-Legacy` | Remote read-only when local unavailable |
| Audit memory | `docs/audit/*`, `docs/knowledge/*` | **Use this first** — do not rescan WP |

**Legacy identity (CONFIRMED):** ayrik-cornstarch.com / ایریک پلاستیک ایرانیان → rebrand to **فروشگاه موثقی**.

Legacy is **not** a sitemap. Extract full DNA:

- Visual, UX, Component, Animation, Content, Product, Media, SEO, Commerce, Business, Responsive, Interaction, Branding

See: `docs/audit/VISUAL-DESIGN-AUDIT.md`, `UX-AUDIT.md`, `COMMERCE-AUDIT.md`, `PRODUCT-CONTENT-MAP.md`.

---

## 5. Product content (HARD)

Every published product MUST include migrated legacy content, not just title + image + price.

| Field | Payload / UI |
|---|---|
| Full description HTML | `legacyDescriptionHtml` → `LegacyProductContent` |
| Short description | `shortDescription` |
| SEO title / meta | `seo.title`, `seo.description` |
| SKU, attributes, pack size | structured fields |
| Internal links in HTML | preserved verbatim |

**Status:** 95/95 products with HTML parity (CONFIRMED — `PRODUCT-CONTENT-MAP.md`).

**Rule:** Do not shorten or rewrite during migration unless documented exception.

---

## 6. Product images (HARD)

| Step | Action |
|---|---|
| Map | Legacy attachment → Payload media |
| Reuse | Real product photos only |
| Optimize | WebP/AVIF derivatives (pending) |
| Missing | 12 products without legacy primary — **do not invent** |

**Status:** 83/95 featured images on server (INFERRED from prior deploy).

---

## 7. SEO (highest priority)

### Primary commercial queries

| Query | Target |
|---|---|
| ظروف یکبار مصرف گیاهی | Category hub `/shop/[root-amylon-slug]` |
| ظروف یکبار مصرف | `/shop` + category hubs |
| ظرف یکبار مصرف گیاهی | Product + category pages |
| قیمت محصولات آملون | `/pricing` |

**Intent:** Commercial → shop/category/product/pricing — **not** blog spam.

**Funnel:** Google → qualified buyer → product → cart / quote / call → purchase

### SEO system requirements

- Keyword map, URL map, redirects, canonicals, schema, sitemap, robots
- Staging/IP: **noindex** (`src/app/robots.ts`)
- Cart/checkout: noindex
- Blog spam slugs: **410 Gone** (`middleware.ts`)
- Post-cutover: Search Console, Merchant Center, regression checklist

Docs: `docs/seo/*`, `docs/MASTER-STRATEGY.md`.

---

## 8. Business model

| Dimension | Requirement |
|---|---|
| Channels | Online, phone, card-to-card, B2B quote, org invoice |
| Shipping | Customer vehicle OR seller transport |
| Pricing | Retail, wholesale (`b2b.wholesalePrice`), MOQ where evidenced |
| Customers | Restaurants, catering, companies, distributors, bulk buyers |
| Market | Tehran primary, nationwide |

Do **not** invent rules missing from legacy — mark UNKNOWN and document.

Docs: `docs/BUSINESS-RULES.md`, `docs/audit/BUSINESS-RULES.md`.

---

## 9. Design

### Direction

Premium evolution of legacy Woodmart DNA — **unique**, not shadcn/Velora/Kairo clone.

| Legacy strength | Preserve / elevate |
|---|---|
| Green brand `#428D42` lineage | Refined `#2D6A4F` tokens |
| Bordered product grid | Signature catalog pattern |
| Category desc before grid | SEO + context |
| Product hover information | Short desc on hover |
| Real hero imagery | `/brand/hero.png` (legacy picMain-1) |
| Trust badges | enamad, samandehi |

### References (patterns only — no blind install)

shadcn/ui · PersianLabs/ui · Velora · Kairo · Easy UI · Cruip · nobruf/shadcn-landing-page

Docs: `docs/DESIGN-SYSTEM.md`, `docs/design/DESIGN-DIRECTION.md`, `docs/design/REFERENCE-MATRIX.md`.

### Known gap (being fixed)

Current storefront was **functionally correct but visually thinner** than legacy — missing hover density, header search prominence, commercial keyword hubs. Phase 3B addresses this.

---

## 10. UI autonomy

Agent may reuse shadcn, adapt registry components, or build custom. Choose by: UX, a11y, RTL, mobile, performance, SSR, bundle size, license.

**No dependency** unless clear value.

---

## 11. Commerce features

### Cart (first-class)

Guest + persistent cart, merge, qty, remove, server validation, mobile sheet.

Implemented: `CartProvider`, `CartSheet`, `POST /api/cart/validate`.

### Checkout

Phone (default), card-to-card, online (when gateway configured), shipping choice, B2B link.

Server validates cart before order persist.

### Payment (provider-independent)

| Provider | Env gate |
|---|---|
| NextPay | `NEXTPAY_API_KEY` |
| ZarinPal | `ZARINPAL_MERCHANT_ID` |
| BalePay | `BALEPAY_API_KEY` (optional) |

Routes: `POST /api/payments/init`, `GET /api/payments/callback/[provider]` — stubs until credentials.

### Product model (canonical SSOT)

Single source for storefront, admin, search, SEO, JSON-LD, sitemap.

Doc: `docs/commerce/PRODUCT-DATA-MODEL.md`.

### Search

Persian normalization: ی/ي, ک/ك, digits, ZWNJ — `src/lib/persian-search.ts`.

---

## 12. Analytics

GTM-compatible `dataLayer` — `src/lib/analytics.ts`.

Events: view_item, add_to_cart, view_cart, begin_checkout, purchase, phone_click.

Doc: `docs/ANALYTICS-PLAN.md`. Container ID deferred until production domain.

---

## 13. Performance

Targets: LCP ≤2.5s, INP ≤200ms, CLS ≤0.1 — `docs/PERFORMANCE-BUDGET.md`.

Optimized images, minimal JS, SSR, code split, reduced-motion safe animations.

---

## 14. Testing & security

- Playwright E2E (mobile viewport)
- CI: build, lint
- Security headers, env secrets, RBAC, input validation, gitleaks

---

## 15. Infrastructure

| Item | Value |
|---|---|
| GitHub | `mmovasseghi/Movasseghi-Store` |
| Stack | Next.js 16, Payload 3, PostgreSQL, Tailwind, shadcn-style UI |
| Deploy | Hetzner `91.107.181.79`, PM2, Nginx |
| Environments | dev → staging (noindex) → production |
| Deploy pack | `npm run deploy:pack` → tar (excludes `.env`) |

---

## 16. Phase status (current)

| Phase | Status |
|---|---|
| 1 Forensics | ✅ |
| 1b Infra | ✅ Live on IP |
| 2 Architecture | ✅ |
| 3 Design system | ✅ + 3B elevation in progress |
| 4 Catalog migration | ✅ 95 products |
| 5 Storefront core | ✅ |
| 6 Commerce hardening | ✅ |
| 7 Payments | ⏸ credentials |
| 8 SEO validation | 🔄 partial |
| 9 QA | ✅ 5 Playwright tests |
| 10 Launch | ⏸ domain/TLS |

---

## 17. Contradictions resolved

| Topic | Old | Final |
|---|---|---|
| Blog posts | 209 indexed | Only 1 legitimate; spam → 410 |
| Gate 1 | Awaiting approval | Forensics complete; routine work unblocked |
| AGENTS.md size | Verbose constitution | Concise + scoped `.cursor/rules/` |
| Token strategy | Deep legacy rescans | One-time audit → docs memory |
| Design | Generic modern cards | Bordered grid + legacy hover density |
| Payment | Client success | Server verify only |
| robots.txt | Static public file | Dynamic `app/robots.ts` with staging noindex |

---

## 18. Open gaps (auto-queue)

1. Payment provider real adapters (credentials gate)
2. ~~WebP/AVIF image pipeline~~ → Next.js `formats` enabled; CDN derivatives optional
3. 12 products without images (no fabrication)
4. Domain + TLS + SEO regression on cutover
5. GitHub Actions deploy secrets + workflow scope
6. Admin ops UI beyond Payload default
7. ~~Merchant Center feed~~ → `/feed/products` ✅
8. Alt-text sync — script fix applied; re-run on server deploy

---

## 19. Document index

| Doc | Purpose |
|---|---|
| `PRODUCT-SPECIFICATION.md` | **This file** — unified spec |
| `MASTER-STRATEGY.md` | Phase tracker + quick links |
| `DESIGN-SYSTEM.md` | Visual SSOT |
| `docs/decisions/*` | ADRs |
| `docs/audit/*` | Legacy evidence |

---

*Autonomous agents: read this + linked docs before acting. Do not re-ask requirements stated here.*
