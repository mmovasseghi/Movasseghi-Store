# GitHub Actions (اختیاری)

فایل نمونهٔ CI در push اول به‌خاطر محدودیت scope توکن `gh` آپلود نشد. برای فعال‌سازی:

1. در GitHub: **Settings → Actions → General** را فعال کنید.
2. فایل `.github/workflows/ci.yml` را از [مستندات dotnet](https://github.com/actions/setup-dotnet) بسازید یا از لوکال با `gh auth refresh -s workflow` دوباره push کنید.

حداقل مراحل CI: `npm ci`, `npm run build:rte`, `dotnet test`.
