# Analytics Plan

**Status:** Event layer implemented (`src/lib/analytics.ts`) — GTM/GA4 wiring pending production IDs.

---

## Standard ecommerce events

| Event | Trigger | Params |
|---|---|---|
| `view_item` | Product page load | item_id, item_name, price |
| `view_item_list` | Shop/category list | item_list_name |
| `select_item` | Product card click | item_id |
| `add_to_cart` | Add to cart button | item_id, quantity, value |
| `view_cart` | Cart sheet/page open | value, items |
| `begin_checkout` | Checkout page | value, items |
| `add_payment_info` | Payment method selected | payment_type |
| `purchase` | Order confirmed | transaction_id, value |

---

## Business events

| Event | Trigger |
|---|---|
| `phone_click` | tel: links (header, product, B2B) |
| `b2b_request` | B2B CTA clicks |
| `contact` | Contact page engagement |

---

## Implementation

- Push to `window.dataLayer[]` (GTM-compatible)
- No PII in event payloads
- `NEXT_PUBLIC_GA_ID` optional future — do not block on ID

---

## Validation

- Manual: DevTools → `dataLayer`
- E2E: optional dataLayer spy in Playwright (phase 2)
