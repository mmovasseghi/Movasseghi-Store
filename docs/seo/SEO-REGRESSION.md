# SEO Regression Checklist

Run before every production release.

## URL & redirects

- [ ] All P0/P1 legacy URLs resolve (301 or approved 410)
- [ ] No spam URLs in sitemap
- [ ] Canonical host enforced (single www/apex)
- [ ] Staging not indexable (noindex + robots)

## Metadata parity

- [ ] Homepage title/description present
- [ ] 95 products have meta descriptions (migrated or improved post-parity)
- [ ] Primary category has description + H1
- [ ] No accidental noindex on commercial pages

## Structured data

- [ ] Product schema valid on sample products
- [ ] Breadcrumb schema on category + product
- [ ] Organization schema updated for new brand

## Technical

- [ ] sitemap.xml lists clean URLs only
- [ ] robots.txt correct
- [ ] Internal links: no broken product/category links
- [ ] Persian slug URLs encode correctly

## Content

- [ ] No casino/spam content published
- [ ] Environmental claims flagged SOURCE_REQUIRED unchanged unless approved
- [ ] Primary keyword pages are commercial (not thin blog)

## Performance (SEO-related)

- [ ] LCP/INP/CLS within budget on homepage, category, product (mobile)

## Tools (future)

- `scripts/seo/validate-urls.ts`
- Lighthouse CI
- Custom parity diff vs `docs/seo/URL-MAP.md`

## Block release if

- Important product URL 404
- Product schema broken site-wide
- Staging indexed
- Primary category missing H1/meta
