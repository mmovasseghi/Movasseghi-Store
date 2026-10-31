# CODEX HANDOFF — کامل (کار انجام‌شده + گزارش‌های واقعی GTmetrix)

تاریخ: ۲۰۲۶-۱۰-۳۱  
مخزن: `mmovasseghi/Movasseghi-Store`  
URL زنده: `https://185.93.88.81/`  
PR: https://github.com/mmovasseghi/Movasseghi-Store/pull/7  
شاخه: `cursor/story-cls-lcp-e83d`  
docs tip شاخه (آخرین commit docs): `a075119`  
**SHA منتشرشده روی سرور (RELEASE_COMMIT):** `07567ba61a59b846ce2845555f6c4cf1d427193c`  
docs tip گزارش‌شدهٔ قبلی روی main قبل از این چرخه: `d4ad797`  
release قبلی زنده: `31f01753449ab39d138cabbf197d56b915c9b4f8`

این سند **دو گزارش را یک‌جا** دارد:
1) گزارش کامل کار این ایجنت (تشخیص، اصلاح، تست، انتشار، محدودیت‌ها)  
2) متن/اعداد کامل قابل‌مشاهدهٔ گزارش‌های واقعی GTmetrix بعد از انتشار  

**اعلام Grade A یا Performance/Structure بالای ۹۵ ممنوع است** مگر گزارش واقعی بعدی آن را نشان دهد. عدد Lighthouse محلی = GTmetrix نیست.

---

# بخش A — گزارش کامل کار این چرخه

## A0. محدودیت‌های قطعی کاربر (هنوز معتبر)
- دامنهٔ `.com` بعداً خریده می‌شود → بررسی دامنه، DNS، خرید، CDN، صدور گواهی جدید، تغییر معماری SSL **جزو کار نیست**.
- HTTPS/HTTP2 فعلی و تمدید خودکار گواهی موجود **حفظ** شود؛ سایت به HTTP برنگردد.
- هدف وابسته به خرید دامنه نشود.
- طراحی، تصاویر، انیمیشن‌ها و امکانات حفظ شوند.
- عدد محلی/تخمینی را نتیجهٔ GTmetrix معرفی نکن.
- اصلاحات قبلی را کورکورانه تکرار/برنگردان: حذف preloadهای JS، تقسیم راه‌اندازی `home.js`، حفظ preload فونت/LCP، dual slider، HTTPS/HTTP2 انجام شده‌اند.

## A1. نقطهٔ شروع (قبل از این چرخه)
### GTmetrix واقعی قبلی روی HTTPS (مرجع کاربر)
| Report | Grade | Perf | Struct | LCP | TBT | CLS | تنظیمات |
|--------|-------|------|--------|-----|-----|-----|---------|
| https://gtmetrix.com/reports/185.93.88.81/SlGIpwvK/ | B | 71 | 98 | 2.5s | 40ms | 0 | Seattle / Chrome154 / LH12.6.1 |
| https://gtmetrix.com/reports/185.93.88.81/HFun8t5f/ | B | 75 | 98 | 2.4s | 0ms | 0 | تکرار |

جزئیات FCP / SI / TTFB / LCP phases / Waterfall این دو گزارش برای ناظر ناشناس در دسترس نبود (login-gated).  
اعداد گزارش‌های HTTP قبلی را به این دو نسبت نده. HTTP قبلی ≈84/93 و LCP≈1.8s بوده ولی URL/پروتکل عوض شده.

### شواهد محلی قبلی (صرفاً محلی — نه GT)
- Lighthouse mobile simulated سه اجرا: Perf≈94، LCP≈1324–1368ms، TBT≈10.5–47.5ms، SI≈2786–2862ms، CLS≈0.139–0.140
- تست استوری گاهی `open:false` داده بود.

### وضعیت سرور در شروع این چرخه
- تأیید شد: `RELEASE_COMMIT = 31f0175…`، سرویس active، خانه 200.
- `origin/main` در tip روی `d4ad797` (فقط docs بعد از 31f0175) بود؛ عقب‌گرد نشد.

## A2. اولویت ۱ — بازتولید و اثبات UX

### A2.1 استوری
**بازتولید روی live `31f0175`:**
- بعد از بارگذاری کامل: کلیک روی `.ms-story[data-story-id]` → `body.ms-story-open`، modal `is-open`، تصویر اسلاید حاضر. **کار می‌کند.**
- کلیک زودهنگام هنگام `readyState=loading`:  
  - `__msEarlyCapture` وجود داشت ولی فقط cart/auth/menu را می‌گرفت، **نه** `.ms-story`.  
  - در نتیجه intent ذخیره نمی‌شد؛ بعد از boot هم modal باز نمی‌شد.  
  - وجود ۱۲ استوری در DOM ≠ موفقیت تعامل.

