#!/usr/bin/env bash
# Update an existing /MOVASSEGHISTORE installation; run as root on the VPS.
set -Eeuo pipefail
umask 077
export PATH="/usr/share/dotnet:/usr/local/bin:/usr/bin:/bin:$PATH"
INSTALL_DIR="/MOVASSEGHISTORE"
SERVICE="movasseghi-shop"
# Route the loopback probe through the same virtual host as production.
MS_SMOKE_HOST="${MS_SMOKE_HOST:-185.93.88.81}"
SITE="/etc/nginx/sites-available/movasseghi-shop"
LINK="/etc/nginx/sites-enabled/movasseghi-shop"
CACHE_CONF="/etc/nginx/conf.d/ms-cache.conf"
CACHE_DIR="/var/cache/nginx/ms"
# Shared locations included by nginx-movasseghi.conf (must ship with the site file).
PROXY_INC="/etc/nginx/snippets/movasseghi-shop-proxy.inc"
PROXY_SRC="deploy/linux/nginx-movasseghi-proxy.inc"
MARKER="$INSTALL_DIR/.deploying"
STAGING=""
BACKUP=""
stopped=0
swapped=0
nginx_changed=0
assets_built=0
own_marker=0
committed=0
proxy_inc_existed=0

restore_nginx() {
  local name destination
  # Restore site + include together so a partial rollback cannot leave an include-less site.
  for name in site link cache proxy; do
    case "$name" in
      site) destination="$SITE";;
      link) destination="$LINK";;
      cache) destination="$CACHE_CONF";;
      proxy) destination="$PROXY_INC";;
    esac
    rm -f "$destination"
    if [[ -e "$BACKUP/nginx/$name" || -L "$BACKUP/nginx/$name" ]]; then
      mkdir -p "$(dirname "$destination")"
      cp -a "$BACKUP/nginx/$name" "$destination" || return 1
    elif [[ "$name" == proxy && "$proxy_inc_existed" == 0 ]]; then
      # Include was newly introduced this release; remove on rollback.
      rm -f "$PROXY_INC"
    fi
  done
  nginx -t && systemctl reload nginx
}

