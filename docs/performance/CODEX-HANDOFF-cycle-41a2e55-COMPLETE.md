# هندآف کامل Codex — چرخه `41a2e55` + گزارش‌های واقعی GTmetrix

این فایل **هم گزارش کار** و **هم گزارش‌های GTmetrix بعد از تغییر** را یک‌جا دارد.  
هدف بعدی را وابسته به خرید دامنه نکن. CDN / DNS / گواهی جدید / برگرداندن به HTTP خارج از محدوده است.

---

## A0. هویت محیط (همین الان)

| مورد | مقدار |
|------|--------|
| Repo | `mmovasseghi/Movasseghi-Store` |
| URL عمومی | `https://185.93.88.81/` |
| Source روی VPS | `/MOVASSEGHISTORE/src` |
| Publish روی VPS | `/MOVASSEGHISTORE/publish` |
| Kestrel | `127.0.0.1:5080` |
| **RELEASE_COMMIT زنده** | **`41a2e55c3aec15573addfb8452cbd3dbca676a90`** |
| docs tip شاخه | `96fd33c` (docs + gzip6؛ runtime همان `41a2e55`) |
| شاخه | `cursor/story-cls-lcp-e83d` |
| PR | https://github.com/mmovasseghi/Movasseghi-Store/pull/7 |
| HTTPS / HTTP2 | فعال؛ HTTP فقط ۳۰۱ به HTTPS |
| گواهی | Let’s Encrypt YE2، SAN=IP، کوتاه‌عمر؛ تمدید خودکار موجود حفظ شود |
| HSTS | `max-age=0` (عمدی برای IP cert) |
| مسیر انتشار رسمی | `deploy/linux/quick-republish.sh` (برای feature: checkout به `origin/cursor/story-cls-lcp-e83d` به‌جای `main`) |

**قبل از این چرخه روی سرور:** `07567ba` (استوری early-open + قفل CLS هیرو).  
**main در زمان کار هنوز:** `d4ad797` — عقب‌گرد نشده.

---

# بخش ۱ — گزارش کامل کار انجام‌شده

## A1. ورودی / محدودیت‌ها که رعایت شد

- هدف: Grade A واقعی GTmetrix سپس Perf و Structure >95، با حفظ طراحی/تصاویر/انیمیشن/امکانات.
- عدد محلی/تخمینی را به‌جای GTmetrix اعلام نکن؛ تا گزارش واقعی تأیید نکرده A/>95 ادعا نکن.
- دامنه `.com`، DNS، خرید، CDN، صدور گواهی جدید، تغییر معماری SSL: **خارج از کار**.
- HTTPS/HTTP2 و تمدید خودکار فعلی حفظ؛ سایت به HTTP برنگردد.
- اصلاحات قبلی را کورکورانه تکرار/برنگردان: حذف preload JS، تقسیم home.js، preload فونت/LCP، dual slider، HTTPS/HTTP2.

## A2. وضعیت شروع (قبل از این چرخه)

### GTmetrix Seattle HTTPS قبل از `41a2e55` (بعد از `07567ba`)

| لینک | Grade | Perf | Struct | LCP | TBT | CLS |
|------|-------|------|--------|-----|-----|-----|
| https://gtmetrix.com/reports/185.93.88.81/POzYv29X/ | B | 79 | 98 | 2.1s | 40ms | 0 |
| https://gtmetrix.com/reports/185.93.88.81/zLI2yUty/ | B | 80 | 98 | 2.1s | 58ms | 0 |

### قبل‌تر (HTTPS اولیه، نه مبنای این چرخه)

| لینک | Grade | Perf | Struct | LCP |
|------|-------|------|--------|-----|
| https://gtmetrix.com/reports/185.93.88.81/SlGIpwvK/ | B | 71 | 98 | 2.5s |
| https://gtmetrix.com/reports/185.93.88.81/HFun8t5f/ | B | 75 | 98 | 2.4s |

### تشخیص محلی قبل از فیکس (ابزار تشخیص، نه GT)

