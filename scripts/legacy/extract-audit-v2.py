#!/usr/bin/env python3
"""Robust legacy SQL extractor — streams large INSERT blocks safely."""
from __future__ import annotations

import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SQL_PATH = ROOT / ".legacy-extract" / "db" / "site17896548586.sql"
OUT_DIR = ROOT / ".legacy-extract"


def read_sql() -> str:
    return SQL_PATH.read_text(encoding="utf-8", errors="replace")


def extract_insert_table(sql: str, table: str) -> str:
    marker = f"INSERT INTO `{table}` VALUES "
    parts: list[str] = []
    start = 0
    while True:
        idx = sql.find(marker, start)
        if idx == -1:
            break
        i = idx + len(marker)
        # read until semicolon at depth 0 outside strings
        in_str = False
        esc = False
        while i < len(sql):
            ch = sql[i]
            if in_str:
                if esc:
                    esc = False
                elif ch == "\\":
                    esc = True
                elif ch == "'":
                    in_str = False
            else:
                if ch == "'":
                    in_str = True
                elif ch == ";":
                    parts.append(sql[idx + len(marker) : i])
                    i += 1
                    break
            i += 1
        start = i
    return "),(".join(parts)


def split_tuples(values_blob: str) -> list[str]:
    rows: list[str] = []
    cur: list[str] = []
    depth = 0
    in_str = False
    esc = False
    for ch in values_blob:
        if in_str:
            cur.append(ch)
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == "'":
                in_str = False
        else:
            if ch == "'":
                in_str = True
                cur.append(ch)
            elif ch == "(":
                if depth == 0:
                    cur = ["("]
                else:
                    cur.append(ch)
                depth += 1
            elif ch == ")":
                depth -= 1
                cur.append(ch)
                if depth == 0:
                    row = "".join(cur)
                    rows.append(row[1:-1])  # strip parens
                    cur = []
            else:
                if depth > 0:
                    cur.append(ch)
    return rows


def parse_fields(row: str) -> list[str]:
    fields: list[str] = []
    cur: list[str] = []
    in_str = False
    esc = False
    for ch in row:
        if in_str:
            if esc:
                cur.append(ch)
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == "'":
                in_str = False
            else:
                cur.append(ch)
        else:
            if ch == "'":
                in_str = True
            elif ch == ",":
                fields.append("".join(cur))
                cur = []
            else:
                cur.append(ch)
    fields.append("".join(cur))
    return fields


def unq(s: str) -> str:
    s = s.strip()
    if s == "NULL":
        return ""
    return s


