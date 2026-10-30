#!/usr/bin/env bash
set -euo pipefail
export PATH="/usr/share/dotnet:/usr/local/bin:/usr/bin:/bin"
INSTALL_DIR="/MOVASSEGHISTORE"
cd "$INSTALL_DIR/src"
git pull --ff-only origin main || true
PUBLISH_NEW="$INSTALL_DIR/publish-next"
rm -rf "$PUBLISH_NEW"
dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj \
  -c Release -r linux-x64 --self-contained true -o "$PUBLISH_NEW"
systemctl stop movasseghi-shop || true
sleep 2
rm -rf "$INSTALL_DIR/publish-old"
mv "$INSTALL_DIR/publish" "$INSTALL_DIR/publish-old"
mv "$PUBLISH_NEW" "$INSTALL_DIR/publish"
cp -a "$INSTALL_DIR/publish-old/movasseghi.db" \
  "$INSTALL_DIR/publish-old/appsettings.Production.json" \
  "$INSTALL_DIR/publish/"
if [[ -d "$INSTALL_DIR/publish-old/App_Data" ]]; then
  cp -a "$INSTALL_DIR/publish-old/App_Data" "$INSTALL_DIR/publish/"
fi
if [[ -d "$INSTALL_DIR/publish-old/wwwroot/images" ]]; then
  mkdir -p "$INSTALL_DIR/publish/wwwroot/images"
  cp -a "$INSTALL_DIR/publish-old/wwwroot/images/." "$INSTALL_DIR/publish/wwwroot/images/" 2>/dev/null || true
fi
chown -R www-data:www-data "$INSTALL_DIR/publish"
if [[ "${RUN_OPTIMIZE_IMAGES:-1}" == "1" ]]; then
  echo "==> Optimizing product images (-lcp.webp for hero)..."
  (cd "$INSTALL_DIR/publish" && "$INSTALL_DIR/publish/MovasseghiShop.Web" --optimize-images) || echo "WARN: --optimize-images failed"
fi
systemctl start movasseghi-shop
sleep 6
if [[ -f "$INSTALL_DIR/src/deploy/linux/nginx-movasseghi.conf" ]]; then
  mkdir -p /var/cache/nginx/ms
  printf '%s\n' 'proxy_cache_path /var/cache/nginx/ms levels=1:2 keys_zone=ms_pages:8m max_size=64m inactive=20m use_temp_path=off;' > /etc/nginx/conf.d/ms-cache.conf
  cp "$INSTALL_DIR/src/deploy/linux/nginx-movasseghi.conf" /etc/nginx/sites-available/movasseghi-shop
  ln -sf /etc/nginx/sites-available/movasseghi-shop /etc/nginx/sites-enabled/movasseghi-shop
  rm -f /etc/nginx/sites-enabled/default 2>/dev/null || true
  /usr/sbin/nginx -t && systemctl reload nginx
fi
curl -s -o /dev/null -w "home:%{http_code}\n" --max-time 15 http://127.0.0.1:5080/
curl -s -o /dev/null -w "lcp:%{http_code}:%{size_download}\n" \
  --max-time 10 "http://127.0.0.1:5080/images/products/0056/01-lcp.webp" || echo "lcp:missing"
