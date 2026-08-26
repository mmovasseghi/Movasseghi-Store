#!/usr/bin/env bash
# Cloud Agent start: per-boot reconciliation.
# Ensures PostgreSQL is running before the dev server terminal starts.
# Must tolerate restarts and return once the database is ready.
set -euo pipefail

PG_VERSION=16
PG_CLUSTER=main

echo "==> Starting PostgreSQL cluster (idempotent)"
sudo pg_ctlcluster "$PG_VERSION" "$PG_CLUSTER" start 2>/dev/null || true

echo "==> Waiting for PostgreSQL to accept connections"
for _ in $(seq 1 30); do
  if sudo -u postgres pg_isready -q; then
    echo "==> PostgreSQL is ready"
    exit 0
  fi
  sleep 1
done

echo "!! PostgreSQL did not become ready in time" >&2
exit 1
