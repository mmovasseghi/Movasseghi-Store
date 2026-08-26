# Legacy Audit — Movasseghi Store

**Phase:** 1 — Legacy Forensics  
**Status:** COMPLETE — awaiting **Gate 1** approval  
**Audit date:** 2026-08-26  
**Legacy source:** `Old Verison WebSite/backup` (JetBackup, read-only)  
**Extracted analysis workspace:** `.legacy-extract/` (gitignored, derived data)

---

## Executive summary

The available legacy backup is a **cPanel JetBackup** snapshot (account `ayrikcor`, package `IR-WP-1_ayrikcor`, backup name `snap.download`, ~390 MB compressed, created **2025-12-13** per `jetbackup.index` timestamp `1765619237`).

The WordPress site in this backup operated as **ایریک پلاستیک ایرانیان** at **`https://www.ayrik-cornstarch.com`**. This is the direct commercial predecessor to the planned **فروشگاه موثقی (Movasseghi Store)** rebrand. Evidence: administrator user `Movasseghi` (Mohammad Sina Movasseghi Nezhad) in `wp_users`; product catalog aligned with disposable plant-based / Amorphous (آملون) food-service containers.

**Critical finding:** The legacy installation is **severely compromised**. ~7,779 spam user accounts, hundreds of casino/gambling spam posts, and a malicious plugin (`HelloDollyV2_ixwz`) were present at backup time. The live site currently returns **HTTP 500** and could not be behavior-verified.

---

## Discovered component totals

| Domain | Count | Confidence |
|---|---:|---|
| Database tables | 102 | CONFIRMED |
| Published WooCommerce products | 95 | CONFIRMED |
| Product categories | 22 | CONFIRMED |
| Published pages | 11 | CONFIRMED |
| Published blog posts | 209 | CONFIRMED (many are spam) |
| Yoast indexable URLs | 606 unique | CONFIRMED |
| Legitimate-looking URLs (heuristic) | ~288 | INFERRED |
| Spam/gambling URLs (heuristic) | ~318 | INFERRED |
| Media attachments | 149 | CONFIRMED |
| Legitimate admin users | 15 | CONFIRMED |
| Spam subscriber accounts | 7,779 | CONFIRMED |
| Active plugin directories (filesystem) | 17 | CONFIRMED |
| RevSlider sliders | 1+ | CONFIRMED |

---

## Major features (legacy)

| Feature | Present | Notes |
|---|---|---|
| WooCommerce catalog | Yes | 95 published products, IRT currency, Tehran default |
| Woodmart theme | Yes | v7.0.4, AJAX shop, bordered product grid |
| Elementor | Yes | Page builder, product/content layouts |
| Yoast SEO Premium | Yes | Indexables, focus keywords on products |
| Product categories (hierarchical) | Yes | آملون-focused taxonomy, bulk + retail packaging splits |
| Blog / magazine (`/mag/`) | Yes | Mixed legitimate + massive spam injection |
| User registration | Yes | **Open registration enabled** — abuse vector |
| Wishlist (Woodmart) | Yes | Theme feature |
| Wordfence WAF | Yes | Alert email configured |
| Flying Press (cache) | Yes | Performance plugin |
| Special Offer Woodmart widget | Yes | Custom/purchased Elementor offer carousel |
| Iranian payment gateway plugin | **NOT FOUND** | Only PayPal + MaxMind settings keys in DB |
| Wholesale pricing plugin | **NOT FOUND** | No B2B/wholesale plugin detected |
| Shipping zones/rules | UNKNOWN | Not fully extracted; needs dedicated parse |
| Phone click / organizational invoice flows | UNKNOWN | Not evidenced in DB |

---

## Major business rules (evidence-based)

| Rule | Status | Source |
|---|---|---|
| Sells disposable plant-based (آملون) food-service products | CONFIRMED | Product categories, descriptions |
| Retail + bulk packaging categories | CONFIRMED | Category tree (فله vs شیرینک) |
| Currency: Toman (IRT), 0 decimals | CONFIRMED | `woocommerce_currency`, `woocommerce_price_num_decimals` |
| Default market: Iran, Tehran (`IR:THR`) | CONFIRMED | `woocommerce_default_country` |
| Permalink: `/%postname%/` | CONFIRMED | `permalink_structure` |
| Brand name in legacy: ایریک پلاستیک ایرانیان | CONFIRMED | `blogname`, product copy |
| Planned brand: فروشگاه موثقی | INFERRED | Project spec + admin user lineage |
| Phone 09125199105 | **NOT FOUND** in legacy | Project spec only — SOURCE_REQUIRED for legacy parity |
| Instagram movasseghiStore | **NOT FOUND** in legacy | Project spec only |
| Wholesale pricing tiers | **NOT FOUND** | No plugin/meta pattern detected |
| Card-to-card / organizational invoice | **NOT FOUND** | No gateway/form evidence |
| Card-to-card / phone order (business intent) | INFERRED | Project spec; not in legacy DB |

---

## Major SEO assets

