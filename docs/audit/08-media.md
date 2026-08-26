# 08 — Media (updated 2026-08-26)

**Confidence:** CONFIRMED (DB inventory) / PARTIAL (filesystem — 52/149 masters local)

## Media phase status: DOCUMENTED ✅

Full reports:

- [MEDIA-INVENTORY.md](./MEDIA-INVENTORY.md)
- [PRODUCT-MEDIA-MAP.md](./PRODUCT-MEDIA-MAP.md)
- `docs/audit/generated/product-media-map.json`

## Summary

| Metric | Value |
|---|---:|
| Attachment records (DB) | 149 |
| Image files in local uploads tree | ~902 (incl. WP sizes) |
| Unique attachment masters mapped | 149 |
| Published products | 95 |
| Products with primary image ID | 82 |
| Products with gallery | 17 |
| Masters copied to `media-master/` | 52 |
| Masters missing locally | 97 (mostly `2024/01/` — need full JetBackup tar) |
| Unknown product↔image mappings | 0 |
| Orphaned attachments | 64 (content/brand/theme — review before archive) |

## Policy

**PRESERVE → MAP → REUSE → OPTIMIZE** — no stock/AI replacement of authentic product photos.

## Migration scripts

```bash
python scripts/legacy/extract-media-inventory.py
python scripts/legacy/sync-media-masters.py
npm run import:media   # Payload upload + product link
```

## Storage (new platform)

| Tier | Path | Git |
|---|---|---|
| Masters | `media-master/legacy-uploads/` | ❌ gitignored |
| Production | Payload `media` + server `/media` | metadata only |

Media collection tracks `legacyAttachmentId` + `legacyPath` for provenance.

## Remaining gap

97 attachment files referenced in DB but not in partial extract. Extract full `wp-content/uploads/` from JetBackup homedir tarball, then re-run sync + import.

Legacy live URLs return **503** — cannot HTTP-fetch missing files.
