import contextlib
import http.server
import importlib.util
import io
from pathlib import Path
import threading
import unittest
from urllib.parse import urlsplit
from urllib.error import HTTPError

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('release_smoke', ROOT / 'deploy/linux/verify-home.py')
smoke = importlib.util.module_from_spec(spec)
spec.loader.exec_module(smoke)
HTML = ('<link rel="preload" as="image" href="/images/hero-lcp.webp">'
        '<img fetchpriority="high" src="/images/hero-lcp.webp">'
        '<button class="ms-story" data-story-id="one"></button>'
        '<div id="msStoriesData" data-stories-src="/Home/Stories.json"></div>'
        + '<a class="ms-feat-card"></a>' * 8 + '<a class="ms-offer-card"></a>' * 8)


NGINX_HTML = (HTML.replace('/images/hero-lcp.webp', '/images/hero-lcp.webp?v=test')
              + '<link rel="preload" as="font" href="/fonts/test.woff2?v=test">'
              + '<template><link rel="stylesheet" href="/css/storefront-cart.bundle.css?v=test"></template>')

RESPONSIVE_HTML = HTML.replace('as="image"', 'as="image" imagesrcset="/images/hero-display-v2-420w.webp?v=test 420w" imagesizes="420px"').replace('fetchpriority="high"', 'fetchpriority="high" srcset="/images/hero-display-v2-420w.webp?v=test 420w" sizes="420px"')

class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        mode = self.server.mode
        path = urlsplit(self.path).path
        self.server.hosts.append(self.headers.get("Host"))
        status, mime, body = 200, 'text/html', b'asset'
        if path == '/':
            body = (NGINX_HTML if mode.startswith("nginx-") else HTML).encode()
            if mode == "nginx-no-version": body = NGINX_HTML.replace("?v=test", "").encode()
            if mode.startswith('responsive-'):
                body = RESPONSIVE_HTML.encode()
                if mode == 'responsive-mismatch': body = RESPONSIVE_HTML.replace('imagesizes="420px"', 'imagesizes="600px"').encode()
            if mode == 'counts': body = HTML.replace('<a class="ms-feat-card"></a>', '', 1).encode()
            if mode == 'home-error': status = 500
            if mode == 'redirect':
                self.send_response(302); self.send_header('Location', '/elsewhere'); self.end_headers(); return
        elif path in ['/images/hero-lcp.webp', '/images/hero-display-v2-420w.webp']:
            mime, body = 'image/webp', b'RIFF\x04\0\0\0WEBP'
            if mode == 'image-error': status = 404
            if mode == 'invalid-image': body = b'<html>not an image</html>'
            if path.endswith('display-v2-420w.webp') and mode == 'responsive-missing': status = 404
            if path.endswith('display-v2-420w.webp') and mode == 'responsive-wrong-type': mime = 'text/html'
        elif path == '/Home/Stories.json':
            mime, body = 'application/json', b'[{}]' if mode != 'stories' else b'[]'
        elif path.endswith('.css'):
            mime, body = 'text/css', b'body{}'
            if path == '/css/storefront-home.bundle.css':
                if mode == 'missing-style': status = 404
                if mode == 'css-html': mime = 'text/html'
        elif path.endswith('.js'):
            mime, body = 'application/javascript', b'console.log(1)'
        elif path.endswith('.woff2'):
            mime, body = 'font/woff2', b'font-data'
        self.send_response(status)
        self.send_header('Content-Type', mime)
        correct_host = mode != 'nginx-vhost' or self.headers.get('Host') == 'shop.test'
        versioned = 'v=test' in self.path
        cache = 'public, max-age=31536000, immutable' if versioned else 'public, max-age=3600'
        if path == '/':
            hit = correct_host and mode != 'nginx-miss' and not (mode == 'nginx-analytics-bypass' and self.headers.get('Cookie'))
            self.send_header('X-Cache', 'HIT' if hit else 'MISS')
            cache = 'public, max-age=900'
        if versioned and (mode == 'nginx-short-cache' or not correct_host): cache = 'public, max-age=2592000'
        if not versioned and mode == 'nginx-immutable-mutable': cache = 'public, max-age=31536000, immutable'
        if mode == 'nginx-private': self.send_header('Set-Cookie', 'test-session=private')
        if versioned and mode == 'nginx-double-ttl': self.send_header('Cache-Control', 'public, max-age=31536000, immutable')
        self.send_header('Cache-Control', cache)
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, *args): pass


class SmokeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        cls.server.hosts = []
        threading.Thread(target=cls.server.serve_forever, daemon=True).start()

    @classmethod
    def tearDownClass(cls):
        cls.server.shutdown(); cls.server.server_close()

    def test_complete_home_passes(self):
        self.server.mode = 'success'
        with contextlib.redirect_stdout(io.StringIO()): smoke.verify(f'http://127.0.0.1:{self.server.server_port}')

    def test_errors_missing_lcp_invalid_images_missing_products_empty_stories_and_redirects_fail(self):
        for mode in ['home-error', 'image-error', 'invalid-image', 'counts', 'stories', 'redirect', 'missing-style', 'css-html']:
            with self.subTest(mode=mode):
                self.server.mode = mode
                with self.assertRaises(Exception): smoke.verify(f'http://127.0.0.1:{self.server.server_port}')

    def test_nginx_checks_fingerprints_headers_and_public_host(self):
        self.server.mode = 'nginx-success'
        self.server.hosts = []
        with contextlib.redirect_stdout(io.StringIO()):
            smoke.verify(f'http://127.0.0.1:{self.server.server_port}', host='shop.test', check_nginx=True)
        self.assertEqual(set(self.server.hosts), {'shop.test'})

    def test_nginx_rejects_incorrect_cache_policies(self):
        for mode in ['nginx-miss', 'nginx-analytics-bypass', 'nginx-short-cache', 'nginx-immutable-mutable', 'nginx-private', 'nginx-no-version', 'nginx-double-ttl']:
            with self.subTest(mode=mode):
                self.server.mode = mode
                with self.assertRaises(RuntimeError):
                    smoke.verify(f'http://127.0.0.1:{self.server.server_port}', check_nginx=True)

    def test_default_host_200_does_not_prove_the_public_vhost_is_healthy(self):
        self.server.mode = 'nginx-vhost'
        url = f'http://127.0.0.1:{self.server.server_port}'
        with contextlib.redirect_stdout(io.StringIO()): smoke.verify(url)
        with self.assertRaises(RuntimeError): smoke.verify(url, check_nginx=True)
        with contextlib.redirect_stdout(io.StringIO()): smoke.verify(url, host='shop.test', check_nginx=True)

    def test_responsive_lcp_candidates_are_fetched_and_validated(self):
        self.server.mode = 'responsive-good'
        with contextlib.redirect_stdout(io.StringIO()):
            smoke.verify(f'http://127.0.0.1:{self.server.server_port}')

    def test_responsive_lcp_mismatch_and_broken_candidate_fail_release(self):
        for mode in ['responsive-mismatch', 'responsive-missing', 'responsive-wrong-type']:
            with self.subTest(mode=mode):
                self.server.mode = mode
                with self.assertRaises((RuntimeError, HTTPError)):
                    smoke.verify(f'http://127.0.0.1:{self.server.server_port}')

    def test_invalid_host_is_rejected_before_sending_headers(self):
        for host in ['user:password@shop.test', '@shop.test', 'shop.test?', 'shop.test/path', 'shop.test\r\nX-Test: injected']:
            with self.subTest(host=host), self.assertRaises((RuntimeError, ValueError)):
                smoke.verify(f'http://127.0.0.1:{self.server.server_port}', host=host)


if __name__ == '__main__': unittest.main()
