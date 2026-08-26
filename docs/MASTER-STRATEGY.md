# Master Strategy — Movasseghi Store

**Version:** 1.0 · 2026-08-26  
**Role:** Unified specification index — details live in linked docs (do not rescan legacy).

---

## Mission

Production-grade Persian RTL ecommerce for **ظروف یکبار مصرف گیاهی** — B2B + B2C, SEO-first, mobile-first, legacy content preserved.

**Operating loop:** UNDERSTAND → DECIDE → BUILD → TEST → FIX → DOCUMENT → CONTINUE

---

## Phase status

| Phase | Scope | Status |
|---|---|---|
| 1 Forensics | `docs/audit/*`, `docs/knowledge/*` | ✅ Complete |
| 1b Infra | Hetzner, GitHub, PM2 | ✅ Live http://91.107.181.79 |
| 2 Architecture | Payload + Next commerce | ✅ [`decisions/ARCHITECTURE-DECISION.md`](decisions/ARCHITECTURE-DECISION.md) |
| 3 Design system | Reference matrix + SSOT + UI elevation | ✅ Phase 3B |
| 4 Catalog migration | 95 products, media, HTML | ✅ Server synced |
| 5 Storefront core | Product, shop, cart, checkout | ✅ Complete |
| 6 Commerce hardening | Server cart validation, search, analytics | ✅ [`decisions/20260826-phase-6-commerce-hardening.md`](decisions/20260826-phase-6-commerce-hardening.md) |
| 7 Payments | NextPay/ZarinPal adapters | ⏸ Needs credentials |
| 8 SEO validation | Canonicals, robots, merchant feed | 🔄 [`decisions/20260826-phase-8-seo-merchant.md`](decisions/20260826-phase-8-seo-merchant.md) |
| 9 QA | Playwright smoke | 🔄 8 tests |
| 10 Launch | Domain, TLS, GSC | ⏸ Blocked on domain |

---

## Non-negotiables (from AGENTS.md)

1. Legacy **read-only** — `Old Verison WebSite/backup`
2. **Migrate → parity → SEO check** before content improvement
3. Real product images + full legacy HTML — no stock/AI product media
4. Client never authoritative for **price, stock, payment success**
5. Token efficiency — read `docs/audit/` not raw WP

---

## Document map (project memory)

| Domain | Path |
|---|---|
| **Unified spec** | `docs/PRODUCT-SPECIFICATION.md` |
| Legacy DNA | `docs/audit/VISUAL-DESIGN-AUDIT.md`, `UX-AUDIT.md`, `COMMERCE-AUDIT.md` |
| Business | `docs/BUSINESS-RULES.md` |
| SEO | `docs/seo/SEO-STRATEGY.md`, `KEYWORD-MAP.md`, `URL-MAP.md` |
| Design | `docs/DESIGN-SYSTEM.md`, `docs/design/*` |
| Architecture | `docs/decisions/ARCHITECTURE-DECISION.md` |
| Commerce model | `docs/commerce/PRODUCT-DATA-MODEL.md` |
| Analytics | `docs/ANALYTICS-PLAN.md` |
| Performance | `docs/PERFORMANCE-BUDGET.md` |
| Decisions log | `docs/decisions/` |

---

## Primary SEO queries → pages

| Query | Target page |
|---|---|
| ظروف یکبار مصرف گیاهی | `/shop/[root-category-slug]` |
| ظروف یکبار مصرف | `/shop` + category hubs |
| ظرف یکبار مصرف گیاهی | Product + category pages |
| قیمت محصولات آملون | `/pricing` |

Commercial intent → **shop/category/product/pricing**, not blog spam.

---

## Approval gates (stop only here)

- Destructive production data migration (Gate 4)
- Live payment merchant credentials
- Irreversible SEO URL changes without redirect map
- Legacy backup modification

Everything else: **autonomous execution**.

---

## Next work queue (auto-prioritized)

1. ✅ Server-side cart price/stock validation
2. ✅ Persian search normalization
3. ✅ Analytics event layer (GTM-ready)
4. Checkout UX + order validation integration
5. Payment adapter when env keys available
6. WebP image derivatives
7. Admin dashboard UX (Payload + custom views)
8. Domain + TLS + SEO regression on cutover
