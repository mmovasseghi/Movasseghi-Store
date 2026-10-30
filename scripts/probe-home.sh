#!/usr/bin/env bash
set -euo pipefail
curl -sI http://127.0.0.1:5080/images/products/0071/01.webp | head -3
curl -s http://127.0.0.1:5080/ | grep -o 'src="/[^"]*"' | head -20
echo '---'
curl -sI http://185.93.88.81/images/products/0071/01.webp | head -3
