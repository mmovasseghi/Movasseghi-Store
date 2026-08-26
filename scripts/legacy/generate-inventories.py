#!/usr/bin/env python3
"""Generate compact markdown inventories from audit JSON."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
AUDIT = json.loads((ROOT / ".legacy-extract" / "audit-data-v2.json").read_text(encoding="utf-8"))
OUT = ROOT / "docs" / "audit" / "generated"
OUT.mkdir(parents=True, exist_ok=True)

# Product category table
lines = ["| Category | Slug | Products | Parent |", "|---|---|---:|---|"]
for c in AUDIT["product_categories"]:
    parent = next((x["name"] for x in AUDIT["product_categories"] if x["term_id"] == c["parent"]), "-")
    lines.append(f"| {c['name']} | `{c['slug']}` | {c['count']} | {parent} |")
(OUT / "product-categories.md").write_text("\n".join(lines), encoding="utf-8")

# Pages table
lines = ["| Page | URL | ID |", "|---|---|---:|"]
for p in AUDIT["pages"]:
    lines.append(f"| {p['breadcrumb_title']} | {p['permalink']} | {p['object_id']} |")
(OUT / "pages-inventory.md").write_text("\n".join(lines), encoding="utf-8")

# Product URLs (published only - load full from yoast if we had it; use sample extended)
# Re-parse url inventory for product paths
urls = json.loads((ROOT / ".legacy-extract" / "url-inventory.json").read_text(encoding="utf-8"))
product_urls = sorted(u for u in urls if "/product/" in u and "?" not in u)
lines = ["| # | Product URL |", "|---:|---|"]
for i, u in enumerate(product_urls, 1):
    lines.append(f"| {i} | {u} |")
(OUT / "product-urls.md").write_text("\n".join(lines), encoding="utf-8")

print(f"Wrote {len(product_urls)} product URLs, {len(AUDIT['product_categories'])} categories, {len(AUDIT['pages'])} pages")