- موبایل 412@DPR2.625: `currentSrc` = **`01-384w.webp` (~45KB)** به‌جای **`01-display-v2-420w.webp` (~22KB)**.
- `msStoriesData` اینلاین ≈ **17713 بایت**.
- HTML خام ≈ 347KB؛ preload `href=01-lcp.webp` + `imagesrcset` درست بود (دانلود بلااستفادهٔ 01-lcp ثابت نشد).
- Priority1 قبلاً شipped بود: استوری early-open؛ CLS throttled موبایل از ~0.14 به ~0.06.

## A3. تغییرات کد این چرخه

### Commit runtime: `41a2e55`
**Prefer light studio 420w over heavier mid-width LCP candidates.**

1. **`ResponsiveImageService.cs`**  
   - اگر فایل studio/packed با عرض 420 سبک‌تر از کاندید mid-width باشد، اسلات‌های `<420w` که سنگین‌تر از آن ceiling هستند از srcset حذف می‌شوند.  
   - اثر: موبایل دیگر `384w` چاق را بر `display-v2-420w` ترجیح نمی‌دهد.

2. **`Views/Home/Index.cshtml`**  
   - بدنهٔ `<script id="msStoriesData">` خالی شد؛ فقط `data-stories-src="/Home/Stories.json"`.  
   - `stories.js` از قبل `ensureStoriesLoaded()` / fetch روی خالی‌بودن را پشتیبانی می‌کرد (early click هم با همان مسیر).

3. **`ResponsiveImageTests.cs`**  
   - تست domination؛ مجموعه ResponsiveImageTests: **۱۵/۱۵ پاس**.

### Commit docs/nginx: `1bd6a36` سپس `96fd33c`
- `gzip_comp_level` در `deploy/linux/nginx-movasseghi.conf`: **4 → 6**
- مستندات + شواهد در `docs/performance/cycle-lcp-srcset-41a2e55.md` و `reference/cycle-41a2e55/`

**عمداً انجام نشد:** کاهش کیفیت تصویر، حذف انیمیشن، حذف preload فونت/LCP، برگرداندن HTTP، CDN/دامنه، دست‌زدن به DB/App_Data/MinIO/cert.

## A4. انتشار

- روی VPS: fetch/reset به `origin/cursor/story-cls-lcp-e83d`، کپی `quick-republish` با جایگزینی `git pull origin main` → fetch/reset همان feature.
- `RUN_OPTIMIZE_IMAGES=0` (مشتق‌ها موجود؛ display-v2 حفظ).
- نتیجه: `Released 41a2e55…`؛ `verify-home`: featured:8 offers:8 stories:12 lcp:200 (Kestrel + nginx HTTPS با Host عمومی).
- بعداً فقط conf nginx با gzip6 reload شد؛ **RELEASE همچنان `41a2e55`**.
- کش صفحه nginx purge شد؛ HTML gzip عمومی از ~68717 به **63616** بایت رسید.

## A5. پذیرش UX بعد از publish (اثبات، نه حدس)

| آزمون | نتیجه |
|-------|--------|
| موبایل currentSrc | `01-display-v2-420w.webp` |
| دسکتاپ currentSrc | `01-display-v2-420w.webp` |
| استوری after-load موبایل | open=true، label مثلاً «همه محصولات» |
| استوری early-click موبایل | open=true (فوری/later) |
| استوری دسکتاپ after-load | open=true |
| stories inline bytes | **0** |
| CLS موبایل Playwright | ~0.026 |
| CLS دسکتاپ Playwright | ~0.005 |
| LH موبایل simulated ×3 | Perf **0.99**، LCP ~1274–1317ms، CLS ~0.044–0.048، SI ~2.75–2.83s |
| LH دسکتاپ cold ×3 | Perf **0.88**، FCP ~1421–1430، LCP ~1452–1462، SI ~1981–1995 |
| 8+8+12 + LCP image | verify-home OK |
| HTTPS عمومی با تأیید گواهی | HTTP 200، ssl_verify=0 |
| HTTP | 301 → HTTPS |

**نکته:** LH محلی مدرک Grade A نیست؛ فقط تشخیص.

## A6. مشکلات باز / باقی‌مانده (برای دستور بعدی Codex)

