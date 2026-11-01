# هندآف — چرخه hero/stories/Style-Layout روی `4934300`

## هویت

| مورد | مقدار |
|------|--------|
| Repo | `mmovasseghi/Movasseghi-Store` |
| URL | `https://185.93.88.81/` |
| **main / publish RELEASE** | **`493430039be6dbb535cb15fa8cc47fadcda04310`** |
| nginx conf md5 | `a56672754fe83dcb2f370c30f2cac2b6` |
| gzip_comp_level | **6** (جدا از RELEASE برنامه؛ در conf ثبت‌شده) |
| HTTPS / HTTP2 | حفظ |
| مسیر انتشار | رسمی `deploy/linux/quick-republish.sh` از **main** (بدون patch شاخه) |
| backup | `/MOVASSEGHISTORE/backups-release.oqIEWTdL` |
| شواهد | `docs/performance/reference/cycle-style-layout/` |

---

## ۱) اصلاح تشخیص قبلی

Codex روی `gtmetrix-Q8T2c8aj-lighthouse.json` درست بود:

- long-task سند @1228.3ms / 332.7ms → **styleLayout 313.6ms** (نه parse)
- Parse HTML/CSS کل main-thread فقط **~16ms**
- TBT=0 ≠ نبود کار سنگین؛ کار عمده **قبل از FCP** است
- LCP image end ~1134ms؛ FCP=LCP ~1809ms → Style/Layout بعد از تصویر و قبل از paint

CDP محلی قبل از فیکس: قبل از FCP **Layout ~121ms** در برابر **UpdateLayoutTree ~20ms** و ParseAuthorStyleSheet ~1.4ms؛ layout root=`#document`، dirtyObjects≈1760، `partialLayout:false`.

---

## ۲) دو رگرسیون چگونه بسته شدند

### الف) بریدگی هیرو
- علت: `line-clamp:2` + `max-height:3.24em` — حتی با Vazirmatn متن پیش‌فرض scrollH≈62px / clientH≈41px در 320/360/390
- اصلاح: `min-height: calc(3 * 1.62em)` (=4.86em)، بدون clamp/overflow hidden
- تأیید بعد از publish: truncated=false برای Vazirmatn و Tahoma در 320/360/390؛ minH≈62.2px

### ب) Stories.json تکراری
- علت: `boot()` + `consumeEarlyStoryIntent()` هر دو `ensureStoriesLoaded()` بدون promise مشترک
- اصلاح: `storiesLoadPromise` مشترک؛ clear روی شکست برای retry؛ تصویر modal `loading=eager`
- تأیید: با Stories.json مصنوعی کند، mid-flight click → **starts=1, maxInflight=1**، modal open، `naturalWidth>0`

---

## ۳) Style/Layout — علت و اصلاح

| لایه | یافته |
|------|--------|
| تفکیک | Layout غالب است؛ Recalc Style کمتر؛ parse ناچیز |
| DOM | dirty full-document؛ root `#document` |
| اهرم | `content-visibility:auto` + `contain-intrinsic-size` اندازه‌گیری‌شده روی below-fold |

**محلی بعد از publish:** dirtyObjects 1760→818؛ Layout قبل FCP 121→79ms.  
**Seattle بعد از publish:** styleLayout هنوز ~496–534ms؛ FCP/LCP همچنان ~1.8s → **این اهرم نمره Seattle را جابه‌جا نکرد.** فرضیه محلی تأیید شد؛ معیار موفقیت Seattle (FCP/LCP) خیر.

CSS split critical/remainder آزمایش نشد (ریسک FOUC/دومین style pass بدون شاهد موفقیت Seattle).

---

## ۴) GTmetrix Seattle بعد از انتشار رسمی

| # | لینک | Grade | Perf | Struct | FCP | LCP | SI | TBT | CLS |
|---|------|-------|------|--------|-----|-----|----|-----|-----|
| A | https://gtmetrix.com/reports/185.93.88.81/gV9gP6Ev/ | B | **82** | **98** | 1.79s | 1.8s | 2.14s | 21ms | 0 |
| B | https://gtmetrix.com/reports/185.93.88.81/AFqU0xtT/ | B | **82** | **98** | 1.81s | 1.8s | 2.16s | 0ms | 0 |

میانه Perf=82؛ پراکندگی صفر بین دو اجرای موفق.  
یک تلاش میانی با `signup?job_limit=1` شکست خورد (محدودیت مهمان) و در جدول بالا نیست.

**هدف Grade A / Perf>95 حاصل نشده. ادعا نمی‌شود.**

افت همچنان از FCP/LCP/SI است (وزن‌ها: FCP10 / LCP25 / SI10 / TBT30 / CLS25).

---

## ۵) UX بعد از publish

- 8 featured + 8 offers + 12 stories + 3 hero slides
- استوری mid-flight / after-load: تصویر واقعاً load
- منو / سبد / ورود: اولین کلیک باز می‌شود؛ بستن drawer با backdrop؛ scroll آزاد بعد از بستن استوری
- هیرو بدون بریدگی جدید؛ نوار/فوتر بدون فضای خالی مشهود در اسکرین‌ها
- currentSrc LCP: `01-display-v2-420w.webp` (موبایل و دسکتاپ)

---

## ۶) باقی‌مانده برای چرخه بعد

1. Seattle FCP/LCP≈1.8s با Render Delay ~36–37٪ همچنان سقف است؛ content-visibility محلی کمک کرد ولی GT را تکان نداد.
2. اهرم بعدی باید HTML transfer / کار Layout تمام‌سند قبل از FCP را هدف بگیرد بدون حذف انیمیشن/تصویر و بدون FOUC.
3. دامنه/CDN خارج از محدوده.
4. Structure=98 پایدار.
