#!/usr/bin/env python3
"""Export sanitized blog posts from legacy SQL (spam excluded)."""
from __future__ import annotations

import importlib.util
import json
import re
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "scripts" / "import" / "blog-data.json"

SPAM = re.compile(
    r"casino|betting|\bbet\b|bonus|bitcoin|btc|gambl|slot|poker|forex|"
    r"kraken|binance|huobi|bybit|mexc|bitfinex|free.?spin|jackpot|roulette|wagering",
    re.I,
)


def has_persian(text: str) -> bool:
    return bool(re.search(r"[\u0600-\u06FF]", text or ""))


def is_spam(title: str, slug: str, excerpt: str) -> bool:
    combined = f"{title} {slug} {excerpt[:400]}"
    if SPAM.search(combined):
        return True
    return not has_persian(title) and not has_persian(excerpt[:800])


def main() -> int:
    spec = importlib.util.spec_from_file_location(
        "extract_audit_v2", ROOT / "scripts" / "legacy" / "extract-audit-v2.py"
    )
    mod = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(mod)

    sql = mod.read_sql()
    blob = mod.extract_insert_table(sql, "wp_posts")
    posts = []

    for row in mod.split_tuples(blob):
        f = mod.parse_fields(row)
        if len(f) < 22:
            continue
        if mod.unq(f[20]) != "post" or mod.unq(f[7]) != "publish":
            continue
        title = mod.unq(f[5])
        slug = unquote(mod.unq(f[11]))
        excerpt = mod.unq(f[9])
        content = mod.unq(f[4])
        if is_spam(title, slug, excerpt):
            continue
        content = re.sub(r"<script[\s\S]*?</script>", "", content, flags=re.I)
        posts.append(
            {
                "legacyId": int(mod.unq(f[0])),
                "title": title.lstrip("\u200f\u200e"),
                "slug": slug,
                "excerpt": excerpt[:500] if excerpt else None,
                "legacyContentHtml": content,
                "status": "published",
                "publishedAt": mod.unq(f[2]) or None,
            }
        )

    OUT.write_text(json.dumps({"posts": posts}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Exported {len(posts)} legitimate posts → {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
