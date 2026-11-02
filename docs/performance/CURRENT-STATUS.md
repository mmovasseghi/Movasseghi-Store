# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-02 (yaFJoKbZ TBT/CLS → lab Perf >95)

## نسخهٔ زنده

| مورد | مقدار |
|------|--------|
| **RELEASE** | **`033bd86`** (tight critical ~56KB + complement remainder + skip-link in critical) |
| Snap | `ms-home-snap:033bd869384f` |
| URL دامنه | `https://movasseghistore.com/` (NS: viracdn.ir → Vira; HTML `s-maxage=120`) |
| URL IP | `https://185.93.88.81/` (کنترل؛ نتیجهٔ دامنه نیست) |

## GTmetrix دامنه (Seattle) — مرجع

| لینک | Perf | Struct | LCP | TBT | CLS |
|------|------|--------|-----|-----|-----|
| [yaFJoKbZ](https://gtmetrix.com/reports/movasseghistore.com/yaFJoKbZ/) | **71** | **98** | **1.9s** | **311ms** | 0.01 |
| [ZthGFkBz](https://gtmetrix.com/reports/movasseghistore.com/ZthGFkBz/) | 76 | 98 | 2.2s | 10ms | 0 |

هدف کاربر: **Performance > 95** (وزن GT: TBT 30% · LCP 25% · CLS 25% · FCP 10% · SI 10%).

## خود-اندازه‌گیری (Chrome CPU ۴×، همان وزن‌ها) — پس از `033bd86`

| Viewport | FCP میانه | LCP میانه | TBT میانه | CLS میانه | approxPerf میانه |
|----------|-----------|-----------|-----------|-----------|------------------|
| Desktop 1366×768 | ~1.37s | ~1.46s | **~7ms** | **~0.003** | **~99.6** |
| Mobile 412×823 | ~1.43s | ~1.53s | **~16ms** | ~0.067 | **~97.3** |

artifact: `/opt/cursor/artifacts/gt-perf95/desktop-033bd86.jsonl`, `mobile-033bd86.jsonl`.

## ریشهٔ yaFJoKbZ و اصلاحات

1. **TBT 311ms:** Style/Layout روی ~150KB CSS اینلاین + JS زود. → critical اول‌نمای ~56KB؛ enhancement JS با `data-ms-idle` + `setTimeout(7000)`.
2. **`@font-face` از دست می‌رفت:** چک در `mapRule` بی‌اثر بود (at-rule → `mapAtRule`). → نگه داشتن فونت در `mapAtRule` (smoke `verify-home --nginx`).
3. **CLS دسکتاپ ~0.13→1.0:** `.ms-skip-link` و resetهای `*`/`img`/`a` فقط در remainder بودند → لینک skip ~۲۵px در جریان عادی، بعد `position:absolute` → پرش `.ms-app`. همچنین remainder قبلاً **کپی کامل** شیت بود. → skip+reset در critical؛ remainder = مکمل below-fold + `@keyframes`؛ بارگذاری غیرمسدود `media=print`/`onload`.
4. **CDN stale:** `s-maxage=120` + مهر `ms-home-snap:SHA`.

## اقدام بعدی

1. GTmetrix Seattle تازه روی دامنه برای تأیید رسمی Perf >95 / Grade A.
2. Warm چندبارهٔ `/` برای HIT پایدار روی PoPهای Vira.
3. اختیاری: کاهش CLS موبایل (~0.07) اگر GT موبایل هم هدف شد.
