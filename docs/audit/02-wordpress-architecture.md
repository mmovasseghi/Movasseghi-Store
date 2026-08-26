# 02 — WordPress Architecture

**Confidence:** CONFIRMED (filesystem + database)

## Core versions

| Component | Version | Source |
|---|---|---|
| WordPress | **6.8.3** | `wp-includes/version.php` |
| WooCommerce | **9.4.2** | `wp-content/plugins/woocommerce/woocommerce.php` |
| Woodmart theme | **7.0.4** | `wp-content/themes/woodmart/style.css` |
| PHP (hosting config) | 7.4 | JetBackup domain config |

## Database

| Setting | Value |
|---|---|
| Database name | `ayrikcor_site17896548586` |
| Table prefix | `wp_` |
| Charset | `utf8mb4` |
| Tables | 102 |
| Collation | `utf8mb4_unicode_ci` (majority) |

## URL / routing

| Setting | Value |
|---|---|
| `siteurl` | `https://ayrik-cornstarch.com/` |
| `home` | `https://www.ayrik-cornstarch.com` |
| Permalink structure | `/%postname%/` |
| WWW vs non-WWW | **Split** — siteurl non-www, home www |

**Migration note:** Canonical/host consistency should be enforced on the new platform (pick one hostname).

## Content model (post types observed)

| Post type | Approx. count | Role |
|---|---:|---|
| `product` | 135 total rows / **95 published** | WooCommerce catalog |
| `page` | 14 rows / **11 published** | Store, checkout, content |
| `post` | 212 rows / **209 published** | Blog (+ heavy spam) |
| `attachment` | 149 | Media |
| `revision` | 192 | Editor history |
| `nav_menu_item` | 32 | Navigation |
| `elementor_library` | 1 | Elementor templates |

## Custom post types

No major custom post types beyond WooCommerce and Elementor/Woodmart standard types detected in post type counts.

## Architecture diagram (legacy)

```
┌─────────────────────────────────────────────────────────┐
│  Browser (RTL Persian)                                   │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│  WordPress 6.8.3 + Woodmart 7.0.4 (parent theme)       │
│  Elementor page builder                                  │
└───────────────┬─────────────────────┬───────────────────┘
                │                     │
    ┌───────────▼──────────┐  ┌───────▼────────┐
    │  WooCommerce 9.4.2   │  │  Yoast SEO     │
    │  Catalog / Cart      │  │  Premium       │
    └───────────┬──────────┘  └────────────────┘
                │
    ┌───────────▼──────────────────────────┐
    │  MySQL (102 tables)                   │
    │  wp_posts, wp_postmeta, wp_terms, ... │
    └──────────────────────────────────────┘
```

## Security architecture (legacy — deficient)

| Control | Status |
|---|---|
| Wordfence WAF | Installed |
| Open user registration | **Enabled** — exploited |
| wp-file-manager plugin | Present — common attack vector |
| Malware plugin | `HelloDollyV2_ixwz` — **confirmed malicious** |
| Account spam | 7,779+ fake subscribers |

## Filesystem layout (extracted)

```
public_html/
├── wp-admin/
├── wp-includes/
├── wp-content/
│   ├── themes/woodmart/
│   ├── plugins/ (17 directories)
│   └── uploads/
└── wp-config.php  (SENSITIVE — do not commit)
```

## Child theme

**NOT FOUND.** `stylesheet` and `template` both equal `woodmart`. Customization via theme options (Woodmart panel) and Elementor.

## Hooks / custom PHP architecture

No standalone custom plugin authored for core commerce logic detected. Customization is primarily:

- Woodmart theme options (`xts-theme_settings`, `xts_backups_auto` in options)
- Elementor templates
- `special-offer-woodmart` plugin (third-party/purchased widget)
- WooCommerce + Yoast standard hooks

See [16-custom-code.md](./16-custom-code.md).
