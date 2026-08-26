#!/usr/bin/env python3
"""One-shot legacy SQL forensic extractor for Movasseghi Store audit."""
from __future__ import annotations

import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SQL_PATH = ROOT / ".legacy-extract" / "db" / "site17896548586.sql"
OUT_PATH = ROOT / ".legacy-extract" / "audit-data.json"


def parse_sql_values_tuple_block(text: str, table: str) -> list[str]:
    """Extract raw VALUE tuples from INSERT INTO `table` VALUES ... statements."""
    pattern = rf"INSERT INTO `{re.escape(table)}` VALUES "
    rows: list[str] = []
    idx = 0
    while True:
        pos = text.find(pattern, idx)
        if pos == -1:
            break
        start = pos + len(pattern)
        i = start
        depth = 0
        in_str = False
        esc = False
        while i < len(text):
            ch = text[i]
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
                elif ch == "(":
                    depth += 1
                elif ch == ")":
                    depth -= 1
                    if depth == 0:
                        rows.append(text[start : i + 1])
                        i += 1
                        if i < len(text) and text[i] == ",":
                            start = i + 1
                            depth = 0
                            continue
                        break
                elif ch == ";" and depth == 0:
                    break
            i += 1
        idx = i + 1
    return rows


def split_sql_row(row: str) -> list[str]:
    row = row.strip()
    if row.startswith("("):
        row = row[1:]
    if row.endswith(")"):
        row = row[:-1]
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
                fields.append("".join(cur).strip())
                cur = []
            else:
                cur.append(ch)
    fields.append("".join(cur).strip())
    return fields


def unescape_sql(s: str) -> str:
    if s == "NULL":
        return ""
    return (
        s.replace("\\'", "'")
        .replace('\\"', '"')
        .replace("\\n", "\n")
        .replace("\\r", "\r")
        .replace("\\t", "\t")
        .replace("\\\\", "\\")
    )


