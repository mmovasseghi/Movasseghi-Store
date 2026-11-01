# Cycle: LCP srcset domination + stories fetch (SHA `41a2e55`)

## Baseline (live before this tip)

- RELEASE: `07567ba` (Priority‑1 story early-open + hero-desc CLS)
- Seattle HTTPS GTmetrix: [POzYv29X](https://gtmetrix.com/reports/185.93.88.81/POzYv29X/) B / 79 / 98 / LCP 2.1s  
  [zLI2yUty](https://gtmetrix.com/reports/185.93.88.81/zLI2yUty/) B / 80 / 98 / LCP 2.1s

## Measured problem (local attribution, not GT)

- Mobile 412×2.625 `currentSrc` picked fat `01-384w.webp` (~45 KB) over `01-display-v2-420w.webp` (~22 KB) because srcset listed both.
- Home HTML inlined ~17 KB stories JSON (`msStoriesData` body).
- Preload `href=01-lcp.webp` + `imagesrcset` was already coherent (no unused `01-lcp` download); not a bug by itself.

## Fix shipped (`41a2e55`)

1. `ResponsiveImageService`: skip mid-widths `< 420w` when heavier than the lightest studio/packed 420w file.
2. `Index.cshtml`: empty `msStoriesData` body; `stories.js` fetches `/Home/Stories.json`.
3. Unit tests: ResponsiveImageTests 15/15.

## Deploy

- Patched `quick-republish` → `origin/cursor/story-cls-lcp-e83d` (`RUN_OPTIMIZE_IMAGES=0`).
- `RELEASE_COMMIT` = `41a2e55c3aec15573addfb8452cbd3dbca676a90`
- `nginx -t` OK; `verify-home`: featured 8, offers 8, stories 12, LCP 200.
- Public HTTPS cert verify OK (Let’s Encrypt YE2 short-lived IP); HTTP→HTTPS 301 preserved.

## UX / lab (same agent VM; diagnostic only)

| Check | Result |
|-------|--------|
| Mobile currentSrc | `01-display-v2-420w.webp` |
| Desktop currentSrc | `01-display-v2-420w.webp` |
| Story after load (mobile/desktop) | open=true |
| Story early click → later | open=true |
| CLS mobile (Playwright, unthrottled wait) | ~0.026 |
| CLS desktop | ~0.005 |
| Lighthouse mobile simulated ×3 | Perf **0.99**, LCP ~1274–1317 ms, CLS ~0.044–0.048, SI ~2755–2834 ms |
| Lighthouse desktop cold ×3 (provided/unthrottled) | Perf **0.88**, FCP ~1421–1430, LCP ~1452–1462, SI ~1981–1995 |

## Real Seattle GTmetrix (HTTPS) — all runs

| Report | Notes | Grade* | Perf | Struct | LCP | TBT | CLS | FCP† | SI† |
|--------|-------|--------|------|--------|-----|-----|-----|------|-----|
| run1 | Analysis Error: No CPU idle period | — | — | — | — | — | — | — | — |
| [5c8n84AR](https://gtmetrix.com/reports/185.93.88.81/5c8n84AR/) | Seattle / Chrome 154 / LH 12.6.1 | B | **78** | **98** | **2.1s** | 0ms | 0 | **1.9s** | **2.2s** |
| [o3qhorfs](https://gtmetrix.com/reports/185.93.88.81/o3qhorfs/) | same | B | **78** | **98** | **2.2s** | 40ms | 0 | **1.9s** | **2.3s** |
| [8qGeUXxY](https://gtmetrix.com/reports/185.93.88.81/8qGeUXxY/) | same | B | **74** | **98** | **2.4s** | 0ms | 0 | **2.2s** | **2.5s** |

\*Grade letter from GTmetrix header matrix (Perf/Struct); guest UI omits the glyph in text extract.  
†From public `lighthouse.json` (not shown on locked guest Performance tab).

### Proven LCP element (Seattle desktop, all successful runs)

`img` → `/images/products/0056/01-display-v2-420w.webp` (not `01-lcp.webp`, not `01-384w.webp`).

### HAR phase snapshot (`5c8n84AR`)

- HTML: gzip ~69 KB, wait ~219 ms, receive ~470 ms, wall ~1.13 s (connect+TLS ~442 ms).
- LCP image display-v2: ~22 KB, ~440 ms total.
- Inline `<style>` (critical-home) ≈ **150 KB** of the 325 KB uncompressed HTML.
- Long task ~357 ms attributed to document HTML; TBT still 0–40 ms.
- FCP≈1.9 s and LCP≈2.1 s track together → Seattle score is **FCP/HTML+CSS bound**, not fat mid-width LCP.

## Verdict

- **Do not claim Grade A or Perf/Struct >95.** Real Seattle remains **B / 74–78 / 98**.
- Srcset fix is correct for high-DPR mobile (lab Perf 0.99; currentSrc proven) but Seattle GTmetrix is desktop and already used display-v2 — LCP stayed ~2.1–2.4 s.
- Stories externalization verified (inline 0; early/after-load open).
- Residual blocker with numbers: Seattle FCP ≥1.9 s from TLS+TTFB+~69 KB gz HTML receive + parse of ~150 KB inlined complete home CSS. Without edge/CDN (out of scope), next lever is a **measured critical-CSS architecture trim** that keeps first paint, all hero slides, and first menu/cart/login/story interaction — not coverage deletion of one viewport.
- Follow-up micro-tweak: `gzip_comp_level` 4→6 (~5 KB HTML savings at level 6 vs 4); not expected alone to reach A.

## Artifacts

`docs/performance/reference/cycle-41a2e55/`
