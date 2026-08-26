#!/usr/bin/env bash
set -euo pipefail
cd /var/www/movasseghi/app
tar -xf /tmp/release.tar -C /var/www/movasseghi/app
rm -f /tmp/release.tar
npx payload migrate
npm run sync:content
npm run build
pm2 restart movasseghi
sleep 4
echo "WITH_CONTENT=$(sudo -u postgres psql -d movasseghi -tAc \"SELECT count(*) FROM products WHERE legacy_description_html IS NOT NULL AND length(legacy_description_html) > 100;\")"
curl -s -o /dev/null -w 'shop:%{http_code}\n' http://127.0.0.1:3000/shop
