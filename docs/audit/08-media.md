# 08 — Media

**Confidence:** CONFIRMED (attachment count) / PARTIAL (filesystem extraction)

## Summary

| Metric | Value |
|---|---:|
| Attachment post type rows | 149 |
| Homepage hero image | `uploads/2023/12/picMain-1.png` |
| WooCommerce placeholder | `woocommerce-placeholder.png` (default) |

## Storage layout (legacy)

```
wp-content/uploads/
├── YYYY/MM/   (standard WordPress dated folders)
└── woocommerce_uploads/  (downloadable product files if any)
```

Download directory registrations in DB:

- `file:///home/ayrikcor/public_html/wp-content/uploads/woocommerce_uploads/`
- `https://ayrik-cornstarch.com/wp-content/uploads/woocommerce_uploads/`
- `https://www.ayrik-cornstarch.com/wp-content/uploads/woocommerce_uploads/`

## Product images

Product galleries stored via:

- `_thumbnail_id` — featured image
- `_product_image_gallery` — additional images (attachment IDs)

**Migration task:** Build ID → URL map from `wp_posts` (attachment type) + `_wp_attached_file` postmeta.

## Image SEO

- Yoast references `first-content-image` for homepage OG
- Product alt text: **NOT FULLY AUDITED** — requires attachment meta pass

## Extraction status

Full `uploads/` tree is inside `homedir/ayrikcor.tar.gz`. Partial extraction to `.legacy-extract/homedir2/` succeeded for PHP code; uploads directory size not fully enumerated in this pass.

## Migration requirements

1. Export all product-linked media (featured + gallery)
2. Preserve filenames where possible for redirect/CDN mapping
3. Generate responsive variants on new platform (WebP/AVIF)
4. Reconcile alt text from legacy or mark SOURCE_REQUIRED
5. Do not migrate malware-adjacent uploads without virus scan

## Risks

| Risk | Mitigation |
|---|---|
| Missing files in backup | Compare attachment meta vs filesystem in deeper pass |
| Hotlinked / external images | Audit `post_content` for external URLs |
| Large uncompressed archive | Stream copy during migration, not full re-read |

## Unknowns

- Total upload disk size
- Complete alt-text coverage
- Whether custom category images were configured (mostly empty in termmeta)