finish() {
  local code=$?
  trap - EXIT
  set +e
  if [[ "$code" -ne 0 && "$committed" == 0 ]]; then
    echo "ERROR: deployment failed; diagnostics retained in ${BACKUP:-the current checkout}." >&2
    local recovered=1 ready=0
    if [[ "$stopped" == 1 ]]; then
      systemctl stop "$SERVICE" || recovered=0
      if [[ "$swapped" == 1 ]]; then
        if [[ -d "$INSTALL_DIR/publish" ]]; then
          mv "$INSTALL_DIR/publish" "$BACKUP/failed-publish" || recovered=0
        fi
        mv "$BACKUP/previous-publish" "$INSTALL_DIR/publish" || recovered=0
      fi
      systemctl start "$SERVICE" || recovered=0
      for ((i=0; i<30; i++)); do
        if [[ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 2 http://127.0.0.1:5080/ 2>/dev/null)" == 200 ]]; then ready=1; break; fi
        sleep 1
      done
      [[ "$ready" == 1 ]] || recovered=0
    fi
    # Do not expose a failed new app by restoring rules without maintenance.
    if [[ "$recovered" == 1 && "$nginx_changed" == 1 ]]; then
      restore_nginx || recovered=0
    fi
    if [[ "$recovered" == 1 ]]; then
      [[ "$own_marker" == 0 ]] || rm -f "$MARKER"
      echo "Previous release/configuration restored; deployment remains FAILED." >&2
    else
      echo "CRITICAL: recovery needs operator attention; maintenance marker retained." >&2
    fi
  fi
  if [[ "$assets_built" == 1 ]]; then
    git -C "$INSTALL_DIR/src" restore -- src/MovasseghiShop.Web/wwwroot/js/rich-text-editor.bundle.js \
      src/MovasseghiShop.Web/wwwroot/css/storefront.bundle.css src/MovasseghiShop.Web/wwwroot/css/storefront-home.bundle.css \
      src/MovasseghiShop.Web/wwwroot/css/storefront-cart.bundle.css src/MovasseghiShop.Web/wwwroot/css/critical-home.css
  fi
  [[ -z "$STAGING" || ! -d "$STAGING" ]] || rm -rf "$STAGING"
  exit "$code"
}
trap finish EXIT

[[ "$(id -u)" == 0 ]] || { echo "Run as root on the VPS" >&2; exit 1; }
for command in git npm dotnet python3 nginx curl systemctl flock ss; do command -v "$command" >/dev/null; done
exec 9>"$INSTALL_DIR/.deploy.lock"
flock -n 9 || { echo "Another deployment is running" >&2; exit 1; }
[[ ! -e "$MARKER" ]] || { echo "Existing maintenance marker requires investigation" >&2; exit 1; }
[[ -f "$INSTALL_DIR/publish/movasseghi.db" && -f "$INSTALL_DIR/publish/appsettings.Production.json" ]]
python3 - "$INSTALL_DIR/publish/appsettings.Production.json" "$INSTALL_DIR/publish" <<'PY'
import json, pathlib, sys
settings = {k.lower(): v for k, v in json.load(open(sys.argv[1])).items()}
connections = {k.lower(): v for k, v in settings.get('connectionstrings', {}).items()}
connection = connections.get('defaultconnection', 'Data Source=movasseghi.db')
parts = dict(item.split('=', 1) for item in connection.split(';') if '=' in item)
parts = {k.lower().replace(' ', ''): v.strip().strip('\"\'') for k, v in parts.items()}
database = parts.get('datasource', parts.get('filename'))
assert database and (pathlib.Path(sys.argv[2]) / database).resolve() == (pathlib.Path(sys.argv[2]) / 'movasseghi.db').resolve(), 'Unexpected SQLite location; adapt persistence before deploying'
PY
[[ "$(systemctl show "$SERVICE" -p WorkingDirectory --value)" == "$INSTALL_DIR/publish" ]]
systemctl is-active --quiet "$SERVICE"
ss -H -ltn 'sport = :5080' | python3 -c 'import sys; rows=[r.split() for r in sys.stdin if r.strip()]; assert rows and all(r[3] in ("127.0.0.1:5080", "[::1]:5080") for r in rows), "Kestrel must listen only on loopback during deployment"'
cd "$INSTALL_DIR/src"
[[ -z "$(git status --porcelain)" ]] || { echo "Preserve checkout changes before deploying" >&2; exit 1; }
git pull --ff-only origin main
SHA="$(git rev-parse HEAD)"
BACKUP="$(mktemp -d "$INSTALL_DIR/backups-release.XXXXXXXX")"
STAGING="$(mktemp -d "$INSTALL_DIR/publish-next.XXXXXXXX")"
echo "Preparing release $SHA; backup directory: $BACKUP"
assets_built=1
npm ci --registry=https://registry.npmjs.org --replace-registry-host=always
npm run build
dotnet publish src/MovasseghiShop.Web/MovasseghiShop.Web.csproj \
  -c Release -r linux-x64 --self-contained true -o "$STAGING"
printf '%s\n' "$SHA" > "$STAGING/RELEASE_COMMIT"

# Prepare expensive image derivatives while the existing storefront stays online.
# The final copy/optimizer pass after stopping the service catches newer uploads.
mkdir -p "$STAGING/wwwroot/images"
if [[ -d "$INSTALL_DIR/publish/wwwroot/images" ]]; then
  cp -a "$INSTALL_DIR/publish/wwwroot/images/." "$STAGING/wwwroot/images/"
fi
if [[ "${RUN_OPTIMIZE_IMAGES:-1}" == 1 ]]; then
  (cd "$STAGING" && ASPNETCORE_ENVIRONMENT=Production ./MovasseghiShop.Web --optimize-images)
fi

# Validate maintenance-capable nginx rules before stopping the old app.
# Site conf includes PROXY_INC — install/backup/rollback both files as one unit.
[[ -f "$PROXY_SRC" ]] || { echo "Missing $PROXY_SRC (required by nginx site include)" >&2; exit 1; }
mkdir -p "$BACKUP/nginx" "$CACHE_DIR" "$(dirname "$PROXY_INC")"
for name in site link cache proxy; do
  case "$name" in
    site) source="$SITE";;
    link) source="$LINK";;
    cache) source="$CACHE_CONF";;
    proxy) source="$PROXY_INC";;
  esac
  if [[ -e "$source" || -L "$source" ]]; then
    cp -a "$source" "$BACKUP/nginx/$name"
    [[ "$name" == proxy ]] && proxy_inc_existed=1
  fi
