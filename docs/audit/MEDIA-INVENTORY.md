# MEDIA-INVENTORY — Legacy Forensic Report

**Status:** Phase 1 media pass — CONFIRMED (DB) / PARTIAL (filesystem)
**Source:** `.legacy-extract/db/site17896548586.sql` + local uploads scan

## Executive summary

| Metric | Count | Confidence |
|---|---:|---|
| Total attachment records (DB) | 149 | CONFIRMED |
| Published products | 95 | CONFIRMED |
| Products with primary image ID | 82 | CONFIRMED |
| Products with gallery | 17 | CONFIRMED |
| Product-linked attachment IDs | 85 | CONFIRMED |
| Category thumbnail references | 21 | CONFIRMED |
| Files found in local extract | 149 | CONFIRMED |
| Files **missing** locally (need tar/backup) | 0 | CONFIRMED |
| Confident product↔media mappings | 99 | CONFIRMED |
| Unknown mappings | 0 | CONFIRMED |
| Orphaned attachments (not product-linked) | 64 | INFERRED |

## MIME type breakdown

| MIME | Count |
|---|---:|
| `image/png` | 115 |
| `image/jpeg` | 23 |
| `image/webp` | 10 |
| `image/svg+xml` | 1 |

## Storage architecture (legacy)

```
wp-content/uploads/YYYY/MM/filename.ext
  ├── Master (original upload)
  └── WordPress generated sizes (in _wp_attachment_metadata)
```

## Storage architecture (new platform — TARGET)

| Tier | Location | Purpose |
|---|---|---|
| **MASTER** | Server object storage `/var/www/movasseghi/media-master/` (not in Git) | Original legacy files preserved read-only |
| **PRODUCTION** | Payload `media` collection + `/media` or S3 | Optimized WebP/AVIF derivatives |

## Policy (HARD REQUIREMENT)

1. **PRESERVE → MAP → REUSE → OPTIMIZE** — never delete/replace/invent
2. No stock photos, no AI-generated product visuals when legacy exists
3. Original masters kept; derivatives generated at deploy time
4. `UNKNOWN_MAPPING` items require manual review — never guess

## Filesystem gap

**0** attachment files referenced in DB are **NOT** present in `.legacy-extract/` uploads tree.

The JetBackup homedir tarball (`ayrikcor.tar.gz`) was not fully extracted locally.

**Next step:** Extract `wp-content/uploads/` from backup tarball to `media-master/` then re-run:

```bash
python scripts/legacy/extract-media-inventory.py
python scripts/legacy/sync-media-masters.py  # copies verified files
```

## Brand assets (CONFIRMED in DB)

| Attachment ID | Title | File |
|---|---|---|
| 12 | logo-lux-wood | `2022/01/logo-lux-wood.png` |
| 188 | logo-lux-wood | `2022/01/logo-lux-wood-1.png` |
| 214 | logo_top | `2023/12/logo_top.png` |
| 215 | cropped-logo_top.png | `2023/12/cropped-logo_top.png` |
| 250 | logo_topMain | `2023/12/logo_topMain.png` |
| 251 | cropped-logo_topMain.png | `2023/12/cropped-logo_topMain.png` |
| 253 | logo_main | `2023/12/logo_main.png` |
| 255 | logo-mobile | `2023/12/logo-mobile.png` |
| 259 | picMain-1 | `2023/12/picMain-1.png` |

## Orphaned assets (sample — may be content/theme/plugin)

Attachments not linked to any published product. **Do not delete** without review.

| ID | Title | File | MIME |
|---|---|---|---|
| 6 | woocommerce-placeholder | `woocommerce-placeholder.png` | image/png |
| 12 | logo-lux-wood | `2022/01/logo-lux-wood.png` | image/png |
| 25 | phone-call-svgrepo-com (1) | `2022/01/phone-call-svgrepo-com-1.svg` | image/svg+xml |
| 31 | high-heels.png | `2022/01/high-heels.png` | image/png |
| 32 | high-heels-1-1.png | `2022/01/high-heels-1-1.png` | image/png |
| 80 | samandehi | `2022/01/samandehi.png` | image/png |
| 81 | enamad | `2022/01/enamad.png` | image/png |
| 164 | 1-1-524x617 | `2022/01/1-1-524x617-1.webp` | image/webp |
| 167 | pro_02 | `2022/01/pro_02.jpg` | image/jpeg |
| 168 | pro_03 | `2022/01/pro_03.jpg` | image/jpeg |
| 169 | pro_05 | `2022/01/pro_05.jpg` | image/jpeg |
| 170 | pro_06 (1) | `2022/01/pro_06-1.jpg` | image/jpeg |
| 172 | pro_07 | `2022/01/pro_07.jpg` | image/jpeg |
| 173 | product_24 | `2022/01/product_24.jpg` | image/jpeg |
| 174 | product_26 | `2022/01/product_26.jpg` | image/jpeg |

## Machine-readable export

- `.legacy-extract/media-inventory.json` (gitignored — local forensic)
- `docs/audit/generated/product-media-map.json` (sanitized mapping)

See also: [08-media.md](./08-media.md), [PRODUCT-MEDIA-MAP.md](./PRODUCT-MEDIA-MAP.md)
