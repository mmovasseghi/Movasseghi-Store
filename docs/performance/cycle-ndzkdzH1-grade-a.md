# چرخهٔ اصلاح پس از گزارش ndzkdzH1 (هدف Grade A / >۹۵)

تاریخ: ۲۰۲۶-۱۰-۳۱  
نسخهٔ مبنا روی سرور هنگام شروع: `fc02b40e79209f0be51b5598e62a9956bdc3a698` (= `origin/main`)  
گزارش مبنا: https://gtmetrix.com/reports/185.93.88.81/ndzkdzH1/  
شرایط: Seattle / Chrome 154 / Lighthouse 12.6.1  
نتیجهٔ مبنا: **Grade B · Perf 75 · Struct 94** · LCP 2.0s · TBT 251ms · CLS 0.01 · SI 1.9s · FL 2.4s

## خط پایهٔ محلی (۳ اجرا، همان URL عمومی، بدون ادعا به‌جای GTmetrix)

شرایط: headless Chrome 156 از محیط agent به `http://185.93.88.81/`، بدون throttle لایت‌هاوس، `networkidle2`.

| اجرا | FCP (ms) | LCP (ms) | TTFB (ms) | long tasks | LayoutDuration (s) |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1352 | 1692 | 459 | 0 | 0.135 |
| 2 | 636 | 824 | 231 | 1×70ms | 0.089 |
| 3 | 632 | 832 | 232 | 1×71ms | 0.093 |

میانهٔ تقریبی (اجرای ۲/۳ پس از گرم‌شدن کش): FCP ~634ms، LCP ~828ms، TTFB ~232ms، یک long task ~70ms.  
پراکندگی اجرای ۱ عمدتاً cold path / کش nginx است.  
**نتیجه:** long-taskهای ۲۳۶ms گزارش Seattle در این محیط بدون CPU throttle لایت‌هاوس بازتولید نشدند؛ با ضریب ~۴× لایت‌هاوس، کار ~۶۰ms واقعی می‌تواند به ~۲۴۰ms throttled برسد.

Trace خلاصه (اجرای ۲): `UpdateLayoutTree` تا ~28ms، `Layout` تا ~17ms، `RunTask` تا ~47ms. فایل‌ها: `/opt/cursor/artifacts/baseline-3runs.json`, `baseline-trace-summary.json`.

## علت‌های اثبات‌شده / رد‌شده

### Redirect HTTP→HTTPS→HTTP (~237ms)
- nginx فقط `:80` گوش می‌دهد؛ `:443` باز نیست.
- `SiteSettings:PublicBaseUrl=http://185.93.88.81` ⇒ `UseHttpsRedirection` خاموش است.
- curl مستقیم به origin: **بدون Location/307**، `X-Cache HIT/MISS` سالم.
- `movasseghi.ir` از این محیط **DNS resolve نمی‌شود**؛ certbot بدون دامنهٔ معتبر قابل صدور LE نیست.
- **نتیجه‌گیری:** زنجیرهٔ گزارش با رفتار HTTPS-First مرورگر لاب روی IP سازگار است، نه redirect پیکربندی‌شدهٔ سرور. حذف حدسی HTTPS middleware انجام نشد.

### Backend 230ms از Seattle
- localhost Kestrel TTFB ~2–4ms؛ nginx HIT ~1ms.
- عدد Seattle = تأخیر شبکه + پردازش، نه ۲۳۰ms CPU برنامه.

### TBT / home.js 236ms
- جدول long-task: سنگین‌ترین اسکریپت نام‌دار = `home.js`؛ `product-card-dual.js` نیست.
- JS execution audit فقط ۱۷ms؛ main-thread: Other 739 + Style&Layout 669.
- در `home.js` کار همزمان شامل: اندازه‌گیری/کلون trust marquee، FLIP دات‌های هیرو، `getBoundingClientRect`، راه‌اندازی autoplay/WAAPI.
- **فرضیهٔ کاری با شواهد کد + throttle:** همان کار هندسه/layout زیر CPU throttle لایت‌هاوس به long-taskهای >50ms می‌رسد.

### LCP Render Delay 648ms
- عنصر LCP همان `0056/01-display-v2-420w.webp` (eager + high) است؛ preload تصویر درست است.
- preload اسکریپت‌های home/stories/dual و حتی preload کم‌اولویت auth/site با تصویر LCP روی لینک کند Seattle رقابت پهنای باند می‌کردند (شاهد waterfall قبلی و preloadهای `_Layout`).

### HTTP/2 (~860ms تخمینی) و CDN (۱۷ منبع)
- بدون TLS معتبر روی دامنهٔ resolveشونده قابل فعال‌سازی مرورگری نیست.
- h2c روی HTTP خام به‌عنوان حل اعلام نمی‌شود.
- صرفه‌جویی auditها جمع نمی‌شوند.

## اصلاح این دسته (یک دستهٔ مرتبط)

1. حذف preload اسکریپت‌های home/stories/dual و preload کم‌اولویت auth/site/scroll-reveal از صفحهٔ خانه؛ بارگذاری defer/idle باقی است تا با LCP رقابت نکند.
2. شکستن راه‌اندازی `home.js`: countdown فوری؛ هیرو/مارquee/particles پس از `scheduler.yield` / idle و بعد از paint.

`product-card-dual.js` عمداً دست نخورده (در long-taskها نیست).

## محدودیت زیرساخت (مانع واقعی برای A/>۹۵ از Seattle)

- تست از Seattle به VPS ایران: Connection~۲۲۰ms + TTFB بالا سقف LCP را نگه می‌دارد.
- بدون دامنهٔ DNS + گواهی + HTTP/2/CDN نزدیک کاربر، ممیزی‌های Structure HTTP/2 و CDN و بخش بزرگی از TTFB قابل بستن نیستند.
- تا گزارش GTmetrix تازه هدف را تأیید نکرده، ادعای Grade A یا Perf/Struct >۹۵ نمی‌شود.

## وضعیت ادامه

- پس از انتشار این SHA، GTmetrix Seattle تازه گرفته می‌شود و جدول قبل/بعد تکمیل می‌گردد.
- اگر TBT پایین آمد ولی LCP به‌خاطر TTFB جغرافیایی ماند، گام بعدی زیرساختی (دامنه+TLS+HTTP/2 و در صورت امکان CDN) است نه حذف UX.