1. **هدف Grade A و Perf/Struct >95 هنوز حاصل نشده** — بهترین Seattle این چرخه **B / 82 / 98**.
2. **گلوگاه اثبات‌شده Seattle دسکتاپ = FCP/HTML+CSS، نه انتخاب اشتباه 384w.**  
   - LCP element در همهٔ GTهای موفق: همان `01-display-v2-420w.webp`.  
   - FCP≈LCP≈1.8s بعد از gzip6 → رنگ محتوا و LCP تقریباً هم‌زمان‌اند.  
   - فازهای LCP (نمونه Q8T2c8aj): TTFB ~37٪ / Load Delay ~12٪ / Load Time ~13٪ / **Render Delay ~37٪**.  
   - **اصلاح تشخیص (Codex روی lighthouse.json):** long-task سند @1228.3ms مدت 332.7ms عمدتاً **Style/Layout 313.6ms** است (Rendering 8.6 / Other 9.5 / ScriptEvaluation 1) — **نه** parse کردن ۱۵۰KB CSS. کل main-thread: Style/Layout **471.3ms** در برابر Parse HTML/CSS فقط **15.9ms**. task بعدی سند @1564.2ms هم ۶۵ms Style/Layout دارد. LCP image download end ~1134ms و FCP=LCP 1809ms → بعد از آماده‌شدن تصویر، Style/Layout پیش از اولین نمایش رخ می‌دهد. CSS بزرگ می‌تواند روی محاسبهٔ استایل اثر بگذارد، ولی علت دقیق از `styleLayout` استخراج می‌شود نه از parse. **TBT=0** به‌معنی نبود کار سنگین نیست؛ این taskها عمدتاً **قبل از FCP** بوده‌اند.
3. **critical-home.css ≈ 150KB** عمداً «استایل کامل خانه» اینلاین است (`build-critical-home.mjs`). تغییر معماری CSS فقط با حفظ first paint + همه اسلایدهای هیرو + اولین تعامل منو/سبد/ورود/استوری مجاز است؛ حذف coverage یک viewport ممنوع.
4. **یک اجرای GT با «Lighthouse Error: The page took too long to load (No CPU idle period)»** — احتمال نقش انیمیشن‌های infinite CSS؛ حذف انیمیشن ممنوع؛ اگر idle دوباره بشکند باید با اندازه‌گیری جدا شود.
5. **Structure=98**؛ فرصت باقی: `use-cdn` (۱۷ منبع، خارج از محدوده فعلی)، `unused-css-rules` ~15KiB (صرفه FCP ثابت نشده)، `dom-size` 1425.
6. **CLS دسکتاپ GT ≈0.002** از `h1.ms-hero-title > em` به‌خاطر Web font — کوچک؛ موبایل lab هنوز ~0.04–0.05.
7. دامنه/CDN را به‌جای کار فنی ننویس؛ اگر سقف RTT Seattle→IP باقی است با عدد بگو، ولی اول اهرم CSS/HTML را تمام کن.

## A7. کارهایی که عمداً تکرار/برنگشت نشدند

- حذف preloadهای JS / تقسیم home.js / dual / HTTPS-IP (قبلاً انجام شده).
- دستکاری کیفیت عکس بی‌دلیل.
- ادعا که LH موبایل 0.99 = Grade A.

---

# بخش ۲ — گزارش کامل GTmetrix بعد از تغییرات

همه روی **`https://185.93.88.81/`**، **Seattle, WA, USA**، **Chrome 154.0.0.0**، **Lighthouse 12.6.1**، دسکتاپ unthrottled (مگر خلافش گفته شود).  
تب‌های جزئی guest اغلب login-lock هستند؛ اعداد زیر از **هدر گزارش + `lighthouse.json` عمومی** استخراج شده‌اند.

## B1. جدول همهٔ اجراهای این چرخه (هیچ‌کدام را دور نریز)

