#!/usr/bin/env bash
set -euo pipefail

cd /var/www/movasseghi/app

sudo -u postgres psql -d movasseghi <<'SQL'
INSERT INTO payload_migrations (name, batch, created_at, updated_at)
VALUES ('20260826_144329_release_schema', 1, NOW(), NOW())
ON CONFLICT DO NOTHING;
SQL

tar -xf /tmp/release.tar -C /var/www/movasseghi/app
rm -rf /var/www/movasseghi/app/media-master/legacy-uploads
mkdir -p /var/www/movasseghi/app/media-master
tar -xf /tmp/media-master.tar -C /var/www/movasseghi/app/media-master
rm -f /tmp/release.tar /tmp/media-master.tar

echo "Masters on disk: $(find /var/www/movasseghi/app/media-master -type f | wc -l)"

npx payload migrate
npm run import:media
npm run build
pm2 restart movasseghi
sleep 4

echo "MEDIA_COUNT=$(sudo -u postgres psql -d movasseghi -tAc 'SELECT count(*) FROM media;')"
echo "PRODUCTS_WITH_IMAGE=$(sudo -u postgres psql -d movasseghi -tAc 'SELECT count(*) FROM products WHERE featured_image_id IS NOT NULL;')"
curl -s -o /dev/null -w 'home:%{http_code} shop:%{http_code}\n' http://127.0.0.1:3000/ http://127.0.0.1:3000/shop
