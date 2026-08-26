# 20 — Unknowns

Items that could **not** be verified from legacy evidence alone. Each includes recommended verification method.

## Infrastructure & runtime

| ID | Unknown | Verify via |
|---|---|---|
| U1 | Current production domain for Movasseghi brand | Business / DNS lookup |
| U2 | Live checkout payment methods | Staging restore or business interview |
| U3 | Live shipping methods and costs | Staging restore |
| U4 | Guest checkout enabled? | WC settings in restored admin |
| U5 | Coupon/discount rules | WC admin + DB `shop_coupon` posts |
| U6 | Actual order history | DB deeper query + business records |
| U7 | Email delivery provider (SMTP) | Hosting panel / WP mail plugin config |

## Business operations

| ID | Unknown | Verify via |
|---|---|---|
| U8 | Wholesale pricing rules | Business owner |
| U9 | MOQ per SKU/category | Business owner + legacy price sheets |
| U10 | Card-to-card workflow (which bank, verification) | Business owner |
| U11 | Organizational invoice required fields | Business/legal |
| U12 | Phone order logging process | Business owner |
| U13 | Transport: customer vs seller — when each applies | Business owner |
| U14 | Return/refund policy | Legacy pages + business |
| U15 | Active phone number on legacy site (09125199105?) | Live site / business |

## SEO & analytics

| ID | Unknown | Verify via |
|---|---|---|
| U16 | Google Search Console historical performance | GSC access |
| U17 | Google Analytics property | GA admin |
| U18 | Backlink profile quality | Ahrefs/GSC |
| U19 | Which blog posts are legitimate vs spam (full list) | Content review queue |
| U20 | robots.txt and live sitemap contents | Live crawl when restored |

## Product data

| ID | Unknown | Verify via |
|---|---|---|
| U21 | Complete SKU coverage | Export `_sku` meta |
| U22 | Stock management actively used? | `_manage_stock` stats |
| U23 | Variable products exist? | `product_variation` count |
| U24 | Downloadable/virtual products | Product type terms |
| U25 | Heat resistance specs accuracy | Manufacturer docs SOURCE_REQUIRED |

## Technical

| ID | Unknown | Verify via |
|---|---|---|
| U26 | Full uploads directory file list | Tar extraction + diff vs attachments |
| U27 | Menu structure URLs | Parse nav_menu_item posts |
| U28 | Elementor JSON templates per page | Export from restored site |
| U29 | Revolution Slider still active on front | Live site |
| U30 | Custom functions.php modifications | Theme file diff |

## Brand

| ID | Unknown | Verify via |
|---|---|---|
| U31 | Official relationship Ayrik ↔ Movasseghi | Business/legal |
| U32 | Whether ayrik-cornstarch.com will redirect to new domain | Business decision |
| U33 | Instagram handle change/continuity | Social accounts |

## Competitor/SERP

| ID | Unknown | Verify via |
|---|---|---|
| U34 | Exact SERP rankings for primary 3 keywords | Manual SERP check Iran VPN |
| U35 | ayrik-cornstarch.com current index status | `site:` search |

---

## Verification priority queue

**P0 (before migration planning):**

- U2, U8, U9, U10, U13, U15, U31, U32

**P1 (before content migration):**

- U19, U21, U26, U27

**P2 (before launch):**

- U16, U17, U34, U35

---

When an unknown is resolved, update:

- `docs/BUSINESS-RULES.md`
- `docs/knowledge/PROVENANCE.md`
- Relevant audit section with CONFIRMED status
