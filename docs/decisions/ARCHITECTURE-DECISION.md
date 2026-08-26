# Architecture Decision — APPROVED

**Status:** ✅ APPROVED (autonomous Gate 2 — 2026-08-26)  
**Decision:** **Option B — Payload CMS + custom commerce domain in Next.js**

## Rationale

| Factor | Conclusion |
|---|---|
| 95 SKUs, 22 categories | Payload collections sufficient |
| No legacy orders in backup | No Medusa migration burden |
| B2B/wholesale/Iran payments | Custom commerce layer required anyway |
| Persian RTL + SEO | Next.js App Router + Payload admin |
| Single team/maintenance | One repo, one deploy |

**Rejected:** Medusa + Payload (dual stack overhead), WordPress restore (compromised).

## Stack

| Layer | Choice |
|---|---|
| Frontend | Next.js 15 App Router, TypeScript, Tailwind |
| CMS/Admin | Payload 3.x |
| Database | PostgreSQL 16 |
| UI | shadcn/ui + custom RTL components |
| Persian forms | PersianLabs/ui (when needed) |
| Commerce | Custom `src/commerce/*` domain services |
| Payments | `PaymentService` → NextPay, ZarinPal, BalePay adapters |
| Cache/session | Redis (optional phase 2) — start with Postgres + cookies |
| Deploy | GitHub Actions → Hetzner staging → production |

## Repository layout

```
/
├── docs/           # audit, knowledge, seo (existing)
├── src/
│   ├── app/        # Next.js routes (RTL storefront + API)
│   ├── payload/    # collections, globals, access
│   ├── commerce/   # cart, checkout, orders, payments
│   ├── components/ # UI
│   └── lib/        # utils, search, seo
├── scripts/
├── docker-compose.yml
└── payload.config.ts
```

## Commerce boundaries

- **Payload:** products, categories, media, pages, SEO fields, admin users
- **Commerce services:** cart, pricing engine, stock, orders, payment state machine
- **Never:** client-authoritative prices or payment success

## Migration path

1. Payload collections match `docs/audit/17-data-model.md`
2. Import script from Legacy repo JSON (phase 8)
3. URL redirects from `docs/seo/REDIRECT-MAP.md`

## References

- Payload E-commerce Template (architecture reference only, not cloned)
- Legacy audit: 95 products, IRT, Tehran default
