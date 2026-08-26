#!/bin/bash
set -euo pipefail

# Movasseghi Store — Hetzner bootstrap (run once as root)
export DEBIAN_FRONTEND=noninteractive

apt-get update -qq
apt-get install -y -qq curl git nginx ufw fail2ban

# Node 22 via NodeSource
if ! command -v node >/dev/null 2>&1; then
  curl -fsSL https://deb.nodesource.com/setup_22.x | bash -
  apt-get install -y -qq nodejs
fi

# Deploy user
if ! id deploy >/dev/null 2>&1; then
  useradd -m -s /bin/bash deploy
  usermod -aG www-data deploy
fi

mkdir -p /var/www/movasseghi-staging
chown -R deploy:www-data /var/www/movasseghi-staging

# Staging placeholder with noindex
cat > /var/www/movasseghi-staging/index.html <<'HTML'
<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
  <meta charset="utf-8"/>
  <meta name="robots" content="noindex,nofollow"/>
  <title>فروشگاه موثقی — Staging</title>
  <style>
    body{font-family:system-ui,sans-serif;background:#f0f9f8;color:#134e4a;
    display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0}
    .c{text-align:center;padding:2rem}
    h1{font-size:1.5rem}
    p{color:#64748b}
  </style>
</head>
<body><div class="c">
  <h1>فروشگاه موثقی</h1>
  <p>Staging environment — deployment pipeline preparing</p>
</div></body>
</html>
HTML

chown deploy:www-data /var/www/movasseghi-staging/index.html

# Nginx staging vhost
cat > /etc/nginx/sites-available/movasseghi-staging <<'NGINX'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;
    root /var/www/movasseghi-staging;
    index index.html;
    add_header X-Robots-Tag "noindex, nofollow" always;
    location / {
        try_files $uri $uri/ =404;
    }
}
NGINX

ln -sf /etc/nginx/sites-available/movasseghi-staging /etc/nginx/sites-enabled/movasseghi-staging
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx

# Firewall
ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable

echo "BOOTSTRAP_OK node=$(node -v) nginx=$(nginx -v 2>&1)"
