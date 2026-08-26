#!/usr/bin/env bash
# App deploy on server — code must exist in APP_DIR (via git archive or pull)
set -euo pipefail

APP_DIR="${APP_DIR:-/var/www/movasseghi/app}"

if [ ! -f "$APP_DIR/package.json" ]; then
  echo "Missing $APP_DIR/package.json — deploy code first"
  exit 1
fi

cd "$APP_DIR"

if [ ! -f .env ]; then
  SECRET=$(openssl rand -hex 32)
  cat > .env <<EOF
DATABASE_URL=postgresql://movasseghi:movasseghi_prod_change_me@127.0.0.1:5432/movasseghi
PAYLOAD_SECRET=${SECRET}
NEXT_PUBLIC_SITE_URL=http://91.107.181.79
NODE_ENV=production
ADMIN_EMAIL=admin@movasseghi.ir
ADMIN_PASSWORD=${SECRET}
EOF
  chmod 600 .env
fi

npm ci
npm run build
npm run seed:legacy || true

pm2 delete movasseghi 2>/dev/null || true
pm2 start npm --name movasseghi -- start
pm2 save

echo "DEPLOY_OK $(curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/ || echo fail)"
