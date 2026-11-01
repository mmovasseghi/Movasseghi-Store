# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-01 (چرخه دامنه: deploy include + اندازه‌گیری A/B/C)

## نسخهٔ زنده

| مورد | مقدار |
|------|--------|
| **RELEASE اپ+config** | **`a14f43c79ee03c5a1f39b2dd0e923e01d6455e89`** (quick-republish رسمی EXIT:0) |
| URL دامنه | `https://movasseghistore.com/` (NS: viracdn.ir → Vira) |
| URL IP | `https://185.93.88.81/` (کنترل ثانویه؛ **نتیجهٔ دامنه نیست**) |
| TLS origin | dual-vhost؛ `fullchain` دامنه = leaf+cross-sign (**۴** PEM) |
| nginx conf / inc md5 | `821aa8bce64a64e96d343588e7e6941e` / `60fcc2eea841c52be339c6749a55abdd` |
| `PublicBaseUrl` | `https://movasseghistore.com` (حفظ در publish) |
| stamp | `/MOVASSEGHISTORE/ops/nginx-domain-ssl-20261101/DEPLOYED.txt` |

## اثبات کندی دامنه (اندازه‌گیری‌شده)

شواهد: `docs/performance/reference/domain-path-compare/`

| مسیر | معنی | یافته |
|------|------|--------|
| **A** | DNS عمومی Vira | `vira-cache-status: MISS` پایدار روی HTML و static؛ TTFB مرورگر ~0.95s از این محیط |
| **B** | همان hostname/SNI/URL → TCP به origin | TLS verify پس از تعمیر fullchain OK؛ TTFB ~0.69s؛ FCP/LCP ~1.43s |
| **C** | IP مستقیم | نزدیک به B |

HTML A و B **md5 یکسان**. تفاوت Seattle دامنه ([72oFmIEc](https://gtmetrix.com/reports/movasseghistore.com/72oFmIEc/) Perf58/LCP4.7s/TTFB2.5s/Conn1.9s) با IP ([xfrCIxd1](https://gtmetrix.com/reports/185.93.88.81/xfrCIxd1/) 76/LCP2.1s) با **مسیر client→لبه ایران Vira (+ miss به origin)** سازگار است، نه با Structure اپ (۹۶).

`vira-cache-status: MISS` + `Cache-Control` قابل‌کش → کش لبه **عملاً خاموش/bypass**؛ بدون دسترسی پنل Vira قابل روشن‌کردن از اینجا نیست.

## GTmetrix دامنه

| لینک | Perf | Struct | LCP | TBT | CLS | Conn | TTFB |
|------|------|--------|-----|-----|-----|------|------|
| [72oFmIEc](https://gtmetrix.com/reports/movasseghistore.com/72oFmIEc/) | **58** | **96** | **4.7s** | 65ms | 0 | 1.9s | 2.5s |

**Grade A اعلام نمی‌شود.**

## اصلاحات انجام‌شده این دور

1. `quick-republish` / remote-install / setup: نصب+بکاپ+rollback برای `snippets/movasseghi-shop-proxy.inc` هماهنگ با site
2. Merge به **main**
3. تعمیر `fullchain.pem` origin (cross-sign) تا B با verify فعال کار کند
4. کلید جدید ECDSA روی سرور در `…/rotate/` آماده؛ **نصب نشده** (ACME از CDN = 404)
5. اسکریپت `scripts/measure-domain-paths.mjs` + گزارش مسیرها

## موانع واقعی (پنهان نشده)

1. **پنل/API Vira در دسترس این عامل نیست** → نمی‌توان کش لبه یا passthrough ACME را روشن کرد
2. **چرخش گواهی دامنه** تا ACME/DNS-01 ممکن نیست (CDN ACME را می‌بلعد)
3. **GTmetrix Seattle تازه** از این محیط: guest/quota و computerUse مدل محدود؛ گزارش مبنا همان 72oFmIEc تا تست تازه موفق

## اقدام بعدی (ادامهٔ خودکار وقتی دسترسی باشد)

1. در پنل Vira: فعال‌سازی کش لبه برای HTML عمومی + static؛ ACME `/.well-known/acme-challenge/*` → origin بدون intercept
2. اتمام چرخش کلید با certbot پس از passthrough؛ revoke قبلی
3. `quick-republish` از main پس از هر تغییر پذیرفته
4. دو GTmetrix Seattle همسان روی دامنه پس از HIT شدن کش لبه
5. فقط بعد از افت TTFB دامنه: ادامهٔ Style/Layout اپ (بدون تکرار critical-split)

## UX

هیرو/۸+۸/۱۲ استوری از verify-home روی origin تأیید شده‌اند؛ مسیر CDN همان HTML را می‌دهد.
