# هندآف کامل Codex — چرخه `4934300` (کار + GTmetrix)

این فایل **هم گزارش کامل کار انجام‌شده** و **هم گزارش‌های واقعی GTmetrix بعد از تغییر** را یک‌جا دارد.
هدف بعدی را وابسته به خرید دامنه نکن. CDN / DNS / گواهی جدید / برگرداندن به HTTP خارج از محدوده است.
**تا گزارش واقعی تأیید نکرده، رسیدن به Grade A یا Perf/Struct بالای ۹۵ را اعلام نکن.**

---

## A0. هویت محیط (همین الان روی سرور)

| مورد | مقدار |
|------|--------|
| Repo | `mmovasseghi/Movasseghi-Store` |
| URL عمومی | `https://185.93.88.81/` |
| Source روی VPS | `/MOVASSEGHISTORE/src` |
| Publish روی VPS | `/MOVASSEGHISTORE/publish` |
| Kestrel | `127.0.0.1:5080` |
| **RELEASE_COMMIT زنده** | **`493430039be6dbb535cb15fa8cc47fadcda04310`** |
| SRC HEAD روی VPS | همان `4934300…` |
| tip docs روی main | `83a11ef` (docs بعد از publish؛ runtime همان `4934300`) |
| شاخه کار | `cursor/story-cls-lcp-e83d` (در main ادغام شد؛ PR #7 MERGED) |
| مسیر انتشار | رسمی `deploy/linux/quick-republish.sh` از **main** — بدون patch / بدون fetch-reset شاخه فیچر |
| backup این release | `/MOVASSEGHISTORE/backups-release.oqIEWTdL` |
| HTTPS / HTTP2 | فعال؛ `listen 443 ssl http2` |
| گواهی | Let’s Encrypt روی IP: `/etc/letsencrypt/live/ip-185-93-88-81/fullchain.pem`؛ تمدید موجود حفظ شود |
| HSTS | `max-age=0` (عمدی برای IP cert) |
| **nginx conf md5** | **`a56672754fe83dcb2f370c30f2cac2b6`** |
| **gzip_comp_level** | **6** (جدا از RELEASE برنامه؛ در conf ثبت شده) |
| شواهد | `docs/performance/reference/cycle-style-layout/` |
| هندآف کوتاه‌تر | `docs/performance/CODEX-HANDOFF-cycle-style-layout-4934300.md` |

**قبل از این چرخه روی سرور:** `41a2e55` + gzip6؛ بهترین Seattle آن چرخه B/82/98 (Q8T2c8aj / u7ZOUnNi).  
**origin/main هنگام شروع این چرخه:** `d4ad797` — سپس کل PR #7 + فیکس‌های این چرخه روی main آمد و رسمی publish شد.

---

# بخش ۱ — گزارش کامل کار انجام‌شده

## A1. ورودی / محدودیت‌هایی که رعایت شد

- هدف: Grade A واقعی GTmetrix سپس Perf و Structure >95، با حفظ کامل ظاهر/متن/تصاویر/انیمیشن/امکانات.
- عدد محلی را به‌جای GTmetrix اعلام نکن.
- دامنه `.com`، DNS، CDN، صدور گواهی جدید، تغییر معماری SSL: **خارج از کار**.
- HTTPS/HTTP2 و تمدید خودکار فعلی حفظ؛ سایت به HTTP برنگردد.
- اصلاحات قبلی را کورکورانه تکرار/برنگردان: حذف preload JS، تقسیم home.js، preload فونت/LCP، dual slider، HTTPS/HTTP2، انتخاب display-v2.
- تغییرات قبل از انتشار رسمی باید در **main** باشد؛ کپی‌کردن quick-republish و جایگزینی با fetch/reset شاخه **تکرار نشود**.
- reset مخرب روی تغییرات محلی سرور انجام نشد؛ checkout VPS تمیز بود و ff-only به main.

## A2. وضعیت شروع (قبل از فیکس این چرخه)

### GTmetrix Seattle HTTPS مبنای شروع (بعد از `41a2e55` + gzip6)

| لینک | Grade | Perf | Struct | LCP | TBT | CLS | FCP | SI |
|------|-------|------|--------|-----|-----|-----|-----|-----|
| https://gtmetrix.com/reports/185.93.88.81/Q8T2c8aj/ | B | 82 | 98 | 1.8s | 0 | 0 | 1.8s | 2.1s |
| https://gtmetrix.com/reports/185.93.88.81/u7ZOUnNi/ | B | 82 | 98 | 1.8s | ~40ms | 0 | 1.8s | 2.1s |

### وزن Performance در Q8T2c8aj (و همین الگو بعد از فیکس)

| متریک | وزن | امتیاز جزئی (Q8) |
|--------|------|-------------------|
| FCP | 10% | 0.38 |
| LCP | 25% | 0.69 |
| SI | 10% | 0.56 |
| TBT | 30% | 1.00 |
| CLS | 25% | 1.00 |

→ افت عمدتاً از **FCP / LCP / SI** است، نه TBT/CLS.

### تشخیص اشتباه قبلی که باید اصلاح می‌شد

گزارش چرخه `41a2e55` نوشته بود long-task ~333ms «ناشی از parse کردن ~150KB CSS».  
Codex با خواندن مستقیم `gtmetrix-Q8T2c8aj-lighthouse.json` رد کرد:

```
long-tasks.details.debugData.tasks[0]:
  startTime 1228.3ms, duration 332.7ms
  styleLayout 313.6ms
  paintCompositeRender 8.6ms
  other 9.5ms
  scriptEvaluation 1ms

mainthread-work-breakdown:
  styleLayout 471.3ms
  parseHTML     15.9ms
```

- task بعدی سند @1564.2ms: مدت 84ms شامل 65ms Style/Layout
- LCP image download end ≈1134ms؛ FCP=LCP≈1809ms
- پس از آماده‌شدن تصویر، Style/Layout پیش از اولین نمایش رخ می‌دهد
- CSS بزرگ می‌تواند روی محاسبهٔ استایل اثر بگذارد، ولی علت دقیق از styleLayout/trace است نه parse
- **TBT=0 ≠ نبود کار سنگین**؛ بخش عمدهٔ این taskها **قبل از FCP** بوده‌اند
- اگر فقط FCP را جلو بیندازی و همان کار سنگین بعد از آن بماند، TBT می‌تواند بالا برود

### دو رگرسیون باز در شروع

1. **بریدگی متن هیرو:** در `site.css` هنوز  
   `.ms-page-home .ms-hero-desc { line-clamp:2; max-height:3.24em; overflow:hidden }`  
   بازتولید Codex/ما در 320/360/390 با متن Index و فونت پروژه: متن کامل ≈62px، ارتفاع نمایش ≈41px → خط سوم پنهان.
2. **درخواست تکراری Stories.json:** `msStoriesData` خالی است؛ `boot()` و `consumeEarlyStoryIntent()` هر دو `ensureStoriesLoaded()` را صدا می‌زنند بدون promise مشترک → دو fetch هم‌زمان ممکن است. تست صرف `open=true` این را نمی‌گیرد.

## A3. تغییرات کد این چرخه

### Commitهای runtime (قبل از merge به main)

1. **`2302ca0`** — Fix hero truncation, share Stories fetch, cut pre-FCP Layout.
2. **`c66e116`** — Handle Stories fetch rejection on early click paths.
3. **`4934300`** — Merge `cursor/story-cls-lcp-e83d` into main (شامل کل PR #7 از قبل + فیکس‌های بالا).
4. Docs بعداً: `5be1d4e`, `83a11ef` (runtime عوض نشد).

### جزئیات اصلاحات

#### ۱) هیرو — بستن بریدگی بدون مخفی‌کردن متن برای امتیاز
فایل: `src/MovasseghiShop.Web/wwwroot/css/site.css` (+ rebuild `storefront*.css` / `critical-home.css`)

- حذف `line-clamp:2` / `-webkit-line-clamp:2` / `max-height` / `overflow:hidden` روی `.ms-page-home .ms-hero-desc` و قاعدهٔ موبایل عمومی `.ms-hero-desc`
- جایگزینی با:
  - `display:block`
  - `min-height: calc(3 * 1.62em)` → minify شده به `min-height:4.86em`
  - `overflow:visible`، clamp unset
- منطق: فضای پایدار متناسب با متن کامل (≈۳ خط Tahoma/Vazirmatn در عرض موبایل)؛ متن برای امتیاز مخفی نمی‌شود؛ CLS از font-swap با min-height ثابت کنترل می‌شود
- دسکتاپ از قبل در `ux-desktop.css` clamp را unset کرده بود

#### ۲) Stories — یک promise مشترک
فایل: `src/MovasseghiShop.Web/wwwroot/js/stories.js`

- `storiesLoadPromise` برای اشتراک inflight بین `boot` / `consumeEarlyStoryIntent` / کلیک
- روی شکست: `storiesLoadPromise = null` تا retry سالم بماند
- `loadStoriesPayload`: دیگر روی HTTP error/`catch` آرایهٔ خالی برنمی‌گرداند؛ خطا پرتاب می‌شود
- مسیرهای open: `.catch(() => {})` تا unhandled rejection نباشد
- تصویر modal: `loading="eager"` + `fetchpriority="high"` (نه lazy) تا تصویر واقعاً بارگذاری شود
- اسکریپت تست: `scripts/verify-stories-inflight.mjs`

#### ۳) Style/Layout — اهرم اندازه‌گیری‌شده
فایل: `src/MovasseghiShop.Web/wwwroot/css/ux-scroll-reveal.css` (+ bundles)

فرضیه: قبل از FCP، Layout تمام‌سند روی ~1760 dirty object گران است؛ `content-visibility:auto` روی below-fold با `contain-intrinsic-size` اندازه‌گیری‌شده باید Layout/dirty را کم کند بدون FOUC/pop-in قبلی.

اعمال روی:
- `.ms-offers` → `auto 560px`
- `.ms-cat-section` → `auto 280px`
- `.ms-featured-section` → `auto 1300px`
- `.ms-segments` → `auto 480px`
- `.ms-news` → `auto 580px`
- `.ms-closing` → `auto 560px`
- `.ms-home-faq` → `auto 360px`
- `.ms-footer` → `auto 400px`

عمداً روی `#ms-features` / هیرو / استوری اعمال نشد (نزدیک/داخل اولین viewport موبایل).

**عمداً انجام نشد:**
- تقسیم critical کوچک + remainder خارجی (ریسک بازآرایی دوم / رقابت شبکه / flash / TBT)
- حذف CSS با coverage یک viewport
- دست زدن به preload / display-v2 بدون شاهد جدید
- حذف/مخفی انیمیشن، تصویر، محصول، متن، فونت
- دامنه/CDN/گواهی جدید

#### ۴) اصلاح متن تشخیص در هندآف قبلی
فایل: `docs/performance/CODEX-HANDOFF-cycle-41a2e55-COMPLETE.md`  
عبارت «parse کردن 150KB CSS» با دادهٔ styleLayout جایگزین شد.

## A4. اندازه‌گیری تشخیصی (نه به‌جای GTmetrix)

### CDP trace قبل از فیکس (زنده `41a2e55`، دسکتاپ)

| قبل از FCP | ms |
|------------|-----|
| Layout | ~120.6 |
| UpdateLayoutTree (Recalc Style) | ~20.2 |
| ParseAuthorStyleSheet | ~1.4 |

Top Layout: duration ~91.6ms، dirtyObjects **1760**، totalObjects 1760، `partialLayout:false`، layout root `#document`.

### CDP trace بعد از publish (`4934300`)

| قبل از FCP | ms |
|------------|-----|
| Layout | ~78.8 |
| UpdateLayoutTree | ~13.5 |

Top Layout: duration ~52.3ms، dirtyObjects **818**، root هنوز `#document`.

→ فرضیهٔ محلی تأیید شد (Layout↓، dirty↓).  
→ معیار موفقیت Seattle (FCP/LCP↓ / Perf↑) **تأیید نشد** (پایین ببین).

### Cold محلی بعد از publish (تشخیصی؛ Grade A نیست)

HTML transfer ≈ **63953** بایت (gzip). LCP currentSrc در همه: `01-display-v2-420w.webp`.

| اجرا | FCP | TTFB | CLS |
|------|-----|------|-----|
| desktop-cold-1 | 1396 | 681 | 0.0017 |
| desktop-cold-2 | 924 | 459 | 0.0054 |
| desktop-cold-3 | 936 | 463 | 0.0054 |
| mobile-cpu4-1 | 1140 | 462 | 0.055 |
| mobile-cpu4-2 | 1140 | 456 | 0.055 |
| mobile-cpu4-3 | 1132 | 456 | 0.055 |

میانه desktop FCP≈936ms؛ میانه mobile-cpu4 FCP≈1140ms.  
CLS موبایل throttled محلی ~0.055 — بالاتر از GT دسکتاپ؛ جدا نگه داشته شود.

## A5. پذیرش UX بعد از publish (اثبات)

| آزمون | نتیجه |
|-------|--------|
| هیرو 320/360/390 Vazirmatn | truncated=false؛ minH≈62.208px؛ scrollH=clientH=62 |
| هیرو 320/360/390 Tahoma | truncated=false |
| هیرو دسکتاپ | truncated=false |
| zoom متن 150% | اسکرین گرفته شد؛ بدون clamp مخفی‌ساز |
| featured / offers / stories / hero slides | 8 / 8 / 12 / 3 |
| Stories mid-flight (پاسخ کند مصنوعی) | starts=1، maxInflight=1، modal open، naturalWidth=310 |
| منو / سبد / ورود | اولین کلیک باز می‌شود (`ms-drawer.is-open` / `ms-modal-open`) |
| بستن استوری | Escape → `ms-story-open` false؛ scroll آزاد |
| بستن منو | backdrop `.ms-drawer-bg` → closed |
| LCP currentSrc موبایل+دسکتاپ | `01-display-v2-420w.webp` |
| verify-home روی publish | featured:8 offers:8 stories:12 lcp:200 |
| HTTPS عمومی | HTTP/2 200 |
| HTTP | 301 به HTTPS (حفظ مسیر فعلی) |

اسکرین‌ها: `docs/performance/reference/cycle-style-layout/screens/`

## A6. انتشار رسمی

1. Merge فیچر → `main` (`4934300`) و push
2. روی VPS: `git pull --ff-only origin main` (checkout تمیز؛ بدون reset مخرب)
3. `RUN_OPTIMIZE_IMAGES=0 bash deploy/linux/quick-republish.sh` (اسکریپت رسمی؛ هنوز `git pull --ff-only origin main`)
4. `nginx -t` موفق؛ conf با gzip6 reload شد
5. DB / App_Data / images / appsettings.Production / MinIO / گواهی حفظ
6. `RELEASE_COMMIT` و SRC SHA منطبق: `4934300…`
7. backup قبلی در `backups-release.oqIEWTdL`

## A7. کارهایی که عمداً تکرار/برنگشت نشدند

- حذف preloadهای JS / تقسیم home.js / dual / HTTPS-IP
- دستکاری کیفیت عکس بی‌دلیل
- ادعا که LH/CDP محلی = Grade A
- patch کردن quick-republish برای شاخه فیچر
- CSS split critical/remainder بدون شاهد Seattle

## A8. مشکلات باز / باقی‌مانده (برای دستور بعدی Codex)

1. **هدف Grade A و Perf/Struct >95 هنوز حاصل نشده** — بهترین Seattle این چرخه همچنان **B / 82 / 98**.
2. **گلوگاه اثبات‌شده Seattle دسکتاپ هنوز FCP≈LCP≈1.8s با Render Delay ~36–37٪ است.**  
   - LCP element همان `01-display-v2-420w.webp`.  
   - content-visibility محلی Layout/dirty را کم کرد، ولی styleLayout در Seattle هنوز ~496–534ms و Perf همان 82 ماند.
3. **اهرم بعدی باید HTML transfer / کار Layout تمام‌سند قبل از FCP را هدف بگیرد** بدون FOUC، بدون حذف انیمیشن/تصویر/متن، بدون coverage-deletion یک viewport.
4. اگر critical کوچک + remainder خارجی آزمایش می‌شود: فرضیه و معیار موفقیت صریح لازم است (کاهش دریافت HTML؟ Style/Layout؟ زودتر شدن first viewport؟). اگر بازآرایی دوم / رقابت شبکه / flash / TBT بیشتر شد → اصلاح یا برگردان.
5. Structure=98؛ فرصت‌های باقی: `use-cdn` (۱۷ منبع، **خارج از محدوده فعلی دامنه/CDN**)، `unused-css-rules` ~18KiB (صرفه FCP ثابت نشده)، `dom-size` 1425.
6. CLS دسکتاپ GT ≈0.002؛ CLS موبایل lab محلی ~0.055 — جدا نگه دار.
7. یک تلاش GT به `signup?job_limit=1` خورد (محدودیت مهمان)؛ دو اجرای موفق بعداً ثبت شد.
8. دامنه/CDN را به‌جای کار فنی ننویس.

---

# بخش ۲ — گزارش کامل GTmetrix بعد از تغییرات

همه روی **`https://185.93.88.81/`**، **Seattle, WA, USA**، **Chrome 154.0.0.0**، **Lighthouse 12.6.1**، دسکتاپ، `throttlingMethod: provided`.  
اعداد از **هدر گزارش + `lighthouse.json` عمومی ذخیره‌شده** استخراج شده‌اند.

## B1. جدول همهٔ اجراهای این چرخه (هیچ‌کدام را دور نریز)

| # | لینک | وضعیت | Grade | Perf | Struct | LCP | TBT | CLS | FCP | SI | توضیح |
|---|------|--------|-------|------|--------|-----|-----|-----|-----|-----|--------|
| 0a | https://gtmetrix.com/reports/185.93.88.81/Q8T2c8aj/ | قبل این فیکس | B | 82 | 98 | 1.8s | 0 | 0 | 1.8s | 2.1s | baseline `41a2e55`+gzip6 |
| 0b | https://gtmetrix.com/reports/185.93.88.81/u7ZOUnNi/ | قبل این فیکس | B | 82 | 98 | 1.8s | ~40ms | 0 | 1.8s | 2.1s | baseline |
| 1 | https://gtmetrix.com/reports/185.93.88.81/gV9gP6Ev/ | بعد `4934300` | B | **82** | **98** | **1.8s** | **21ms** | 0 | **1.79s** | **2.14s** | runA |
| — | — | **شکست** | — | — | — | — | — | — | — | — | `signup?job_limit=1` (محدودیت مهمان) |
| 2 | https://gtmetrix.com/reports/185.93.88.81/AFqU0xtT/ | بعد `4934300` | B | **82** | **98** | **1.8s** | **0ms** | 0 | **1.81s** | **2.16s** | runB2 |

**میانه Perf این چرخه بعد از فیکس: 82. پراکندگی بین دو موفق: 0.**  
**بهترین واقعی: هنوز 82 — Grade A نیست. ادعای A ممنوع.**

مقایسه با baseline: Perf/LCP/FCP/SI عملاً **بدون تغییر معنادار**؛ content-visibility نمره Seattle را جابه‌جا نکرد.

## B2. گزارش کامل — `gV9gP6Ev` (runA)

**لینک:** https://gtmetrix.com/reports/185.93.88.81/gV9gP6Ev/  
**URL تست:** `https://185.93.88.81/`  
**زمان LH fetchTime:** 2026-11-01T04:39:28.332Z  
**Generated (هدر):** Sat, Oct 31, 2026 9:39 PM -0700  
**مکان:** Seattle, WA, USA  
**مرورگر:** Chrome 154.0.0.0 / Lighthouse 12.6.1  
**formFactor:** desktop · throttlingMethod: provided  
**BenchmarkIndex:** ~2167.5  
**فایل ذخیره‌شده:** `docs/performance/reference/cycle-style-layout/runA-lighthouse.json`

### نمرات هدر

| مورد | مقدار |
|------|--------|
| GTmetrix Grade | **B** |
| Performance | **82%** |
| Structure | **98%** |
| LCP | **1.8s** |
| TBT | **20ms** (هدر) / LH numeric ≈20.8ms |
| CLS | **0** (دقیق LH: 0.001715) |

### متریک‌های Performance (از lighthouse.json)

| متریک | مقدار | score جزئی |
|--------|--------|------------|
| FCP | **1.79s** (1790ms) | 0.39 |
| Speed Index | **2.14s** (2143ms) | 0.56 |
| LCP | **1.79s** (1790ms) | 0.70 |
| TTI | 1.86s | 0.97 |
| TBT | 21ms | 1.00 |
| CLS | 0.002 | 1.00 |
| Max Potential FID | 110ms | 0.94 |
| Server response (document) | 220ms | 1.00 |
| Total byte weight | 356 KiB | 1.00 |
| DOM size | **1425** elements | 0.5 |
| Main-thread work | 1.6s | 1.00 |
| JS bootup | 0.1s | 1.00 |

### عنصر LCP

- Selector: `article.ms-hero-slide > div.ms-hero-grid > div.ms-hero-product > img`
- فایل: **`/images/products/0056/01-display-v2-420w.webp`**
- `loading=eager` · `fetchpriority=high` · sizes هیرو استاندارد
- نه `01-lcp.webp`، نه `01-384w.webp`

### فازهای LCP

| Phase | % | ms |
|-------|---|-----|
| TTFB | 37% | ~663 |
| Load Delay | 12% | ~221 |
| Load Time | 15% | ~263 |
| **Render Delay** | **36%** | **~643** |

تفسیر: بعد از بایت‌های تصویر، Render Delay هم‌وزن TTFB است → کار Style/Layout/paint سند نمایش LCP را عقب می‌اندازد.

### Main-thread breakdown

| Group | ms |
|-------|-----|
| Other | 714.6 |
| **Style & Layout** | **534.2** |
| Rendering | 192.0 |
| Script Evaluation | 81.2 |
| Script Parse/Compile | 51.2 |
| **Parse HTML & CSS** | **16.1** |

### Long tasks (debugData)

1. @1205.5ms · dur 349.8ms · **styleLayout 339.2** · paint 5.8 · other 3.8 · script 1.1  
2. @1747.5ms · dur 113.8ms · other 111.9 · styleLayout 0.7  
3. @1687.3ms · dur 56.2ms · other 56.2  

### Bootup

| URL | total | scripting |
|-----|-------|-----------|
| `https://185.93.88.81/` (سند) | 1180.9 | 51.0 |
| `js/home.js?v=0lCmijd…` | 181.7 | 6.3 |
| Unattributable | 146.0 | 4.2 |

### Audits ناقص / فرصت Structure/Perf (score < 1)

| score | id | عنوان | display |
|-------|-----|--------|---------|
| 0.00 | largest-contentful-paint-element | LCP element | 1,790 ms |
| 0.39 | first-contentful-paint | FCP | 1.8 s |
| 0.50 | dom-size | Avoid excessive DOM size | 1,425 elements |
| 0.50 | unused-css-rules | Reduce unused CSS | Potential savings of **18 KiB** |
| 0.53 | use-cdn | Use a CDN | **17 resources** (خارج از محدوده دامنه/CDN) |
| 0.56 | speed-index | Speed Index | 2.1 s |
| 0.70 | largest-contentful-paint | LCP | 1.8 s |
| 0.94 | max-potential-fid | Max Potential FID | 110 ms |
| 0.97 | interactive | TTI | 1.9 s |

unused-css: روی critical-home اینلاین ≈ wasted 18473 / total 29443 بایت (~63٪ از آن آیتم؛ این «unused» پوشش یک viewport است — حذف کور ممنوع).

### چیزهایی که سبز/OK بودند (نمونه)

render-blocking 0ms · prioritize-lcp-image OK · responsive-images OK · font-display OK · text-compression OK · http2 OK · redirects OK · total-byte-weight OK

## B3. گزارش کامل — `AFqU0xtT` (runB2)

**لینک:** https://gtmetrix.com/reports/185.93.88.81/AFqU0xtT/  
**fetchTime:** 2026-11-01T04:53:20.680Z  
**Generated:** Sat, Oct 31, 2026 9:53 PM -0700  
**BenchmarkIndex:** ~2739.5  
**فایل:** `docs/performance/reference/cycle-style-layout/runB2-lighthouse.json`

### نمرات هدر

| مورد | مقدار |
|------|--------|
| GTmetrix Grade | **B** |
| Performance | **82%** |
| Structure | **98%** |
| LCP | **1.8s** |
| TBT | **0ms** |
| CLS | **0** (LH: 0.001715) |

### متریک‌ها

| متریک | مقدار | score |
|--------|--------|--------|
| FCP | **1.81s** (1811ms) | 0.38 |
| SI | **2.16s** (2158ms) | 0.55 |
| LCP | **1.81s** (1811ms) | 0.69 |
| TTI | 1.81s | 0.97 |
| TBT | 0ms | 1.00 |
| CLS | 0.002 | 1.00 |
| Max Potential FID | 110ms | 0.95 |
| Server response | **440ms** | 1.00 |
| Total bytes | 356 KiB | 1.00 |
| DOM | 1425 | 0.5 |
| Main-thread | 1.3s | 1.00 |
| JS bootup | 0.1s | 1.00 |

### LCP element

همان `01-display-v2-420w.webp` روی `article.ms-hero-slide … img`.

### فازهای LCP (الگو همان runA)

Render Delay همچنان بخش بزرگ (~36٪ کلاس)؛ TTFB این اجرا بالاتر دیده شد (server response 440ms در audit) ولی FCP/LCP نهایی همان ~1.8s ماند.

### Main-thread

| Group | ms |
|-------|-----|
| Other | 527.7 |
| **Style & Layout** | **496.3** |
| Rendering | 146.8 |
| Script Evaluation | 89.5 |
| **Parse HTML & CSS** | **15.9** |
| Script Parse/Compile | 4.7 |
| GC | 3.2 |

### Long tasks

1. @1207.8ms · 339.6ms · **styleLayout 327** · paint 7.6 · other 4.1 · script 0.9  
2. @1709.5ms · 105.3ms · other 103.4  
3. @1633.1ms · 75.9ms · scriptEvaluation 55.8 · styleLayout 2.9  
4. @1559.8ms · 52.2ms · **styleLayout 50.8**

### Audits ناقص (همان الگوی runA)

FCP 0.38 · LCP 0.69 · SI 0.55 · DOM 0.5 · unused-css 18KiB 0.5 · use-cdn 17 resources 0.53 · max-FID 0.95 · TTI 0.97

## B4. جمع‌بندی عددی قبل/بعد برای Codex

| شاخص | Q8T2c8aj (قبل) | gV9gP6Ev | AFqU0xtT |
|------|----------------|----------|----------|
| Perf | 82 | 82 | 82 |
| Struct | 98 | 98 | 98 |
| FCP | 1809 | 1790 | 1811 |
| LCP | 1809 | 1790 | 1811 |
| SI | 2138 | 2143 | 2158 |
| TBT | 0 | 21 | 0 |
| CLS | 0.00174 | 0.00172 | 0.00172 |
| styleLayout ms | 471 | 534 | 496 |
| parseHTML ms | 16 | 16 | 16 |
| long-task#1 styleLayout | 314 | 339 | 327 |
| LCP file | display-v2-420w | display-v2-420w | display-v2-420w |

**نتیجه تصادفی بهتر را تنها نتیجه معرفی نکن — هر دو موفق 82 هستند.**

## B5. آنچه این چرخه ثابت کرد / رد کرد

| ادعا | وضعیت |
|------|--------|
| long-task از parse CSS است | **رد** (parse≈16ms؛ styleLayout≈300ms+) |
| TBT=0 یعنی کار سنگین نیست | **رد** (کار قبل از FCP) |
| بستن بریدگی هیرو با فضای ۳ خط بدون clamp | **تأیید UX** |
| اشتراک inflight Stories.json | **تأیید** (1 request حتی با پاسخ کند) |
| content-visibility باعث Layout↓ محلی می‌شود | **تأیید محلی** (dirty 1760→818) |
| content-visibility Perf/FCP Seattle را بالا می‌برد | **رد در این دو GT** |
| Grade A رسیده‌ایم | **رد — هنوز B/82** |

---

# بخش ۳ — دستور کار پیشنهادی برای Codex بعدی (خلاصه محدودیت‌ها)

- کار را خودت انجام بده؛ انتقال فایل/دستور به کاربر نسپار.
- Repo/URL/Source/Publish همان بالا؛ RELEASE فعلی `4934300`؛ nginx md5 و gzip6 را ثبت کن.
- دامنه/DNS/CDN/گواهی جدید خارج از محدوده؛ HTTPS/HTTP2 حفظ.
- ظاهر/متن/تصاویر/انیمیشن/امکانات حفظ؛ 8+8+12؛ dual؛ اولین کلیک استوری/منو/سبد/ورود؛ بستن overlay.
- preload و display-v2 را بدون شاهد جدید دست نزن.
- CSS را با coverage یک viewport حذف نکن.
- قبل از publish رسمی: تغییرات در **main**؛ quick-republish رسمی؛ بدون reset مخرب.
- ≥۲ GTmetrix تازه Seattle روی HTTPS؛ جدول همهٔ اجراها + شکست‌ها؛ بدون ادعای A تا گزارش واقعی.
- گزارش کوتاه آخر: چه بسته شد، علت Style/Layout و آیا اهرم Seattle را تکان داد، SHA main/سرور + نسخه nginx، جدول GT، UX، باقی‌مانده.

**خط پایان این چرخه:** کار انجام‌شده = بستن دو رگرسیون + اصلاح تشخیص + content-visibility اندازه‌گیری‌شده + land روی main + publish رسمی `4934300`؛ زنده = همان SHA؛ بهترین GT واقعی = **B/82/98/LCP1.8s** ([gV9gP6Ev](https://gtmetrix.com/reports/185.93.88.81/gV9gP6Ev/) و [AFqU0xtT](https://gtmetrix.com/reports/185.93.88.81/AFqU0xtT/)). هدف A باز است؛ گلوگاه بعدی اندازه‌گیری‌شده = **FCP/Render Delay همراه Style/Layout تمام‌سند قبل از FCP** که اهرم فعلی Seattle را جابه‌جا نکرد.
