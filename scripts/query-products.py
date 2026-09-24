import sqlite3
import sys

db = sys.argv[1] if len(sys.argv) > 1 else r"f:/Projects/Movasseghi Shop/src/MovasseghiShop.Web/movasseghi.db"
needle = sys.argv[2] if len(sys.argv) > 2 else "کیک"

conn = sqlite3.connect(db)
cur = conn.cursor()
cur.execute(
    "SELECT Id, Slug, Name, IsActive, ProductCode FROM Products WHERE Slug LIKE ? OR Name LIKE ?",
    (f"%{needle}%", f"%{needle}%"),
)
for row in cur.fetchall():
    print(row)
