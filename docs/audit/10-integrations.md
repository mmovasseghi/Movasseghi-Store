# 10 — Integrations

**Confidence:** CONFIRMED (installed) / UNKNOWN (runtime credentials)

## Detected integrations

| Service | Plugin / config | Status | Notes |
|---|---|---|---|
| **Yoast SEO Premium** | wordpress-seo-premium | Active | Primary SEO engine |
| **Google Search Console** | Yoast / WP | UNKNOWN | No verification token audited |
| **PayPal** | WooCommerce | Config key only | Likely unused for IR market |
| **MaxMind GeoIP** | WooCommerce | Config present | Geolocation |
| **Mailchimp** | mailchimp-for-wp | Installed | List building |
| **Site Mailer** | site-mailer | Installed | Transactional/reviews module |
| **Wordfence** | wordfence | Active | WAF, alert email: `myteb_str@yahoo.com` |
| **Flying Press** | flying-press | Installed | Page cache |
| **Elementor** | elementor | Active | Page builder |
| **Revolution Slider** | DB tables | Residual | Templates in options |
| **Instagram** | Woodmart theme | **Not configured** | Empty token |
| **Google Maps** | Woodmart theme | **Not configured** | Empty API key |

## Payment gateways (Iran)

| Provider | Legacy evidence | New platform (spec) |
|---|---|---|
| NextPay | NOT FOUND | Primary candidate |
| ZarinPal | NOT FOUND | Secondary candidate |
| BalePay | NOT FOUND | Optional |
| Card-to-card (manual) | NOT FOUND | Required by spec |
| COD (cash on delivery) | NOT FOUND in options | Common in IR — verify |

## Analytics

| Service | Legacy | New platform |
|---|---|---|
| Google Analytics 4 | NOT FOUND in audit pass | Required by spec |
| Google Merchant Center | NOT FOUND | Required by spec |
| Search Console | UNKNOWN historical | Connect before indexing |

## Email

| Channel | Config |
|---|---|
| WordPress mail | Placeholder `mail.example.com` in options |
| Admin email | `info@localhostt.ir` |
| Wordfence alerts | `myteb_str@yahoo.com` |

## CDN / external assets

| Asset | Source |
|---|---|
| Swiper JS | cdnjs (special-offer-woodmart plugin) |

## Hosting / infra (legacy)

| Item | Value |
|---|---|
| Provider | nocmdp.com / server83 |
| IP | 37.156.144.83 |
| Panel | cPanel + JetBackup |
| SSL | ayrik-cornstarch.com certificate in backup |

## Hosting (target — new)

| Item | Value |
|---|---|
| Server | Hetzner 91.107.181.79 |
| Deploy | GitHub → CI → staging → production |

## Integration migration policy

1. **Never migrate** Wordfence/FlyingPress credentials
2. **Re-create** payment integrations from official provider docs
3. **Re-connect** GSC/GA4/GMC on new domain
4. **Rotate** all third-party API keys
5. Do not commit secrets extracted from legacy wp-config

## Unknowns

- Active SMS provider (if any)
- WhatsApp / phone CRM integration
- Accounting / invoice ERP hookup
- Live chat widget
