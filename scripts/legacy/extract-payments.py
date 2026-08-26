#!/usr/bin/env python3
import re
from pathlib import Path

sql = Path(__file__).resolve().parents[2] / ".legacy-extract" / "db" / "site17896548586.sql"
text = sql.read_text(encoding="utf-8", errors="replace")
keys = set(re.findall(r"'(woocommerce_[^']+_settings)'", text))
print("woocommerce_*_settings keys:")
for k in sorted(keys):
    print(" ", k)
