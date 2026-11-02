# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-02 (F1St9hRu LCP bandwidth)

## نسخهٔ زنده

| مورد | مقدار |
|------|--------|
| **RELEASE** | **`ad69008`** (post-LCP remainder/fonts/stories/logo) |
| Snap | `ms-home-snap:ad69008…` پس از republish |
| URL دامنه | `https://movasseghistore.com/` |

## GTmetrix Seattle

| لینک | Perf | Struct | LCP | TBT | CLS | نکته |
|------|------|--------|-----|-----|-----|------|
| [F1St9hRu](https://gtmetrix.com/reports/movasseghistore.com/F1St9hRu/) | **74** | **98** | **2.3s** | **9ms** | 0 | TBT درست شد؛ LCP بدتر از قبل |
| [yaFJoKbZ](https://gtmetrix.com/reports/movasseghistore.com/yaFJoKbZ/) | 71 | 98 | 1.9s | 311ms | 0.01 | TBT از Style/Layout |

## اشکال روند قبلی

آزمایش لوکال با RTT پایین، TBT را خوب نشان می‌داد ولی **رقابت پهنای‌باند روی مسیر Seattle** را نمی‌دید:

1. `media=print` همچنان remainder را **دانلود** می‌کرد
2. `@font-face` در critical → woff2 حدود **۸۶KB** همزمان با هیرو
3. کاور استوری‌های بالای هیرو با `loading=lazy` در viewport باز هم لود می‌شدند

نتیجه: TBT↓ ولی LCP↑ → Perf تقریباً ثابت (~74).

## اصلاح `8c4b042` / `ad69008`

- تزریق remainder فقط بعد از LCP (سقف ۲.۵s)
- فونت فقط در remainder
- `data-ms-after-lcp` برای استوری و لوگو
- skip-link/reset همچنان در critical (CLS دسکتاپ)

## خود-اندازه (CPU۴× + latency~۱۸۰ms)

| | LCP | TBT | approxPerf |
|--|-----|-----|------------|
| قبل (033bd86، همان throttle) | ~2.0s | کم | — |
| بعد (8c4b042) | **~1.45–1.7s** | ~90–120ms | **~98–99** |

نیاز: گزارش تازهٔ GTmetrix Seattle روی دامنه.
