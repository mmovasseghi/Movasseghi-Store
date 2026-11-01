# هندآف کامل Codex — چرخه TLS دامنه + dual-vhost SNI

این فایل **گزارش کار + دیپلوی سرور + تأیید زنده** را یک‌جا دارد.
**Grade A اعلام نمی‌شود** (این چرخه TLS/دیپلوی است، نه حلقهٔ GTmetrix تازه).

---

## A0. هویت

| مورد | مقدار |
|------|--------|
| Repo | `mmovasseghi/Movasseghi-Store` |
| URL دامنه (عمومی) | `https://movasseghistore.com/` (Vira CDN → origin `185.93.88.81`) |
| URL IP (origin مستقیم) | `https://185.93.88.81/` |
| **RELEASE زنده (runtime اپ)** | **`c53290e0a8d554df0311075be7802204b5dfe773`** |
| تغییر این چرخه | فقط TLS/nginx + `PublicBaseUrl` — **بدون republish دات‌نت** |
| شاخه | `cursor/domain-ssl-dual-vhost-e83d` |
| PR | https://github.com/mmovasseghi/Movasseghi-Store/pull/9 |
| nginx conf md5 | `821aa8bce64a64e96d343588e7e6941e` |
| nginx proxy.inc md5 | `064b10ed06f1a9de4cb5aa21299138c0` |
| gzip | **6** (حفظ) |
| HTTP/2 | حفظ روی هر دو vhost |
| stamp روی VPS | `/MOVASSEGHISTORE/ops/nginx-domain-ssl-20261101/` |

هیرو / ۱۲ استوری / layout-CV از چرخهٔ `c53290e` **حفظ** شدند.

---

# بخش ۱ — درخواست و هدف

کاربر گواهی Let’s Encrypt دامنه را به‌صورت PEM فرستاد:

- `fullchain.pem` (برگ + زنجیره)
- `privkey.pem`
- زنجیرهٔ صادرکننده (intermediates / roots)
- URL: `https://movasseghistore.com/`

هدف: نصب روی origin، سرو HTTPS دامنه بدون شکستن HTTPS مستقیم IP، و گزارش کامل برای Codex.

---

# بخش ۲ — مشکل فنی که پیش آمد

اولین تلاش: هر دو `ssl_certificate` داخل **یک** `server` 443.

نتیجه: کلاینت‌ها زنجیرهٔ ECDSA دامنه را ترجیح دادند → درخواست به `https://185.93.88.81/` با SAN دامنه مواجه می‌شد و **IP SAN mismatch** می‌گرفت.

**قانون ثابت:** گواهی IP و گواهی دامنه باید در **دو `server` جدا** (SNI) باشند؛ هرگز دو `ssl_certificate` در یک بلاک برای این سناریو.

---

# بخش ۳ — آنچه روی سرور دیپلوی شد

## ۳.۱ فایل‌های گواهی (فقط روی VPS — **نه در Git**)

مسیر: `/etc/ssl/movasseghistore.com/`

| فایل | نقش |
|------|-----|
| `fullchain.pem` | برگ + intermediate(s) برای nginx |
| `privkey.pem` | کلید خصوصی (mode `600`) |
| `cert.pem` | فقط برگ |
| `chain.pem` | زنجیره بدون برگ |

- Subject: `CN=movasseghistore.com`
- SAN: `DNS:movasseghistore.com` (**بدون www**)
- Issuer origin: Let’s Encrypt **YE1**
- اعتبار: `2026-11-01` → `2027-01-30`
- الگوریتم: ECDSA P-256
- `cert_key_match`: **True**
- تعداد cert در fullchain نصب‌شده: **2**

گواهی IP کوتاه‌عمر (حفظ‌شده):

- `/etc/letsencrypt/live/ip-185-93-88-81/`
- SAN: `IP Address:185.93.88.81`
- Issuer: Let’s Encrypt **YE2**
- اعتبار تا `2026-11-07`

## ۳.۲ nginx dual-vhost

فایل‌ها:

- `/etc/nginx/sites-available/movasseghi-shop` ← از `deploy/linux/nginx-movasseghi.conf`
- `/etc/nginx/snippets/movasseghi-shop-proxy.inc` ← از `deploy/linux/nginx-movasseghi-proxy.inc`

ساختار:

1. `:80` — ACME + redirect به HTTPS
2. `:443 default_server` — `server_name 185.93.88.81 movasseghi.ir www.movasseghi.ir _` → **فقط** گواهی IP
3. `:443` — `server_name movasseghistore.com www.movasseghistore.com` → **فقط** گواهی دامنه

