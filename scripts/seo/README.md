# SEO validation scripts (placeholder)

Scripts to be implemented during migration phase.

## Planned scripts

| Script | Purpose |
|---|---|
| `validate-redirects.ts` | Check legacy URL → new URL status |
| `validate-sitemap.ts` | Ensure clean URLs only |
| `validate-schema.ts` | JSON-LD parity on product/category |
| `validate-metadata.ts` | Title/H1/meta regression vs baseline |

## Inputs

- `docs/seo/REDIRECT-MAP.md`
- `docs/audit/generated/product-urls.md`
- `.legacy-extract/url-inventory.json`

## Usage (future)

```bash
npm run seo:validate
```

Not implemented in Phase 1.