| # | لینک | وضعیت | Grade | Perf | Struct | LCP | TBT | CLS | FCP | SI | توضیح |
|---|------|--------|-------|------|--------|-----|-----|-----|-----|-----|--------|
| 0a | https://gtmetrix.com/reports/185.93.88.81/POzYv29X/ | قبل تغییر | B | 79 | 98 | 2.1s | 40ms | 0 | — | — | baseline `07567ba` |
| 0b | https://gtmetrix.com/reports/185.93.88.81/zLI2yUty/ | قبل تغییر | B | 80 | 98 | 2.1s | 58ms | 0 | — | — | baseline |
| 1 | — | **شکست** | — | — | — | — | — | — | — | — | Analysis Error: **No CPU idle period** |
| 2 | https://gtmetrix.com/reports/185.93.88.81/5c8n84AR/ | بعد `41a2e55` | B | **78** | **98** | **2.1s** | 0ms | 0 | **1.9s** | **2.2s** | قبل gzip6 |
| 3 | https://gtmetrix.com/reports/185.93.88.81/o3qhorfs/ | بعد `41a2e55` | B | **78** | **98** | **2.2s** | 40ms | 0 | **1.9s** | **2.3s** | قبل gzip6 |
| 4 | https://gtmetrix.com/reports/185.93.88.81/8qGeUXxY/ | بعد `41a2e55` | B | **74** | **98** | **2.4s** | 0ms | 0 | **2.2s** | **2.5s** | قبل gzip6 |
| 5 | https://gtmetrix.com/reports/185.93.88.81/JVPFNhtX/ | بعد gzip6 | B | **81** | **98** | **1.8s** | 0ms | 0 | **1.8s** | **2.2s** | |
| 6 | https://gtmetrix.com/reports/185.93.88.81/Q8T2c8aj/ | بعد gzip6 | B | **82** | **98** | **1.8s** | 0ms | 0 | **1.8s** | **2.1s** | بهترین تا قبل از آخرین |
| 7 | https://gtmetrix.com/reports/185.93.88.81/u7ZOUnNi/ | **تازه‌ترین (درخواست کاربر)** | B | **82** | **98** | **1.8s** | ~40–43ms | 0 | **1.8s** | **2.1s** | تازه گرفته‌شده |

**بهترین Perf واقعی این چرخه: 82 — هنوز Grade A نیست. ادعای A ممنوع.**

---

## B2. گزارش کامل نمونهٔ عمیق — `Q8T2c8aj` (بهترین با LH JSON کامل ذخیره‌شده)

**لینک:** https://gtmetrix.com/reports/185.93.88.81/Q8T2c8aj/  
**URL تست:** `https://185.93.88.81/`  
**زمان:** 2026-11-01T04:01:18.987Z  
**مکان:** Seattle, WA, USA  
**مرورگر:** Chrome 154.0.0.0 / Lighthouse 12.6.1  
**formFactor:** desktop · throttlingMethod: provided  
**BenchmarkIndex:** ~2741.5  

### نمرات هدر

| مورد | مقدار |
|------|--------|
| GTmetrix Grade | **B** |
| Performance | **82%** |
| Structure | **98%** |
| LCP | **1.8s** |
| TBT | **0ms** |
| CLS | **0** (دقیق LH: 0.00174) |

### متریک‌های Performance (از lighthouse.json)

| متریک | مقدار | score جزئی |
|--------|--------|------------|
| FCP | **1.8s** (1809ms) | 0.38 |
| Speed Index | **2.1s** (2138ms) | 0.56 |
| LCP | **1.8s** (1809ms) | 0.69 |
| TTI | 1.8s | 0.97 |
| TBT | 0ms | 1.00 |
| CLS | 0.002 | 1.00 |
| Max Potential FID | 90ms | 0.97 |
| Server response (document) | 220ms | 1.00 |
| Total byte weight | 356 KiB | 1.00 |
| DOM size | **1425** elements | 0.5 |
| Main-thread work | 1.5s | 1.00 |
| JS bootup | 0.1s | 1.00 |

### عنصر LCP (ثابت در این چرخه)

- Selector: `article.ms-hero-slide > div.ms-hero-grid > div.ms-hero-product > img`
- فایل: **`/images/products/0056/01-display-v2-420w.webp`**
- نه `01-lcp.webp`، نه `01-384w.webp`
- `loading=eager` · `fetchpriority=high` · sizes هیرو استاندارد

### فازهای LCP (`Q8T2c8aj`)

| Phase | % | ms |
|-------|---|-----|
| TTFB | 37% | ~676 |
| Load Delay | 12% | ~226 |
| Load Time | 13% | ~232 |
| **Render Delay** | **37%** | **~675** |

