# معماری (خلاصه)

```
Browser ──► nginx (:443) ──► Kestrel (MovasseghiShop.Web)
                              │
                              ├── SQLite (movasseghi.db)
                              ├── wwwroot (استاتیک + تصاویر)
                              └── App_Data (JSON maps, GSC key, کش amelon)
```

## لایه‌ها

- **Controllers / Areas/Admin**: MVC + Razor
- **Services**: کاتالوگ، سفارش، SEO، Editorial، Growth، Amelon sync
- **Data**: EF Core + Identity (`ApplicationDbContext`)
- **Background**: `SeoMaintenanceBackgroundService`, Editorial growth hosted timers

## SEO integrity

امتیاز کلی UI از `SeoUltimateAnalyzer.ResolveDisplayOverall` — حداکثرِ میانگین وزنی، readiness و competitive strength؛ بدون دادهٔ SERP/GSC عدد ساختگی نشان داده نمی‌شود.

## فرانت فروشگاه

CSS/JS در `wwwroot`؛ بدون SPA جدا. ادیتور ادمین: TipTap bundle (`Client/rich-text-editor.mjs` → esbuild).
