#!/usr/bin/env bash
# Prepare Nginx + Certbot for production domain (run on server as root)
# Usage: DOMAIN=movasseghi.ir EMAIL=admin@movasseghi.ir bash nginx-tls.sh
set -euo pipefail

DOMAIN="${DOMAIN:?Set DOMAIN env var}"
EMAIL="${EMAIL:-admin@movasseghi.ir}"
APP_PORT="${APP_PORT:-3000}"

apt-get update -qq
apt-get install -y -qq certbot python3-certbot-nginx

cat > "/etc/nginx/sites-available/${DOMAIN}" <<EOF
server {
    listen 80;
    server_name ${DOMAIN} www.${DOMAIN};

    location / {
        proxy_pass http://127.0.0.1:${APP_PORT};
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF

ln -sf "/etc/nginx/sites-available/${DOMAIN}" "/etc/nginx/sites-enabled/${DOMAIN}"
nginx -t && systemctl reload nginx

certbot --nginx -d "${DOMAIN}" -d "www.${DOMAIN}" --non-interactive --agree-tos -m "${EMAIL}" --redirect

echo "TLS_OK https://${DOMAIN}/"