done
nginx_changed=1
# Write include first so nginx -t against the new site file can resolve it.
cp "$PROXY_SRC" "$PROXY_INC.next"
mv "$PROXY_INC.next" "$PROXY_INC"
cp deploy/linux/nginx-movasseghi.conf "$SITE.next"
mv "$SITE.next" "$SITE"
ln -sfn "$SITE" "$LINK"
printf '%s\n' "proxy_cache_path $CACHE_DIR levels=1:2 keys_zone=ms_pages:8m max_size=64m inactive=20m use_temp_path=off;" > "$CACHE_CONF.next"
mv "$CACHE_CONF.next" "$CACHE_CONF"
# On nginx -t failure the EXIT trap restores site+include+cache from BACKUP (or removes a new include).
nginx -t
systemctl reload nginx
touch "$MARKER"
own_marker=1

# Drain requests before taking a consistent SQLite backup and copying uploads.
stopped=1
systemctl stop "$SERVICE"
python3 - "$INSTALL_DIR/publish/movasseghi.db" "$BACKUP/movasseghi.db" <<'PY'
import sqlite3, sys
with sqlite3.connect(sys.argv[1]) as source, sqlite3.connect(sys.argv[2]) as target:
    source.backup(target)
    if target.execute('pragma quick_check').fetchone()[0] != 'ok':
        raise RuntimeError('SQLite backup validation failed')
PY
cp -a "$BACKUP/movasseghi.db" "$STAGING/movasseghi.db"
cp -a "$INSTALL_DIR/publish/appsettings.Production.json" "$STAGING/"
mkdir -p "$STAGING/App_Data" "$STAGING/wwwroot/images"
if [[ -d "$INSTALL_DIR/publish/App_Data" ]]; then
  cp -a "$INSTALL_DIR/publish/App_Data/." "$STAGING/App_Data/"
fi
if [[ -d "$INSTALL_DIR/publish/wwwroot/images" ]]; then
  cp -a "$INSTALL_DIR/publish/wwwroot/images/." "$STAGING/wwwroot/images/"
fi
if [[ "${RUN_OPTIMIZE_IMAGES:-1}" == 1 ]]; then
  (cd "$STAGING" && ASPNETCORE_ENVIRONMENT=Production ./MovasseghiShop.Web --optimize-images)
fi
chown -R www-data:www-data "$STAGING"

mv "$INSTALL_DIR/publish" "$BACKUP/previous-publish"
swapped=1
mv "$STAGING" "$INSTALL_DIR/publish"
STAGING=""
systemctl start "$SERVICE"
ready=0
for ((i=0; i<60; i++)); do
  if [[ "$(curl -s -o /dev/null -w '%{http_code}' --max-time 2 http://127.0.0.1:5080/ 2>/dev/null || true)" == 200 ]]; then ready=1; break; fi
  sleep 1
done
[[ "$ready" == 1 ]] || { echo "Kestrel readiness failed" >&2; exit 1; }
python3 deploy/linux/verify-home.py http://127.0.0.1:5080
# Guest home snapshot for nginx location = / (Vira can edge-HIT static HTML).
curl -fsS -H 'Accept-Encoding: identity' --max-time 20 http://127.0.0.1:5080/ \
  -o "$INSTALL_DIR/publish/wwwroot/index.html"
python3 - "$INSTALL_DIR/publish/wwwroot/index.html" <<'PY'
import gzip, pathlib, sys
src = pathlib.Path(sys.argv[1])
html = src.read_bytes()
if len(html) < 10_000 or b"ms-hero" not in html:
    raise SystemExit("home snapshot looks invalid")
src.with_suffix(".html.gz").write_bytes(gzip.compress(html, compresslevel=6))
print(f"home-snapshot:{len(html)} gzip:{src.with_suffix('.html.gz').stat().st_size}")
PY
chown www-data:www-data "$INSTALL_DIR/publish/wwwroot/index.html" "$INSTALL_DIR/publish/wwwroot/index.html.gz"
find "$CACHE_DIR" -type f -delete
# Public edge is HTTPS. Smoke through loopback so the .deploying maintenance
# bypass ($ms_local_probe) still applies, with Host = public IP / vhost.
python3 deploy/linux/verify-home.py https://127.0.0.1 --host "$MS_SMOKE_HOST" --nginx --insecure
[[ "$(cat "$INSTALL_DIR/publish/RELEASE_COMMIT")" == "$SHA" ]]
systemctl is-active --quiet "$SERVICE"
rm -f "$MARKER"
committed=1
echo "Released $SHA; previous release and verified DB backup retained in $BACKUP"
