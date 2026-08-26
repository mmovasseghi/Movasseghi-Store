#!/usr/bin/env python3
"""Export migration bundle: categories + published products with prices from legacy SQL."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlparse

import importlib.util

ROOT = Path(__file__).resolve().parents[2]
SQL_PATH = ROOT / ".legacy-extract" / "db" / "site17896548586.sql"
OUT_PATH = ROOT / "scripts" / "import" / "migration-data.json"

_spec = importlib.util.spec_from_file_location(
    "extract_audit_v2", ROOT / "scripts" / "legacy" / "extract-audit-v2.py"
)
_mod = importlib.util.module_from_spec(_spec)
assert _spec and _spec.loader
_spec.loader.exec_module(_mod)
extract_insert_table = _mod.extract_insert_table
parse_fields = _mod.parse_fields
split_tuples = _mod.split_tuples
unq = _mod.unq
read_sql = _mod.read_sql


def slug_from_permalink(url: str) -> str:
    path = urlparse(url).path.rstrip("/")
    if "/product/" in path:
        raw = path.split("/product/")[-1]
    else:
        raw = path.split("/")[-1]
    return unquote(raw)


def main() -> int:
    if not SQL_PATH.exists():
        print(f"SQL not found: {SQL_PATH}", file=sys.stderr)
        return 1

    sql = read_sql()

    # Categories
    terms_blob = extract_insert_table(sql, "wp_terms")
    terms = {}
    for row in split_tuples(terms_blob):
        f = parse_fields(row)
        if len(f) >= 3:
            terms[int(f[0])] = {"term_id": int(f[0]), "name": unq(f[1]), "slug": unquote(unq(f[2]))}

    tax_blob = extract_insert_table(sql, "wp_term_taxonomy")
    categories = []
    for row in split_tuples(tax_blob):
        f = parse_fields(row)
        if len(f) >= 6 and unq(f[2]) == "product_cat":
            tid = int(f[1])
            t = terms.get(tid, {"term_id": tid, "name": "?", "slug": str(tid)})
            categories.append(
                {
                    "legacyTermId": tid,
                    "name": t["name"],
                    "slug": unquote(t["slug"]),
                    "description": unq(f[3]),
                    "parentLegacyTermId": int(f[4]) if f[4].isdigit() else 0,
                    "productCount": int(f[5]) if f[5].isdigit() else 0,
                }
            )

    # Product posts
    posts_blob = extract_insert_table(sql, "wp_posts")
    products_by_id: dict[str, dict] = {}
    for row in split_tuples(posts_blob):
        f = parse_fields(row)
        if len(f) < 22:
            continue
        post_id = unq(f[0])
        post_type = unq(f[20])
        status = unq(f[7])
        if post_type != "product" or status != "publish":
            continue
        products_by_id[post_id] = {
            "legacyId": int(post_id),
            "name": unq(f[5]),
            "slug": unq(f[11]),
            "shortDescription": unq(f[9])[:500],
            "descriptionHtml": unq(f[4]),
            "status": "published",
        }

    # WooCommerce product lookup (prices + SKU)
    lookup_blob = extract_insert_table(sql, "wp_wc_product_meta_lookup")
    for row in split_tuples(lookup_blob):
        f = parse_fields(row)
        if len(f) < 6:
            continue
        pid = unq(f[0])
        if pid not in products_by_id:
            continue
        p = products_by_id[pid]
        sku = unq(f[1])
        if sku:
            p["sku"] = sku
        try:
            min_price = float(unq(f[4]))
            max_price = float(unq(f[5]))
            price = int(min_price) if min_price > 0 else int(max_price)
            if price > 0:
                p["regularPrice"] = price
        except ValueError:
            pass

    # Postmeta: stock and fallback prices
    meta_blob = extract_insert_table(sql, "wp_postmeta")
    term_rel: dict[str, list[int]] = {}
    for row in split_tuples(meta_blob):
        f = parse_fields(row)
        if len(f) < 4:
            continue
        post_id, key, val = unq(f[1]), unq(f[2]), unq(f[3])
        if post_id not in products_by_id:
            continue
        p = products_by_id[post_id]
        if key == "_sku":
            p["sku"] = val
        elif key == "_regular_price":
            p["regularPrice"] = int(float(val)) if val else 0
        elif key == "_sale_price" and val:
            p["salePrice"] = int(float(val))
        elif key == "_stock" and val.isdigit():
            p["stockQuantity"] = int(val)
        elif key == "_manage_stock":
            p["manageStock"] = val == "yes"

    # term_relationships for product categories
    rel_blob = extract_insert_table(sql, "wp_term_relationships")
    for row in split_tuples(rel_blob):
        f = parse_fields(row)
        if len(f) < 3:
            continue
        post_id = unq(f[0])
        term_tax_id = unq(f[1])
        if post_id in products_by_id:
            term_rel.setdefault(post_id, []).append(int(term_tax_id))

    # Map term_taxonomy id -> term_id
    tax_id_to_term: dict[int, int] = {}
    for row in split_tuples(tax_blob):
        f = parse_fields(row)
        if len(f) >= 3:
            tax_id_to_term[int(f[0])] = int(f[1])

    for post_id, p in products_by_id.items():
        legacy_cats = []
        for tax_id in term_rel.get(post_id, []):
            term_id = tax_id_to_term.get(tax_id)
            if term_id:
                legacy_cats.append(term_id)
        p["legacyCategoryIds"] = legacy_cats
        if "regularPrice" not in p:
            p["regularPrice"] = 0

    # Yoast SEO from indexables
    yoast_blob = extract_insert_table(sql, "wp_yoast_indexable")
    for row in split_tuples(yoast_blob):
        f = parse_fields(row)
        if len(f) < 12:
            continue
        if unq(f[5]) != "product" or unq(f[11]) != "publish":
            continue
        oid = unq(f[3])
        if oid not in products_by_id:
            continue
        products_by_id[oid]["seo"] = {
            "title": unq(f[8]),
            "description": unq(f[9])[:300],
            "focusKeyword": unq(f[17]) if len(f) > 17 else "",
        }

    products = sorted(products_by_id.values(), key=lambda x: x["legacyId"])

    bundle = {
        "exportedAt": __import__("datetime").datetime.utcnow().isoformat() + "Z",
        "source": "legacy-sql",
        "categories": categories,
        "products": products,
        "counts": {"categories": len(categories), "products": len(products)},
    }

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(bundle, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {OUT_PATH} — {len(categories)} categories, {len(products)} products")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