تفسیر: بعد از آمدن بایت‌های تصویر، **Render Delay** هم‌وزن TTFB است → کار استایل/layout/paint سند (CSS اینلاین بزرگ) نمایش LCP را عقب می‌اندازد. فقط کوچک‌کردن فایل LCP کافی نیست.

### Layout shift (۱ مورد، کوچک)

- Node: `div.ms-hero-grid > div.ms-hero-copy > h1.ms-hero-title > em`
- Cause: **Web font loaded** (`Vazirmatn-storefront.woff2`)
- Score: ~0.00174 → در هدر CLS=0 گزارش می‌شود

### Long tasks (`Q8T2c8aj`)

| شروع تقریبی | مدت | منبع |
|-------------|-----|------|
| 1.23s | **333ms** | سند `/` (HTML+CSS اینلاین) |
| 1.73s | 93ms | `home.js` |
| 1.56s | 84ms | سند `/` |
| 1.65s | 55ms | `stories.js` |

TBT همچنان 0 چون gap بین long taskها اجازهٔ ورودی می‌دهد؛ اما long task سند با FCP/LCP هم‌پوشانی دارد.

### Structure / audits مهم

| Audit | وضعیت |
|-------|--------|
| HTTP/2 | پاس |
| Text compression | پاس |
| Redirects | پاس (۰ صرفه‌جویی) |
| Render-blocking | پاس (۰ms) |
| Prioritize LCP image | پاس |
| Responsive images | پاس |
| Unused JS | پاس |
| **Unused CSS** | ~**15 KiB** potential (score 0.5) — به‌تنهایی تشخیص FCP نیست |
| **Use a CDN** | ۱۷ منبع (خارج از محدودهٔ دامنه/CDN فعلی) |
| DOM size | 1425 (med) |
| Third-party | تقريباً صفر مسدودسازی |

### اندازه صفحه (حدود)

- Total ≈ **356–362 KiB**
- HTML gz بعد از gzip6 ≈ **64 KiB** (اندازه‌گیری مستقیم agent: 63616)
- CSS اینلاین خام داخل HTML ≈ **150 KiB**
- LCP image display-v2 ≈ **22 KB**
- فونت storefront ≈ **86 KB**

---

## B3. گزارش کامل `JVPFNhtX` (همسایه gzip6)

**لینک:** https://gtmetrix.com/reports/185.93.88.81/JVPFNhtX/  
Perf **81** / Struct **98** / LCP **1.8s** / TBT 0 / CLS 0  
FCP **1.8s** / SI **2.2s**  
Server response document این اجرا **440ms** (بالاتر از Q8؛ نویز شبکه).  
فاز LCP: TTFB **49٪ (~897ms)** · Load Delay 1٪ · Load Time 12٪ · Render Delay **38٪ (~697ms)**.  
همان LCP element display-v2. Long task سند ~348ms + home.js ~118ms.

---

## B4. گزارش‌های بلافاصله بعد از `41a2e55` و قبل از gzip6

### `5c8n84AR` — https://gtmetrix.com/reports/185.93.88.81/5c8n84AR/
- B / **78** / 98 / LCP **2.1s** / TBT 0 / CLS 0  
- FCP **1.9s** / SI **2.2s** / Total ~362 KiB  
- فاز LCP: TTFB 31٪ (663ms) · Load Delay 12٪ · Load Time 21٪ (439ms) · Render Delay **37٪ (778ms)**  
- HAR: HTML gzip ~69KB، connect+TLS~442ms، wait~219، receive~470؛ تصویر LCP ~22KB / ~440ms  
- Long tasks: سند **357ms**، home.js 87ms، scroll-reveal 57ms  

### `o3qhorfs` — https://gtmetrix.com/reports/185.93.88.81/o3qhorfs/
- B / **78** / 98 / LCP **2.2s** / TBT 40ms / CLS 0  
- FCP **1.9s** / SI **2.3s**  
- LCP element: display-v2  

### `8qGeUXxY` — https://gtmetrix.com/reports/185.93.88.81/8qGeUXxY/
- B / **74** / 98 / LCP **2.4s** / TBT 0 / CLS 0  
- FCP **2.2s** / SI **2.5s**  
- بدترین نویز این دسته؛ باز هم همان LCP element  

