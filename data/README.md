# دادهٔ فروشگاه (SQLite)

کد و تصاویر در گیت هستند؛ **دیتابیس کامل** (`movasseghi.db`) عمداً در گیت نیست (حجم، کاربران ادمین، لاگ).

برای نصب تازه یا سرور بدون دادهٔ قبلی:

- `movasseghi.db.gz` — نسخهٔ فشردهٔ کاتالوگ + تنظیمات صفحهٔ اصلی (محصولات ویژه، پیشنهاد ویژه، مسیرهای `/images/...`).
- استقرار: `deploy/linux/remote-install.sh` در اولین نشر، اگر `publish/movasseghi.db` وجود نداشته باشد، از `data/movasseghi.db.gz` استخراج می‌کند.

به‌روزرسانی دستی روی سرور:

```bash
# از ویندوز (مثال)
pscp movasseghi.db root@SERVER:/MOVASSEGHISTORE/publish/
ssh root@SERVER 'chown www-data:www-data /MOVASSEGHISTORE/publish/movasseghi.db && systemctl restart movasseghi-shop'
```

بعد از هر `git pull` + publish، اسکript استقرار **دیتابیس موجود را نگه می‌دارد**؛ فقط اگر فایل publish حذف شده باشد از بکاپ یا `data/movasseghi.db.gz` پر می‌شود.
