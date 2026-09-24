#!/usr/bin/env python3
"""Flag bulk nylon variants where Price looks like unit×1 instead of unit×pack."""
import json
import sqlite3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / "src/MovasseghiShop.Web/movasseghi.db"
PRICES = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-prices.json"
MAP = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-wholesale-map.json"


def compute_bulk(price_rial: int, pack: int, cartons: int) -> tuple[int, int]:
    unit = round(price_rial / 10)
    pack_price = unit * pack
    return unit, pack_price


def main() -> int:
    prices = json.loads(PRICES.read_text(encoding="utf-8"))
    bulk_by_code = {r["code"]: r for r in prices["bulk"]}
    wholesale_map = json.loads(MAP.read_text(encoding="utf-8"))["map"]

    conn = sqlite3.connect(DB)
    issues = []
    ok = 0
    for row in conn.execute(
        """
        SELECT p.Id, p.ProductCode, p.Name, v.Sku, v.Price, v.UnitsPerPack
        FROM ProductVariants v
        JOIN Products p ON p.Id = v.ProductId
        WHERE v.IsActive = 1 AND v.Sku LIKE 'AM-%-BN'
        """
    ):
        pid, code, name, sku, price, upp = row
        price = int(round(float(price)))
        upp = int(upp or 0)
        if upp <= 1:
            continue
        # price equals unit (too small for full nylon)
        if price > 0 and price * upp > price * 1.5 and price < 500_000:
            unit_guess = price
            expected_pack = unit_guess * upp
            if abs(price - unit_guess) < 2 and expected_pack != price:
                issues.append((pid, code, name[:40], sku, price, upp, expected_pack))
                continue
        ok += 1

    print(f"Active BN variants checked; suspicious (unit stored as nylon): {len(issues)}")
    for i in issues[:40]:
        print(i)
    if len(issues) > 40:
        print(f"... and {len(issues) - 40} more")
    return 1 if issues else 0


if __name__ == "__main__":
    raise SystemExit(main())
