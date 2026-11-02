#!/usr/bin/env python3
"""Read-only release checks for the SSR home, selected LCP image and stories."""
import argparse
from html.parser import HTMLParser
import json
import re
import sys
import ssl
from urllib.parse import parse_qs, quote, urljoin, urlsplit
from urllib.request import HTTPRedirectHandler, HTTPSHandler, Request, build_opener


class NoRedirect(HTTPRedirectHandler):
    """Block open redirects; allow one same-host HTTP→HTTPS upgrade (edge canonical)."""

    def redirect_request(self, request, response, code, message, headers, new_url):
        source = urlsplit(request.full_url)
        target = urlsplit(new_url)
        http_to_https = (
            code in (301, 302, 307, 308)
            and source.scheme == "http"
            and target.scheme == "https"
            and source.hostname == target.hostname
            and (source.path or "/") == (target.path or "/")
            and source.query == target.query
        )
        if http_to_https:
            return Request(new_url, headers=request.headers, method=request.get_method())
        raise RuntimeError(
            f"Unexpected redirect during health check: HTTP {code} to {target.scheme}://{target.hostname}{target.path}"
        )


class Home(HTMLParser):
    def __init__(self):
        super().__init__()
        self.featured = self.offers = self.stories = 0
        self.preloads = []
        self.heroes = []
        self.story_src = None
        self.cache_assets = []
        self.hero_responsive = []
        self.preload_responsive = []

    def handle_starttag(self, tag, attributes):
        a = dict(attributes)
        classes = a.get("class", "").split()
        self.featured += "ms-feat-card" in classes
        self.offers += "ms-offer-card" in classes
        self.stories += tag == "button" and "data-story-id" in a
        if tag == "link" and a.get("rel") == "preload" and a.get("as") == "image":
            self.preloads.append(a.get("href"))
            self.preload_responsive.append((a.get("imagesrcset", ""), a.get("imagesizes", "")))
        if tag == "link" and (a.get("rel") == "stylesheet" or (a.get("rel") == "preload" and a.get("as") == "font")):
            self.cache_assets.append((a.get("href"), a.get("as") == "font"))
        if tag == "img" and a.get("fetchpriority") == "high":
            self.heroes.append(a.get("src"))
            self.hero_responsive.append((a.get("srcset", ""), a.get("sizes", "")))
        if a.get("id") == "msStoriesData":
            self.story_src = a.get("data-stories-src")


def cache_directives(headers):
    return [part.strip().lower() for value in headers.get_all("Cache-Control", []) for part in value.split(',')]


def cache_ttls(headers):
    return [int(match[1] or match[2]) for part in cache_directives(headers)
            if (match := re.fullmatch(r'max-age\s*=\s*(?:([0-9]+)|"([0-9]+)")', part))]


