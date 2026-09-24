# توسعهٔ محلی

## پیش‌نیاز

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (برای `npm run build:rte`)

## اجرا

```powershell
cd "F:\Projects\Movasseghi Shop"
npm ci
dotnet run --project src\MovasseghiShop.Web\MovasseghiShop.Web.csproj
```

پیش‌فرض: `http://localhost:5274`

## تنظیمات محلی

کپی نکنید در گیت — فایل `appsettings.Development.json` در `.gitignore` است:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=movasseghi.db"
  }
}
```

## تست

```powershell
dotnet test MovasseghiShop.sln
```

## ساختار

```
MovasseghiShop.sln
src/MovasseghiShop.Web/     # MVC + Areas/Admin
tests/MovasseghiShop.Web.Tests/
scripts/                    # Playwright mobile audit (اختیاری)
deploy/linux/               # systemd + nginx
```

## ادیتور متن (TipTap)

```bash
npm run build:rte
```

خروجی: `src/MovasseghiShop.Web/wwwroot/js/rich-text-editor.bundle.js` (در گیت commit می‌شود؛ بیلد MSBuild هم قبل از compile اجرا می‌شود).

## پنل ادمین

- URL: `/Admin`
- موبایل: `admin-mobile-shell.css` فقط ≤960px

## SEO / Editorial

- قوانین نمایش امتیاز: `.cursor/rules/seo-score-integrity.mdc`
- پیکربندی: `appsettings.json` → بخش‌های `Seo`, `EditorialGrowth`, `GoogleSearchConsole`
