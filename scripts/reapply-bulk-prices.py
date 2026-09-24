#!/usr/bin/env python3
"""Reapply bulk nylon/carton prices from amelon-prices.json (unit × pack/carton)."""
import json
import sqlite3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / "src/MovasseghiShop.Web/movasseghi.db"
PRICES = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-prices.json"
MAP = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-wholesale-map.json"

MANUAL_WEB = {
    "201009": "000606",
    "201050": "008912",
    "201059": "009208",
    "201121": "009312",
    "201076": "008912",
    "201074": "008814",
    "201077": "008924",
    "401122": "008815",
    "000702": "008612",
    "201003": "008612",
    "201032": "000407",
    "201033": "000412",
    "201034": "000406",
    "201035": "000406",
    "201036": "000405",
    "201037": "008706",
    "201038": "000404",
    "201039": "000434",
    "201080": "008706",
    "201081": "000404",
    "201082": "000210",
    "201001": "000532",
    "201105": "000530",
}


def main() -> int:
    prices = json.loads(PRICES.read_text(encoding="utf-8"))
    wholesale_map = json.loads(MAP.read_text(encoding="utf-8"))["map"]
    conn = sqlite3.connect(DB)
    cur = conn.cursor()

    prods = cur.execute("SELECT Id, ProductCode, Name, Slug FROM Products").fetchall()
    by_code: dict[str, list] = {}
    by_slug = {r[3]: r for r in prods if r[3]}
    for p in prods:
        if p[1]:
            by_code.setdefault(p[1], []).append(p)

    updated_products = 0
    bulk_sort = lambda e: (int(e.get("pack") or 0), int(e.get("price") or 0))
    for entry in sorted(prices["bulk"], key=bulk_sort):
        code = entry["code"]
        me = wholesale_map.get(code, {})
        web = MANUAL_WEB.get(code) or me.get("web") or code
        slug = me.get("slug")
        pack_units = max(1, int(entry.get("pack") or 1))
        carton_units = int(entry.get("cartons") or 0)
        unit = round(int(entry["price"]) / 10)
        pack_price = unit * pack_units
        carton_price = unit * carton_units if carton_units > 0 else None

        target = None
        if slug and slug in by_slug:
            target = by_slug[slug]
        elif web in by_code:
            candidates = by_code[web]
            target = candidates[0] if len(candidates) == 1 else None
        if target is None and slug:
            # همان کد وب برای چند محصول — فقط با اسلاگ PDF قابل تطبیق است
            continue

        if not target:
            continue

        pid, pcode, _, _ = target
        prefix = (pcode or web).strip()
        bn_sku = f"AM-{prefix}-BN"
        cur.execute(
            """
            UPDATE ProductVariants
            SET Price = ?, UnitsPerPack = ?, UnitsPerCarton = ?, PackLabel = ?,
                ShowPrice = 1, IsActive = 1, SellUnit = 1
            WHERE ProductId = ? AND SellUnit = 1
            """,
            (pack_price, pack_units, pack_units, f"نایلون {pack_units:,} عددی", pid),
        )
        if cur.rowcount:
            updated_products += 1

        if carton_price and carton_units > 0:
            nylons = carton_units // pack_units if pack_units else 0
            bc_sku = f"AM-{prefix}-BC"
            cur.execute(
                """
                UPDATE ProductVariants
                SET Price = ?, UnitsPerPack = ?, UnitsPerCarton = ?, PacksPerCarton = ?,
                    PackLabel = ?, ShowPrice = 1, IsActive = 1, SellUnit = 2
                WHERE ProductId = ? AND SellUnit = 2
                """,
                (
                    carton_price,
                    pack_units,
                    carton_units,
                    nylons,
                    f"کارتن {carton_units:,} عددی ({nylons} نایلون)",
                    pid,
                ),
            )

    for entry in sorted(prices.get("shrink", []), key=bulk_sort):
        code = entry["code"]
        me = wholesale_map.get(code, {})
        web = MANUAL_WEB.get(code) or me.get("web") or code
        slug = me.get("slug")
        pack_units = max(1, int(entry.get("pack") or 1))
        pack_price = round(int(entry["price"]) / 10)

        target = None
        if slug and slug in by_slug:
            target = by_slug[slug]
        elif web in by_code:
            candidates = by_code[web]
            target = candidates[0] if len(candidates) == 1 else None
        if not target:
            continue

        pid, pcode, _, _ = target
        prefix = (pcode or web).strip()
        sp_sku = f"AM-{prefix}-SP"
        cur.execute(
            """
            UPDATE ProductVariants
            SET Price = ?, UnitsPerPack = ?, PackLabel = ?, ShowPrice = 1, IsActive = 1, SellUnit = 3
            WHERE ProductId = ? AND SellUnit IN (3, 0) AND (UnitsPerPack = ? OR UnitsPerPack IN (0, 1))
            """,
            (pack_price, pack_units, f"بسته {pack_units:,} عددی", pid, pack_units),
        )
        if cur.rowcount:
            updated_products += 1

    # Fix variants that still store unit toman as nylon price (cartons=0 rows in PDF).
    bulk_unit_by_pack: dict[int, set[int]] = {}
    for entry in prices["bulk"]:
        pack_units = int(entry.get("pack") or 0)
        if pack_units <= 1:
            continue
        unit = round(int(entry["price"]) / 10)
        bulk_unit_by_pack.setdefault(pack_units, set()).add(unit)

    unit_fix = 0
    for _ in range(40):
        round_fix = 0
        for vid, price, upp in cur.execute(
            """
            SELECT v.Id, v.Price, v.UnitsPerPack
            FROM ProductVariants v
            WHERE v.IsActive = 1 AND v.SellUnit = 1 AND v.UnitsPerPack > 1
            """
        ):
            price_i = int(round(float(price)))
            upp_i = int(upp)
            known_units = bulk_unit_by_pack.get(upp_i) or set()
            if price_i in known_units:
                cur.execute(
                    "UPDATE ProductVariants SET Price = ? WHERE Id = ?",
                    (price_i * upp_i, vid),
                )
                round_fix += 1
                continue
            # قیمت نایلون اشتباهاً برابر قیمت واحد (تومان) مانده — معمولاً زیر ۵۰٬۰۰۰ با بستهٔ بزرگ
            if (
                0 < price_i < 50_000
                and upp_i >= 50
                and price_i * upp_i > price_i * 3
            ):
                cur.execute(
                    "UPDATE ProductVariants SET Price = ? WHERE Id = ?",
                    (price_i * upp_i, vid),
                )
                round_fix += 1
        unit_fix += round_fix
        if round_fix == 0:
            break

    conn.commit()
    print(f"Updated bulk pricing on {updated_products} product rows; unit→nylon fixes: {unit_fix}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
