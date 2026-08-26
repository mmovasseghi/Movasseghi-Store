# 12 — SEO

**Plugin:** Yoast SEO + Yoast SEO Premium  
**Confidence:** CONFIRMED

## Site-wide SEO settings

| Field | Value | Source |
|---|---|---|
| Site title pattern | `%%sitename%% %%page%% %%sep%% %%sitedesc%%` | Yoast home indexable |
| Site description | `\| فروشگاه ظروف یکبار مصرف` | `blogdescription` |
| Separator | `\|` | Yoast |
| Permalink | `/%postname%/` | WordPress |
| Language | Persian (fa) | Content |
| RTL | Yes | Theme/market |

## Indexable inventory

| Type | Published count |
|---|---:|
| Products | 95 |
| Pages | 11 |
| Posts | 209 |
| **Total parsed** | 613 rows |

## Product SEO pattern

Typical Yoast product record includes:

- `primary_focus_keyword` — Persian product name + size
- `description` — commercial meta description (150–160 char style)
- `breadcrumb_title` — Persian product title
- `permalink` — pretty URL under `/product/`

Example (CONFIRMED):

- **Keyword:** `لیوان 500 سی سی آملون`
- **Meta:** `لیوان 500 سی سی آملون - لیوان گیاهی بزرگ از نشاسته ذرت، مقاوم تا 140 درجه...`

## Homepage SEO

| Element | Value |
|---|---|
| Title | Uses Yoast template with sitename + tagline |
| OG image | `picMain-1.png` |
| Schema page type | Set in indexables (verify in 14-schema) |

## SEO pollution (CRITICAL)

~209 published posts include **casino/gambling spam** in English/European languages with unrelated keywords. These:

- Pollute crawl budget
- Create toxic backlink/index patterns
- Must **NOT** be migrated

**Heuristic:** Posts with slugs containing `casino`, `bet`, `bonus`, `bitcoin`, etc.

## Primary keyword targets (project spec vs legacy)

| Target query | Legacy alignment | Gap |
|---|---|---|
| ظروف یکبار مصرف گیاهی | Category + product copy use "گیاهی" / "آملون" | Strong — category hub exists |
| ظروف یکبار مصرف | Shop + categories | Adequate |
| ظرف یکبار مصرف گیاهی | Product long-tail | Adequate via product pages |

Legacy did **not** use "موثقی" branding in SEO metadata.

## Technical SEO observations

| Item | Status |
|---|---|
| Yoast indexables table | Present — good migration source |
| Internal link table (`wp_yoast_seo_links`) | Present — analyze in 13-internal-links |
| XML sitemap | Expected via Yoast — file not extracted |
| robots.txt | NOT AUDITED |
| Canonical host consistency | **Issue** — www vs non-www split |
| Search Console history | UNKNOWN |

## SEO migration rules

1. Migrate Yoast meta for **products + legitimate pages** only
2. Preserve product/category slugs where possible
3. Do not migrate spam post indexables
4. Rebuild sitemap from clean dataset
5. Connect GSC before production indexing
6. Baseline documented in `docs/seo/SEO-BASELINE.md`

## Content governance

Environmental and health claims in meta descriptions must be migrated verbatim first, then reviewed. Do not strengthen claims without SOURCE_REQUIRED verification.

See also: `docs/seo/` directory.
