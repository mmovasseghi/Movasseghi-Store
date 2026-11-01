# هندآف کامل Codex — چرخه آزمایش critical-split (روی پایه `4934300`)

این فایل **گزارش کار + نتایج اندازه‌گیری ایزوله + GTmetrix تازه** را یک‌جا دارد.
**نمونهٔ critical کوچک‌تر منتشر نشد** چون سود خالص FCP/LCP نداشت.
**Grade A اعلام نمی‌شود.**

---

## A0. هویت

| مورد | مقدار |
|------|--------|
| Repo | `mmovasseghi/Movasseghi-Store` |
| URL | `https://185.93.88.81/` |
| **RELEASE زنده (بدون تغییر runtime)** | **`493430039be6dbb535cb15fa8cc47fadcda04310`** |
| main هنگام شروع | `1baea52` |
| شاخه آزمایش | `cursor/critical-css-split-e83d` |
| nginx md5 | `a56672754fe83dcb2f370c30f2cac2b6` · gzip **6** |
| HTTPS/HTTP2 | حفظ |
| شواهد | `docs/performance/reference/cycle-critical-split/` |
| اسکریپت اندازه‌گیری | `scripts/measure-first-paint-nav.mjs` |
| پروکسی A/B | `scripts/ab-css-proxy.mjs` |
| سازندهٔ نمونه | `scripts/build-critical-home-split.mjs` (فقط sidecar) |

هیرو بدون بریدگی و Stories promise مشترک از چرخه قبل **حفظ** شدند (روی سرور همان `4934300`).

---

# بخش ۱ — آزمایش مشخص

## فرضیه

`build-critical-home.mjs` کل `storefront-home.bundle.css` (~150KB) را در head اینلاین می‌کند.
**فرضیه:** critical کوچک‌تر (اولین viewport + chrome + تعامل اول) + بارگذاری فوری remainder غیرمسدودکننده، حجم HTML قبل از محتوای صفحه را کم می‌کند و دریافت/نمایش اولین viewport را جلو می‌اندازد.

این فرضیه ≠ «parse CSS عامل 333ms است» (آن ادعا قبلاً رد شده؛ parse≈16ms).

## نمونه چه بود

- **critical-home-first:** ~104KB (فونت، متغیر، reset، هدر، استوری، هیرو، فیچرز نزدیک fold، drawer/auth/cart/tabbar، keyframes)
- **remainder:** ~46KB (offers/cat/featured/segments/news/closing/faq/footer و …)
- بارگذاری remainder: `<link media="print" onload="this.media='all'">` فوری در head (بدون timeout/idle/interaction)
- noscript: لینک remainder
- cascade: فقط قوانین below-fold-only به remainder؛ گروه‌های mixed در critical ماندند
- حذف coverage یک viewport: نشد

### اصلاح مرتبط پس از شکست اول
بدون استایل below-fold، dirtyObjects قبل از FCP از ~800 به **2394** جهش کرد.
نسخهٔ دوم: scaffolding `content-visibility` / `contain-intrinsic-size` داخل critical ماند؛ ظاهر below-fold در remainder.

## روش اندازه‌گیری (قابل تکرار)

- اسکریپت: `scripts/measure-first-paint-nav.mjs`
- همیشه `page.goto` سرد؛ **بدون setContent / تزریق روی صفحهٔ ازپیش‌بارگذاری‌شده**
- `Network.clearBrowserCache` + `setCacheDisabled`
- viewport دسکتاپ 1366×768
- کنترل و آزمایش هر دو از پروکسی محلی یکسان (`ab-css-proxy.mjs`) به origin زنده تا مسیر شبکه قابل مقایسه باشد
- HTML پروکسی با `accept-encoding: identity` بازنویسی می‌شود (اندازهٔ خام قابل مقایسه؛ نه جایگزین Seattle gzip)
- تنظیمات هر اجرا در `*-settings.json` کنار شواهد

### خط پایهٔ زندهٔ HTTPS (gzip واقعی، نه پروکسی)

| اجرا | nav TTFB | htmlEnd | htmlTransfer | inlineCSS | FCP | LCP | CLS | Layout قبل FCP | dirty |
|------|----------|---------|--------------|-----------|-----|-----|-----|----------------|-------|
| live-1 | 673 | 1127 | **63953** | 150310 | 1384 | 1384 | 0.0017 | 88 | 812 |
| live-2 | 677 | 1135 | 63953 | 150310 | 1384 | 1384 | 0.0017 | 94 | 812 |
| live-3 | 674 | 1130 | 63953 | 150310 | 1408 | 1408 | 0.0017 | 99 | 812 |
| **میانه** | **674** | **1130** | **63953** | | **1384** | **1384** | **0.0017** | | |

جداسازی: `navigationTTFB` (=responseStart کامل nav) ≠ server-response-time audit تنها.

توجه: FCP محلی ~1.4s ≠ Seattle ~1.8s؛ عدد محلی را Grade نخوان.

## جدول کنترل در برابر نمونه (پروکسی یکسان)

| نسخه | میانه htmlTransfer | inlineCSS | FCP | LCP | CLS | Layout قبل FCP | dirty top |
|------|-------------------|-----------|-----|-----|-----|----------------|-----------|
| **control** (complete inline) | 325675 | 150310 | **1968** | **2744** | 0.013 | ~100 | ~815 |
| **exp1** (split head rem) | 279389 | 103753 | **1980** | **2752** | 0.012 | ~112 | **2394** |
| **exp2** (split + CV در critical) | 279993 | 104357 | **2056** | **2864** | 0.013* | ~93 | ~829 |

