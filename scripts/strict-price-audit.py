#!/usr/bin/env python3
"""Strict catalog price audit: DB variants vs amelon-prices.json (+ wholesale map)."""
import json
import sqlite3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DB = ROOT / "src/MovasseghiShop.Web/movasseghi.db"
PRICES = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-prices.json"
MAP = ROOT / "src/MovasseghiShop.Web/App_Data/amelon-wholesale-map.json"
OUT = ROOT / "src/MovasseghiShop.Web/App_Data/strict-price-audit.json"

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

TOLERANCE = 2  # toman


def resolve_product(by_code, by_slug, code: str, me: dict):
    web = MANUAL_WEB.get(code) or me.get("web") or code
    slug = me.get("slug")
    if slug and slug in by_slug:
        return by_slug[slug]
    if web in by_code:
        cands = by_code[web]
        return cands[0] if len(cands) == 1 else None
    return None


def main() -> int:
    prices = json.loads(PRICES.read_text(encoding="utf-8"))
    wholesale_map = json.loads(MAP.read_text(encoding="utf-8"))["map"]
    conn = sqlite3.connect(DB)
    conn.row_factory = sqlite3.Row

    prods = conn.execute("SELECT Id, ProductCode, Name, Slug, IsActive FROM Products").fetchall()
    by_code: dict[str, list] = {}
    by_slug = {r["Slug"]: r for r in prods if r["Slug"]}
    for p in prods:
        if p["ProductCode"]:
            by_code.setdefault(p["ProductCode"], []).append(p)

    variants = conn.execute(
        """
        SELECT v.Id, v.ProductId, v.Sku, v.Price, v.SellUnit, v.UnitsPerPack, v.UnitsPerCarton,
               v.PacksPerCarton, v.IsActive, p.ProductCode, p.Name
        FROM ProductVariants v
        JOIN Products p ON p.Id = v.ProductId
        WHERE v.IsActive = 1 AND v.ShowPrice = 1
        """
    ).fetchall()

    mismatches = []
    unmapped_pdf = []
    no_variant = []
    ok_checks = 0

    def expect_bulk(entry):
        pack = max(1, int(entry.get("pack") or 1))
        carton_units = int(entry.get("cartons") or 0)
        unit = round(int(entry["price"]) / 10)
        pack_price = unit * pack
        carton_price = unit * carton_units if carton_units > 0 else None
        return unit, pack, pack_price, carton_units, carton_price

    def expect_shrink(entry):
        pack = max(1, int(entry.get("pack") or 1))
        packs_per_carton = int(entry.get("cartons") or 0)
        pack_price = round(int(entry["price"]) / 10)
        unit = round(pack_price / pack) if pack else pack_price
        carton_price = pack_price * packs_per_carton if packs_per_carton > 0 else None
        return unit, pack, pack_price, packs_per_carton, carton_price

    mapped_product_ids = set()

    for list_name, compute in (("bulk", expect_bulk), ("shrink", expect_shrink)):
        for entry in prices.get(list_name, []):
            code = entry["code"]
            me = wholesale_map.get(code, {})
            target = resolve_product(by_code, by_slug, code, me)
            if not target:
                unmapped_pdf.append({"pdfCode": code, "type": list_name, "name": entry.get("name")})
                continue
            pid = target["Id"]
            prefix = (target["ProductCode"] or MANUAL_WEB.get(code) or me.get("web") or code).strip()
            mapped_product_ids.add(pid)
            unit, pack, pack_price, carton_field, carton_price = compute(entry)

            bn = conn.execute(
                "SELECT Id, Price, UnitsPerPack FROM ProductVariants WHERE ProductId=? AND SellUnit=1 AND IsActive=1",
                (pid,),
            ).fetchone()
            if list_name == "bulk" and bn and int(bn["UnitsPerPack"] or 0) == pack:
                db_price = int(round(float(bn["Price"])))
                db_upp = int(bn["UnitsPerPack"] or 0)
                if abs(db_price - pack_price) > TOLERANCE or db_upp != pack:
                    mismatches.append(
                        {
                            "productId": pid,
                            "code": prefix,
                            "pdfCode": code,
                            "sku": f"AM-{prefix}-BN",
                            "expected": pack_price,
                            "actual": db_price,
                            "expectedUpp": pack,
                            "actualUpp": db_upp,
                            "kind": "bulk-nylon",
                        }
                    )
                else:
                    ok_checks += 1
                if db_price > 0 and db_price < unit * 2 and pack > 1:
                    mismatches.append(
                        {
                            "productId": pid,
                            "code": prefix,
                            "sku": f"AM-{prefix}-BN",
                            "expected": pack_price,
                            "actual": db_price,
                            "kind": "unit-stored-as-nylon",
                        }
                    )

            if carton_price and list_name == "bulk":
                bc = conn.execute(
                    "SELECT Id, Price, UnitsPerCarton FROM ProductVariants WHERE ProductId=? AND Sku=? AND IsActive=1",
                    (pid, f"AM-{prefix}-BC"),
                ).fetchone()
                if bc:
                    db_price = int(round(float(bc["Price"])))
                    if abs(db_price - carton_price) > TOLERANCE:
                        mismatches.append(
                            {
                                "productId": pid,
                                "code": prefix,
                                "pdfCode": code,
                                "sku": f"AM-{prefix}-BC",
                                "expected": carton_price,
                                "actual": db_price,
                                "kind": "bulk-carton",
                            }
                        )
                    else:
                        ok_checks += 1

            if list_name == "shrink":
                bs = conn.execute(
                    "SELECT Id, Sku, Price, UnitsPerPack FROM ProductVariants WHERE ProductId=? AND SellUnit=3 AND IsActive=1",
                    (pid,),
                ).fetchone()
                if bs and int(bs["UnitsPerPack"] or 0) == pack:
                    db_price = int(round(float(bs["Price"])))
                    if abs(db_price - pack_price) > TOLERANCE:
                        mismatches.append(
                            {
                                "productId": pid,
                                "code": prefix,
                                "pdfCode": code,
                                "sku": bs["Sku"],
                                "expected": pack_price,
                                "actual": db_price,
                                "kind": "shrink-pack",
                            }
                        )
                    else:
                        ok_checks += 1

    active_with_price = [v for v in variants if v["IsActive"]]
    orphan = []
    for v in active_with_price:
        sku = v["Sku"] or ""
        if not sku.startswith("AM-"):
            continue
        pid = v["ProductId"]
        if pid not in mapped_product_ids:
            orphan.append(
                {
                    "productId": pid,
                    "productCode": v["ProductCode"],
                    "name": v["Name"],
                    "sku": sku,
                    "price": int(round(float(v["Price"]))),
                }
            )

    report = {
        "okChecks": ok_checks,
        "mismatchCount": len(mismatches),
        "unmappedPdfCodes": len(unmapped_pdf),
        "orphanActiveVariants": len(orphan),
        "mismatches": mismatches,
        "unmappedPdf": unmapped_pdf[:80],
        "orphans": orphan[:80],
    }
    OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"OK checks: {ok_checks}")
    print(f"Mismatches: {len(mismatches)}")
    print(f"Unmapped PDF codes: {len(unmapped_pdf)}")
    print(f"Active priced variants on unmapped products: {len(orphan)}")
    print(f"Report: {OUT}")
    if mismatches[:15]:
        print("Sample mismatches:")
        for m in mismatches[:15]:
            print(m)
    return 1 if mismatches else 0


if __name__ == "__main__":
    raise SystemExit(main())
