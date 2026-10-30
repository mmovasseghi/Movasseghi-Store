#!/usr/bin/env python3
"""Exercise the production nginx rules against an isolated local upstream."""
import http.server
import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import threading
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
NGINX = os.environ.get("NGINX_BINARY") or shutil.which("nginx")
if not NGINX:
    raise SystemExit("Set NGINX_BINARY to an installed nginx executable")


class Upstream(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        self.send_response(404 if "missing" in self.path else 200)
        if "set_cookie" in self.path:
            self.send_header("Set-Cookie", "test-session=unique; HttpOnly")
        self.send_header("Cache-Control", "private, no-store" if "private" in self.path else "public, max-age=60")
        self.end_headers()
        self.wfile.write(b"local upstream response")

    def log_message(self, *args):
        pass


def free_port():
    with socket.socket() as s:
        s.bind(("127.0.0.1", 0))
        return s.getsockname()[1]


with tempfile.TemporaryDirectory(prefix="movasseghi-nginx-") as temporary:
    directory = Path(temporary)
    upstream = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Upstream)
    threading.Thread(target=upstream.serve_forever, daemon=True).start()
    port = free_port()
    site = (ROOT / "deploy/linux/nginx-movasseghi.conf").read_text()
    site = site.replace("listen 80 default_server;", f"listen 127.0.0.1:{port};")
    site = site.replace("listen [::]:80 default_server;", "")
    site = site.replace("127.0.0.1:5080", f"127.0.0.1:{upstream.server_port}")
    site = site.replace("/MOVASSEGHISTORE/publish/wwwroot", str(ROOT / "src/MovasseghiShop.Web/wwwroot"))
    config = directory / "nginx.conf"
    config.write_text(f"""pid {directory}/nginx.pid;
error_log {directory}/error.log;
events {{}}
http {{
  access_log off;
  proxy_temp_path {directory}/proxy;
  client_body_temp_path {directory}/body;
  fastcgi_temp_path {directory}/fastcgi;
  scgi_temp_path {directory}/scgi;
  uwsgi_temp_path {directory}/uwsgi;
  proxy_cache_path {directory}/cache keys_zone=ms_pages:8m;
  {site}
}}
""")
    subprocess.run([NGINX, "-t", "-p", temporary, "-c", str(config)], check=True)
    process = subprocess.Popen([NGINX, "-p", temporary, "-c", str(config), "-g", "daemon off;"])

    def request(path, cookie="", authorization=""):
        req = urllib.request.Request(f"http://127.0.0.1:{port}{path}")
        if cookie:
            req.add_header("Cookie", cookie)
        if authorization:
            req.add_header("Authorization", authorization)
        try:
            response = urllib.request.urlopen(req, timeout=5)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            response.read()
            return response.status, response.headers

    try:
        for _ in range(50):
            if process.poll() is not None:
                raise RuntimeError((directory / "error.log").read_text())
            try:
                request("/robots.txt")
                break
            except urllib.error.URLError:
                time.sleep(.1)
        else:
            raise RuntimeError("nginx did not become ready")

        assert request("/")[1]["X-Cache"] == "MISS"
        assert request("/")[1]["X-Cache"] == "HIT"
        assert request("/", "_mv=analytics")[1]["X-Cache"] == "HIT"
        assert request("/", authorization="Bearer local-test")[1]["X-Cache"] == "BYPASS"
        for cookie in ["ms_cart_n=1", ".AspNetCore.Session=existing", ".AspNetCore.Identity.Application=existing"]:
            assert request("/", cookie)[1]["X-Cache"] == "BYPASS", cookie
        for path in ["/Order/Invoice?number=test", "/Payment/Mock", "/Cart/Panel", "/api/analytics/visitor", "/account/modal", "/Admin"]:
            for _ in range(2):
                _, headers = request(path)
                assert headers.get("X-Cache") == "BYPASS" or (
                    headers.get("X-Cache") is None and headers.get("Cache-Control") == "no-store"
                ), (path, headers)
        for path in ["/?set_cookie=1", "/Catalog?private=1"]:
            for _ in range(2):
                _, headers = request(path)
                assert headers.get("X-Cache") == "MISS", (path, headers)
        assert request("/?set_cookie=1")[1].get("Set-Cookie")
        status, headers = request("/Shop/missing")
        assert status == 404 and headers["Cache-Control"] == "public, max-age=60"
        for query, expected in [("", "max-age=3600"), ("?v=contenthash", "max-age=31536000, immutable")]:
            status, headers = request("/css/critical-home.css" + query)
            assert status == 200 and expected in headers["Cache-Control"]
        print("nginx syntax, public cache HIT, analytics cookie, session/cart bypass, private paths, Set-Cookie, private headers, errors and asset versioning: PASS")
    finally:
        process.terminate()
        process.wait(timeout=10)
        upstream.shutdown()
