#!/usr/bin/env bash
# Deploy Movasseghi Store to Hetzner staging
# Requires: SSH key, deploy user, secrets in GitHub Actions
set -euo pipefail

APP_DIR="${APP_DIR:-/var/www/movasseghi-staging/app}"
BRANCH="${BRANCH:-main}"
REPO="${REPO:-git@github.com:mmovasseghi/Movasseghi-Store.git}"

echo "==> Deploying to ${APP_DIR} (${BRANCH})"

mkdir -p "$(dirname "$APP_DIR")"
if [ ! -d "$APP_DIR/.git" ]; then
  git clone --depth 1 -b "$BRANCH" "$REPO" "$APP_DIR"
fi

cd "$APP_DIR"
git fetch origin "$BRANCH"
git reset --hard "origin/$BRANCH"

npm ci
npm run build

# PM2 or systemd restart — adjust when process manager is configured
if command -v pm2 >/dev/null 2>&1; then
  pm2 restart movasseghi-staging || pm2 start npm --name movasseghi-staging -- start
else
  echo "PM2 not installed — run: npm start manually or configure systemd"
fi

echo "==> Deploy complete"
