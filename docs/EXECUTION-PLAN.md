# Execution Plan — Movasseghi Store

**Owner:** Autonomous Composer Agent  
**Updated:** 2026-08-26  
**Mode:** Cloud-first (GitHub + Hetzner + Cursor Cloud Agent)

---

## Architecture

```
GitHub (Private)
├── mmovasseghi/Movasseghi-Store        ← NEW platform (this repo)
└── mmovasseghi/Movasseghi-Store-Legacy ← Sanitized forensic export (read-only)

Cursor Cloud Agent / Composer 2.5 Fast
        │
        ├── reads Legacy repo OR docs/audit (never rescans raw WP)
        ├── implements in Movasseghi-Store
        └── opens PRs / commits

GitHub Actions
        │
        ├── lint / typecheck / test / build
        └── deploy → Hetzner staging

Hetzner 91.107.181.79
        ├── staging (noindex)
        └── production (after domain + gates)
```

---

## Phase map

| Phase | Deliverable | Status |
|---|---|---|
| **1** Legacy forensics | docs/audit, knowledge, seo | ✅ DONE |
| **1b** GitHub + Legacy repo + CI skeleton | repos pushed | 🔄 IN PROGRESS |
| **2** Architecture decision (Gate 2) | ARCHITECTURE-DECISION approved | Pending |
| **3** Design system + IA (Gate 3) | DESIGN-SYSTEM.md | Pending |
| **4** Foundation | Next.js + Payload + Postgres + Biome | Pending |
| **5** Commerce core | cart, checkout, payments abstraction | Pending |
| **6** Storefront | catalog, product, search, mobile | Pending |
| **7** Admin | products, orders, SEO, B2B | Pending |
| **8** Migration | clean product/category/media import | Gate 4 |
| **9** QA | Playwright, Lighthouse, SEO regression | Pending |
| **10** Launch | domain, GSC, production | Pending |

---

## Token efficiency (HARD)

```
RAW LEGACY (once)
    → docs/audit + docs/knowledge + docs/seo
    → future tasks read COMPACT DOCS only
```

Agent must **not** re-scan WordPress backup for routine tasks.

---

## Technology tiers

### Tier A — Core (install when foundation starts)

- Next.js, TypeScript, Tailwind
- Payload CMS (candidate #1 — confirm Gate 2)
- shadcn/ui
- PostgreSQL
- Biome, Playwright, Gitleaks

### Tier B — If justified

- PersianLabs/ui (RTL forms)
- Motion (animation)
- TanStack Query (heavy client state only)

### Tier C — Alternative if audit demands

- Medusa (only if Payload commerce insufficient)

### External services

- NextPay (primary), ZarinPal (secondary), BalePay (optional)
- GA4, GSC, Merchant Center, Sentry

---

## Approval gates

| Gate | Status |
|---|---|
| Gate 1 — Forensics | ✅ Complete |
| Gate 2 — Architecture | After Payload eval + business rules |
| Gate 3 — Design + IA | Before UI build |
| Gate 4 — Production migration | Before live data cutover |

---

## Server policy

- Deploy user (non-root) + SSH keys
- Secrets in environment only — **never in repo**
- Staging: noindex, isolated DB
- CI deploys via GitHub Actions — not manual SCP

---

## Immediate next actions (agent)

1. ✅ Push forensic docs to GitHub
2. ✅ Create Legacy private repo (sanitized)
3. 🔄 Bootstrap Hetzner staging host
4. Gate 2: finalize Payload + custom commerce decision
5. Initialize Next.js + Payload monorepo skeleton

---

## References

- `AGENTS.md` — agent constitution
- `docs/MASTER-AGENT-SPEC.md` — full specification
- `docs/audit/README.md` — forensic summary
- `docs/decisions/ARCHITECTURE-DECISION.md` — architecture draft
