# اجرای لوکال — اگر «فایل قفل است» یا پورت اشغال است، پروسهٔ قبلی را می‌بندد.
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Get-Process -Name "MovasseghiShop.Web" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

dotnet build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "→ http://localhost:5274" -ForegroundColor Green
Write-Host ""

dotnet run --no-build