### شکست idle
- پیام: `Lighthouse Error: The page took too long to load (No CPU idle period)`  
- نمره ندارد؛ در جدول همهٔ اجراها نگه داشته شود.

---

## B5. گزارش تازهٔ درخواستی کاربر — `u7ZOUnNi`

**لینک:** https://gtmetrix.com/reports/185.93.88.81/u7ZOUnNi/  
**تولید:** Sat, Oct 31, 2026 9:07 PM -0700 (زمان سرور GTmetrix)  
**مکان/موتور:** Seattle · Chrome 154 · LH 12.6.1  
**URL:** `https://185.93.88.81/`  

| مورد | مقدار |
|------|--------|
| Grade | **B** |
| Performance | **82%** |
| Structure | **98%** |
| LCP | **1.8s** |
| TBT | **~40–43ms** |
| CLS | **0** |
| FCP (LH) | **1.8s** |
| Speed Index (LH) | **2.1s** |

`lighthouse.json` این اسلاگ کمی بعد از تولید با 403 بسته شد؛ هدر UI + استخراج همان جلسه معتبر است. الگوی متریک با `Q8T2c8aj` یکی است.

---

## B6. جمع‌بندی علّی برای Codex (از روی GT، نه حدس)

1. **انتخاب منبع LCP در Seattle دسکتاپ درست است** (`display-v2-420w`). فیکس srcset برای موبایل ضروری بود و در lab موبایل Perf را به ~0.99 رساند، ولی نمرهٔ Seattle دسکتاپ را به‌تنهایی به A نمی‌رساند.  
2. **FCP و LCP بعد از gzip6 هر دو ~1.8s** → مسیر رنگ اول همان مسیر محدودیت نمره است.  
3. **Render Delay LCP ~37–38٪** + **long task سند ~330–350ms** → هزینهٔ HTML با **~150KB CSS اینلاین کامل خانه**.  
4. gzip6 HTML را از ~69KB به ~64KB gz آورد و Perf را از باند 74–78 به **81–82** برد؛ برای A کافی نیست.  
5. Structure از قبل ۹۸ است؛ تمرکز بعدی Performance/FCP است نه TBT (TBT اغلب ۰ است).  
6. CDN در Structure دیده می‌شود ولی **خرید دامنه/CDN دستور کار این هندآف نیست** مگر کاربر صریحاً باز کند.

---

## B7. مسیر پیشنهادی دستور بعدی (قابل اندازه‌گیری)

1. یک تغییر مرتبط روی **هزینهٔ first paint**: جداکردن استایل below-the-fold از critical اینلاین، با حفظ ظاهر اولین نمایش، همه اسلایدهای هیرو، و اولین کلیک منو/سبد/ورود/استوری.  
2. سه اندازه‌گیری همسان محلی (دسکتاپ و موبایل جدا) سپس Seattle GT HTTPS ≥2 اجرا؛ **همهٔ اجراها** را گزارش کن.  
3. اگر تغییر بی‌اثر/مضر بود برگردان.  
4. تا GT واقعی A یا >95 ندادی ادعا نکن.  
5. شواهد را در `docs/performance/` ذخیره کن.

---

## B8. مسیر فایل‌های شاهد

- `docs/performance/cycle-lcp-srcset-41a2e55.md`
- `docs/performance/CODEX-HANDOFF-cycle-41a2e55-COMPLETE.md` (همین فایل)
- `docs/performance/reference/cycle-41a2e55/`  
  (ux-report، اسکرین استوری/CLS/LCP، LH JSONهای GT، HAR مربوط به 5c8n84AR، خلاصه LH محلی)

---

**خط پایان برای Codex:** کار انجام‌شده = `41a2e55` + gzip6؛ زنده = همان SHA؛ بهترین GT واقعی = **B/82/98/LCP1.8s** ([Q8T2c8aj](https://gtmetrix.com/reports/185.93.88.81/Q8T2c8aj/) و [u7ZOUnNi](https://gtmetrix.com/reports/185.93.88.81/u7ZOUnNi/)). هدف A باز است؛ گلوگاه بعدی اندازه‌گیری‌شده = **FCP/Render Delay ناشی از HTML+CSS اینلاین ~150KB**، نه انتخاب اشتباه تصویر LCP در دسکتاپ Seattle.
