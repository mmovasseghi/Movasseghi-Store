# وضعیت جاری Performance — Movasseghi-Store

آخرین به‌روزرسانی: 2026-11-02 (hero mobile + static home → Vira HIT)

## نسخهٔ زنده

| مورد | مقدار |
|------|--------|
| **RELEASE** | **`6454c47`** (static guest home + Vira HTML HIT; gzip9 snapshot) |
| URL دامنه | `https://movasseghistore.com/` (NS: viracdn.ir → Vira) |
| URL IP | `https://185.93.88.81/` (کنترل؛ نتیجهٔ دامنه نیست) |

## GTmetrix دامنه (Seattle)

| لینک | Perf | Struct | LCP | TBT | CLS | Conn | TTFB | Fully |
|------|------|--------|-----|-----|-----|------|------|-------|
| [ZthGFkBz](https://gtmetrix.com/reports/movasseghistore.com/ZthGFkBz/) | **76** | **98** | **2.2s** | 10ms | 0 | 536ms | 818ms | 2.8s |
| [72oFmIEc](https://gtmetrix.com/reports/movasseghistore.com/72oFmIEc/) | 58 | 96 | 4.7s | 65ms | 0 | 1.9s | 2.5s | 10.2s |

هدف Grade A: تقریباً Perf ≥ **85** با Struct ~98 → LCP/FCP/TTFB باید از سطح ZthGFkBz پایین‌تر بیاید.

## اصلاحات این دور

1. **هیرو موبایل:** `contain-intrinsic-size:auto 520px` روی اسلاید غیرفعال باعث جعبهٔ ۵۲۰px روی گوشی ~۳۹۰px و پرت شدن تصویر/متن می‌شد → CV با intrinsic **0×0** + `max-width:100%`.
2. **LCP bandwidth:** اسلاید غیرفعال `loading=lazy`؛ بدون preload فونت ۸۶KB روی home؛ فقط تصویر LCP اول لود می‌شود.
3. **`verify-home`:** کشف فونت versioned از `@font-face` اینلاین (دیگر وابسته به preload نیست)؛ X-Cache=`STATIC` هم قبول.
4. **Static guest home:** `wwwroot/index.html[.gz]` + `location = /` → **`vira-cache-status: HIT`** مشاهده شد (قبلاً HTML همیشه MISS بود). Cookie لاگین/سبد → Kestrel.

## اندازه‌گیری HIT در این محیط

| | TTFB میانه | FCP میانه |
|--|------------|-----------|
| Vira MISS | ~936ms | ~1227ms |
| Vira HIT | ~759ms | ~1041ms |

Seattle باید Backend ~282ms را با HIT حذف کند (Conn می‌ماند).

## موانع

1. GTmetrix از این محیط: Cloudflare guest؛ computerUse مدل محدود — گزارش تازه را کاربر/پنل باید بفرستد.
2. HIT لبه روی چند PoP پایدار نیست؛ warm کمک می‌کند؛ پنل Vira همچنان بهترین کنترل است.
3. critical-split / ultra-critical: FCP بهتر، **LCP بدتر** تحت RTT شبیه‌سازی‌شده → منتشر نشد.

## اقدام بعدی

1. quick-republish با تولید snapshot + conf نهایی
2. warm چندبارهٔ `/` تا HIT پایدار
3. GTmetrix Seattle تازه روی دامنه — هدف A
4. اگر HIT پایدار و هنوز B: ادامه Style/Layout بدون تکرار critical-split ردشده
