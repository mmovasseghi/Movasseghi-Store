# اپراتور — تنظیمات لازم Vira برای Grade A دامنه

بدون این دسترسی‌ها عامل نمی‌تواند چرخهٔ دامنه را تا A ببندد.

## ۱) کش لبه (الزامی)

شواهد: همهٔ درخواست‌های عمومی `vira-cache-status: MISS` می‌مانند در حالی که origin می‌فرستد:

`Cache-Control: public, max-age=900, s-maxage=900`

در پنل Vira برای `movasseghistore.com`:

1. کش لبه را برای مسیرهای عمومی روشن کنید (`/`, `/css/*`, `/js/*`, `/fonts/*`, `/images/*`).
2. از هدر `Cache-Control` / `s-maxage` پیروی کنید یا TTL دستی ≥ origin برای HTML عمومی.
3. **کش نکنید:** `/Account/*`, `/Admin/*`, `/Cart*`, سفارش، پرداخت، هر پاسخی با `Set-Cookie` هویتی / antiforgery.
4. پس از publish: purge HTML و در صورت نیاز static fingerprint‌شده.
5. تأیید: چند `GET` پشت‌سرهم → `vira-cache-status: HIT` (نه فقط `X-Cache` origin).

## ۲) ACME passthrough (برای چرخش کلید)

الان CDN روی HTTP چالش LE را می‌بلعد و `404 acme/autocert…` می‌دهد.

1. `http://movasseghistore.com/.well-known/acme-challenge/*` را بدون کش و بدون autocert به origin `:80` بفرستید.
2. بعد از آن روی origin:

```bash
certbot certonly --webroot -w /var/www/acme -d movasseghistore.com \
  --cert-name movasseghistore-com --key-type ecdsa \
  --elliptic-curve secp256r1 --non-interactive --agree-tos \
  --register-unsafely-without-email
# سپس privkey/fullchain را به /etc/ssl/movasseghistore.com/ نصب و nginx reload
# کلید آمادهٔ rotate در /etc/ssl/movasseghistore.com/rotate/privkey.new.pem هست یا از certbot استفاده شود
certbot revoke --cert-path /path/to/old/cert.pem  # پس از نصب موفق
```

3. تمدید خودکار + deploy-hook `nginx -t && systemctl reload nginx`.

## ۳) پس از HIT شدن کش

دو تست همسان Seattle روی `https://movasseghistore.com/`؛ فقط بعد از افت TTFB، Style/Layout اپ را ادامه دهید.
IP را به‌جای دامنه گزارش نکنید.
