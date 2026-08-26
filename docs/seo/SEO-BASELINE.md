# SEO Baseline

**Audit date:** 2026-08-26  
**Legacy domain:** www.ayrik-cornstarch.com  
**Search Console history:** UNKNOWN — do not assume rankings

## Index inventory (from Yoast DB)

| Type | Published URLs |
|---|---:|
| Products | 95 |
| Pages | 11 |
| Posts | 209 |
| **Unique URLs total** | 606 |

## Hostname baseline

| Variant | In use |
|---|---|
| www.ayrik-cornstarch.com | Primary in Yoast permalinks |
| ayrik-cornstarch.com (non-www) | siteurl option |

**Issue:** inconsistent canonical host.

## Site metadata baseline

| Element | Value |
|---|---|
| Site name | ایریک پلاستیک ایرانیان |
| Tagline | \| فروشگاه ظروف یکبار مصرف |
| Permalink | /%postname%/ |
| Language | Persian (RTL) |

## Primary keyword baseline (project targets)

| Keyword | Legacy landing candidate | Indexed spam risk |
|---|---|---|
| ظروف یکبار مصرف گیاهی | `/product-category/ظروف-یکبار-مصرف-آملون/` | Low on category |
| ظروف یکبار مصرف | `/shop/` + categories | Medium |
| ظرف یکبار مصرف گیاهی | Product long-tail URLs | Low |

**Note:** Legacy brand does not use "موثقی". New brand starts without inherited SERP for that name.

## Toxic index baseline

~318 URLs classified as spam/gambling (heuristic). These must not appear in new sitemap.

Examples of **exclude** patterns:

- `/casino/`, `/bet/`, `/bonus/`, `/bitcoin/` slugs
- Non-Persian gambling content

## Technical baseline

| Check | Status |
|---|---|
| Live crawl | FAILED (HTTP 500) |
| robots.txt | NOT CAPTURED |
| XML sitemap | NOT CAPTURED (Yoast expected) |
| Page speed | NOT MEASURED |
| Mobile-friendly | NOT MEASURED (Woodmart is responsive — inferred) |
| HTTPS | CONFIRMED in URLs |
| Structured data | INFERRED WooCommerce+Yoast |

## Asset baseline

| Asset | Notes |
|---|---|
| Product meta descriptions | Present via Yoast — commercial Persian copy |
| Focus keywords | Per-product in Yoast |
| Category descriptions | SEO paragraphs on main categories |
| OG image (home) | picMain-1.png |

## Baseline metrics to capture at new site launch

- [ ] GSC impressions/clicks (starting point)
- [ ] Indexed page count (clean)
- [ ] Core Web Vitals (Lighthouse CI)
- [ ] Rich result status for Product schema

## Historical performance

**NOT AVAILABLE.** Do not claim legacy Google rankings without GSC export.