**علت ریشه‌ای برنامه (نه باگ تست):**
- `stories.js` با `defer` می‌آید و listenerها دیر وصل می‌شوند.
- early-capture سربرگ فقط `[data-open-cart],[data-open-auth],[data-menu-toggle],.ms-drawer-quick-btn` را پوشش می‌داد.
- اسکریپت inline انتهای body همان early-capture را برمی‌دارد و فقط intent سبد/منو را replay می‌کند.

### A2.2 CLS موبایل ≈0.14
**بازتولید شرطی:**
| شرط | CLS تقریبی | توضیح |
|-----|------------|--------|
| موبایل بدون throttle | ~0.004 | تقریباً خوب |
| موبایل Slow 4G + CPU 4× (شبیه LH mobile) | **~0.136** | مشکل واقعی lab |
| دسکتاپ | ~0.002–0.005 | خوب |
| GTmetrix Seattle desktop قبلی | CLS 0 | با lab موبایل یکی نیست |

**Shift غالب throttled:** value≈0.127 در ~fonts.ready (~4.6s). گره‌ها: `#ms-features`, `.ms-features-band`, hero orbs/cta.  
**اندازه‌گیری فونت:** با Vazirmatn، `.ms-hero-desc` ≈41.4px؛ با Tahoma ≈62.2px → hero/~features حدود **+19.7px** جابه‌جا می‌شوند.

**علت ریشه‌ای CSS:**
```css
/* داخل @media(max-width:767px) روی خانه */
.ms-page-home .ms-hero-desc { line-clamp: 3; min-height: 0; }
```
این قانون خاص‌تر از قفل دوخطی عمومی بود → ارتفاع content-driven → swap فونت CLS می‌ساخت.  
انیمیشن/محتوا/فونت حذف نشد؛ فقط قفل هندسی پایدار شد.

## A3. اصلاحات کد (یک batch مرتبط)
Commit deploy: `07567ba`

### فایل‌ها
1. `src/MovasseghiShop.Web/Views/Shared/_Layout.cshtml`
   - early-capture شامل `.ms-story[data-story-id]`
   - `__msEarlyStoryCapture` جدا تا boot شدن `stories.js`
   - `__msEarlyStoryIntent` برای استوری
   - bootHeavy استوری را replay نکند
2. `src/MovasseghiShop.Web/wwwroot/js/stories.js`
   - `consumeEarlyStoryIntent()` → remove capture → `ensureStoriesLoaded()` → `open(id)`
3. `src/MovasseghiShop.Web/wwwroot/css/site.css`
   - `.ms-page-home .ms-hero-desc` → `line-clamp:2` + `min/max-height: calc(2 * 1.62em)` (+ display -webkit-box / overflow hidden)
4. باندل‌های بازتولیدشده: `critical-home.css`, `storefront-home.bundle.css`, `storefront.bundle.css`  
   (esbuild: `3.24em`)

### چیزهایی که عمداً دست نخورد
- preload فونت/LCP، حذف‌نشدن preloadهای JS قبلی، dual slider، HTTPS/HTTP2/nginx cert renew، کیفیت تصویر بدون دلیل، معماری CSS کامل inline خانه.

## A4. انتشار
- مسیر رسمی: `deploy/linux/quick-republish.sh` روی VPS.
- چون `quick-republish` فقط `git pull --ff-only origin main` می‌کند و commit هنوز روی feature بود، برای این deploy یک کپی اسکریپت با checkout به `origin/cursor/story-cls-lcp-e83d` اجرا شد (DB/App_Data/images/production settings/MinIO/cert حفظ).
- `RUN_OPTIMIZE_IMAGES=0` برای سرعت؛ تصاویر موجود کپی شدند.
- `nginx -t` OK؛ `verify-home`: featured:8 offers:8 stories:12 lcp:200.
- `RELEASE_COMMIT` روی سرور = `07567ba61a59b846ce2845555f6c4cf1d427193c`.
- HTTPS عمومی: HTTP/2 200، `strict-transport-security: max-age=0`، گواهی LE کوتاه‌عمر IP فعال (تأیید بدون اتکا به smoke `--insecure` به‌عنوان مدرک نهایی).
- backup/rollback مسیر: `backups-release.*` روی VPS.

**نکته برای Codex:** commitهای docs بعدی روی شاخه (`cce134d`, `83e75fe`, `a075119`) هنوز لازم نیست redeploy شوند مگر بخواهی docs روی سرور هم باشد؛ runtime همان `07567ba` است.

