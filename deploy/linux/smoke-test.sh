#!/usr/bin/env bash
# Smoke tests against local Kestrel or public URL
set -euo pipefail
BASE="${1:-http://127.0.0.1:5080}"
fail=0

check() {
  local path="$1" expect="$2"
  local code
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 25 "${BASE}${path}" || echo "000")
  if [[ "$code" != "$expect" ]]; then
    echo "FAIL $path expected $expect got $code"
    fail=1
  else
    echo "OK   $path $code"
  fi
}

check "/" "200"
check "/Catalog" "200"
check "/Blog" "200"
check "/Admin/Auth/Login" "200"
check "/css/site.css" "200"
check "/js/site.js" "200"

if [[ "$fail" -ne 0 ]]; then
  exit 1
fi
echo "==> All smoke checks passed"
