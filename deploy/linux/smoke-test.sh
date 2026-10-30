#!/usr/bin/env bash
# Usage: smoke-test.sh [BASE_URL] [PATH_PREFIX]
# Example: smoke-test.sh http://127.0.0.1 ""
set -euo pipefail
BASE="${1:-http://127.0.0.1:5080}"
PREFIX="${2:-}"
fail=0

check() {
  local path="$1" expect="$2"
  local code
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 25 "${BASE}${path}" || echo "000")
  if [[ "$code" != "$expect" ]]; then
    echo "FAIL ${path} expected ${expect} got ${code}"
    fail=1
  else
    echo "OK   ${path} ${code}"
  fi
}

check "${PREFIX}/" "200"
check "${PREFIX}/Catalog" "200"
check "${PREFIX}/Blog" "200"
check "${PREFIX}/Admin/Auth/Login" "200"
check "${PREFIX}/css/site.css" "200"
check "${PREFIX}/js/site.js" "200"
check "${PREFIX}/css/storefront.bundle.css" "200"
check "${PREFIX}/js/ms-gsap-boot.js" "200"
check "${PREFIX}/Home/Stories.json" "200"
check "${PREFIX}/llms.txt" "200"

cache=$(curl -sI --max-time 25 "${BASE}${PREFIX}/" | tr -d '\r' | grep -i '^cache-control:' || true)
if echo "$cache" | grep -qi 'max-age=900'; then
  echo "OK   / Cache-Control public max-age"
else
  echo "FAIL / Cache-Control missing public max-age (got: ${cache:-none})"
  fail=1
fi

check "${PREFIX}/images/products/0071/01.webp" "200"
check "/images/products/0071/01.webp" "200"

if [[ "$fail" -ne 0 ]]; then
  exit 1
fi
echo "==> All smoke checks passed"
