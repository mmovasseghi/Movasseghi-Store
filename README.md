# فروشگاه موثقی | Movasseghi Shop

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: Proprietary](https://img.shields.io/badge/License-Proprietary-red)]()

**فروشگاه آنلاین نمایندگی رسمی ظروف گیاهی آملون** — نسخهٔ دوم پلتفرم (جایگزین نسل اول Next.js در [همین ریپو](https://github.com/mmovasseghi/Movasseghi-Store)).

Monolith **ASP.NET Core MVC** با پنل مدیریت یکپارچه، SEO پیشرفته، موتور محتوای editorial، اتصال Google Search Console، سبد خرید، استعلام و گزارش رشد.

---

## ویژگی‌های اصلی

| بخش | توضیح |
|-----|--------|
| **فروشگاه** | محصولات، واریانت، سبد، تسویه (درگاه mock در dev)، نظرات |
| **پنل ادمین** | محصولات، دسته‌ها، سفارش‌ها، استعلام‌ها، کوپن، بلاگ، استودیو صفحهٔ اصلی |
| **SEO** | آنالیز Ultimate، auto-fix، pipeline رقبا، SERP، publish gate |
| **Editorial** | صف زمان‌بندی انتشار، Hub، کیفیت محتوا |
| **Growth** | گزارش هفتگی، insights، Command Center |
| **موبایل ادمین** | لایهٔ responsive ≤960px بدون تغییر دسکتاپ |

---

## شروع سریع (توسعه)

```powershell
git clone https://github.com/mmovasseghi/Movasseghi-Store.git
cd Movasseghi-Store
npm ci
dotnet run --project src/MovasseghiShop.Web/MovasseghiShop.Web.csproj
```

- فروشگاه: `http://localhost:5274`
- ادمین: `http://localhost:5274/Admin`

جزئیات: [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)

---

## استقرار روی سرور

مسیر پیشنهادی: **`/opt/MOVASSEGHISTORE`**

```bash
sudo bash deploy/linux/setup-from-github.sh /opt/MOVASSEGHISTORE
```

راهنمای کامل SSL، دیتابیس، nginx و به‌روزرسانی: **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)**

---

## ساختار ریپو

```
├── MovasseghiShop.sln
├── src/MovasseghiShop.Web/          # اپلیکیشن وب
├── tests/MovasseghiShop.Web.Tests/  # تست واحد
├── deploy/linux/                    # systemd + nginx + اسکریپت نصب
├── docs/                            # مستندات فارسی
├── scripts/                         # ابزارهای audit موبایل (Playwright)
└── package.json                     # بیلد rich-text-editor (TipTap)
```

---

## پیکربندی

| فایل | نقش |
|------|-----|
| `appsettings.json` | پیش‌فرض commit‌شده (بدون رمز) |
| `appsettings.Development.json` | محلی — **در گیت نیست** |
| `appsettings.Production.json` | روی سرور — از `appsettings.Production.json.example` |

کلیدهای حساس: `Security:CheckoutHmacSecret`, `SerpApi:ApiKey`, `App_Data/gsc-service-account.json`

---

## تست

```powershell
dotnet test MovasseghiShop.sln
```

---

## تاریخچهٔ ریپو

این مخزن قبلاً نسل اول فروشگاه (Next.js) بود. **شاخهٔ `main` اکنون کد MVC فعلی است.** برای آرشیو نسل اول به تاریخچهٔ git قبل از مهاجرت مراجعه کنید.

---

## پشتیبانی و مالکیت

پروژهٔ خصوصی — © موثقی شاپ. برای دسترسی و استقرار با تیم فنی هماهنگ کنید.

**مخزن:** https://github.com/mmovasseghi/Movasseghi-Store
