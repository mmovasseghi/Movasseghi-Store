# استقرار روی سرور لینوکس

این سند برای **نسخهٔ فعلی فروشگاه موثقی** (ASP.NET Core 10 + SQLite) است — جایگزین نسل اول (Next.js) در همان ریپوی GitHub.

## پیش‌نیاز

| مورد | نسخه پیشنهادی |
|------|----------------|
| OS | Ubuntu 22.04+ / Debian 12+ |
| .NET | SDK + ASP.NET Core Runtime **10.0** |
| Node.js | 20 LTS (فقط برای بیلد ادیتور TipTap) |
| nginx | reverse proxy + SSL (certbot) |
| فضا | حداقل 2GB RAM، 10GB دیسک |

## مسیر پیشنهادی روی سرور

```
/opt/MOVASSEGHISTORE/          # کلون گیت + publish
/opt/MOVASSEGHISTORE/publish/  # خروجی dotnet publish
/var/lib/movasseghi/           # دیتابیس و فایل‌های persistent (اختیاری)
```

## نصب خودکار

```bash
sudo mkdir -p /opt/MOVASSEGHISTORE
cd /opt/MOVASSEGHISTORE
sudo git clone https://github.com/mmovasseghi/Movasseghi-Store.git src
cd src
sudo bash deploy/linux/setup-from-github.sh /opt/MOVASSEGHISTORE
```

## نصب دستی (خلاصه)

```bash
git clone https://github.com/mmovasseghi/Movasseghi-Store.git
cd Movasseghi-Store
npm ci && npm run build:rte
dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj -c Release -o ./publish
cp src/MovasseghiShop.Web/appsettings.Production.json.example ./publish/appsettings.Production.json
# ویرایش PublicBaseUrl، AllowedHosts، CheckoutHmacSecret
export ASPNETCORE_ENVIRONMENT=Production
dotnet ./publish/MovasseghiShop.Web.dll --urls http://127.0.0.1:5080
```

سرویس systemd و nginx: فایل‌های `deploy/linux/movasseghi-shop.service` و `deploy/linux/nginx-movasseghi.conf`.

## SSL

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d movasseghi.ir -d www.movasseghi.ir
```

## انتقال دیتابیس از لوکال

فایل `movasseghi.db` در `.gitignore` است (عمداً).

```bash
scp ./src/MovasseghiShop.Web/movasseghi.db user@SERVER:/opt/MOVASSEGHISTORE/publish/
sudo chown www-data:www-data /opt/MOVASSEGHISTORE/publish/movasseghi.db
sudo systemctl restart movasseghi-shop
```

در `appsettings.Production.json` مسیر `Data Source` را با محل واقعی فایل یکی کنید.

## پس از بالا آمدن

1. ورود ادمین: `/Admin/Auth/Login`
2. اگر دیتابیس تازه است، کاربر seed: `admin` — **رمز را بلافاصله عوض کنید** (در Development از `DbInitializer` استفاده می‌شود؛ در Production رمز را در پنل تغییر دهید).
3. `SiteSettings:PublicBaseUrl` را روی دامنهٔ نهایی بگذارید.
4. `App_Data/gsc-service-account.json` را فقط روی سرور کپی کنید (هرگز در گیت نیست).

## به‌روزرسانی

```bash
cd /opt/MOVASSEGHISTORE/src
git pull
npm ci && npm run build:rte
dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj -c Release -o /opt/MOVASSEGHISTORE/publish
sudo systemctl restart movasseghi-shop
```

## عیب‌یابی

```bash
sudo journalctl -u movasseghi-shop -f
curl -I http://127.0.0.1:5080/
```

- **502 از nginx**: سرویس dotnet بالا نیست یا پورت اشتباه است.
- **Permission denied روی SQLite**: مالکیت `www-data` روی پوشهٔ دیتابیس.
- **استایل/ادمین موبایل قدیمی**: کش CDN/مرورگر — hard refresh.

## امنیت

- رمز SSH و ادمین را در چت/گیت قرار ندهید.
- `CheckoutHmacSecret` و کلیدهای GSC/Serp فقط در `appsettings.Production.json` روی سرور.
- فایروال: 80/443 باز؛ پورت 5080 فقط localhost.
