# SEO-AUDIT — Legacy SEO Forensics

**Canonical detail:** [12-seo.md](./12-seo.md) · **Strategy:** [../seo/SEO-STRATEGY.md](../seo/SEO-STRATEGY.md)

## Summary (CONFIRMED)

| Asset | Count / finding |
|---|---|
| Yoast indexables | 606 URLs |
| Product SEO records | 95 with focus keywords |
| Permalink | `/%postname%/` |
| Language | Persian fa, RTL |
| Primary commercial targets | ظروف یکبار مصرف گیاهی, ظروف یکبار مصرف, ظرف یکبار مصرف گیاهی |

## Migration rules

1. Preserve product slugs where possible
2. Preserve Yoast titles/descriptions → Payload `products.seo`
3. Preserve long product HTML → `legacyDescriptionHtml`
4. Category pages = commercial destinations (not blog posts for main keywords)
5. Redirect map required before domain cutover — see [../seo/REDIRECT-MAP.md](../seo/REDIRECT-MAP.md)

## Regression gate

**BLOCK release** if important product loses title, meta, H1 content, or primary image.

See [../seo/SEO-REGRESSION.md](../seo/SEO-REGRESSION.md).