## A5. تأیید بعد از publish (UX)
| Probe | نتیجه |
|-------|--------|
| موبایل early story | هنگام loading: `earlyStoryIntent=true`, `aria-busy=true` → بعد boot: `bodyOpen` + `is-open` + `hasImg` |
| دسکتاپ early story | همان موفقیت |
| after-load story | باز می‌شود |
| موبایل CLS Slow4G+4×CPU | **~0.062** (از ~0.136) — desc قفل‌شده روی 41.5px / clamp 2 |
| دسکتاپ CLS | ~0.002 |
| باقی CLS throttled | ~0.06 از جابه‌جایی جزئی glyph / عرض band — در باند «خوب» lab؛ اختیاری برای بعد |

## A6. Preload / LCP attribution (محلی، تشخیصی)
Preload:
- `href=/images/products/0056/01-lcp.webp`
- `imagesrcset` شامل 64w…800w از جمله `01-display-v2-420w.webp` و `01-384w.webp`
- `imagesizes=(min-width: 768px) 420px, (max-width: 480px) 35vw, 240px`

| Viewport | currentSrc / LCP URL | دانلود اضافهٔ بلااستفادهٔ `01-lcp.webp`؟ |
|----------|----------------------|------------------------------------------|
| 412@2.625 | `01-384w.webp` | خیر — imagesrcset انتخاب کرد |
| 390@3 | `01-display-v2-420w.webp` | خیر |
| 1280 / 1920 | `01-display-v2-420w.webp` | خیر |

**نتیجه:** تفاوت href با LCP تاریخی به‌تنهایی باگ نیست؛ اصلاح preload جدید/تکراری اضافه نشد.

عنصر LCP محلی (desktop+mobile LH):  
`article.ms-hero-slide > div.ms-hero-grid > div.ms-hero-product > img`  
معمولاً `01-display-v2-420w.webp` (موبایل 412 گاهی `01-384w.webp`).

## A7. Lighthouse محلی سرد ×۳ (فقط تشخیص — مدرک Grade A نیست)
URL: `https://185.93.88.81/` ، متوالی، cache سرد مرورگر شبیه‌سازی‌شده.

| Run | Perf | FCP ms | SI ms | LCP ms | TBT ms | CLS | TTFB ms* |
|-----|------|--------|-------|--------|--------|-----|----------|
| desktop-1 | 0.86 | 1326 | 2825 | 1326 | 88 | 0.002 | ~224 |
| desktop-2 | 0.86 | 1313 | 2777 | 1313 | 82 | 0.002 | ~224 |
| desktop-3 | 0.85 | 1326 | 2822 | 1326 | 115 | 0.002 | ~224 |
| mobile-1 | 0.99 | 1278 | 2766 | 1278 | 6 | 0.043 | ~225 |
| mobile-2 | 0.97 | 1978 | 3155 | 1978 | 4 | 0.045 | ~237 |
| mobile-3 | 0.99 | 1299 | 2865 | 1299 | 1 | 0.042 | ~224 |

\* مقدار audit `server-response-time`؛ در metrics داخلی desktop-1: TTFB≈677، lcpLoadStart≈808، lcpLoadEnd≈1203 (شبیه‌سازی LH).

مشاهدهٔ محلی مهم: **SI دسکتاپ ≈2.8s** در حالی که FCP=LCP≈1.3s → کامل‌شدن بصری دیرتر از LCP؛ این سیگنال محلی است نه عدد Seattle GT.

## A8. پذیرش طراحی/عملکردی این چرخه
- 8 featured + 8 offers + 12 stories + LCP image: verify-home OK
- early click منو/سبد/ورود قبلاً توسط early-capture پوشش داشت؛ استوری الان اضافه شد
- scroll lock استوری: با `ms-story-open` و mutation observer موجود
- flash بی‌استایل دیده نشد (critical inline باقی است)
- محتوای تست مخفی/حذف نشد

## A9. مشکلات باقی‌مانده / دستور کار بعدی برای Codex (بدون حدس)
### باقی‌ماندهٔ قطعی از GTmetrix واقعی بعد از publish
- هنوز **Grade B**
- Performance **79–80** (هدف: A سپس >95)
- Structure **98** (نزدیک هدف >95؛ هنوز باید با Perf بالا بیاید برای Grade A)
- **LCP ثابت 2.1s** در هر دو اجرای Seattle (بهبود از 2.4–2.5 ولی ناکافی)
- TBT 40–58ms (خوب؛ وقت اصلی صرف کوتاه‌کردن TBT نشود مگر trace نشان دهد نمایش را عقب می‌اندازد)
- CLS در GT Seattle = 0 (مشکل CLS موبایل lab جداست و تا حد زیادی بسته شد)

