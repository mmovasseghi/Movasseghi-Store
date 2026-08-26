#!/usr/bin/env python3
"""Legacy media forensic inventory — DB + metadata mapping (read-only)."""
from __future__ import annotations

import importlib.util
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path
from urllib.parse import quote

ROOT = Path(__file__).resolve().parents[2]
SQL_PATH = ROOT / ".legacy-extract" / "db" / "site17896548586.sql"
OUT_JSON = ROOT / ".legacy-extract" / "media-inventory.json"
OUT_PRODUCT_MAP = ROOT / "docs" / "audit" / "generated" / "product-media-map.json"
DOCS_MEDIA = ROOT / "docs" / "audit" / "MEDIA-INVENTORY.md"
DOCS_PRODUCT = ROOT / "docs" / "audit" / "PRODUCT-MEDIA-MAP.md"
LEGACY_UPLOADS_BASE = ROOT / ".legacy-extract" / "homedir" / "public_html" / "wp-content" / "uploads"
LEGACY_DOMAIN = "https://www.ayrik-cornstarch.com/wp-content/uploads/"

_spec = importlib.util.spec_from_file_location(
    "extract_audit_v2", ROOT / "scripts" / "legacy" / "extract-audit-v2.py"
)
_mod = importlib.util.module_from_spec(_spec)
assert _spec and _spec.loader
_spec.loader.exec_module(_mod)
extract_insert_table = _mod.extract_insert_table
parse_fields = _mod.parse_fields
split_tuples = _mod.split_tuples
unq = _mod.unq
read_sql = _mod.read_sql


def parse_attachment_metadata(raw: str) -> dict:
    """Best-effort parse of PHP serialized attachment metadata for dimensions."""
    out: dict = {}
    w = re.search(r's:5:"width";i:(\d+)', raw)
    h = re.search(r's:6:"height";i:(\d+)', raw)
    fs = re.search(r's:8:"filesize";i:(\d+)', raw)
    if w:
        out["width"] = int(w.group(1))
    if h:
        out["height"] = int(h.group(1))
    if fs:
        out["filesize"] = int(fs.group(1))
    return out


def resolve_local_file(relative_path: str) -> Path | None:
    if not relative_path:
        return None
    rel = relative_path.replace("\\", "/").lstrip("/")
    for base in (
        LEGACY_UPLOADS_BASE,
        ROOT / ".legacy-extract" / "homedir2" / "public_html" / "wp-content" / "uploads",
    ):
        direct = base / rel
        if direct.is_file():
            return direct
        # WordPress may store Persian names; try basename glob
        name = Path(rel).name
        if name:
            matches = list(base.rglob(name))
            if matches:
                return matches[0]
    return None


def file_exists_local(relative_path: str) -> bool:
    return resolve_local_file(relative_path) is not None


