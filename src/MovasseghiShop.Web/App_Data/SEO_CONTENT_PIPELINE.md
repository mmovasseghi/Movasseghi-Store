# موتور تولید محتوا SEO / GEO / AEO (Research-Driven)

## اجرا

1. `SerpApi:ApiKey` را در `appsettings` یا secrets تنظیم کنید.
2. Auto-Fix محصول (`درست‌سازی خودکار SEO`) از `ISeoContentPipelineService` استفاده می‌کند.

## مراحل

| مرحله | کلاس |
|--------|------|
| جستجو | `SerpOrganicSearchService` |
| استخراج HTML رقیب | `CompetitorPageFetcher` |
| راستی‌آزمایی | `SeoContentRelevanceModule` |
| شکاف محتوا | `SeoContentGapModule` |
| اولویت ۲۰ کلمه | `SeoKeywordPriorityEngine` + `SiteKeywordStrategy.Pillars` |
| تولید | `SeoResearchAwareWriter` + `SeoUltimateWriter` |
| Quality Gate | `SeoContentQualityGate` |

## تنظیمات

`Seo:ContentPipeline` در `appsettings.json`

## پرامپت‌ها

دو پرامپت مرجع در `SeoContentPipelinePrompts.cs` (الزامات کاربر + نسخه مهندسی).

## محصول جدید

Auto-Fix معمولی کافی است؛ قفل توضیحات (`LockProductDescription`) تولید را متوقف می‌کند.
