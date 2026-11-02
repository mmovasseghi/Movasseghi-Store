# Shared HTTPS proxy/cache locations for movasseghi-shop TLS vhosts.
# Included from nginx-movasseghi.conf — do not enable this file as a site.

client_max_body_size 32M;

# Only direct local probes can bypass deployment maintenance.
set $ms_deploying 0;
if (-f /MOVASSEGHISTORE/.deploying) { set $ms_deploying 1; }
if ($ms_local_probe = 1) { set $ms_deploying 0; }
if ($ms_deploying = 1) { return 503; }

gzip on;
gzip_vary on;
gzip_min_length 256;
gzip_types text/plain text/css text/javascript application/javascript application/json application/xml image/svg+xml;
gzip_proxied any;
gzip_comp_level 6;

# صفحهٔ عمومی مهمان — GTmetrix بدون کوکی؛ کاربر لاگین‌شده از کش رد می‌شود
set $ms_skip_cache 0;
if ($http_authorization != "") {
    set $ms_skip_cache 1;
}
if ($http_cookie ~* "(Identity|AspNetCore|ms_cart_n=)") {
    set $ms_skip_cache 1;
}
# Only explicitly public pages may enter the shared cache. In particular,
# guest orders, invoices, payment callbacks and cart fragments are private.
if ($uri !~* "^(/$|/Home/?$|/Home/Stories\.json$|/(Catalog|Shop|Blog|News|Page)(/|$)|/sitemap[^/]*\.xml$|/(robots|llms|ai)\.txt$)") {
    set $ms_skip_cache 1;
}

# Legacy subpath (301 to site root)
location = /MOVASSEGHISTORE {
    return 301 /;
}
location ^~ /MOVASSEGHISTORE/ {
    rewrite ^/MOVASSEGHISTORE(/.*)$ $1 permanent;
}

# Static assets — مستقیم از publish/wwwroot (بدون Kestrel) برای LCP و کش
location ~* ^/(css|js|fonts|images|lib)/ {
    root /MOVASSEGHISTORE/publish/wwwroot;
    try_files $uri =404;
    access_log off;
    # asp-append-version fingerprints CSS/JS. Image URLs can be overwritten
    # by uploads or optimization, so unversioned assets must revalidate.
    add_header Cache-Control $ms_asset_cache_control;
}

# LCP resize — Kestrel (ImageSharp)
location ^~ /media/ {
    proxy_pass http://127.0.0.1:5080;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto https;
    proxy_hide_header Strict-Transport-Security;
    add_header Strict-Transport-Security "max-age=0" always;
}

location ^~ /Admin/ {
    proxy_pass http://127.0.0.1:5080;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto https;
    proxy_set_header Connection "";
    proxy_hide_header Cache-Control;
    proxy_hide_header Strict-Transport-Security;
    add_header Cache-Control "no-store" always;
    add_header Strict-Transport-Security "max-age=0" always;
}

location ^~ /Account/ {
    proxy_pass http://127.0.0.1:5080;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto https;
    proxy_set_header Connection "";
    proxy_hide_header Cache-Control;
    proxy_hide_header Strict-Transport-Security;
    add_header Cache-Control "no-store" always;
    add_header Strict-Transport-Security "max-age=0" always;
}

# Guest home as a static gzip snapshot so Vira can edge-HIT HTML (/).
# Logged-in / cart visitors fall through to Kestrel via 418 → @ms_home_dynamic.
# Snapshot is written by quick-republish after smoke (wwwroot/index.html[.gz]).
location = /index.html {
    root /MOVASSEGHISTORE/publish/wwwroot;
    default_type text/html;
    charset utf-8;
    gzip_static on;
    access_log off;
    add_header Cache-Control "public, max-age=900, s-maxage=3600" always;
    add_header CDN-Cache-Control "public, max-age=3600" always;
    add_header X-Cache "STATIC" always;
    add_header Strict-Transport-Security "max-age=0" always;
}

location = / {
    error_page 418 = @ms_home_dynamic;
    if ($http_cookie ~* "(Identity|AspNetCore|ms_cart_n=)") { return 418; }
    if ($http_authorization != "") { return 418; }
    root /MOVASSEGHISTORE/publish/wwwroot;
    try_files /index.html =418;
    default_type text/html;
    charset utf-8;
    gzip_static on;
    add_header Cache-Control "public, max-age=900, s-maxage=3600" always;
    add_header CDN-Cache-Control "public, max-age=3600" always;
    add_header X-Cache "STATIC" always;
    add_header Strict-Transport-Security "max-age=0" always;
}

location @ms_home_dynamic {
    proxy_pass http://127.0.0.1:5080;
    proxy_http_version 1.1;
    proxy_set_header Accept-Encoding "";
    proxy_set_header Connection "";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto https;
    proxy_cache ms_pages;
    proxy_cache_methods GET HEAD;
    proxy_cache_valid 200 30m;
    proxy_cache_key "$host$request_method$request_uri|$http_accept_encoding";
    proxy_cache_bypass $ms_skip_cache;
    proxy_no_cache $ms_skip_cache;
    proxy_cache_lock on;
    proxy_hide_header Strict-Transport-Security;
    proxy_hide_header Age;
    add_header CDN-Cache-Control "public, max-age=900" always;
    add_header Strict-Transport-Security "max-age=0" always;
    add_header X-Cache $upstream_cache_status always;
}

location / {
    proxy_pass http://127.0.0.1:5080;
    proxy_http_version 1.1;
    # Compress once at nginx; avoid caching Kestrel's larger streamed gzip.
    proxy_set_header Accept-Encoding "";
    proxy_set_header Connection "";
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto https;
    proxy_cache ms_pages;
    proxy_cache_methods GET HEAD;
    proxy_cache_valid 200 30m;
    proxy_cache_key "$host$request_method$request_uri|$http_accept_encoding";
    proxy_cache_bypass $ms_skip_cache;
    proxy_no_cache $ms_skip_cache;
    proxy_cache_lock on;
    # Hide Kestrel HSTS (unsafe on short-lived IP certs) and clear any prior pin.
    # Domain edge/CDN may set its own HSTS; origin stays neutral for IP cert safety.
    proxy_hide_header Strict-Transport-Security;
    # Do not forward nginx Age to the public edge (confuses CDN diagnostics).
    proxy_hide_header Age;
    # Hint shared caches / Vira explicitly; guest HTML already uses public+s-maxage.
    add_header CDN-Cache-Control "public, max-age=900" always;
    add_header Strict-Transport-Security "max-age=0" always;
    add_header X-Cache $upstream_cache_status always;
}
