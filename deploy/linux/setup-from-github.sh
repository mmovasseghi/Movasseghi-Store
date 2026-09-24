#!/usr/bin/env bash
# نصب فروشگاه موثقی روی Ubuntu/Debian — اجرا با کاربر sudo-capable
# Usage: sudo bash setup-from-github.sh [install_dir]
set -euo pipefail

INSTALL_DIR="${1:-/opt/MOVASSEGHISTORE}"
REPO_URL="https://github.com/mmovasseghi/Movasseghi-Store.git"
DATA_DIR="/var/lib/movasseghi"
SERVICE_NAME="movasseghi-shop"

echo "==> Install dir: $INSTALL_DIR"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "==> Installing .NET 10 SDK (Microsoft package feed)..."
  wget -q https://packages.microsoft.com/config/ubuntu/$(. /etc/os-release; echo "$VERSION_ID")/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  apt-get update
  apt-get install -y dotnet-sdk-10.0 aspnetcore-runtime-10.0
fi

if ! command -v node >/dev/null 2>&1; then
  echo "==> Installing Node.js 20 LTS..."
  curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  apt-get install -y nodejs
fi

mkdir -p "$INSTALL_DIR"
if [[ ! -d "$INSTALL_DIR/src/.git" ]]; then
  git clone "$REPO_URL" "$INSTALL_DIR/src"
else
  cd "$INSTALL_DIR/src" && git pull --ff-only
fi

cd "$INSTALL_DIR/src"
npm ci
npm run build:rte

dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj \
  -c Release \
  -o "$INSTALL_DIR/publish"

mkdir -p "$DATA_DIR"
chown -R www-data:www-data "$DATA_DIR"

if [[ ! -f "$INSTALL_DIR/publish/appsettings.Production.json" ]]; then
  cp src/MovasseghiShop.Web/appsettings.Production.json.example \
    "$INSTALL_DIR/publish/appsettings.Production.json"
  echo "!! Edit $INSTALL_DIR/publish/appsettings.Production.json (domain, secrets)"
fi

# SQLite + uploads live next to app unless overridden in config
touch "$DATA_DIR/.keep"
chown -R www-data:www-data "$INSTALL_DIR/publish"

cp deploy/linux/movasseghi-shop.service "/etc/systemd/system/${SERVICE_NAME}.service"
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl restart "$SERVICE_NAME"

if command -v nginx >/dev/null 2>&1; then
  cp deploy/linux/nginx-movasseghi.conf /etc/nginx/sites-available/movasseghi
  ln -sf /etc/nginx/sites-available/movasseghi /etc/nginx/sites-enabled/movasseghi
  nginx -t && systemctl reload nginx
fi

echo "==> Done. App listens on http://127.0.0.1:5080 (behind nginx on :80)"
echo "==> Admin: /Admin/Auth/Login — change default admin password after first login."
