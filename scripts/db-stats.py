#!/usr/bin/env python3
import sqlite3
import sys

path = sys.argv[1] if len(sys.argv) > 1 else "src/MovasseghiShop.Web/movasseghi.db"
c = sqlite3.connect(path)
print("products", c.execute("select count(*) from Products").fetchone()[0])
print("active", c.execute("select count(*) from Products where IsActive=1").fetchone()[0])
print("featured", c.execute("select count(*) from Products where IsHomeFeatured=1").fetchone()[0])
print("special", c.execute("select count(*) from Products where IsHomeSpecialOffer=1").fetchone()[0])
print("images", c.execute("select count(*) from ProductImages").fetchone()[0])
print("local_paths", c.execute("select count(*) from ProductImages where Url like '/images/%'").fetchone()[0])
print("http_paths", c.execute("select count(*) from ProductImages where Url like 'http%'").fetchone()[0])