### آنچه در گزارش GT هنوز خوانده نشد (مانع)
- تب‌های Performance جزئیات / Structure لیست / Waterfall / Video / FCP / Speed Index / TTFB / LCP phases برای کاربر ناشناس پشت **Login/Sign up** است.
- بدون دور زدن لاگین نمی‌توان فازهای LCP Seattle را از خود GT استخراج کرد.
- Codex باید یا با نشست معتبر موجود جزئیات را بخواند، یا با cold trace محلی/قابل‌مقایسهٔ شبکه ادامه دهد — منتظر فایل از کاربر نماند.

### مسیر اندازه‌گیری بعدی پیشنهادی (اولویت)
1. نسبت‌دادن LCP 2.1s Seattle: TTFB vs discovery vs download vs delay-to-render روی همان نسخه `07567ba`.
2. مخلوط نکردن mobile simulated با Seattle desktop.
3. سه اجرای متوالی cold؛ همه را گزارش کن نه فقط بهترین.
4. SI/FCP را هم اندازه بگیر؛ افت را فقط به LCP نسبت نده.
5. یک تغییر مرتبط؛ اگر بی‌اثر/مضر UX بود برگردان.
6. دامنه/CDN/SSL جدید پیشنهاد جایگزین کار قابل‌انجام نباشد.

### باقی اختیاری UX
- CLS throttled باقی ~0.06 (glyph / features-band width) — اختیاری؛ GT desktop CLS=0.

---

# بخش B — گزارش کامل GTmetrix دریافتی بعد از تغییرات

## B1. تنظیمات مشترک هر دو اجرا
- URL تست‌شده: **`https://185.93.88.81/`** (HTTPS — نه HTTP)
- Test Server Location: **Seattle, WA, USA**
- Browser: **Chrome 154.0.0.0**
- Engine: **Lighthouse 12.6.1**
- Connection UI پیش‌فرض صفحه: Unthrottled (همان تنظیمات ثبت‌شدهٔ عمومی رایگان)
- وضعیت حساب: **ناشناس / بدون لاگین** → بخش‌های عمیق گزارش قفل

## B2. اجرای ۱ — بعد از publish `07567ba`
**لینک:** https://gtmetrix.com/reports/185.93.88.81/POzYv29X/  
**زمان تولید گزارش (از صفحه):** Sat, Oct 31, 2026 9:31 AM -0700  

### خلاصهٔ هدر (متن قابل استخراج از صفحه)
```
Latest Performance Report for: https://185.93.88.81/
Test Server Location: Seattle, WA, USA
Using: Chrome 154.0.0.0, Lighthouse 12.6.1

GTmetrix Grade: B
Performance: 79%
Structure: 98%

Web Vitals:
  LCP: 2.1s
  TBT: 40ms
  CLS: 0
```

### آنچه صفحه صریحاً نشان می‌دهد / نشان نمی‌دهد
**دارد:** Grade حرف B، Performance 79%، Structure 98%، LCP، TBT، CLS، مکان، مرورگر، پیش‌نمایش اسکرین خانه (فروشگاه موثقی).  
**ندارد (قفل لاگین):** جزئیات تب Performance (FCP, SI, TTFB, LCP element/phases, long tasks)، Structure audits، Waterfall کامل، Video، History جزئی، CrUX جزئیات.

پیام صفحه: «Log in or Sign up to see the rest of the Report» / «Uncover full report details with a FREE GTmetrix account».

### اسکرین‌شات‌های ذخیره‌شده
- `/opt/cursor/artifacts/cycle4-ux/gtmetrix-POzYv29X-clean.png`
- `docs/performance/reference/cycle-07567ba/gtmetrix-POzYv29X.png`
- JSON: `docs/performance/reference/cycle-07567ba/gtmetrix-POzYv29X.json`

## B3. اجرای ۲ — مقایسه (همان تنظیمات، نه cherry-pick)
**لینک:** https://gtmetrix.com/reports/185.93.88.81/zLI2yUty/  
**زمان تولید گزارش:** Sat, Oct 31, 2026 9:46 AM -0700  

### خلاصهٔ هدر
```
Latest Performance Report for: https://185.93.88.81/
Test Server Location: Seattle, WA, USA
Using: Chrome 154.0.0.0, Lighthouse 12.6.1

GTmetrix Grade: B
Performance: 80%
Structure: 98%

Web Vitals:
  LCP: 2.1s
  TBT: 58ms
  CLS: 0
```