| Asset | Value | Migration priority |
|---|---|---|
| Homepage | `https://www.ayrik-cornstarch.com/` | HIGH — rebrand + redirect strategy needed |
| Shop archive | `/shop/` | HIGH |
| Primary category hub | `/product-category/ظروف-یکبار-مصرف-آملون/` (URL-encoded slug) | **CRITICAL** — aligns with target query "ظروف یکبار مصرف گیاهی" |
| 95 product URLs | `/product/{slug}/` | HIGH — preserve slugs where possible |
| Product meta descriptions | Yoast-generated, Persian, commercial | HIGH |
| Focus keywords per product | Present in Yoast indexables | MEDIUM |
| Blog posts (legitimate Persian) | Small subset of 209 | MEDIUM — audit individually |
| Casino spam posts | ~100+ URLs | **DO NOT MIGRATE** — 410 or drop |

**Primary SEO target alignment (project spec):**

1. ظروف یکبار مصرف گیاهی → map to main commercial category/shop landing (legacy: آملون category family)
2. ظروف یکبار مصرف → shop + category hub
3. ظرف یکبار مصرف گیاهی → product/category long-tail support (not duplicate doorway pages)

---

## Major migration risks

| Risk | Severity | Mitigation |
|---|---|---|
| **Site compromise / malware** | CRITICAL | Do not deploy legacy files; migrate data only; full security review |
| **Spam URL index pollution** | CRITICAL | Exclude spam posts from sitemap/redirect map; 410 on migration |
| **7,779 fake users** | HIGH | Do not migrate subscriber spam; migrate only verified customers/orders |
| **Domain change** (ayrik-cornstarch.com → new domain) | HIGH | Full 301 redirect map; GSC change-of-address |
| **Brand rename** (Ayrik → Movasseghi) | MEDIUM | Content parity check; update Organization schema |
| **Live site unavailable (500)** | HIGH | Cannot verify runtime behavior; staging parity required |
| **Credentials in wp-config** | HIGH | Rotate all secrets; never commit; exclude from repo |
| **Open user registration** | HIGH | Disable public registration on new platform unless required |
| **Missing payment gateway config** | MEDIUM | Re-implement from business requirements, not legacy DB |
| **Environmental/marketing claims** | MEDIUM | Migrate verbatim first; mark unverified claims SOURCE_REQUIRED |

---

## Unknowns

See [20-unknowns.md](./20-unknowns.md).

---

## Items requiring live-site verification

- [ ] Checkout flow and enabled payment methods
- [ ] Shipping methods (customer vehicle vs seller transport)
- [ ] Wholesale / MOQ behavior at checkout
- [ ] Cart persistence and guest checkout
- [ ] Actual homepage layout and mobile UX
- [ ] Search and filter behavior
- [ ] Forms (contact, quote, B2B)
- [ ] Google Search Console historical performance
- [ ] Current DNS / active domain for Movasseghi brand (if any)
- [ ] Whether ayrik-cornstarch.com still holds SEO authority

---

## Recommended next phase

**After Gate 1 approval:**

1. **Gate 2** — Architecture decision (`docs/decisions/ARCHITECTURE-DECISION.md` draft started; final choice pending)
2. **Gate 3** — Design system + information architecture
3. **Content sanitization plan** — separate legitimate catalog/blog from spam
4. **Competitor/SERP deep dive** — expand `docs/competition/` with manual SERP capture
5. **Clean migration dataset** — products, categories, media, legitimate pages only
6. **Do not build storefront** until Gates 1–3 approved

---

## Audit document index

| Doc | Topic |
|---|---|
| [01-project-overview.md](./01-project-overview.md) | Identity, backup provenance, domain mapping |
| [02-wordpress-architecture.md](./02-wordpress-architecture.md) | Core stack |
| [03-theme-and-plugins.md](./03-theme-and-plugins.md) | Theme + plugin inventory |
| [04-database.md](./04-database.md) | Schema summary |
| [05-pages.md](./05-pages.md) | Page inventory |
| [06-products.md](./06-products.md) | Product catalog |
| [07-categories.md](./07-categories.md) | Taxonomy |
| [08-media.md](./08-media.md) | Uploads / attachments |
| [09-features.md](./09-features.md) | Feature map |
| [10-integrations.md](./10-integrations.md) | External services |
| [11-urls.md](./11-urls.md) | URL inventory |
| [12-seo.md](./12-seo.md) | SEO plugin + metadata |
| [13-internal-links.md](./13-internal-links.md) | Linking patterns |
| [14-schema.md](./14-schema.md) | Structured data |
| [15-forms-and-checkout.md](./15-forms-and-checkout.md) | Commerce flows |
| [16-custom-code.md](./16-custom-code.md) | Custom PHP/JS |
| [17-data-model.md](./17-data-model.md) | Entity model |
| [18-business-rules.md](./18-business-rules.md) | Commerce rules |
| [19-risks.md](./19-risks.md) | Risk register |
| [20-unknowns.md](./20-unknowns.md) | Gaps |
| [generated/](./generated/) | Machine-generated tables |

---

## Extraction tooling

| Script | Purpose |
|---|---|
| `scripts/legacy/extract-audit-v2.py` | SQL → JSON audit summary |
| `scripts/legacy/generate-inventories.py` | JSON → markdown tables |
| `scripts/legacy/extract-payments.py` | Payment option key scan |

Raw extracted JSON: `.legacy-extract/audit-data-v2.json`, `.legacy-extract/url-inventory.json`
