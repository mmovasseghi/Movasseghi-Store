# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-02 (OvtAqFtf TBT/LCP decouple)

## نسخهٔ زنده

| مورد | مقدار |
|------|--------|
| **RELEASE** | **`cbe285b`** (critical ~34KB; remainder بعد از load+7s؛ تصاویر بعد از LCP) |
| URL | `https://movasseghistore.com/` |

## GTmetrix Seattle — روند

| گزارش | Perf | LCP | TBT | علت |
|-------|------|-----|-----|-----|
| yaFJoKbZ | 71 | 1.9s | **311ms** | CSS اینلاین ~150KB |
| F1St9hRu | 74 | **2.3s** | 9ms | remainder زود دانلود → LCP↑ |
| OvtAqFtf | 77 | 1.9s | **207ms** | inject remainder روی LCP → TBT↑ |

## اشکال روند

بهینه‌سازی لوکال یک متریک را درست می‌کرد و متریک دیگر را خراب. باید **پهنای‌باند LCP** و **پنجره TBT** جدا شوند:

- تا LCP: بدون دانلود remainder/فونت/استوری
- تا پایان پنجره TBT (~۵–۷s): بدون parse سنگین CSS
- بعداً: remainder + فونت

## اصلاحات `ba7ef96` → `cbe285b`

1. hydrate تصاویر سبک بعد از LCP
2. remainder+fonts فقط `load + 7s` (یا تعامل)
3. critical ~34KB (chrome+hero + stub استوری)

## سقف Seattle

Conn/TTFB از Seattle به CDN ایران اغلب **>500ms** است؛ با LCP~2s و TBT~0 همگزارش‌های قبلی Perf حدود **۷۵–۸۰** می‌دادند. Perf>۹۵ بدون PoP نزدیک‌تر یا HIT پایدار لبه سخت است.

اقدام: گزارش تازه روی `cbe285b`؛ در صورت ماندن LCP≥1.9s → CDN/edge (نه فقط CSS).