\*exp2-run2: LCP 7044 / CLS 0.086 — رگرسیون هنگام اعمال remainder.

### نتیجهٔ تصمیم
- HTML خام ≈46KB کوچک‌تر شد، ولی **FCP/LCP بهتر نشد** (مساوی یا بدتر).
- exp1: هزینهٔ Layout/dirty قبل از FCP بدتر شد (DOM بدون استایل below-fold).
- exp2: dirty درست شد، ولی FCP هنوز بهتر از control نیست؛ یک اجرا CLS/LCP معیوب.
- TBT تقریبی در هر دو ≈0؛ بهبود FCP با TBT/CLS بد قبول نبود.
- **نمونه منتشر نشد. Layout production همان complete inline باقی ماند.**

### علت شکست از timeline
در پروکسی، `htmlResponseEnd ≈ navigationTTFB` → گلوگاه دریافت بدنه نیست؛ کوچک‌کردن HTML پیش از محتوا FCP را جابه‌جا نکرد.
remainder با `media=print` دیرتر از FCP می‌رسید (~3.1s) ولی اولین Layout (dirty 2394) **قبل از رسیدن remainder** و به‌خاطر CSS ناقص روی DOM کامل بود.

---

# بخش ۲ — content-visibility واقعیت

روی زنده `4934300`، دسکتاپ 1366×768، بدون خواندن هندسهٔ فرزندان skipped:

| بخش | content-visibility | در/نزدیک viewport | checkVisibility |
|------|--------------------|-------------------|-----------------|
| #ms-hero | visible | بله | true |
| #ms-features | visible | خیر (پایین fold) | true |
| .ms-offers | **auto** | خیر | **false** (skipped) |
| .ms-cat-section | auto | خیر | false |
| #ms-featured | auto | خیر | false |
| #ms-segments | auto | خیر | false |
| .ms-closing | auto | خیر | false |
| .ms-news | auto | خیر | false |
| .ms-home-faq | auto | خیر | true (مرورگر skip نکرد) |
| footer | auto | خیر | true (مرورگر skip نکرد) |

→ «پایین صفحه» ≠ حتماً skipped. FAQ/فوتر با `auto` هنوز visible بودند.
گسترش CV بدون شاهد جدید انجام نشد.
اسکرول سریع به featured/فوتر و تصاویر هنگام ورود: تصاویر featured complete با naturalWidth>0.

---

# بخش ۳ — UX / انتشار

## UX (سرور بدون تغییر split)
همان پذیرش چرخه قبل روی `4934300` برقرار است: هیرو کامل، 8+8+12، dual، استوری یک fetch، منو/سبد/ورود، overlay.
چون runtime عوض نشد، اسکرین جدید اجباری برای split نیست؛ live intact.

## انتشار
**انتشار رسمی split انجام نشد.**  
SHA زنده همچنان `4934300`.  
تغییرات این شاخه: ابزار اندازه‌گیری، sidecarهای آزمایش، مستندات، توضیح Layout — بدون فعال‌سازی remainder در production.

---

# بخش ۴ — GTmetrix Seattle تازه (همان RELEASE بدون split)

| # | لینک | وضعیت | Grade | Perf | Struct | LCP | TBT | CLS | FCP | SI |
|---|------|--------|-------|------|--------|-----|-----|-----|-----|-----|
| 1 | https://gtmetrix.com/reports/185.93.88.81/1T9pqHjZ/ | موفق | B | **81** | **98** | **1.9s** | 28ms | 0 | 1860ms | 2179 |
| 2 | https://gtmetrix.com/reports/185.93.88.81/jxkvddVe/ | موفق | B | **77** | **98** | **2.1s** | 36ms | 0 | 2094ms | 2343 |

میانه Perf این دو: **79**. پراکندگی نسبت به baseline قبلی (82) = واریانس Seattle روی همان کد؛ **بهبود آزمایش split نیست** (split منتشر نشد).

1T9pqHjZ: styleLayout 520ms · parseHTML 17 · longtask#1 styleLayout 305 @1304ms  
jxkvddVe: styleLayout 662ms · parseHTML 24 · longtask#1 styleLayout 435 @1232ms  

**هدف A / >95 حاصل نشده.**

---

# بخش ۵ — باقی‌مانده برای Codex بعدی

1. گلوگاه Seattle همچنان FCP/LCP≈1.8–2.1s با Render Delay و Style/Layout قبل از FCP؛ کوچک‌کردن inline CSS به‌تنهایی در این آزمایش سود خالص نداشت.
2. اهرم بعدی باید چیزی غیر از «همان split بدون شاهد» باشد — مثلاً کاهش کار Layout تمام‌سند / هزینهٔ دریافت واقعی gzip HTML در مسیر Seattle، بدون FOUC و بدون حذف انیمیشن.
3. content-visibility را راه‌حل امتیاز معرفی نکن؛ روی FAQ/فوتر حتی skipped نیست.
4. دامنه/CDN خارج از محدوده.
5. هیرو/Stories فیکس‌شده را برنگردان.

**خط پایان:** آزمایش critical-split **اندازه‌گیری و رد شد**؛ منتشر نشد؛ زنده=`4934300`؛ GT تازه B/81 و B/77؛ هدف A باز است.
