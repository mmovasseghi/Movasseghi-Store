#!/usr/bin/env bash
set -euo pipefail
export PATH="/usr/share/dotnet:/usr/local/bin:/usr/bin:/bin"
INSTALL_DIR="/MOVASSEGHISTORE"
cd "$INSTALL_DIR/src"
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
chown -R www-data:www-data "$INSTALL_DIR/publish"
systemctl start movasseghi-shop
sleep 6
curl -s -o /dev/null -w "media:%{http_code}:%{size_download}\n" \
  "http://127.0.0.1:5080/media/320/images/products/0056/01.webp"