اسکرین/JSON:  
`docs/performance/reference/cycle-07567ba/gtmetrix-zLI2yUty.{png,json}`  
`/opt/cursor/artifacts/cycle4-ux/gtmetrix-zLI2yUty.*`

## B4. جدول مقایسهٔ کامل قبل/بعد (فقط GTmetrix واقعی)
| Report | زمان نسبی | Grade | Perf | Struct | LCP | TBT | CLS |
|--------|-----------|-------|------|--------|-----|-----|-----|
| SlGIpwvK (قبل، HTTPS) | قبل از این چرخه | B | 71 | 98 | 2.5s | 40ms | 0 |
| HFun8t5f (قبل، HTTPS) | قبل از این چرخه | B | 75 | 98 | 2.4s | 0ms | 0 |
| **POzYv29X (بعد `07567ba`)** | بعد publish | **B** | **79** | **98** | **2.1s** | **40ms** | **0** |
| **zLI2yUty (بعد `07567ba`)** | تکرار | **B** | **80** | **98** | **2.1s** | **58ms** | **0** |

### تفسیر صادقانه
- Performance از بازهٔ 71–75 به **79–80** آمد (~+4 تا +9 نقطه).
- LCP از 2.4–2.5s به **2.1s** آمد (~−0.3 تا −0.4s).
- Structure همان **98** ماند.
- Grade هنوز **B** است؛ هدف A و سپس Perf/Struct>95 **حاصل نشده**.
- بهبود احتمالاً ترکیبی از CLS/استوری و واریانس شبکه است؛ **نمی‌توان با اطمینان گفت تنها علت UX-CLS، LCP Seattle را این‌قدر جابه‌جا کرده** چون فازهای LCP GT خوانده نشد.
- TBT در بازهٔ سبز باقی است؛ گلوگاه اصلی عمومی هنوز **LCP 2.1s روی مسیر Seattle→IP** است.

## B5. چیزهایی که عمداً به این دو گزارش نسبت داده نمی‌شود
- اعداد HTTP قدیمی (≈84/93، LCP 1.8s)
- اعداد Lighthouse محلی (desktop ~86، mobile ~97–99)
- فرض اینکه عنصر LCP در Seattle لزوماً همیشه همان `01-display-v2-420w` است (محلی غالباً این است؛ GT جزئیات عنصر قفل است)

---

# بخش C — نقشهٔ سریع برای دستور بعدی Codex

## وضعیت فعلی سرور
- Live: `https://185.93.88.81/`
- RELEASE: `07567ba61a59b846ce2845555f6c4cf1d427193c`
- شاخهٔ کار/PR: `cursor/story-cls-lcp-e83d` / PR #7 (روی main هنوز merge نشده مگر کاربر merge کند)
- Source/Publish روی VPS: `/MOVASSEGHISTORE/src`, `/MOVASSEGHISTORE/publish`، Kestrel `127.0.0.1:5080`

## اسناد این چرخه
- `docs/performance/cycle-story-cls-07567ba.md`
- `docs/performance/CODEX-HANDOFF-cycle-story-cls-07567ba-COMPLETE.md` (همین فایل)
- `docs/performance/reference/cycle-07567ba/*`
- `docs/performance/README.fa.md` (شاهد POzYv29X به‌روز شد)

## کارهای بعدی مجاز/ممنوع (خلاصه)
**بکن:** اندازه‌گیری LCP/FCP/SI واقعی مسیر Seattle یا معادل؛ یک fix مرتبط؛ redeploy رسمی؛ GT تازه Seattle روی HTTPS؛ همهٔ اجراها را گزارش کن.  
**نکن:** خرید دامنه به‌عنوان راه‌حل؛ برگرداندن به HTTP؛ ادعای A بدون GT؛ دستکاری نمره؛ حذف انیمیشن/تصویر/فونت برای نمره؛ preload تکراری بدون اثبات؛ صرف TBT از صفر وقتی TBT سبز است.

## پیام صریح برای Codex
کار UX اولویت‌۱ (استوری early-click + CLS موبایل lab) انجام و منتشر شد.  
هدف Grade A / >95 هنوز باز است.  
مبنای اصلاح بعدی باید گزارش‌های واقعی  
https://gtmetrix.com/reports/185.93.88.81/POzYv29X/  
و  
https://gtmetrix.com/reports/185.93.88.81/zLI2yUty/  
با LCP=2.1s و Perf≈79–80 باشد — نه حدس، نه عدد محلی.