def verify(base, *, host=None, check_nginx=False, insecure=False):
    origin = urlsplit(base)
    handlers = [NoRedirect]
    if insecure and origin.scheme == "https":
        # Loopback HTTPS smoke against an IP certificate (SNI/Host = public IP).
        ctx = ssl.create_default_context()
        ctx.check_hostname = False
        ctx.verify_mode = ssl.CERT_NONE
        handlers.append(HTTPSHandler(context=ctx))
    opener = build_opener(*handlers)

    if host:
        authority = urlsplit("http://" + host)
        if (any(c.isspace() for c in host) or not authority.hostname or authority.path
                or authority.netloc != host or authority.query or authority.fragment
                or authority.username is not None or authority.password is not None):
            raise RuntimeError("Host must be a hostname or IP with an optional port")
        authority.port  # Validate the port before sending a request.

    def get(path, extra_headers=None):
        url = urljoin(base.rstrip("/") + "/", path)
        target = urlsplit(url)
        if (target.scheme, target.netloc) != (origin.scheme, origin.netloc):
            raise RuntimeError("Resource must stay on the selected origin")
        headers = {"Host": host} if host else {}
        headers.update(extra_headers or {})
        request = Request(quote(url, safe=":/%?=&"), headers=headers)
        with opener.open(request, timeout=15) as response:
            if response.status != 200:
                raise RuntimeError(f"Expected HTTP 200: {target.path}")
            data = response.read()
            if not data:
                raise RuntimeError(f"Empty response: {target.path}")
            return data, response.headers.get_content_type(), response.headers

    html, _, _ = get("/")
    html_text = html.decode("utf-8")
    home = Home()
    home.feed(html_text)
    # Home inlines critical CSS and may omit font preload to protect LCP bandwidth.
    # Still probe versioned fonts declared via @font-face url(...woff2?v=...).
    seen_fonts = {href for href, is_font in home.cache_assets if is_font}
    for match in re.finditer(r"url\((['\"]?)(/fonts/[^)'\"]+\.woff2\?v=[^)'\"]+)\1\)", html_text):
        href = match.group(2)
        if href not in seen_fonts:
            home.cache_assets.append((href, True))
            seen_fonts.add(href)
    if (home.featured, home.offers) != (8, 8) or not home.stories:
        raise RuntimeError(f"Invalid home counts: featured={home.featured}, offers={home.offers}, stories={home.stories}")
    if not home.heroes or home.heroes[0] not in home.preloads:
        raise RuntimeError("The first high-priority hero must match an image preload")
    hero = home.heroes[0]
    if not urlsplit(hero).path.endswith("-lcp.webp"):
        raise RuntimeError("The selected hero has no LCP WebP variant")
    image, mime, image_headers = get(hero)
    if mime != "image/webp" or image[:4] != b"RIFF" or image[8:12] != b"WEBP":
        raise RuntimeError("The selected hero did not return a WebP image")
    responsive_resources = []
    responsive = home.hero_responsive[0]
    preload = home.preload_responsive[home.preloads.index(hero)]
    if responsive != preload:
        raise RuntimeError("Hero and preload responsive candidates/sizes must match")
    if responsive[0]:
        widths = set()
        for candidate in responsive[0].split(','):
            match = re.fullmatch(r'\s*(\S+)\s+([1-9][0-9]*)w\s*', candidate)
            if not match or match[2] in widths:
                raise RuntimeError("Invalid or duplicated responsive hero candidate")
            widths.add(match[2])
            asset = match[1]
            data, mime, headers = get(asset)
            valid = ((mime == "image/webp" and data[:4] == b"RIFF" and data[8:12] == b"WEBP")
                     or (mime == "image/png" and data.startswith(b"\x89PNG\r\n\x1a\n"))
                     or (mime == "image/jpeg" and data.startswith(b"\xff\xd8\xff")))
            if not valid:
                raise RuntimeError(f"Responsive hero returned invalid image data: {urlsplit(asset).path}")
            responsive_resources.append((asset, headers))
    if not home.story_src:
        raise RuntimeError("Missing stories payload URL")
    stories = json.loads(get(home.story_src)[0])
    if not isinstance(stories, list) or not stories:
        raise RuntimeError("Empty stories payload")
    for asset in ["/css/storefront.bundle.css", "/css/storefront-home.bundle.css", "/css/storefront-cart.bundle.css", "/css/critical-home.css", "/js/site.js", "/js/home.js", "/js/stories.js"]:
        _, mime, _ = get(asset)
        if asset.endswith('.css') and mime != 'text/css':
            raise RuntimeError(f"Stylesheet returned the wrong content type: {asset}")
        if asset.endswith('.js') and mime not in ('text/javascript', 'application/javascript'):
            raise RuntimeError(f"Script returned the wrong content type: {asset}")
    if check_nginx:
        # Check the actual public virtual host through loopback; a generic
        # default server can return 200 while the public host has broken caching.
        for request_headers in [{}, {"Cookie": "_mv=00000000000000000000000000000000"}]:
            _, _, headers = get("/", request_headers)
            directives = cache_directives(headers)
            if (headers.get("X-Cache", "").strip().upper() != "HIT" or "public" not in directives
                    or "Set-Cookie" in headers or any(p.split('=')[0] in ('private', 'no-store', 'no-cache') for p in directives)):
                raise RuntimeError("Public host must return a public guest cache HIT, including with the analytics cookie")
        if not any(font for _, font in home.cache_assets) or not any(not font for _, font in home.cache_assets):
            raise RuntimeError("Missing versioned font or stylesheet cache probe")
        resources = [(hero, image_headers)] + responsive_resources
        for asset, font in home.cache_assets:
            _, mime, headers = get(asset)
            if mime != ('font/woff2' if font else 'text/css'):
                raise RuntimeError(f"Invalid cache probe content type: {urlsplit(asset).path}")
            resources.append((asset, headers))
        for asset, headers in resources:
            directives, ttls = cache_directives(headers), cache_ttls(headers)
            if (not parse_qs(urlsplit(asset).query).get('v') or len(ttls) != 1 or min(ttls) < 31536000
                    or not {'public', 'immutable'}.issubset(directives) or 'Set-Cookie' in headers
                    or any(p.split('=')[0] in ('private', 'no-store', 'no-cache') for p in directives)):
                raise RuntimeError(f"Versioned asset needs a public, immutable year-long cache: {urlsplit(asset).path}")
        _, _, headers = get(urlsplit(hero).path)
        ttls = cache_ttls(headers)
        if len(ttls) != 1 or max(ttls) > 3600 or 'immutable' in cache_directives(headers):
            raise RuntimeError("Mutable unversioned image must revalidate within one hour")
    print(f"home:200 featured:8 offers:8 stories:{home.stories} lcp:200:{urlsplit(hero).path}"
          + (" nginx-cache:verified" if check_nginx else ""))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("url", nargs="?", default="http://127.0.0.1:5080")
    parser.add_argument("--host", help="Public Host header while connecting through loopback")
    parser.add_argument("--nginx", action="store_true", help="Verify guest and fingerprinted static cache headers")
    parser.add_argument("--insecure", action="store_true", help="Skip TLS verify (loopback HTTPS vs IP cert)")
    arguments = parser.parse_args()
    try:
        verify(arguments.url, host=arguments.host, check_nginx=arguments.nginx, insecure=arguments.insecure)
    except Exception as error:
        print(f"Release smoke FAILED: {error}", file=sys.stderr)
        raise SystemExit(1)
