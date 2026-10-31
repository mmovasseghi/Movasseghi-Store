# چرخهٔ HTTPS روی IP + HTTP/2 (پس از mDQriT19)

تاریخ: ۲۰۲۶-۱۰-۳۱  
SHA منتشرشده: `31f01753449ab39d138cabbf197d56b915c9b4f8`  
مبدأ اندازه‌گیری قبلی (HTTP): `fb35054` / گزارش‌های mDQriT19 و ZjSkJa1K  
مبدأ canonical جدید: **`https://185.93.88.81/`** (HTTP فقط ۳۰۱ به HTTPS؛ ACME روی `:80` باقی است)

## دادهٔ کافی قبل از تغییر (مرحله ۱)

شرایط: Lighthouse **12.6.1** از محیط agent به `http://185.93.88.81/`، throttling شبیه‌سازی‌شده موبایل؛ **این اعداد Seattle GTmetrix نیستند.**

| اجرا | Score | FCP ms | SI ms | LCP ms | TBT ms | CLS | TTFB ms | X-Cache قبل |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 0.89 | 2058 | 2936 | 2330 | 133 | 0.144 | 230 | EXPIRED |
| 2 | 0.90 | 2051 | 2957 | 2403 | 104 | 0.138 | 227 | HIT |
| 3 | 0.88 | ~2052 | — | 2554 | ~160 | 0.139 | 254 | HIT |

میانهٔ فازهای LCP (LH محلی، HTTP): TTFB ~699 · Load Delay ~646 · Load Time ~839 · Render Delay ~218  
Redirect HTTP→HTTPS→HTTP در LH محلی هنوز دیده شد (~۱٫۳s تخمین audit).  
`home.js` دیگر long-task اولیه نبود؛ در یک اجرا ~۵۳ms حوالی ۳٫۱s.

Artifacts: `/opt/cursor/artifacts/cycle2-lh/`, `cycle2-trace/`.

## DNS / TLS (مرحله ۴ — منابع رسمی)

- `movasseghi.ir` / `www.movasseghi.ir`: از resolverهای 1.1.1.1 و 8.8.8.8 وضعیت **NXDOMAIN** (نه یک lookup ناموفق تکی).
- authoritative nic.ir از این محیط timeout شد؛ نتیجهٔ عمومی resolverها برای «عدم وجود رکورد» کافی است تا دامنه را قابل‌استفاده برای LE DNS فرض نکنیم.
- Let’s Encrypt: گواهی **IP** با پروفایل shortlived از ۱۵ ژانویه ۲۰۲۶ عموماً در دسترس است؛ ACME directory هنوز `shortlived` را advertise می‌کند؛ IP باید shortlived باشد (~۶ روز).
- Certbot سیستمی ۱٫۲۱ کافی نبود؛ **snap certbot 5.9.0** نصب شد و با `--preferred-profile shortlived --webroot --ip-address 185.93.88.81` گواهی تولید شد (SAN=IP).
- nginx: `:443 ssl http2` + ۳۰۱ از `:80` (جز ACME). `PublicBaseUrl=https://185.93.88.81`. HSTS برای IP عمداً خاموش/`max-age=0` (گواهی کوتاه‌عمر).
- renew: deploy-hook `nginx -t && systemctl reload nginx`. تمدید خودکار هر ~۲–۳ روز الزامی است.

## نتیجهٔ GTmetrix واقعی پس از HTTPS (Seattle / Chrome 154 / LH 12.6.1)

| گزارش | URL | Grade | Perf | Struct | LCP | TBT | CLS |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| mDQriT19 (قبل، HTTP) | http://… | B | 84% | 93% | 1.8s | 137ms | 0 |
| **SlGIpwvK (بعد)** | **https://…** | **B** | **71%** | **98%** | **2.5s** | **40ms** | **0** |
| HFun8t5f (تکرار) | https://… | B* | 75% | 98% | 2.4s | 0ms | 0 |

\*حرف Grade در og قفل/ناقص بود؛ از UI گزارش اول B خوانده شد؛ تکرار همان باند Perf است.

لینک‌ها:
- https://gtmetrix.com/reports/185.93.88.81/SlGIpwvK/
- https://gtmetrix.com/reports/185.93.88.81/HFun8t5f/

جزئیات Structure/Performance این دو گزارش در این محیط **لاگین‌قفل** بودند؛ اعداد بالا فقط vitals هدر.

### تفسیر شواهد (بدون ادعای سقف مطلق)

- **Structure 93→98**: با HTTP/2 واقعی + حذف زنجیرهٔ HTTPS-First شکست‌خورده هم‌خوان است.
- **TBT 137→0–40ms**: هم‌راستا با defer قبلی + مسیر HTTPS پایدار.
- **LCP 1.8→2.4–2.5s و Perf 84→71–75**: روی مسیر Seattle، هزینهٔ TLS/اتصال روی IP دور غالب شده؛ LH محلی agent روی همان HTTPS برعکس بهبود LCP (~۱٫۳s، score 0.94) نشان داد — **محیط محلی ≠ Seattle**.

## LH محلی پس از HTTPS (نه GTmetrix)

میانه: Score 0.94 · FCP/LCP ~1336ms · TBT ~17.5ms · SI ~2809 · redirects=[] · uses-http2=pass  
فاز LCP میانه: TTFB ~674 · Load Delay ~182 · Load Time ~418 · Render Delay ~60

## UX

Desktop/mobile: ۸+۸، ۱۲ استوری، dual، منو (`ms-drawer-open`)، سبد، UI ورود، `protocol=https` و `nextHopProtocol=h2`، بدون console error.  
Early click منو هنگام بارگذاری OK. Story click در اتوماسیون `open:false` ماند (نیاز به بررسی دستی/سلکتور؛ حذف امکانات نشد).

## مانع باقی‌مانده / دسترسی لازم

1. برای نزدیک‌شدن به Grade A / Perf>95 از **Seattle**: کاهش LCP زیر RTT بلند — محتمل‌ترین اهرم بیرونی: دامنهٔ resolveشونده + CDN نزدیک کاربر/لاب (مالکیت DNS دامنه فعلاً NXDOMAIN؛ بدون دسترسی ثبت/NS نمی‌توان انجام داد).
2. لاگین GTmetrix برای فازهای LCP و جدول Structure گزارش‌های HTTPS.
3. پایش تمدید گواهی ۶روزه IP (snap certbot + deploy-hook). مناسب‌بودن IP cert برای CDN با دامنه یکی نیست.

**ادعای Grade A یا Perf>95 نمی‌شود.**
