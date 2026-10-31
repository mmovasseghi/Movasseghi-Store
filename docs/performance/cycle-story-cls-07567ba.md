# Cycle: early story open + mobile CLS (SHA `07567ba`)

## Live before
- Release: `31f01753449ab39d138cabbf197d56b915c9b4f8`
- Seattle GTmetrix (prior HTTPS): [SlGIpwvK](https://gtmetrix.com/reports/185.93.88.81/SlGIpwvK/) B / Perf 71 / Struct 98 / LCP 2.5s / TBT 40ms / CLS 0  
  [HFun8t5f](https://gtmetrix.com/reports/185.93.88.81/HFun8t5f/) B / Perf 75 / Struct 98 / LCP 2.4s / TBT 0ms / CLS 0

## Proven UX defects (local, against live `31f0175`)
1. **Early story click lost**  
   During `document.readyState=loading`, `__msEarlyCapture` was active but did **not** match `.ms-story`. Click before deferred `stories.js` did nothing; after-load open worked (modal `is-open` + slide image).
2. **Mobile CLS ≈ 0.136 under Slow 4G + 4× CPU** (LH-like throttle)  
   Dominant shift ≈ 0.127 at `fonts.ready` (~4.6s throttled).  
   Root cause: `.ms-page-home .ms-hero-desc { line-clamp:3; min-height:0 }` let Tahoma render ~62px (3 lines) then Vazirmatn ~41px (2 lines) → `#ms-features` moved ~20px.  
   Unthrottled mobile CLS was already ~0.004; GTmetrix Seattle desktop CLS was already 0. Viewport/throttle difference matters.

## Fixes (one related batch)
- `_Layout.cshtml`: capture `.ms-story[data-story-id]` into `__msEarlyStoryIntent`; keep `__msEarlyStoryCapture` until `stories.js` boots; do not replay story intents through `bootHeavy`.
- `stories.js`: consume early story intent and `open(id)`.
- `site.css` (+ rebuilt storefront/critical home CSS): lock homepage mobile hero description to two em-based lines (`min/max-height: 3.24em`, `line-clamp:2`).

## Deploy
- Official path: patched `quick-republish` on VPS to check out `origin/cursor/story-cls-lcp-e83d` (feature ahead of `origin/main`).
- Published SHA: `07567ba61a59b846ce2845555f6c4cf1d427193c` (matches `publish/RELEASE_COMMIT`).
- `verify-home`: featured 8, offers 8, stories 12, LCP image 200.
- Public HTTPS kept (HTTP/2 200, LE short-lived cert, `strict-transport-security: max-age=0`). No domain/DNS/CDN/SSL architecture change.

## After — UX (same probes)
| Probe | Before | After |
|-------|--------|-------|
| Mobile early story | intent not captured; stayed closed | `earlyStoryIntent=true` → `bodyOpen` + `hasImg` |
| Desktop early story | same failure | opens after boot |
| After-load story | open | open |
| Mobile CLS Slow4G+4×CPU | ~0.136 | ~0.062 (desc locked at 41.5px; features no longer jump ~20px) |
| Desktop CLS | ~0.002–0.005 | ~0.002 |

Artifacts: `/opt/cursor/artifacts/cycle4-ux/story-cls-*.json`, screenshots `after-*-early.png`.

## Preload vs `currentSrc` (no change)
| Viewport | preload `href` | `img.currentSrc` / LCP URL | Extra unused `01-lcp.webp`? |
|----------|----------------|----------------------------|-----------------------------|
| mobile 412@2.625 | `01-lcp.webp` + imagesrcset | `01-384w.webp` | No |
| mobile 390@3 | same | `01-display-v2-420w.webp` | No |
| desktop 1280/1920 | same | `01-display-v2-420w.webp` | No |

`imagesrcset`/`imagesizes` select the real candidate; href mismatch alone is not a bug here.

## Local Lighthouse (diagnostic only — not GTmetrix)
Cold sequential, simulated throttle, HTTPS IP:

| Run | Perf | FCP | SI | LCP | TBT | CLS | TTFB |
|-----|------|-----|----|-----|-----|-----|------|
| desktop-1 | 0.86 | 1326 | 2825 | 1326 | 88 | 0.002 | 224 |
| desktop-2 | 0.86 | 1313 | 2777 | 1313 | 82 | 0.002 | 224 |
| desktop-3 | 0.85 | 1326 | 2822 | 1326 | 115 | 0.002 | 224 |
| mobile-1 | 0.99 | 1278 | 2766 | 1278 | 6 | 0.043 | 225 |
| mobile-2 | 0.97 | 1978 | 3155 | 1978 | 4 | 0.045 | 237 |
| mobile-3 | 0.99 | 1299 | 2865 | 1299 | 1 | 0.042 | 224 |

LCP element (desktop): `article.ms-hero-slide > … > img` → `01-display-v2-420w.webp`.  
Mobile CLS improved vs prior ~0.14 lab; desktop SI ~2.8s remains a local signal, not a GT claim.

## Real GTmetrix after deploy
| Report | Location | Browser | Grade | Perf | Struct | LCP | TBT | CLS |
|--------|----------|---------|-------|------|--------|-----|-----|-----|
| [POzYv29X](https://gtmetrix.com/reports/185.93.88.81/POzYv29X/) | Seattle, WA, USA | Chrome 154 / LH 12.6.1 | **B** | **79** | **98** | **2.1s** | 40ms | 0 |

Detail tabs (FCP/SI/waterfall phases) still login-gated for anonymous view. Header vitals only.

## Remaining (next measured cycle — no guesswork)
- Seattle Performance still **79** (target A then >95). Primary public gap vs target is still **LCP 2.1s** on Seattle path (improved from 2.4–2.5s, not enough).
- Do not treat local LH 0.85–0.99 as Grade A.
- Next: attribute Seattle LCP phases (TTFB vs download vs render) with login session or fresh trace from comparable RTT; one change per batch.
- Residual throttled mobile CLS ~0.06 (glyph/features-band width) — optional follow-up, already in “good” CLS band for lab.

## Design acceptance (smoke)
- 8 featured + 8 offers, 12 stories, dual/LCP image verified by `verify-home`.
- HTTPS/HTTP2 retained; site not reverted to HTTP.
