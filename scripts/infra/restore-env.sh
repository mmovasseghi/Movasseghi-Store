#!/usr/bin/env bash
set -euo pipefail
APP_DIR="/var/www/movasseghi/app"
SECRET=$(openssl rand -hex 32)
cat > "$APP_DIR/.env" <<EOF
DATABASE_URL=postgresql://movasseghi:movasseghi_prod_change_me@127.0.0.1:5432/movasseghi
PAYLOAD_SECRET=${SECRET}
NEXT_PUBLIC_SITE_URL=http://91.107.181.79
NODE_ENV=production
EOF
chmod 600 "$APP_DIR/.env"
echo "ENV_RESTORED"
