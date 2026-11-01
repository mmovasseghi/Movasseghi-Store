# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-01 (پس از publish `c53290e`)

## نسخهٔ زنده

| مورد | مقدار |
|------|--------|
| **RELEASE** | **`c53290e0a8d554df0311075be7802204b5dfe773`** |
| URL | `https://185.93.88.81/` |
| nginx md5 | `a56672754fe83dcb2f370c30f2cac2b6` · gzip **6** |
| شاخه | تغییرات روی `main` (فیچر `cursor/valid-ab-html-gzip-e83d` ادغام شد) |

## اصلاح منتشرشده این چرخه

1. `content-visibility:hidden` روی اسلاید هیرو غیرفعال
2. `content-visibility:auto` روی `#ms-features`
3. `content-visibility:hidden` روی `.ms-drawer[hidden]` (قبلاً با `display:block!important` Layout کامل می‌شد)
4. harness معتبر A/B: `prepare-ab-variants` + `ab-prebuilt-server` (gzip6 ازپیش‌ساخته؛ پروکسی معیوب منسوخ)

## A/B معتبر (قبل از publish)

| آزمایش | نتیجه |
|--------|--------|
| critical-split gzip6 ازپیش‌ساخته | رد — FCP میانه برابر 944 |
| layout-CV + drawer | پذیرفته — dirty 654→371، Layout 88→61، FCP 924→904، CLS بهتر، TBT≈0 |

## اندازه‌گیری زنده پس از publish (لوکال cold×3)

| | TTFB | htmlEnd | FCP | LCP | Layout قبل FCP | dirty | CLS |
|--|------|---------|-----|-----|----------------|-------|-----|
| میانه `c53290e` | 685 | 1145 | **1392** | **1392** | **~84** | **535** | 0.0011 |
| مرجع قبلی `4934300` (همین مسیر) | 685 | 1152 | 1416 | 1416 | ~100 | 818 | 0.0017 |

## GTmetrix Seattle پس از publish

| # | لینک | وضعیت | Grade | Perf | Struct | LCP | TBT | CLS |
|---|------|--------|-------|------|--------|-----|-----|-----|
| 1 | https://gtmetrix.com/reports/185.93.88.81/xfrCIxd1/ | موفق | B | **76** | **98** | **2.1s** | **0ms** | 0 |
| 2 | Retest / تست تازه | **مسدود سهمیه guest** (`job_error` / برگشت به خانه بدون گزارش) | — | — | — | — | — | — |

جزئیات FCP/SI/Lighthouse کامل پشت دیوار ورود guest است؛ عدد Performance/LCP/TBT/CLS از صفحهٔ عمومی خوانده شد.
**Grade A اعلام نمی‌شود.**

## فرضیه‌های ردشده

- critical-split با روش معتبر (این چرخه)
- critical-split با پروکسی معیوب (چرخه قبل)

## مانع بیرونی

سهمیهٔ تست guest GTmetrix اجازهٔ اجرای دوم Seattle را نداد. برای ادامهٔ حلقه با تأیید Grade واقعی: حساب/سهمیه GTmetrix یا صبر تا رفع محدودیت guest لازم است.

## اقدام بعدی

1. تکرار Seattle ×۲ وقتی سهمیه آزاد شد (روی همان `c53290e` یا نسخهٔ بعدی).
2. گلوگاه باقی‌مانده از تنها گزارش تازه: **LCP≈2.1s** (TBT الان 0). اهرم بعدی باید دریافت/Render مسیر Seattle یا کار Style باقی‌مانده باشد؛ تکرار split بدون شاهد تازه ممنوع.
3. UX زنده تأیید شد: هیرو کامل، ۱۲ استوری، منو open/close با CV، قوانین CSS در HTML.
