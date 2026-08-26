#!/usr/bin/env python3
"""Copy legacy master images to media-master/ preserving relative paths."""
from __future__ import annotations

import json
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
INVENTORY = ROOT / ".legacy-extract" / "media-inventory.json"
MASTER_DIR = ROOT / "media-master" / "legacy-uploads"

# Reuse resolver from extract script
import importlib.util

_spec = importlib.util.spec_from_file_location(
    "media_inv", ROOT / "scripts" / "legacy" / "extract-media-inventory.py"
)
_mod = importlib.util.module_from_spec(_spec)
assert _spec and _spec.loader
_spec.loader.exec_module(_mod)
resolve_local_file = _mod.resolve_local_file


def main() -> int:
    if not INVENTORY.exists():
        print("Run extract-media-inventory.py first", file=sys.stderr)
        return 1

    inv = json.loads(INVENTORY.read_text(encoding="utf-8"))
    copied = 0
    skipped = 0
    seen: set[str] = set()

    for att in inv["attachments"]:
        rel = att.get("attachedFile") or ""
        if not rel or rel in seen:
            continue
        seen.add(rel)
        src = resolve_local_file(rel)
        if not src:
            skipped += 1
            continue
        dest = MASTER_DIR / rel.replace("\\", "/")
        dest.parent.mkdir(parents=True, exist_ok=True)
        if not dest.exists():
            shutil.copy2(src, dest)
        copied += 1

    print(f"Copied {copied} master files to {MASTER_DIR}")
    print(f"Skipped {skipped} (not on local disk — need full backup tar)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
