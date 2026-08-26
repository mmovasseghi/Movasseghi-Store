#!/usr/bin/env python3
"""Sanitize forensic JSON before publishing to Legacy repo."""
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / ".legacy-extract" / "audit-data-v2.json"
OUT = ROOT / "legacy-export" / "extracts" / "audit-data-sanitized.json"

REDACT_PATTERNS = [
    (re.compile(r"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"), "[REDACTED_EMAIL]"),
    (re.compile(r"\$P\$[A-Za-z0-9./]{31}"), "[REDACTED_HASH]"),
    (re.compile(r"\$wp\$2y\$10\$[A-Za-z0-9./]+"), "[REDACTED_HASH]"),
]


def redact_obj(obj):
    if isinstance(obj, str):
        s = obj
        for pat, repl in REDACT_PATTERNS:
            s = pat.sub(repl, s)
        return s
    if isinstance(obj, list):
        return [redact_obj(x) for x in obj]
    if isinstance(obj, dict):
        return {k: redact_obj(v) for k, v in obj.items()}
    return obj


def main() -> None:
    if not SRC.exists():
        print(f"Skip: {SRC} not found")
        return
    data = json.loads(SRC.read_text(encoding="utf-8"))
    if "users" in data and "legitimate" in data["users"]:
        for u in data["users"]["legitimate"]:
            u["email"] = "[REDACTED]"
    data = redact_obj(data)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