هر دو TLS vhost: `include` همان proxy.inc (gzip6، cache صفحات مهمان، static از wwwroot، proxy به Kestrel `:5080`).

`nginx -t` OK → `systemctl reload nginx` OK.

## ۳.۳ اپ / تنظیمات

| مورد | مقدار |
|------|--------|
| سرویس `movasseghi-shop` | **active** |
| nginx | **active** |
| `RELEASE_COMMIT` | `c53290e0a8d554df0311075be7802204b5dfe773` |
| `SiteSettings:PublicBaseUrl` | `https://movasseghistore.com` |
| `AllowedHosts` زنده | `*` |
| republish دات‌نت | **انجام نشد** (تغییر runtime اپ لازم نبود) |
| `.deploying` / `.deploy.lock` | پس از دیپلوی پاک / بدون marker |

نمونهٔ ریپو: `appsettings.Production.json.example` → `PublicBaseUrl=https://movasseghistore.com` و AllowedHosts گسترش‌یافته.

---

# بخش ۴ — تأیید زنده (شواهد)

## ۴.۱ OpenSSL SNI روی origin (`185.93.88.81:443`)

| SNI | Subject / SAN | Issuer |
|-----|---------------|--------|
| `185.93.88.81` | SAN IP `185.93.88.81` | YE2 |
| `movasseghistore.com` | `CN=movasseghistore.com` | YE1 |

## ۴.۲ HTTP

| هدف | نتیجه |
|-----|--------|
| `https://185.93.88.81/` | **200** HTTP/2 · gzip ~64KB |
| origin با Host دامنه (`--resolve …:185.93.88.81`) | **200** |
| `https://movasseghistore.com/` (CDN) | **200** · `server: vira-cdn` · HSTS edge |
| DNS دامنه | به IPهای CDN (مثلاً `87.107.94.254` / `92.114.24.*`) — **نه** مستقیم به origin |

برگ CDN جداست (issuer **YR2**)؛ این طبیعی است چون edge گواهی خودش را terminate می‌کند و به origin با SNI دامنه وصل می‌شود.

## ۴.۳ `verify-home.py` روی origin

هر دو با `--insecure --nginx` (دامنه با `/etc/hosts` → `127.0.0.1` موقت):

```
home:200 featured:8 offers:8 stories:12 lcp:200:/images/products/0056/01-lcp.webp nginx-cache:verified
```

- IP: **EXIT 0**
- دامنه (Host/SNI محلی): **EXIT 0**

محتوای HTML هر دو مسیر: `ms-hero` / `ms-features` / `ms-stories` / `ms-drawer` / critical / `movasseghistore.com` حاضر.

## ۴.۴ stamp دیپلوی

`/MOVASSEGHISTORE/ops/nginx-domain-ssl-20261101/DEPLOYED.txt`

```
deployed_utc=2026-11-01T18:46:21Z
release=c53290e0a8d554df0311075be7802204b5dfe773
public_base_url=https://movasseghistore.com
nginx_md5_conf=821aa8bce64a64e96d343588e7e6941e
nginx_md5_inc=064b10ed06f1a9de4cb5aa21299138c0
branch_source=cursor/domain-ssl-dual-vhost-e83d
```

---

# بخش ۵ — تغییرات ریپو (بدون secret)

| مسیر | تغییر |
|------|--------|
| `deploy/linux/nginx-movasseghi.conf` | دو vhost TLS + include |
| `deploy/linux/nginx-movasseghi-proxy.inc` | **جدید** — قوانین مشترک proxy/cache |
| `src/.../appsettings.Production.json.example` | PublicBaseUrl دامنه + AllowedHosts |
| `docs/performance/CURRENT-STATUS.md` | وضعیت TLS زنده |
| این هندآف | گزارش کامل |

**هیچ `privkey` / PEM کامل در Git نیست.**

---

# بخش ۶ — امنیت

`privkey.pem` دامنه در چت paste شده → **افشا**.

اقدام توصیه‌شده (هنوز انجام نشده):

1. صدور مجدد گواهی + کلید تازه برای `movasseghistore.com`
2. جایگزینی فایل‌های `/etc/ssl/movasseghistore.com/`
3. `nginx -t && reload`
4. باطل‌سازی گواهی قبلی در صورت امکان LE

تا آن زمان سایت کار می‌کند ولی کلید را باید compromised فرض کرد.

---

# بخش ۷ — وضعیت Performance + گزارش دامنه تازه‌خوانده‌شده

## ۷.۱ IP (چرخهٔ قبلی پس از `c53290e`)

