#!/usr/bin/env bash
# Full release setup on Hetzner — run as root once, then deploy as deploy user
set -euo pipefail

export DEBIAN_FRONTEND=noninteractive
APP_DIR="/var/www/movasseghi/app"
REPO="https://github.com/mmovasseghi/Movasseghi-Store.git"

echo "==> Installing system packages"
apt-get update -qq
apt-get install -y -qq curl git nginx ufw fail2ban postgresql postgresql-contrib

# Node 22
if ! command -v node >/dev/null 2>&1 || [[ "$(node -v)" != v22* ]]; then
  curl -fsSL https://deb.nodesource.com/setup_22.x | bash -
  apt-get install -y -qq nodejs
fi

# PM2
npm install -g pm2

# Deploy user
if ! id deploy >/dev/null 2>&1; then
  useradd -m -s /bin/bash deploy
  usermod -aG www-data deploy
fi

# PostgreSQL
sudo -u postgres psql -tc "SELECT 1 FROM pg_roles WHERE rolname='movasseghi'" | grep -q 1 || \
  sudo -u postgres psql -c "CREATE USER movasseghi WITH PASSWORD 'movasseghi_prod_change_me';"
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname='movasseghi'" | grep -q 1 || \
  sudo -u postgres psql -c "CREATE DATABASE movasseghi OWNER movasseghi;"

mkdir -p "$APP_DIR"
chown -R deploy:www-data "$APP_DIR"

# Clone or update app
if [ ! -d "$APP_DIR/.git" ]; then
  sudo -u deploy git clone --depth 1 "$REPO" "$APP_DIR"
else
  sudo -u deploy bash -c "cd $APP_DIR && git pull origin main"
fi

# Environment file (secrets must be rotated in production)
ENV_FILE="$APP_DIR/.env"
if [ ! -f "$ENV_FILE" ]; then
  SECRET=$(openssl rand -hex 32)
  cat > "$ENV_FILE" <<EOF
DATABASE_URL=postgresql://movasseghi:movasseghi_prod_change_me@127.0.0.1:5432/movasseghi
PAYLOAD_SECRET=${SECRET}
NEXT_PUBLIC_SITE_URL=http://91.107.181.79
NODE_ENV=production
ADMIN_EMAIL=admin@movasseghi.ir
ADMIN_PASSWORD=${SECRET}
EOF
  chown deploy:www-data "$ENV_FILE"
  chmod 600 "$ENV_FILE"
fi

echo "==> Building app"
sudo -u deploy bash -c "cd $APP_DIR && npm ci && npm run build"

echo "==> Seeding legacy products (if migration-data.json present)"
if [ -f "$APP_DIR/scripts/import/migration-data.json" ]; then
  sudo -u deploy bash -c "cd $APP_DIR && npm run seed:legacy" || echo "Seed skipped or partial"
fi

# Nginx reverse proxy — release (indexable on IP until domain)
cat > /etc/nginx/sites-available/movasseghi <<'NGINX'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;
    client_max_body_size 50M;

    location / {
        proxy_pass http://127.0.0.1:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
NGINX

ln -sf /etc/nginx/sites-available/movasseghi /etc/nginx/sites-enabled/movasseghi
rm -f /etc/nginx/sites-enabled/default /etc/nginx/sites-enabled/movasseghi-staging 2>/dev/null || true
nginx -t && systemctl reload nginx

# PM2 as deploy
sudo -u deploy bash -c "cd $APP_DIR && pm2 delete movasseghi 2>/dev/null || true"
sudo -u deploy bash -c "cd $APP_DIR && pm2 start npm --name movasseghi -- start"
sudo -u deploy pm2 save
env PATH=$PATH:/usr/bin pm2 startup systemd -u deploy --hp /home/deploy | tail -1 | bash || true

ufw allow OpenSSH
ufw allow 'Nginx Full'
ufw --force enable

echo "RELEASE_OK app=$APP_DIR node=$(node -v)"
