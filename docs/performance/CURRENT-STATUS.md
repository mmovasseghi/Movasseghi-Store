# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-01 (چرخه valid-ab + layout-CV)

## نسخهٔ زنده (قبل از publish این چرخه)

| مورد | مقدار |
|------|--------|
| RELEASE | `493430039be6dbb535cb15fa8cc47fadcda04310` |
| URL | `https://185.93.88.81/` |
| بهترین GT تأییدشده | B / Perf≈79–82 / Struct 98 (Seattle) |
| شاخهٔ کار | `cursor/valid-ab-html-gzip-e83d` |

## فرضیهٔ فعال (پذیرفته‌شده برای publish)

کاهش Style/Layout قبل از FCP با:
1. `content-visibility:hidden` روی `.ms-hero-slide:not(.is-active)`
2. `content-visibility:auto` روی `#ms-features`
3. `content-visibility:hidden` روی `.ms-drawer[hidden]` (که با `display:block!important` هنوز Layout می‌شد)

## آزمایش‌های ردشده

| آزمایش | علت رد |
|--------|--------|
| critical-split (چرخه قبل، پروکسی معیوب) | روش اندازه‌گیری: دانلود کامل origin با identity سپس rewrite |
| critical-split (چرخهٔ valid، gzip6 ازپیش‌ساخته روی VPS) | `htmlTransfer` ≈8KB کمتر شد؛ **FCP/LCP بهتر نشد** (میانه FCP هر دو 944ms). `htmlEnd`≈همان |

## اندازه‌گیری معتبر (الزامی)

- `scripts/prepare-ab-variants.mjs` — ساخت کنترل/نمونه + gzip6 قبل از انتقال
- `scripts/ab-prebuilt-server.mjs` — سرو از دیسک با `Content-Encoding:gzip` + stream-proxy دارایی‌ها
- `scripts/ab-css-proxy.mjs` — **منسوخ** (بدون `MS_ALLOW_LEGACY_AB_PROXY=1` خارج می‌شود)

شواهد: `docs/performance/reference/cycle-valid-ab/` و `/opt/cursor/artifacts/cycle-valid-ab/`

### نتیجه layout-CV+drawer (۳ جفت متناوب، همان مسیر VPS)

| | FCP میانه | LCP میانه | Layout قبل FCP | dirty | CLS |
|--|-----------|-----------|----------------|-------|-----|
| control (4934300 CSS) | 924 | 1008 | 88 | 654 | 0.0046 |
| experiment | **904** | 1012 | **61** | **371** | **0.0031** |

TBT_approx=0 در هر دو. UX منو/سبد/ورود/اسلاید/هیرو موبایل پذیرفته شد.

## اقدام بعدی پس از publish

1. `quick-republish` رسمی از main
2. GTmetrix Seattle ×۲ روی URL عمومی
3. اگر هنوز A نیست: گلوگاه گزارش تازه (احتمالاً هنوز FCP/LCP شبکه+باقی Style) → فرضیه بعدی؛ تکرار split بدون شاهد تازه ممنوع
