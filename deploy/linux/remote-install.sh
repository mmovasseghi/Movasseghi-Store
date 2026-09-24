#!/usr/bin/env bash
# Run on VPS as root: curl -sSL ... | bash   OR   bash remote-install.sh
set -euo pipefail

INSTALL_DIR="/MOVASSEGHISTORE"
REPO_PATH="mmovasseghi/Movasseghi-Store.git"
if [[ -n "${GITHUB_TOKEN:-}" ]]; then
  REPO_URL="https://x-access-token:${GITHUB_TOKEN}@github.com/${REPO_PATH}"
else
  REPO_URL="https://github.com/${REPO_PATH}"
fi
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
  if [[ -n "${GITHUB_TOKEN:-}" ]]; then
    git -C "$INSTALL_DIR/src" remote set-url origin "$REPO_URL"
  fi
  git -C "$INSTALL_DIR/src" pull --ff-only
fi

cd "$INSTALL_DIR/src"
npm ci
npm run build:rte

BACKUP_DIR="$INSTALL_DIR/backups/$(date +%Y%m%d-%H%M%S)"
mkdir -p "$BACKUP_DIR"
if [[ -f "$INSTALL_DIR/publish/movasseghi.db" ]]; then
  cp -a "$INSTALL_DIR/publish/movasseghi.db" "$BACKUP_DIR/"
  echo "==> Backed up movasseghi.db to $BACKUP_DIR"
fi
if [[ -f "$INSTALL_DIR/publish/appsettings.Production.json" ]]; then
  cp -a "$INSTALL_DIR/publish/appsettings.Production.json" "$BACKUP_DIR/"
fi
if [[ -d "$INSTALL_DIR/publish/App_Data" ]]; then
  cp -a "$INSTALL_DIR/publish/App_Data" "$BACKUP_DIR/" 2>/dev/null || true
fi

PUBLISH_NEW="$INSTALL_DIR/publish-next"
rm -rf "$PUBLISH_NEW"
mkdir -p "$PUBLISH_NEW"

echo "==> Publishing to $PUBLISH_NEW (service still running)..."
dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$PUBLISH_NEW"

echo "==> Stopping $SERVICE_NAME and swapping publish folders..."
systemctl stop "$SERVICE_NAME" 2>/dev/null || true
sleep 2
pkill -f '/MOVASSEGHISTORE/publish/MovasseghiShop.Web' 2>/dev/null || true
sleep 1
rm -rf "$INSTALL_DIR/publish-old"
if [[ -d "$INSTALL_DIR/publish" ]]; then
  mv "$INSTALL_DIR/publish" "$INSTALL_DIR/publish-old"
fi
mv "$PUBLISH_NEW" "$INSTALL_DIR/publish"

if [[ -f "$BACKUP_DIR/movasseghi.db" ]]; then
  cp -a "$BACKUP_DIR/movasseghi.db" "$INSTALL_DIR/publish/"
elif [[ -f "$INSTALL_DIR/publish-old/movasseghi.db" ]]; then
  cp -a "$INSTALL_DIR/publish-old/movasseghi.db" "$INSTALL_DIR/publish/"
  echo "==> Restored movasseghi.db after publish"
fi
if [[ -f "$BACKUP_DIR/appsettings.Production.json" ]]; then
  cp -a "$BACKUP_DIR/appsettings.Production.json" "$INSTALL_DIR/publish/"
elif [[ -f "$INSTALL_DIR/publish-old/appsettings.Production.json" ]]; then
  cp -a "$INSTALL_DIR/publish-old/appsettings.Production.json" "$INSTALL_DIR/publish/"
else
  cp src/MovasseghiShop.Web/appsettings.Production.json.example \
    "$INSTALL_DIR/publish/appsettings.Production.json"
fi
if [[ -d "$BACKUP_DIR/App_Data" ]]; then
  mkdir -p "$INSTALL_DIR/publish/App_Data"
  cp -a "$BACKUP_DIR/App_Data/." "$INSTALL_DIR/publish/App_Data/" 2>/dev/null || true
elif [[ -d "$INSTALL_DIR/publish-old/App_Data" ]]; then
  mkdir -p "$INSTALL_DIR/publish/App_Data"
  cp -a "$INSTALL_DIR/publish-old/App_Data/." "$INSTALL_DIR/publish/App_Data/" 2>/dev/null || true
fi

if [[ ! -f "$INSTALL_DIR/publish/appsettings.Production.json" ]]; then
  cp src/MovasseghiShop.Web/appsettings.Production.json.example \
    "$INSTALL_DIR/publish/appsettings.Production.json"
fi

# First-time defaults only (preserve restored Production config)
python3 - <<'PY'
import json, pathlib
p = pathlib.Path("/MOVASSEGHISTORE/publish/appsettings.Production.json")
data = json.loads(p.read_text(encoding="utf-8"))
data["AllowedHosts"] = "*"
data["PathBase"] = "/MOVASSEGHISTORE"
data.setdefault("ConnectionStrings", {})["DefaultConnection"] = "Data Source=movasseghi.db"
data["SiteSettings"] = data.get("SiteSettings") or {}
data["SiteSettings"]["PublicBaseUrl"] = "http://85.133.244.142/MOVASSEGHISTORE"
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

# nginx: shop under /MOVASSEGHISTORE only; root empty; keep /Airlock
cat > /etc/nginx/sites-available/movasseghi-shop <<'NGX'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;
    client_max_body_size 32M;

    location = / {
        default_type text/plain;
        return 204;
    }

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

    location = /MOVASSEGHISTORE {
        return 301 /MOVASSEGHISTORE/;
    }

    location /MOVASSEGHISTORE/ {
        proxy_pass http://127.0.0.1:5080/MOVASSEGHISTORE/;
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

echo "==> Waiting for Kestrel (up to 3 min on warm DB)..."
ready=0
for _ in $(seq 1 36); do
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 http://127.0.0.1:5080/MOVASSEGHISTORE/ 2>/dev/null || echo "000")
  if [[ "$code" == "200" ]]; then ready=1; break; fi
  sleep 5
done
if [[ "$ready" -ne 1 ]]; then
  echo "WARN: app not returning 200 yet; check journalctl -u $SERVICE_NAME"
  journalctl -u "$SERVICE_NAME" -n 25 --no-pager || true
fi

bash "$INSTALL_DIR/src/deploy/linux/smoke-test.sh" "http://127.0.0.1:5080" "/MOVASSEGHISTORE" || true
bash "$INSTALL_DIR/src/deploy/linux/smoke-test.sh" "http://127.0.0.1" "/MOVASSEGHISTORE" || true
root_code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 5 http://127.0.0.1/ 2>/dev/null || echo "000")
echo "Root / HTTP $root_code (expect 204)"
systemctl is-active "$SERVICE_NAME"

echo "==> Install complete: http://85.133.244.142/MOVASSEGHISTORE/"
