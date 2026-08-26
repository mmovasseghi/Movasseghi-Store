# RISK-REGISTER — Migration & Launch Risks

**Canonical detail:** [19-risks.md](./19-risks.md)

## Critical (P0)

| Risk | Mitigation |
|---|---|
| SEO regression on cutover | Redirect map + parity checks |
| Product content loss | `legacyDescriptionHtml` verbatim migration |
| Fake product visuals | Legacy media only — enforced |
| Malware in backup | Never import legacy PHP; sanitized extracts only |
| Spam content migration | Heuristic filter on blog/users |

## High (P1)

| Risk | Mitigation |
|---|---|
| Live site unavailable for visual QA | Staging restore or owner screenshots |
| Payment integration unknown | Official gateway docs only; no invention |
| Price data incomplete (0 in DB) | "تماس برای قیمت" until owner provides prices |
| Open registration abuse | Disabled on new platform |

## Medium (P2)

| Risk | Mitigation |
|---|---|
| Brand rebrand confusion | Phased copy update after parity |
| Elementor layout loss | Rebuild homepage from assets + PAGE-DNA |
| Performance regression | PERFORMANCE-BUDGET.md, Lighthouse CI |
