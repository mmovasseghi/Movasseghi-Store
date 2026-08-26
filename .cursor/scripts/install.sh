#!/usr/bin/env bash
# Cloud Agent install: idempotent repository bootstrap.
# Prepares PostgreSQL, project dependencies, generated Payload artifacts,
# database schema (migrations) and seed data. Safe to run repeatedly.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

PG_VERSION=16
PG_CLUSTER=main

echo "==> Ensuring PostgreSQL ${PG_VERSION} is installed"
if ! command -v pg_ctlcluster >/dev/null 2>&1; then
  sudo apt-get update -qq
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq postgresql postgresql-contrib
fi

echo "==> Starting PostgreSQL cluster (idempotent)"
sudo pg_ctlcluster "$PG_VERSION" "$PG_CLUSTER" start 2>/dev/null || true

# Wait for the server to accept connections.
for _ in $(seq 1 30); do
  if sudo -u postgres pg_isready -q; then break; fi
  sleep 1
done

echo "==> Ensuring dev role and database exist"
sudo -u postgres psql -tc "SELECT 1 FROM pg_roles WHERE rolname='movasseghi'" | grep -q 1 \
  || sudo -u postgres psql -c "CREATE USER movasseghi WITH PASSWORD 'movasseghi' SUPERUSER;"
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname='movasseghi'" | grep -q 1 \
  || sudo -u postgres psql -c "CREATE DATABASE movasseghi OWNER movasseghi;"

echo "==> Ensuring local .env exists"
if [ ! -f .env ]; then
  cat > .env <<'EOF'
# PostgreSQL (local dev)
DATABASE_URL=postgresql://movasseghi:movasseghi@localhost:5432/movasseghi

# Payload secret (dev only — not a production value)
PAYLOAD_SECRET=dev_secret_0123456789abcdef0123456789abcdef

# Public site URL
NEXT_PUBLIC_SITE_URL=http://localhost:3000

# Seed admin user (used by seed:legacy)
ADMIN_EMAIL=admin@movasseghi.local
ADMIN_PASSWORD=Admin12345!
EOF
fi

echo "==> Installing Node dependencies"
npm ci

echo "==> Generating Payload import map and types"
npm run generate:importmap
npm run generate:types

echo "==> Applying database migrations"
npm run payload -- migrate

echo "==> Seeding catalog from legacy bundle (idempotent)"
npm run seed:legacy || echo "seed skipped/partial (data may already exist)"

echo "==> Install complete"
