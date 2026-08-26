# Internal Linking Strategy (Draft)

## Principles

1. **Commercial paths first** — homepage → primary category → subcategory → product
2. **No spam link equity** — exclude compromised posts from graph
3. **Persian anchor text** — product/category names naturally
4. **Breadcrumbs everywhere** — category + product (schema-aligned)

## Hub structure (target)

```
Homepage
├── ظروف یکبار مصرف آملون (primary SEO hub)
│   ├── فله آملون → products
│   └── شیرینک آملون → products
├── پرفروش-ها
├── Shop (all products)
├── Pricing page (commercial)
└── Blog (minimal, high-quality only)
```

## Legacy assets to preserve linking value

| Asset | Link role |
|---|---|
| Category descriptions | Link to subcategories/products |
| Product descriptions | Link to related products |
| Pricing page | Link to categories + contact |
| CMS blocks (Woodmart) | Recreate as homepage category shortcuts |

## Rules

| Rule | Detail |
|---|---|
| Primary category | Must link to bulk + retail branches |
| Products | Related products (same subcategory) |
| Blog posts | Only link to relevant category/product if kept |
| Footer | Categories + contact + trust |
| Avoid | Orphan products (no incoming internal links) |

## Phase 2 task

Parse `wp_yoast_seo_links` to produce link graph CSV for migration prioritization.

## New platform implementation

- Server-rendered `<a href>` in HTML (not JS-only)
- Automatic related products from category proximity
- `incoming_link_count` from Yoast can prioritize high-value products