| منبع | Perf | Struct | LCP | TBT | CLS |
|------|------|--------|-----|-----|-----|
| [xfrCIxd1](https://gtmetrix.com/reports/185.93.88.81/xfrCIxd1/) Seattle | 76 | 98 | 2.1s | 0 | 0 |

## ۷.۲ دامنه + CDN — گزارش کاربر [72oFmIEc](https://gtmetrix.com/reports/movasseghistore.com/72oFmIEc/)

| مورد | مقدار |
|------|--------|
| URL | `https://movasseghistore.com/` |
| زمان گزارش | Sun, Nov 1, 2026 12:03 PM -0700 |
| مکان | **Seattle, WA, USA** |
| مرورگر | Chrome 154.0.0.0 · Lighthouse 12.6.1 |
| **Performance** | **58%** |
| **Structure** | **96%** |
| **LCP** | **4.7s** (Much longer than recommended) |
| **TBT** | **65ms** |
| **CLS** | **0** |
| Fully Loaded | 10.2s |
| Requests | 22 |
| Connection Duration | **1.9s** |
| Backend Duration | 583ms |
| **TTFB** | **2.5s** |
| First Paint | 4.2s |
| DOM Content Loaded / Onload | 5.2s |
| Top Issues (Structure) | «No impactful audits found» |

FCP / Speed Index / TTI عددی در خلاصهٔ عمومی دیده نشد (تب‌های جزئیات پشت محدودیت). **Grade A اعلام نمی‌شود.**

### خوانش برای Codex
- Structure دامنه ≈ IP (۹۶ در برابر ۹۸) → ساخت فرانت خراب نشده.
- Perf دامنه **افت شدید** نسبت به IP (۵۸ در برابر ۷۶) با LCP 4.7s در برابر 2.1s.
- گلوگاه اصلی گزارش دامنه: **شبکه/مسیر تا اولین بایت** (Connection 1.9s + TTFB 2.5s)، نه Structure audit.
- DNS دامنه به Vira CDN است نه مستقیم به `185.93.88.81`؛ Seattle باید از edge CDN (IPهای ایران/منطقه‌ای) عبور کند → با تست مستقیم IP قابل‌مقایسهٔ عادلانه نیست.
- TBT هنوز پایین (۶۵ms)؛ CLS صفر؛ پس رگرسیون اصلی «JS مسدودکننده» یا layout shift نیست.

---

# بخش ۸ — اقدام بعدی برای Codex / عامل بعدی

1. **چرخش کلید** دامنه (اولویت امنیتی).
2. Merge PR `#9` وقتی review OK.
3. مقایسهٔ عادلانه Seattle: همان روز IP در برابر دامنه؛ اگر Perf دامنه با TTFB بالا ماند → اهرم CDN/edge/TTL/origin-pull است نه critical-split دوباره.
4. روی origin IP: گلوگاه LCP≈2.1s (TBT≈0) فقط با شاهد تازه.
5. تمدید گواهی IP کوتاه‌عمر قبل از `2026-11-07`.
6. اگر www لازم است: SAN `www` در گواهی جدید.

---

# بخش ۹ — دستورات تکرار دیپلوی nginx (بدون secret)

روی workstation با SSH به origin:

```bash
scp deploy/linux/nginx-movasseghi-proxy.inc root@185.93.88.81:/etc/nginx/snippets/movasseghi-shop-proxy.inc
scp deploy/linux/nginx-movasseghi.conf root@185.93.88.81:/etc/nginx/sites-available/movasseghi-shop
ssh root@185.93.88.81 'nginx -t && systemctl reload nginx'
```

بررسی SNI:

```bash
echo | openssl s_client -connect 185.93.88.81:443 -servername 185.93.88.81 2>/dev/null | openssl x509 -noout -ext subjectAltName
echo | openssl s_client -connect 185.93.88.81:443 -servername movasseghistore.com 2>/dev/null | openssl x509 -noout -subject
curl -sI https://movasseghistore.com/ | head -20
```

smoke:

```bash
python3 deploy/linux/verify-home.py https://185.93.88.81/ --insecure --nginx
```

---

**خط پایان این چرخه:** گواهی دامنه روی origin نصب است؛ dual-vhost SNI زنده است؛ IP و دامنه هر دو HTTPS 200 می‌دهند؛ CDN دامنه 200 است؛ `PublicBaseUrl` روی دامنه است؛ RELEASE اپ همان `c53290e`؛ PR `#9` آماده؛ کلید افشاشده باید بچرخد؛ Grade A باز است و منتظر سهمیه/تست تازه روی دامنه.
