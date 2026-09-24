#!/usr/bin/env bash
# Run on VPS as root: curl -sSL ... | bash   OR   bash remote-install.sh
set -euo pipefail

INSTALL_DIR="/MOVASSEGHISTORE"
REPO_URL="https://github.com/mmovasseghi/Movasseghi-Store.git"
SERVICE_NAME="movasseghi-shop"
DOTNET_ROOT="/usr/share/dotnet"

export DEBIAN_FRONTEND=noninteractive

install_dotnet() {
  if [[ -x "$DOTNET_ROOT/dotnet" ]]; then
    return 0
  fi
  echo "==> Installing .NET 10 SDK via dotnet-install.sh"
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh --channel 10.0 --install-dir "$DOTNET_ROOT"
  ln -sf "$DOTNET_ROOT/dotnet" /usr/local/bin/dotnet
}

install_node() {
  if command -v node >/dev/null 2>&1; then
    return 0
  fi
  echo "==> Installing Node.js 20"
  curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
  apt-get install -y nodejs
}

export PATH="$PATH:$DOTNET_ROOT"
install_dotnet
install_node

mkdir -p "$INSTALL_DIR"
if [[ ! -d "$INSTALL_DIR/src/.git" ]]; then
  git clone "$REPO_URL" "$INSTALL_DIR/src"
else
  git -C "$INSTALL_DIR/src" pull --ff-only
fi

cd "$INSTALL_DIR/src"
npm ci
npm run build:rte

dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$INSTALL_DIR/publish"

if [[ ! -f "$INSTALL_DIR/publish/appsettings.Production.json" ]]; then
  cp src/MovasseghiShop.Web/appsettings.Production.json.example \
    "$INSTALL_DIR/publish/appsettings.Production.json"
fi

# IP deploy until domain is pointed
python3 - <<'PY'
import json, pathlib
p = pathlib.Path("/MOVASSEGHISTORE/publish/appsettings.Production.json")
data = json.loads(p.read_text(encoding="utf-8"))
data["AllowedHosts"] = "*"
data.setdefault("SiteSettings", {})["PublicBaseUrl"] = "http://85.133.244.142"
data.setdefault("EditorialGrowth", {})["Enabled"] = False
data.setdefault("Seo", {}).setdefault("Maintenance", {})["RunFullCatalogOnStartup"] = False
if not data.get("Security", {}).get("CheckoutHmacSecret"):
    data.setdefault("Security", {})["CheckoutHmacSecret"] = "change-me-after-first-login"
p.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
PY

chmod +x "$INSTALL_DIR/publish/MovasseghiShop.Web"
chown -R www-data:www-data "$INSTALL_DIR/publish"

cat > "/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=Movasseghi Shop (ASP.NET Core)
After=network.target

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=$INSTALL_DIR/publish
ExecStart=$INSTALL_DIR/publish/MovasseghiShop.Web
Environment=DOTNET_ROOT=$DOTNET_ROOT
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5080
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=movasseghi-shop

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
systemctl restart "$SERVICE_NAME"

# nginx: shop on /, keep existing /Airlock proxy
cat > /etc/nginx/sites-available/movasseghi-shop <<'NGX'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;
    client_max_body_size 32M;

    location /Airlock {
        proxy_pass http://127.0.0.1:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    location / {
        proxy_pass http://127.0.0.1:5080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
NGX

rm -f /etc/nginx/sites-enabled/weswap-landing
ln -sf /etc/nginx/sites-available/movasseghi-shop /etc/nginx/sites-enabled/movasseghi-shop
nginx -t
systemctl reload nginx

sleep 3
curl -sI -o /dev/null -w "HTTP %{http_code}\n" http://127.0.0.1:5080/ || true
curl -sI -o /dev/null -w "HTTP %{http_code}\n" http://127.0.0.1/ || true
systemctl is-active "$SERVICE_NAME"

echo "==> Install complete: http://85.133.244.142"