def main() -> int:
    if not SQL_PATH.exists():
        print(f"Missing SQL dump: {SQL_PATH}", file=sys.stderr)
        return 1

    print(f"Reading {SQL_PATH} ...")
    text = SQL_PATH.read_text(encoding="utf-8", errors="replace")

    # Tables present
    tables = sorted(set(re.findall(r"CREATE TABLE `([^`]+)`", text)))
    print(f"Tables: {len(tables)}")

    # wp_options key values
    options: dict[str, str] = {}
    for block in parse_sql_values_tuple_block(text, "wp_options"):
        for row in re.findall(r"\([^)]*(?:\([^)]*\)[^)]*)*\)", block):
            pass
    # simpler: regex for option rows in first big insert line chunks
    for m in re.finditer(r"\((\d+),'([^']+)',('(?:\\'|[^'])*'|NULL),'(?:yes|no|off|on)'\)", text):
        oid, key, val = m.group(1), m.group(2), m.group(3)
        if val.startswith("'") and val.endswith("'"):
            val = unescape_sql(val[1:-1])
        options[key] = val

    key_options = [
        "siteurl",
        "home",
        "blogname",
        "blogdescription",
        "admin_email",
        "template",
        "stylesheet",
        "woocommerce_store_address",
        "woocommerce_store_city",
        "woocommerce_default_country",
        "woocommerce_currency",
        "woocommerce_price_thousand_sep",
        "woocommerce_price_decimal_sep",
        "woocommerce_price_num_decimals",
        "permalink_structure",
        "woocommerce_onboarding_profile",
    ]
    site_options = {k: options.get(k, "NOT_FOUND") for k in key_options}

    # Active plugins from serialized active_plugins
    active_plugins: list[str] = []
    ap = options.get("active_plugins", "")
    if ap:
        active_plugins = re.findall(r's:\d+:"([^"]+)"', ap)

    # Posts by type/status
    posts_by_type: Counter[str] = Counter()
    posts_by_status: Counter[str] = Counter()
    pages: list[dict] = []
    products: list[dict] = []
    posts_blog: list[dict] = []

    insert_posts = re.findall(
        r"INSERT INTO `wp_posts` VALUES (.+?);",
        text,
        flags=re.DOTALL,
    )
    post_rows: list[str] = []
    for chunk in insert_posts:
        post_rows.extend(parse_sql_values_tuple_block("INSERT INTO `wp_posts` VALUES " + chunk, "wp_posts"))

    for row in post_rows:
        f = split_sql_row(row)
        if len(f) < 21:
            continue
        pid = f[0]
        post_author = f[1]
        post_date = f[2]
        post_title = unescape_sql(f[5].strip("'")) if f[5].startswith("'") else f[5]
        post_status = f[7]
        post_name = unescape_sql(f[11].strip("'")) if f[11].startswith("'") else f[11]
        post_type = f[20]
        posts_by_type[post_type] += 1
        posts_by_status[post_status] += 1
        item = {
            "id": int(pid),
            "title": post_title,
            "slug": post_name,
            "status": post_status,
            "type": post_type,
            "date": post_date,
        }
        if post_type == "page" and post_status == "publish":
            pages.append(item)
        elif post_type == "product" and post_status in ("publish", "draft", "private"):
            products.append(item)
        elif post_type == "post" and post_status == "publish":
            posts_blog.append(item)

    # Terms / product categories
    terms: list[dict] = []
    for chunk in re.findall(r"INSERT INTO `wp_terms` VALUES (.+?);", text, re.DOTALL):
        for row in parse_sql_values_tuple_block("INSERT INTO `wp_terms` VALUES " + chunk, "wp_terms"):
            f = split_sql_row(row)
            if len(f) >= 3:
                terms.append(
                    {
                        "term_id": int(f[0]),
                        "name": unescape_sql(f[1].strip("'")),
                        "slug": unescape_sql(f[2].strip("'")),
                    }
                )

    term_taxonomy: dict[int, dict] = {}
    for chunk in re.findall(r"INSERT INTO `wp_term_taxonomy` VALUES (.+?);", text, re.DOTALL):
        for row in parse_sql_values_tuple_block(
            "INSERT INTO `wp_term_taxonomy` VALUES " + chunk, "wp_term_taxonomy"
        ):
            f = split_sql_row(row)
            if len(f) >= 6:
                term_taxonomy[int(f[1])] = {
                    "taxonomy": unescape_sql(f[2].strip("'")),
                    "description": unescape_sql(f[3].strip("'"))[:500],
                    "parent": int(f[4]) if f[4].isdigit() else 0,
                    "count": int(f[5]) if f[5].isdigit() else 0,
                }

    categories = []
    product_cats = []
    for t in terms:
        meta = term_taxonomy.get(t["term_id"], {})
        tax = meta.get("taxonomy")
        enriched = {**t, **meta}
        if tax == "category":
            categories.append(enriched)
        elif tax == "product_cat":
            product_cats.append(enriched)

    # Orders count
    shop_orders = posts_by_type.get("shop_order", 0)

    # Users summary (exclude obvious spam pattern)
    users_total = len(re.findall(r"INSERT INTO `wp_users` VALUES", text))
    admin_users = []
    for chunk in re.findall(r"INSERT INTO `wp_users` VALUES (.+?);", text, re.DOTALL):
        for row in parse_sql_values_tuple_block("INSERT INTO `wp_users` VALUES " + chunk, "wp_users"):
            f = split_sql_row(row)
            if len(f) < 8:
                continue
            login = unescape_sql(f[1].strip("'"))
            email = unescape_sql(f[4].strip("'"))
            display = unescape_sql(f[8].strip("'")) if len(f) > 8 else ""
            if "blogspot" in login or "BTC" in login or "ETH" in login or "BINANCE" in login:
                continue
            if login in ("admin", "Movasseghi", "mizbancoadmin") or "movasseghi" in login.lower():
                admin_users.append({"login": login, "email": email, "display": display})

    # Yoast indexables count
    yoast_indexables = text.count("INSERT INTO `wp_yoast_indexable`")

    # Plugin list from wp-content path references in wffilemods (sample)
    plugin_dirs = sorted(
        set(
            re.findall(
                r"wp-content/plugins/([^/]+)/",
                text,
            )
        )
    )

    # Security red flags
    malware_plugins = [p for p in plugin_dirs if re.search(r"hello|kurd|ixwz", p, re.I)]

    audit = {
        "source_sql": str(SQL_PATH),
        "tables_count": len(tables),
        "tables_sample": tables[:40],
        "site_options": site_options,
        "active_plugins_count": len(active_plugins),
        "active_plugins": active_plugins,
        "posts_by_type": dict(posts_by_type),
        "posts_by_status": dict(posts_by_status),
        "published_pages_count": len(pages),
        "published_pages": sorted(pages, key=lambda x: x["id"]),
        "products_count": len(products),
        "products_published": [p for p in products if p["status"] == "publish"],
        "products_all": products,
        "blog_posts_published": posts_blog,
        "product_categories": sorted(product_cats, key=lambda x: x.get("count", 0), reverse=True),
        "blog_categories": categories,
        "shop_orders_count": shop_orders,
        "legitimate_admin_users": admin_users,
        "yoast_indexables_inserts": yoast_indexables,
        "detected_plugin_dirs": plugin_dirs,
        "malware_suspect_plugins": malware_plugins,
    }

    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_text(json.dumps(audit, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {OUT_PATH}")
    print(json.dumps({k: audit[k] for k in ("site_options", "posts_by_type", "published_pages_count", "products_count", "shop_orders_count", "active_plugins_count")}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
