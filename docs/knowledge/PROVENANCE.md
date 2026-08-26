# Provenance Map

Tracks evidence source for knowledge base claims.

| Claim | Value | Source file/URL | ID | Confidence | Verified |
|---|---|---|---|---|---|
| Legacy domain | ayrik-cornstarch.com | `wp_options` in site17896548586.sql | — | CONFIRMED | DB |
| Legacy brand | ایریک پلاستیک ایرانیان | `wp_options.blogname` | — | CONFIRMED | DB |
| Published products | 95 | Yoast indexables parse | — | CONFIRMED | Script |
| Primary category | ظروف یکبار مصرف آملون (84 products) | `wp_term_taxonomy` | term 22 | CONFIRMED | Script |
| Material | Corn starch / نشاسته ذرت | Category descriptions | term 22-24 | CONFIRMED | DB |
| Currency IRT | Toman | `woocommerce_currency` | — | CONFIRMED | DB |
| WordPress version | 6.8.3 | wp-includes/version.php | — | CONFIRMED | FS |
| WooCommerce version | 9.4.2 | woocommerce.php | — | CONFIRMED | FS |
| Woodmart version | 7.0.4 | style.css | — | CONFIRMED | FS |
| Admin user Movasseghi | Mohammad Sina Movasseghi Nezhad | wp_users | user 3 | CONFIRMED | DB |
| Malware plugin | HelloDollyV2_ixwz | wp-content/plugins/ | — | CONFIRMED | FS + wf |
| Spam users | 7,779 | audit script heuristic | — | CONFIRMED | Script |
| Target brand موثقی | فروشگاه موثقی | Project spec | — | SPEC | N/A |
| Phone 09125199105 | Business contact | Project spec | — | SPEC | NOT in legacy |
| Instagram movasseghiStore | Social | Project spec | — | SPEC | NOT in legacy |
| Heat resistance 140°C | Example product claim | Yoast indexable product 268 | — | CONFIRMED in meta | SOURCE_REQUIRED product spec |
| 18-month guarantee | Some product meta | Yoast descriptions | — | CONFIRMED in copy | SOURCE_REQUIRED |
| Wholesale pricing | — | — | — | NOT FOUND | — |
| NextPay integration | — | — | — | SPEC | — |

## Verification status key

- **CONFIRMED** — direct legacy evidence
- **SPEC** — project specification (not legacy)
- **SOURCE_REQUIRED** — present in content but needs manufacturer/business verification
- **NOT FOUND** — no evidence

## Update protocol

When adding knowledge:

1. Add row to this table
2. Set confidence honestly
3. Never cite AI interpretation as primary evidence
