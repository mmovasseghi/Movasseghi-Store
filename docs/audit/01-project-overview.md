# 01 — Project Overview

**Classification:** CONFIRMED (backup metadata) + INFERRED (brand lineage)

## New project identity

| Field | Value | Source |
|---|---|---|
| Project | Movasseghi Store | Project spec |
| Brand (target) | فروشگاه موثقی | Project spec |
| Repository target | `mmovasseghi/Movasseghi-Store` | Project spec |
| Market | Tehran primary; Iran secondary | Project spec |
| Product domain | Disposable plant-based / eco-oriented food-service products | Project spec + legacy catalog |

## Legacy identity (backup evidence)

| Field | Value | Source | Confidence |
|---|---|---|---|
| Legacy domain | `ayrik-cornstarch.com` | `wp_options.siteurl`, DNS zone | CONFIRMED |
| Public home URL | `https://www.ayrik-cornstarch.com` | `wp_options.home` | CONFIRMED |
| Legacy brand name | ایریک پلاستیک ایرانیان | `wp_options.blogname` | CONFIRMED |
| Tagline | فروشگاه ظروف یکبار مصرف | `wp_options.blogdescription` | CONFIRMED |
| Hosting account | `ayrikcor` on `server83.nocmdp.com` | JetBackup index | CONFIRMED |
| Server IP (legacy) | `37.156.144.83` | DNS zone file | CONFIRMED |
| PHP version (domain config) | 7.4 | `ayrik-cornstarch.com.conf` (base64) | CONFIRMED |
| Backup date | ~2025-12-13 | `jetbackup.index` `created: 1765619237` | CONFIRMED |
| Account suspended at parent backup time | yes | `jetbackup.index` `suspended: true` | CONFIRMED |

## Relationship: Ayrik → Movasseghi

| Evidence | Detail | Confidence |
|---|---|---|
| WP user `Movasseghi` | Admin-capable user, registered 2024-04-04, email `mmovaseghi@outlook.com` | CONFIRMED |
| Product material focus | Corn-starch / Amorphous (آملون) plant-based disposables | CONFIRMED |
| Domain name | `ayrik-cornstarch.com` references corn starch (نشاسته ذرت) | CONFIRMED |
| Movasseghi brand in legacy content | **NOT FOUND** as site title or domain | NOT FOUND |
| Planned rebrand to موثقی | Stated in project spec | INFERRED — business decision |

**Conclusion:** Treat this backup as the **commercial predecessor storefront**, not a 1:1 match for the future Movasseghi domain/branding. Migrate **catalog, SEO, and business knowledge**; do not assume URL or brand parity without explicit mapping.

## Backup package contents

| Path | Contents |
|---|---|
| `backup/homedir/ayrikcor.tar.gz` | Full cPanel home (~384 MB) |
| `backup/database/ayrikcor_site17896548586.sql.gz` | **Primary WordPress DB** (~23 MB decompressed) |
| `backup/database/ayrikcor_site1235445166.sql.gz` | Secondary DB — **unrelated** `freefarmdota2.top` site |
| `backup/domain/` | DNS zones (includes unrelated `freefarmdota2.top`) |
| `backup/certificate/` | SSL material for ayrik-cornstarch.com |

## Secondary database (excluded from migration)

Database `ayrikcor_site1235445166` → `freefarmdota2.top` ("Free Farm Dota 2" blog). Table prefix `dswp_`. **Not part of Movasseghi/Ayrik commerce.** Ignore for migration.

## Live site status (audit date)

| Check | Result | Confidence |
|---|---|---|
| `https://www.ayrik-cornstarch.com/` | HTTP **500** | CONFIRMED (2026-08-26 fetch) |
| Behavioral UX audit | **Blocked** | UNKNOWN runtime behavior |

## Target deployment (new platform — not legacy)

| Item | Value | Source |
|---|---|---|
| Production server | `91.107.181.79` (Hetzner) | Project spec |
| Contact phone (business) | `09125199105` | Project spec — **not in legacy DB** |
| Instagram | `movasseghiStore` | Project spec — **not in legacy DB** |

## Sensitive data handling

Legacy `wp-config.php` contains database credentials and WordPress salts. These were observed during forensic extraction only. **Do not commit.** Rotate all credentials if any legacy environment is still reachable.
