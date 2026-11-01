# Domain path compare A / B / C

Generated from agent measurements (not Seattle). TLS verify on. SI not available from Performance API → left null.

## DNS

- NS: `ns2.viracdn.ir` / `ns4.viracdn.ir`
- A: `87.107.94.254`, `92.114.24.238`, `92.114.24.233`, `92.114.24.241`
- AAAA: none
- HTML body A vs B: **identical md5** (CDN does not rewrite HTML)

## Curl cold medians (seconds)

| Path | dns | tcp | tls | ttfb_after_tls | ttfb | html | total | remote | ssl_verify |
|------|-----|-----|-----|----------------|------|------|-------|--------|------------|
| A CDN | ~0.07 | ~0 | ~0.46 | ~0.26 | ~0.80–0.93 | ~0.45 | ~1.24–1.38 | CDN anycast | 0 |
| B origin SNI | 0 | ~0 | ~0.49 | ~0.24 | ~0.73 | ~1.19 | ~1.91 | 185.93.88.81 | 0 (after fullchain fix) |
| C IP | 0 | ~0 | ~0.49 | ~0.25 | ~0.75 | ~1.18 | ~1.92 | 185.93.88.81 | 0 |

Before fullchain fix, **B failed verify** (`unable to get local issuer certificate`) because nginx served leaf+YE1 only; cross-sign chain lived in `chain.pem` but not `fullchain.pem`. Fixed on VPS: `fullchain = cert + chain` (4 PEMs).

## Browser cold medians (ms, desktop 1366×768, LCP via PerformanceObserver)

| Path | TTFB | responseEnd | FP | FCP | LCP | notes |
|------|------|-------------|----|-----|-----|-------|
| A CDN | ~946 | ~1397 | =FCP | ~1415 | ~1730 | LCP = product webp via domain |
| B origin SNI | ~694 | ~1165 | =FCP | ~1430 | ~1430 | same host/SNI, connect→origin |
| C IP | ~688 | ~1158 | =FCP | ~1424 | ~1424 | secondary control |

FP equals FCP in these runs; that is observed, not assumed a priori.

## CDN cache evidence

Repeated requests to `https://movasseghistore.com/` and static CSS/font:

- `server: vira-cdn`
- `vira-cache-status: **MISS**` every time (HTML and static)
- Origin `X-Cache: HIT` sometimes forwarded → **not** proof of edge HIT
- `cache-control` on HTML is already `public, max-age=900, s-maxage=900` (cacheable)
- No `Set-Cookie` on guest HTML

**Conclusion:** Vira edge cache is not storing public responses (disabled or bypass). Seattle report Connection **1.9s** + Backend **583ms** ≈ TTFB **2.5s** aligns with client→Iran edge→origin on miss, not with app Structure (96).

## ACME / key rotation

- Origin HTTP-01 works: `http://movasseghistore.com/.well-known/...` via `--resolve` to origin → 200
- Public CDN HTTP-01 returns 404 body `acme/autocert: certificate cache miss`
- `certbot --dry-run` unauthorized from `92.114.24.238`
- New ECDSA key generated on server at `/etc/ssl/movasseghistore.com/rotate/privkey.new.pem` (**not** installed, **not** in Git)
- Rotation blocked until CDN ACME passthrough or DNS-01 access

## Seattle GTmetrix baseline (user)

[72oFmIEc](https://gtmetrix.com/reports/movasseghistore.com/72oFmIEc/): Perf **58** / Struct **96** / LCP **4.7s** / TBT **65ms** / CLS **0** / Conn **1.9s** / Backend **583ms** / TTFB **2.5s**

Do not treat IP [xfrCIxd1](https://gtmetrix.com/reports/185.93.88.81/xfrCIxd1/) (76/98/LCP2.1s) as domain result.
