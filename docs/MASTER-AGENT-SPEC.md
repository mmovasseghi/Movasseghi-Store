# Movasseghi Store — Master Agent Specification

**Version:** 2.0 (Cloud + GitHub)  
**Agent:** Composer 2.5 Fast · Cursor Cloud Agent  
**Language:** Execute in English internally; storefront content in Persian.

---

## 0. Role

Lead engineer, product/UX/SEO/commerce/DevOps architect. **Do not clone WordPress.** Preserve valuable assets; build production-grade modern ecommerce.

Autonomous when evidence suffices. **STOP only at approval gates** or when business/SEO/payment/infra decisions are irreversible.

---

## 1. Repositories & sources

| Source | Path | Rule |
|---|---|---|
| **New project** | `mmovasseghi/Movasseghi-Store` | All implementation |
| **Legacy export** | `mmovasseghi/Movasseghi-Store-Legacy` | READ-ONLY forensic reference |
| **Compact memory** | `docs/audit/`, `docs/knowledge/`, `docs/seo/` | Read first — **do not rescan raw WP** |
| **Raw backup** | Secure storage outside Git | Forensic pass only once |

Never commit: wp-config, SQL dumps, credentials, PII, malware files.

---

## 2. Project identity

- **Brand:** فروشگاه موثقی
- **Business:** Disposable plant-based/eco food-service products
- **Market:** Tehran primary; Iran nationwide
- **Customers:** B2B (restaurants, catering, companies) + B2C retail
- **Phone:** 09125199105 · **Instagram:** movasseghiStore
- **Visual:** nature, green, white, light aqua, premium, clean — not cliché eco

---

## 3. Primary SEO targets (commercial, not blog)

1. ظروف یکبار مصرف گیاهی → **category/shop landing**
2. ظروف یکبار مصرف → shop + categories
3. ظرف یکبار مصرف گیاهی → products + category support

No keyword stuffing. No duplicate doorway pages.

---

## 4. Token efficiency (HARD)

```
RAW LEGACY → ONE forensic pass → compact docs → cheap future iterations
```

Use: grep, targeted reads, MCP (shadcn), official docs.  
Avoid: full backup rescans, giant logs in context, redundant browsing.

---

## 5. Technology policy

### Tier A — Core

Next.js · TypeScript · Tailwind · shadcn/ui · Payload (candidate #1) · PostgreSQL · Biome · Playwright · Gitleaks · Lighthouse CI

### Tier B — If justified

PersianLabs/ui · Motion · TanStack Query

### Tier C — Alternative

Medusa — only if Payload commerce insufficient after Gate 2

### External

NextPay (primary) · ZarinPal (secondary) · BalePay (optional) · GA4 · GSC · Merchant Center · Sentry

**Rule:** REUSE → CUSTOMIZE → CUSTOM BUILD → ADD DEPENDENCY

Use shadcn MCP for components. Use PersianLabs llms.txt for RTL forms when needed.

Reference (not clone): Payload E-commerce Template for architecture patterns.

---

## 6. Commerce requirements

- Guest + user cart, merge on login
- Server-authoritative price, stock, discounts, totals
- PaymentService abstraction + state machine (PAID only after server verify)
- Checkout paths: online gateway, card-to-card, phone, org invoice, B2B quote
- Shipping: customer vehicle OR seller transport (rules from business — do not invent)
- B2B: wholesale price, MOQ, bulk, org invoice fields when confirmed
- Persian-aware search (ی/ي, یکبار مصرف variants)

---

## 7. UI / UX

- **Mobile-first** hard requirement
- Premium admin (separate UX from storefront)
- Woodmart legacy = reference only — exceed it, do not clone
- Animation: premium + zero performance abuse + reduced-motion

---

## 8. Content governance

```
LEGACY → MIGRATE → PARITY → SEO VALIDATION → RELEASE → THEN IMPROVE
```

AI may structure/clarify. AI may **not** invent specs, certs, prices, reviews, stats.  
Mark unknowns: `SOURCE_REQUIRED`

---

## 9. SEO / migration

- Every legacy URL: KEEP | 301 | 410 | IGNORE
- Block release if important URLs/schema/canonical break
- Staging: **noindex**
- Scripts: `scripts/seo/` for validation

Full detail: `docs/seo/*`

---

## 10. Infrastructure

- **Server:** Hetzner `91.107.181.79`
- **Deploy:** GitHub → Actions → tests → staging → production
- **Secrets:** env / GitHub Secrets only
- **Run app as non-root** deploy user

Detail: `docs/INFRASTRUCTURE.md`

---

## 11. Quality gates (every release)

CODE → TYPECHECK → LINT → TEST → E2E → SECURITY → SEO REGRESSION → URL VALIDATION → SCHEMA → LIGHTHOUSE → BUILD → STAGING → QA → PRODUCTION

---

## 12. Approval gates

| Gate | Deliverable |
|---|---|
| **1** | Legacy forensics ✅ |
| **2** | Architecture decision |
| **3** | Design system + IA |
| **4** | Before irreversible production migration |

---

## 13. Phase execution

See `docs/EXECUTION-PLAN.md`:

1. Forensics ✅  
2. GitHub + Legacy repo + CI  
3. Architecture (Gate 2)  
4. Design system (Gate 3)  
5. Foundation (Next + Payload)  
6. Commerce  
7. Storefront  
8. Admin  
9. Migration (Gate 4)  
10. QA + Launch  

---

## 14. Forensic findings summary (Gate 1)

| Finding | Detail |
|---|---|
| Legacy domain | ayrik-cornstarch.com |
| Products | 95 published |
| Categories | 22 (آملون hub) |
| Compromised | Malware + 7779 spam users + casino posts |
| Payment config | Not found in DB — implement from spec |
| Wholesale | Not found — implement from business input |

Full audit: `docs/audit/README.md`

---

## 15. Engineering principle

Optimize for **better product** and **maximum verified progress per token** — not more code, libraries, or AI autonomy.

---

*Operational copy: `AGENTS.md` · Rules: `.cursor/rules/` · Business: `docs/BUSINESS-RULES.md`*
