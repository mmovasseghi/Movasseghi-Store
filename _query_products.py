import sqlite3
import os

p = r"f:\Projects\Movasseghi Shop\src\MovasseghiShop.Web\movasseghi.db"
print("db exists:", os.path.exists(p))
if not os.path.exists(p):
    raise SystemExit(0)

conn = sqlite3.connect(p)
cur = conn.cursor()
cur.execute(
    "SELECT Id, Name, Slug, ProductCode, IsActive FROM Products "
    "WHERE Name LIKE '%پیش%' OR Slug LIKE '%پیش%' OR ProductCode IN ('000530','007712')"
)
for r in cur.fetchall():
    print(r)
cur.execute("SELECT COUNT(*) FROM Products")
print("total products:", cur.fetchone()[0])
conn.close()
