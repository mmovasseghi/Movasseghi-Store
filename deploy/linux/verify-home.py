#!/usr/bin/env python3
"""Read-only release checks for the SSR home, selected LCP image and stories."""
from html.parser import HTMLParser
import json
import sys
from urllib.parse import quote, urljoin, urlsplit
from urllib.request import HTTPRedirectHandler, build_opener


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        raise RuntimeError(f"Unexpected redirect during health check: HTTP {code}")


class Home(HTMLParser):
    def __init__(self):
        super().__init__()
        self.featured = self.offers = self.stories = 0
        self.preloads = []
        self.heroes = []
        self.story_src = None

    def handle_starttag(self, tag, attributes):
        a = dict(attributes)
        classes = a.get("class", "").split()
        self.featured += "ms-feat-card" in classes
        self.offers += "ms-offer-card" in classes
        self.stories += tag == "button" and "data-story-id" in a
        if tag == "link" and a.get("rel") == "preload" and a.get("as") == "image":
            self.preloads.append(a.get("href"))
        if tag == "img" and a.get("fetchpriority") == "high":
            self.heroes.append(a.get("src"))
        if a.get("id") == "msStoriesData":
            self.story_src = a.get("data-stories-src")


def verify(base):
    origin = urlsplit(base)
    opener = build_opener(NoRedirect)

    def get(path):
        url = urljoin(base.rstrip("/") + "/", path)
        target = urlsplit(url)
        if (target.scheme, target.netloc) != (origin.scheme, origin.netloc):
            raise RuntimeError("Resource must stay on the selected origin")
        with opener.open(quote(url, safe=":/%?=&"), timeout=15) as response:
            if response.status != 200:
                raise RuntimeError(f"Expected HTTP 200: {target.path}")
            data = response.read()
            if not data:
                raise RuntimeError(f"Empty response: {target.path}")
            return data, response.headers.get_content_type()

    html, _ = get("/")
    home = Home()
    home.feed(html.decode("utf-8"))
    if (home.featured, home.offers) != (8, 8) or not home.stories:
        raise RuntimeError(f"Invalid home counts: featured={home.featured}, offers={home.offers}, stories={home.stories}")
    if not home.heroes or home.heroes[0] not in home.preloads:
        raise RuntimeError("The first high-priority hero must match an image preload")
    hero = home.heroes[0]
    if not urlsplit(hero).path.endswith("-lcp.webp"):
        raise RuntimeError("The selected hero has no LCP WebP variant")
    image, mime = get(hero)
    if mime != "image/webp" or image[:4] != b"RIFF" or image[8:12] != b"WEBP":
        raise RuntimeError("The selected hero did not return a WebP image")
    if not home.story_src:
        raise RuntimeError("Missing stories payload URL")
    stories = json.loads(get(home.story_src)[0])
    if not isinstance(stories, list) or not stories:
        raise RuntimeError("Empty stories payload")
    for asset in ["/css/storefront.bundle.css", "/js/site.js", "/js/home.js", "/js/stories.js"]:
        get(asset)
    print(f"home:200 featured:8 offers:8 stories:{home.stories} lcp:200:{urlsplit(hero).path}")


if __name__ == "__main__":
    try:
        verify(sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:5080")
    except Exception as error:
        print(f"Release smoke FAILED: {error}", file=sys.stderr)
        raise SystemExit(1)
