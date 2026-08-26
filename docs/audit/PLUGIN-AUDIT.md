# PLUGIN-AUDIT — Legacy WordPress Plugins

**Canonical detail:** [03-theme-and-plugins.md](./03-theme-and-plugins.md)

## Active stack (CONFIRMED)

| Plugin | Role | Migrate? |
|---|---|---|
| WooCommerce | Catalog, cart | **Reimplement** in Payload + custom commerce |
| Elementor | Page builder | **Do not port** — extract content only |
| Yoast SEO Premium | SEO | **Data only** → Payload SEO fields |
| Woodmart Core | Theme features | **Do not port** |
| Flying Press | Cache | N/A — Next.js performance |
| Wordfence | Security | N/A |
| special-offer-woodmart | Carousel widget | **Redesign** natively |

## NOT FOUND

- Iranian payment gateway
- Wholesale / B2B pricing plugin

## Malware (CONFIRMED — never migrate)

- `HelloDollyV2_ixwz` — malicious