def main() -> int:
    if not SQL_PATH.exists():
        print(f"SQL not found: {SQL_PATH}", file=sys.stderr)
        return 1

    sql = read_sql()

    # Attachments from wp_posts
    posts_blob = extract_insert_table(sql, "wp_posts")
    attachments: dict[str, dict] = {}
    products: dict[str, dict] = {}
    for row in split_tuples(posts_blob):
        f = parse_fields(row)
        if len(f) < 22:
            continue
        pid = unq(f[0])
        post_type = unq(f[20])
        if post_type == "attachment":
            attachments[pid] = {
                "legacyAttachmentId": int(pid),
                "title": unq(f[5]),
                "slug": unq(f[11]),
                "mimeType": unq(f[21]) if len(f) > 21 else "",
                "alt": "",
                "attachedFile": "",
                "legacyUrl": "",
                "width": None,
                "height": None,
                "filesize": None,
                "fileOnDisk": False,
                "migrationStatus": "PENDING",
            }
        elif post_type == "product" and unq(f[7]) == "publish":
            products[pid] = {
                "legacyProductId": int(pid),
                "name": unq(f[5]),
                "slug": unq(f[11]),
                "sku": "",
                "legacyUrl": f"https://www.ayrik-cornstarch.com/product/{quote(unq(f[11]), safe='')}/",
                "primaryImageId": None,
                "galleryIds": [],
                "primaryImage": None,
                "gallery": [],
                "migrationStatus": "PENDING",
            }

    # Postmeta: attachment files, alt, product images
    meta_blob = extract_insert_table(sql, "wp_postmeta")
    for row in split_tuples(meta_blob):
        f = parse_fields(row)
        if len(f) < 4:
            continue
        post_id, key, val = unq(f[1]), unq(f[2]), unq(f[3])
        if post_id in attachments:
            if key == "_wp_attached_file":
                attachments[post_id]["attachedFile"] = val
                attachments[post_id]["legacyUrl"] = LEGACY_DOMAIN + val.lstrip("/")
                attachments[post_id]["fileOnDisk"] = file_exists_local(val)
            elif key == "_wp_attachment_image_alt":
                attachments[post_id]["alt"] = val
            elif key == "_wp_attachment_metadata":
                meta = parse_attachment_metadata(val)
                attachments[post_id].update(meta)
        if post_id in products:
            if key == "_sku":
                products[post_id]["sku"] = val
            elif key == "_thumbnail_id" and val.isdigit():
                products[post_id]["primaryImageId"] = int(val)
            elif key == "_product_image_gallery" and val:
                products[post_id]["galleryIds"] = [
                    int(x) for x in val.split(",") if x.strip().isdigit()
                ]

    # Category thumbnails from termmeta
    termmeta_blob = extract_insert_table(sql, "wp_termmeta")
    category_media: list[dict] = []
    for row in split_tuples(termmeta_blob):
        f = parse_fields(row)
        if len(f) < 4:
            continue
        if unq(f[2]) in ("thumbnail_id", "product_cat_thumbnail_id") and unq(f[3]).isdigit():
            category_media.append(
                {"termId": int(unq(f[1])), "thumbnailAttachmentId": int(unq(f[3]))}
            )

    # Build product → media map
    product_media_map: list[dict] = []
    unknown_mapping = 0
    confidently_mapped = 0
    missing_files = 0

    for pid, prod in sorted(products.items(), key=lambda x: int(x[0])):
        entry = {**prod, "additionalMedia": []}
        thumb_id = prod.get("primaryImageId")
        if thumb_id:
            att = attachments.get(str(thumb_id))
            if att:
                entry["primaryImage"] = att
                if att["attachedFile"] and not att["fileOnDisk"]:
                    missing_files += 1
                confidently_mapped += 1
            else:
                entry["primaryImage"] = {"legacyAttachmentId": thumb_id, "migrationStatus": "UNKNOWN_MAPPING"}
                unknown_mapping += 1
        else:
            entry["migrationStatus"] = "NO_PRIMARY_IMAGE"

        gallery = []
        for gid in prod.get("galleryIds") or []:
            att = attachments.get(str(gid))
            if att:
                gallery.append(att)
                if att["attachedFile"] and not att["fileOnDisk"]:
                    missing_files += 1
                confidently_mapped += 1
            else:
                gallery.append({"legacyAttachmentId": gid, "migrationStatus": "UNKNOWN_MAPPING"})
                unknown_mapping += 1
        entry["gallery"] = gallery
        product_media_map.append(entry)

    # Classify all attachments
    mime_counts = Counter(a["mimeType"] for a in attachments.values())
    image_mimes = {m for m in mime_counts if m.startswith("image/")}
    product_linked_ids: set[int] = set()
    for p in product_media_map:
        if p.get("primaryImageId"):
            product_linked_ids.add(p["primaryImageId"])
        for gid in p.get("galleryIds") or []:
            product_linked_ids.add(gid)

    orphaned = [
        a for aid, a in attachments.items() if int(aid) not in product_linked_ids
    ]

    on_disk = sum(1 for a in attachments.values() if a["fileOnDisk"])
    product_with_primary = sum(1 for p in product_media_map if p.get("primaryImageId"))
    product_with_gallery = sum(1 for p in product_media_map if p.get("galleryIds"))

    inventory = {
        "generatedAt": __import__("datetime").datetime.utcnow().isoformat() + "Z",
        "source": str(SQL_PATH.name),
        "summary": {
            "totalAttachmentsDb": len(attachments),
            "totalPublishedProducts": len(products),
            "productWithPrimaryImage": product_with_primary,
            "productWithGallery": product_with_gallery,
            "totalProductImageLinks": len(product_linked_ids),
            "categoryThumbnailRefs": len(category_media),
            "filesFoundOnLocalExtract": on_disk,
            "filesMissingLocally": len(attachments) - on_disk,
            "confidentlyMappedLinks": confidently_mapped,
            "unknownMappings": unknown_mapping,
            "orphanedAttachments": len(orphaned),
            "mimeTypes": dict(mime_counts),
        },
        "attachments": list(attachments.values()),
        "products": product_media_map,
        "categoryMedia": category_media,
        "orphanedSample": orphaned[:30],
    }

    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    OUT_JSON.write_text(json.dumps(inventory, ensure_ascii=False, indent=2), encoding="utf-8")
    OUT_PRODUCT_MAP.parent.mkdir(parents=True, exist_ok=True)
    OUT_PRODUCT_MAP.write_text(
        json.dumps(product_media_map, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    write_media_inventory_md(inventory)
    write_product_media_map_md(product_media_map, inventory["summary"])

    print(json.dumps(inventory["summary"], indent=2))
    print(f"Wrote {OUT_JSON}")
    print(f"Wrote {DOCS_MEDIA}")
    print(f"Wrote {DOCS_PRODUCT}")
    return 0


def write_media_inventory_md(inv: dict) -> None:
    s = inv["summary"]
    lines = [
        "# MEDIA-INVENTORY — Legacy Forensic Report",
        "",
        "**Status:** Phase 1 media pass — CONFIRMED (DB) / PARTIAL (filesystem)",
        "**Source:** `.legacy-extract/db/site17896548586.sql` + local uploads scan",
        "",
        "## Executive summary",
        "",
        "| Metric | Count | Confidence |",
        "|---|---:|---|",
        f"| Total attachment records (DB) | {s['totalAttachmentsDb']} | CONFIRMED |",
        f"| Published products | {s['totalPublishedProducts']} | CONFIRMED |",
        f"| Products with primary image ID | {s['productWithPrimaryImage']} | CONFIRMED |",
        f"| Products with gallery | {s['productWithGallery']} | CONFIRMED |",
        f"| Product-linked attachment IDs | {s['totalProductImageLinks']} | CONFIRMED |",
        f"| Category thumbnail references | {s['categoryThumbnailRefs']} | CONFIRMED |",
        f"| Files found in local extract | {s['filesFoundOnLocalExtract']} | CONFIRMED |",
        f"| Files **missing** locally (need tar/backup) | {s['filesMissingLocally']} | CONFIRMED |",
        f"| Confident product↔media mappings | {s['confidentlyMappedLinks']} | CONFIRMED |",
        f"| Unknown mappings | {s['unknownMappings']} | CONFIRMED |",
        f"| Orphaned attachments (not product-linked) | {s['orphanedAttachments']} | INFERRED |",
        "",
        "## MIME type breakdown",
        "",
        "| MIME | Count |",
        "|---|---:|",
    ]
    for mime, count in sorted(s["mimeTypes"].items(), key=lambda x: -x[1]):
        lines.append(f"| `{mime or '(empty)'}` | {count} |")

    lines += [
        "",
        "## Storage architecture (legacy)",
        "",
        "```",
        "wp-content/uploads/YYYY/MM/filename.ext",
        "  ├── Master (original upload)",
        "  └── WordPress generated sizes (in _wp_attachment_metadata)",
        "```",
        "",
        "## Storage architecture (new platform — TARGET)",
        "",
        "| Tier | Location | Purpose |",
        "|---|---|---|",
        "| **MASTER** | Server object storage `/var/www/movasseghi/media-master/` (not in Git) | Original legacy files preserved read-only |",
        "| **PRODUCTION** | Payload `media` collection + `/media` or S3 | Optimized WebP/AVIF derivatives |",
        "",
        "## Policy (HARD REQUIREMENT)",
        "",
        "1. **PRESERVE → MAP → REUSE → OPTIMIZE** — never delete/replace/invent",
        "2. No stock photos, no AI-generated product visuals when legacy exists",
        "3. Original masters kept; derivatives generated at deploy time",
        "4. `UNKNOWN_MAPPING` items require manual review — never guess",
        "",
        "## Filesystem gap",
        "",
        f"**{s['filesMissingLocally']}** attachment files referenced in DB are **NOT** present in `.legacy-extract/` uploads tree.",
        "",
        "The JetBackup homedir tarball (`ayrikcor.tar.gz`) was not fully extracted locally.",
        "",
        "**Next step:** Extract `wp-content/uploads/` from backup tarball to `media-master/` then re-run:",
        "",
        "```bash",
        "python scripts/legacy/extract-media-inventory.py",
        "python scripts/legacy/sync-media-masters.py  # copies verified files",
        "```",
        "",
        "## Brand assets (CONFIRMED in DB)",
        "",
        "| Attachment ID | Title | File |",
        "|---|---|---|",
    ]

    brand_keywords = ("logo", "favicon", "brand", "picmain", "hero")
    for a in inv["attachments"]:
        title = (a.get("title") or "").lower()
        path = (a.get("attachedFile") or "").lower()
        if any(k in title or k in path for k in brand_keywords):
            lines.append(
                f"| {a['legacyAttachmentId']} | {a.get('title','')} | `{a.get('attachedFile','')}` |"
            )

    lines += [
        "",
        "## Orphaned assets (sample — may be content/theme/plugin)",
        "",
        "Attachments not linked to any published product. **Do not delete** without review.",
        "",
        "| ID | Title | File | MIME |",
        "|---|---|---|---|",
    ]
    for a in inv.get("orphanedSample", [])[:15]:
        lines.append(
            f"| {a['legacyAttachmentId']} | {a.get('title','')} | `{a.get('attachedFile','')}` | {a.get('mimeType','')} |"
        )

    lines += [
        "",
        "## Machine-readable export",
        "",
        "- `.legacy-extract/media-inventory.json` (gitignored — local forensic)",
        "- `docs/audit/generated/product-media-map.json` (sanitized mapping)",
        "",
        "See also: [08-media.md](./08-media.md), [PRODUCT-MEDIA-MAP.md](./PRODUCT-MEDIA-MAP.md)",
        "",
    ]
    DOCS_MEDIA.write_text("\n".join(lines), encoding="utf-8")


def write_product_media_map_md(products: list[dict], summary: dict) -> None:
    lines = [
        "# PRODUCT-MEDIA-MAP — Legacy → New Platform",
        "",
        "**Status:** CONFIRMED (DB relationships) / PENDING (file copy + Payload upload)",
        f"**Products:** {len(products)} published | **With primary image:** {summary['productWithPrimaryImage']}",
        "",
        "## Mapping schema",
        "",
        "```",
        "Legacy Product (WP ID)",
        "  → Primary Image (attachment ID → _wp_attached_file → URL)",
        "  → Gallery (CSV attachment IDs)",
        "  → New Product (Payload slug / legacyId)",
        "  → New Media (Payload media collection)",
        "```",
        "",
        "## Per-product map (summary table)",
        "",
        "| Legacy ID | Product | SKU | Primary | Gallery # | File on disk | Status |",
        "|---:|---|---|---:|---:|---|---|",
    ]

    for p in products:
        primary = p.get("primaryImage") or {}
        pf = primary.get("attachedFile", "—") if isinstance(primary, dict) else "—"
        on_disk = "✅" if isinstance(primary, dict) and primary.get("fileOnDisk") else "❌"
        if p.get("primaryImageId") and not primary.get("attachedFile"):
            on_disk = "❌"
        if not p.get("primaryImageId"):
            on_disk = "—"
        status = p.get("migrationStatus", "PENDING")
        if isinstance(primary, dict) and primary.get("migrationStatus") == "UNKNOWN_MAPPING":
            status = "UNKNOWN_MAPPING"
        lines.append(
            f"| {p['legacyProductId']} | {p['name'][:40]} | {p.get('sku') or '—'} | "
            f"{p.get('primaryImageId') or '—'} | {len(p.get('galleryIds') or [])} | {on_disk} | {status} |"
        )

    lines += [
        "",
        "## Detailed example (first 5 products)",
        "",
    ]
    for p in products[:5]:
        lines.append(f"### {p['name']} (ID {p['legacyProductId']})")
        lines.append("")
        lines.append(f"- **Legacy URL:** {p['legacyUrl']}")
        lines.append(f"- **SKU:** {p.get('sku') or 'NOT FOUND'}")
        lines.append(f"- **Primary image ID:** {p.get('primaryImageId') or 'NONE'}")
        primary = p.get("primaryImage")
        if primary and isinstance(primary, dict) and primary.get("attachedFile"):
            lines.append(f"- **Primary file:** `{primary['attachedFile']}`")
            lines.append(f"- **Legacy CDN URL:** {primary.get('legacyUrl','')}")
            lines.append(f"- **Alt text:** {primary.get('alt') or 'EMPTY — SOURCE_REQUIRED'}")
            if primary.get("width"):
                lines.append(f"- **Dimensions:** {primary['width']}×{primary['height']}")
        lines.append(f"- **Gallery IDs:** {', '.join(map(str, p.get('galleryIds') or [])) or 'none'}")
        lines.append(f"- **Migration status:** {p.get('migrationStatus','PENDING')}")
        lines.append("")

    lines += [
        "## Image SEO rules (migration)",
        "",
        "- Alt text from `_wp_attachment_image_alt` when present",
        "- Fallback: product name + image role (e.g. \"بشقاب آملون — تصویر محصول\")",
        "- No keyword stuffing",
        "- Preserve meaningful filenames where possible",
        "",
        "## Full JSON",
        "",
        "See `docs/audit/generated/product-media-map.json` for complete machine-readable map.",
        "",
    ]
    DOCS_PRODUCT.write_text("\n".join(lines), encoding="utf-8")


if __name__ == "__main__":
    raise SystemExit(main())
