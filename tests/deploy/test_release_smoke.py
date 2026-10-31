import contextlib
import http.server
import importlib.util
import io
from pathlib import Path
import threading
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('release_smoke', ROOT / 'deploy/linux/verify-home.py')
smoke = importlib.util.module_from_spec(spec)
spec.loader.exec_module(smoke)
HTML = ('<link rel="preload" as="image" href="/images/hero-lcp.webp">'
        '<img fetchpriority="high" src="/images/hero-lcp.webp">'
        '<button class="ms-story" data-story-id="one"></button>'
        '<div id="msStoriesData" data-stories-src="/Home/Stories.json"></div>'
        + '<a class="ms-feat-card"></a>' * 8 + '<a class="ms-offer-card"></a>' * 8)


class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        mode = self.server.mode
        status, mime, body = 200, 'text/html', b'asset'
        if self.path == '/':
            body = HTML.encode()
            if mode == 'counts': body = HTML.replace('<a class="ms-feat-card"></a>', '', 1).encode()
            if mode == 'home-error': status = 500
            if mode == 'redirect':
                self.send_response(302); self.send_header('Location', '/elsewhere'); self.end_headers(); return
        elif self.path == '/images/hero-lcp.webp':
            mime, body = 'image/webp', b'RIFF\x04\0\0\0WEBP'
            if mode == 'image-error': status = 404
            if mode == 'invalid-image': body = b'<html>not an image</html>'
        elif self.path == '/Home/Stories.json':
            mime, body = 'application/json', b'[{}]' if mode != 'stories' else b'[]'
        elif self.path.endswith('.css'):
            mime, body = 'text/css', b'body{}'
            if self.path == '/css/storefront-home.bundle.css':
                if mode == 'missing-style': status = 404
                if mode == 'css-html': mime = 'text/html'
        elif self.path.endswith('.js'):
            mime, body = 'application/javascript', b'console.log(1)'
        self.send_response(status); self.send_header('Content-Type', mime); self.end_headers(); self.wfile.write(body)

    def log_message(self, *args): pass


class SmokeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler)
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


if __name__ == '__main__': unittest.main()
