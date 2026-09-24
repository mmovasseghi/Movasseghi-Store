#!/usr/bin/env python3
"""Parse Amolon bulk/shrink price PDFs into amelon-prices.json (table-aware)."""
import json
import re
import sqlite3
import sys
from pathlib import Path

import pdfplumber

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / "src/MovasseghiShop.Web/movasseghi.db"
BULK_PDF = Path(r"C:/Users/pc1/Downloads/Telegram Desktop/5557227155892739840_222319853499326.pdf")
SHRINK_PDF = Path(r"C:/Users/pc1/Downloads/Telegram Desktop/1684146998327975682_630943551888297.pdf")

CODE_RE = re.compile(r"^(20\d{4}|40\d{4})$")


def parse_price_token(raw: str) -> int:
    s = (raw or "").strip().replace("\n", " ")
    if not s or s in ("_", "-", "—"):
        return 0
    # European thousands: 44.700 or 21,600 or 1.226,000 or 822,000
    if re.fullmatch(r"\d{1,3}(?:[.,]\d{3})+", s):
        return int(re.sub(r"[.,]", "", s))
    digits = re.sub(r"[^\d]", "", s)
    return int(digits) if digits else 0


def parse_plain_int(raw: str) -> int:
    s = (raw or "").strip()
    if not s or not re.fullmatch(r"\d+", s):
        return 0
    return int(s)


def find_code(cells: list[str]) -> str | None:
    for c in cells:
        c = (c or "").strip().replace("\n", "")
        if CODE_RE.fullmatch(c):
            return c
    return None


def row_name(cells: list[str], code: str) -> str:
    parts: list[str] = []
    for c in cells:
        if not c:
            continue
        t = c.replace("\n", " ").strip()
        if not t or code in t:
            continue
        if CODE_RE.fullmatch(t):
            continue
        if re.fullmatch(r"\d+", t):
            continue
        if parse_price_token(t) > 0 and parse_plain_int(t) == 0:
            continue
        parts.append(t)
    return " ".join(parts).strip() or code


def extract_from_tables(path: Path, kind: str) -> list[dict]:
    rows: list[dict] = []
    with pdfplumber.open(path) as pdf:
        for page in pdf.pages:
            for table in page.extract_tables() or []:
                for raw_row in table:
                    if not raw_row:
                        continue
                    cells = [str(c).strip() if c is not None else "" for c in raw_row]
                    code = find_code(cells)
                    if not code:
                        continue

                    price = 0
                    plain_counts: list[int] = []
                    for c in cells:
                        if not c or code in c:
                            continue
                        p = parse_price_token(c)
                        if p >= 100 and price == 0 and ("," in c or "." in c or p >= 1000):
                            price = p
                            continue
                        n = parse_plain_int(c.replace("\n", " "))
                        if n > 0:
                            plain_counts.append(n)

                    # trailing page index (1–99) — drop last if small and other nums exist
                    if len(plain_counts) >= 2 and plain_counts[-1] < 100:
                        plain_counts = plain_counts[:-1]

                    if price <= 0:
                        continue

                    if kind == "bulk":
                        if len(plain_counts) >= 2:
                            pack_units, carton_units = min(plain_counts[-2:]), max(
                                plain_counts[-2:]
                            )
                        elif len(plain_counts) == 1:
                            carton_units, pack_units = 0, plain_counts[0]
                        else:
                            carton_units, pack_units = 0, 0
                    else:
                        if len(plain_counts) >= 2:
                            units_per_pack, packs_per_carton = min(
                                plain_counts[-2:]
                            ), max(plain_counts[-2:])
                        elif len(plain_counts) == 1:
                            units_per_pack, packs_per_carton = plain_counts[0], 0
                        else:
                            units_per_pack, packs_per_carton = 0, 0
                        carton_units, pack_units = packs_per_carton, units_per_pack

                    rows.append(
                        {
                            "code": code,
                            "name": row_name(cells, code),
                            "price": price,
                            "cartons": carton_units,
                            "pack": pack_units,
                            "type": kind,
                        }
                    )

    by_code: dict[str, dict] = {}
    for r in rows:
        by_code[r["code"]] = r
    return list(by_code.values())


def main() -> int:
    bulk = extract_from_tables(BULK_PDF, "bulk")
    shrink = extract_from_tables(SHRINK_PDF, "shrink")
    out = {"bulk": bulk, "shrink": shrink}

    amelon_path = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-prices.json"
    amelon_path.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")
    (ROOT / "scripts/price-list-parsed.json").write_text(
        json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    pdf_codes = {r["code"] for r in bulk} | {r["code"] for r in shrink}
    conn = sqlite3.connect(DB)
    prods = conn.execute("SELECT ProductCode FROM Products").fetchall()
    db_codes = {(p[0] or "").strip() for p in prods if (p[0] or "").strip()}

    report = {
        "bulk_rows": len(bulk),
        "shrink_rows": len(shrink),
        "pdf_unique_codes": len(pdf_codes),
        "sample_201009": next((r for r in bulk if r["code"] == "201009"), None),
        "sample_201058": next((r for r in bulk if r["code"] == "201058"), None),
        "sample_201085": next((r for r in shrink if r["code"] == "201085"), None),
        "in_pdf_not_db": sorted(pdf_codes - db_codes)[:20],
    }
    (ROOT / "scripts/price-sync-report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
