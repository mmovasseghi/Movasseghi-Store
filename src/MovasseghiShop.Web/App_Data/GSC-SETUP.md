# Google Search Console — راه‌اندازی

1. سایت را در https://search.google.com/search-console اضافه کنید.
2. Google Cloud → API Library → **Google Search Console API** → Enable
3. Service Account بسازید و JSON key دانلود کنید.
4. فایل را ذخیره کنید: `App_Data/gsc-service-account.json`
5. ایمیل Service Account را در GSC → Settings → Users → **Owner** اضافه کنید.
6. در `appsettings.json`:
   ```json
   "GoogleSearchConsole": {
     "PropertyUrl": "https://YOUR-DOMAIN.com/",
     "ServiceAccountKeyPath": "App_Data/gsc-service-account.json",
     "SiteVerificationMeta": "کد-تأیید-از-GSC"
   }
   ```
7. سرور را ری‌استارت کنید → `/Admin/Studio/RankRadar` → **Sync از GSC**

### توسعه (localhost)

در `appsettings.Development.json` مقدار `SiteSettings:PublicBaseUrl` و `GoogleSearchConsole:PropertyUrl` روی `http://localhost:5274/` تنظیم شده است.  
GSC روی localhost داده واقعی نمی‌دهد — برای داده واقعی باید دامنهٔ تأیید‌شده در Search Console و فایل JSON سرویس‌اکانت در `App_Data/gsc-service-account.json` (از روی `gsc-service-account.json.example`) باشد.  
با اتصال موفق، همگام‌سازی خودکار هنگام استارت سرور و دکمه Rank Radar فعال می‌شود.
