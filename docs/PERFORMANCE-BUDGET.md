# Performance Budget

**Status:** Baseline targets — validate with Lighthouse CI (future gate).

---

## Core Web Vitals (mobile)

| Metric | Target |
|---|---|
| LCP | ≤ 2.5s |
| INP | ≤ 200ms |
| CLS | ≤ 0.1 |

---

## Payload budgets (initial)

| Asset | Budget |
|---|---|
| JS (route) | ≤ 180 KB gzip |
| CSS | ≤ 40 KB gzip |
| Hero image | ≤ 300 KB (legacy master) |
| Product image (LCP candidate) | ≤ 150 KB served (WebP target) |
| Total page weight | ≤ 1.2 MB mobile homepage |

---

## Rules

- `next/image` for all product/media URLs
- Font: Vazirmatn `display: swap`, subset arabic
- Motion: CSS only; no LCP-blocking animation
- `prefers-reduced-motion` respected
- Dynamic routes for DB-backed pages (no stale static product pages)

---

## Monitoring

- Lighthouse on CI (planned)
- PM2 + server logs for TTFB
- Image derivative pipeline (WebP) — phase 6