def main() -> int:
    sql = read_sql()

    # --- wp_options (regex) ---
    options: dict[str, str] = {}
    for m in re.finditer(r"\(\d+,'([^']+)',('(?:\\'|[^'])*'|NULL),'(?:yes|no|off|on)'\)", sql):
        key = m.group(1)
        raw = m.group(2)
        if raw.startswith("'"):
            val = raw[1:-1].replace("\\'", "'").replace("\\\\", "\\")
        else:
            val = ""
        options[key] = val

    active_plugins = re.findall(r's:\d+:"([^"]+\.php)"', options.get("active_plugins", ""))

    # --- post types via tail pattern ---
    post_types = Counter(re.findall(r",'((?:post|page|product|attachment|revision|shop_order|elementor_library|woodmart_layout|nav_menu_item)[^']*?)','(?:[^']*?)',(?:\d+|NULL)\)", sql))

    # --- wp_terms + wp_term_taxonomy ---
    terms_blob = extract_insert_table(sql, "wp_terms")
    terms = {}
    for row in split_tuples(terms_blob):
        f = parse_fields(row)
        if len(f) >= 3:
            terms[int(f[0])] = {"term_id": int(f[0]), "name": unq(f[1]), "slug": unq(f[2])}

    tax_blob = extract_insert_table(sql, "wp_term_taxonomy")
    product_cats = []
    for row in split_tuples(tax_blob):
        f = parse_fields(row)
        if len(f) >= 6 and unq(f[2]) == "product_cat":
            tid = int(f[1])
            t = terms.get(tid, {"term_id": tid, "name": "?", "slug": "?"})
            product_cats.append(
                {
                    **t,
                    "description": unq(f[3])[:300],
                    "parent": int(f[4]) if f[4].isdigit() else 0,
                    "count": int(f[5]) if f[5].isdigit() else 0,
                }
            )

    # --- Yoast indexables (structured URL inventory) ---
    yoast_blob = extract_insert_table(sql, "wp_yoast_indexable")
    yoast_rows = split_tuples(yoast_blob)
    yoast_fields = [
        "id", "permalink", "permalink_hash", "object_id", "object_type", "object_sub_type",
        "author_id", "post_parent", "title", "description", "breadcrumb_title", "post_status",
        "is_public", "is_protected", "has_public_posts", "number_of_pages", "canonical",
        "primary_focus_keyword", "primary_focus_keyword_score", "readability_score",
        "is_cornerstone", "is_robots_noindex", "is_robots_nofollow", "is_robots_noarchive",
        "is_robots_noimageindex", "is_robots_nosnippet", "twitter_title", "twitter_image",
        "twitter_image_id", "twitter_image_source", "twitter_description", "open_graph_title",
        "open_graph_description", "open_graph_image", "open_graph_image_id", "open_graph_image_source",
        "open_graph_image_meta", "link_count", "incoming_link_count", "prominent_words_version",
        "created_at", "updated_at", "blog_id", "language", "region", "schema_page_type",
        "schema_article_type", "has_ancestors", "estimated_reading_time_minutes", "version",
        "object_last_modified", "object_published_at", "inclusive_language_score",
    ]
    url_inventory = []
    for row in yoast_rows:
        f = parse_fields(row)
        if len(f) < 10:
            continue
        rec = {
            "permalink": unq(f[1]),
            "object_id": unq(f[3]),
            "object_type": unq(f[4]),
            "object_sub_type": unq(f[5]),
            "title": unq(f[8]),
            "description": unq(f[9])[:200],
            "breadcrumb_title": unq(f[10]),
            "post_status": unq(f[11]),
            "primary_focus_keyword": unq(f[17]) if len(f) > 17 else "",
            "is_robots_noindex": unq(f[21]) if len(f) > 21 else "",
        }
        url_inventory.append(rec)

    products_yoast = [u for u in url_inventory if u["object_sub_type"] == "product" and u["post_status"] == "publish"]
    pages_yoast = [u for u in url_inventory if u["object_sub_type"] == "page" and u["post_status"] == "publish"]
    posts_yoast = [u for u in url_inventory if u["object_sub_type"] == "post" and u["post_status"] == "publish"]

    # --- Product prices from postmeta (_price, _regular_price) ---
    price_meta: dict[str, dict[str, str]] = {}
    for m in re.finditer(r"\((\d+),(\d+),'(_(?:regular_)?price)','([^']*)'\)", sql):
        _id, post_id, key, val = m.groups()
        price_meta.setdefault(post_id, {})[key] = val

    # --- Payment gateways ---
    gw = options.get("woocommerce_gateway_order", "")
    enabled_gateways = []
    for m in re.finditer(r"woocommerce_(\w+)_settings", options.keys().__str__()):
        pass
    for key in options:
        if key.startswith("woocommerce_") and key.endswith("_settings"):
            gw_id = key.replace("woocommerce_", "").replace("_settings", "")
            settings = options[key]
            enabled = "enabled";  # parse serialized
            if 's:7:"enabled";s:3:"yes"' in settings or 's:7:"enabled";s:1:"1"' in settings:
                enabled_gateways.append(gw_id)

    # --- Users: spam vs legit ---
    users_blob = extract_insert_table(sql, "wp_users")
    legit_users = []
    spam_users = 0
    for row in split_tuples(users_blob):
        f = parse_fields(row)
        if len(f) < 5:
            continue
        login = unq(f[1])
        email = unq(f[4])
        display = unq(f[8]) if len(f) > 8 else ""
        spam_markers = ("blogspot", "BTC", "ETH", "BINANCE", "KRAKEN", "HUOBI", "BYBIT", "MEXC", "BITFINEX", "casino")
        if any(x.lower() in login.lower() for x in spam_markers):
            spam_users += 1
        else:
            legit_users.append({"login": login, "email": email, "display": display})

    # --- WooCommerce pages from options ---
    wc_pages = {
        k: options.get(k, "")
        for k in options
        if k.startswith("woocommerce_") and k.endswith("_page_id")
    }

    audit = {
        "site": {
            "siteurl": options.get("siteurl"),
            "home": options.get("home"),
            "blogname": options.get("blogname"),
            "blogdescription": options.get("blogdescription"),
            "admin_email": options.get("admin_email"),
            "template": options.get("template"),
            "stylesheet": options.get("stylesheet"),
            "permalink_structure": options.get("permalink_structure"),
            "woocommerce_currency": options.get("woocommerce_currency"),
            "woocommerce_default_country": options.get("woocommerce_default_country"),
            "woocommerce_price_num_decimals": options.get("woocommerce_price_num_decimals"),
        },
        "active_plugins": active_plugins,
        "post_type_counts_regex": dict(post_types),
        "product_categories": sorted(product_cats, key=lambda x: -x["count"]),
        "yoast": {
            "total_indexables_parsed": len(url_inventory),
            "published_products": len(products_yoast),
            "published_pages": len(pages_yoast),
            "published_posts": len(posts_yoast),
        },
        "products_sample": products_yoast[:20],
        "pages": pages_yoast,
        "posts": posts_yoast,
        "url_inventory_count": len(url_inventory),
        "price_meta_count": len(price_meta),
        "enabled_payment_gateways": enabled_gateways,
        "wc_page_ids": wc_pages,
        "users": {"legitimate": legit_users[:20], "legitimate_count": len(legit_users), "spam_count": spam_users},
    }

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "audit-data-v2.json").write_text(json.dumps(audit, ensure_ascii=False, indent=2), encoding="utf-8")

    # Full URL list (compact)
    urls = sorted({u["permalink"] for u in url_inventory if u["permalink"].startswith("http")})
    (OUT_DIR / "url-inventory.json").write_text(json.dumps(urls, ensure_ascii=False, indent=2), encoding="utf-8")

    print(json.dumps({
        "products_published": len(products_yoast),
        "pages_published": len(pages_yoast),
        "posts_published": len(posts_yoast),
        "product_categories": len(product_cats),
        "urls": len(urls),
        "active_plugins": len(active_plugins),
        "spam_users": spam_users,
        "legit_users": len(legit_users),
    }, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
