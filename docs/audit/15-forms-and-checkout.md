# 15 — Forms and Checkout

**Confidence:** PARTIAL — pages exist; runtime behavior UNKNOWN (live site 500)

## WooCommerce flow (expected)

```
Shop → Product → Add to Cart → Cart (/cart/) → Checkout (/checkout/) → Thank you
                              ↘ My Account (/my-account/)
```

System pages confirmed in DB:

| Page | ID | Slug |
|---|---:|---|
| Shop | 7 | `shop` |
| Cart | 8 | `cart` |
| Checkout | 9 | `checkout` |
| My Account | 10 | `my-account` |

## Checkout configuration (database)

| Setting | Finding |
|---|---|
| Payment gateways | Only PayPal + MaxMind settings keys — **no Iranian gateway** |
| BACS / COD / Cheque | Option keys **not found** |
| Store address | **Empty** in WooCommerce settings |
| Default country | IR:THR (Tehran) |
| Currency | IRT, 0 decimals |
| Guest checkout | UNKNOWN |
| Account required | UNKNOWN |

## Forms detected

| Form type | Evidence | Status |
|---|---|---|
| WooCommerce checkout | Pages + WC | Expected |
| WooCommerce registration | `users_can_register=1` | Enabled — abused |
| Contact form | `/تماس-با-ما/` page exists | Content not parsed |
| B2B / quote request | NOT FOUND | Spec requirement for new site |
| Organizational invoice | NOT FOUND | Spec requirement |
| Mailchimp signup | Plugin present | Optional |

## Shipping

| Method | Legacy evidence |
|---|---|
| Flat rate | UNKNOWN |
| Free shipping | UNKNOWN |
| Local pickup | UNKNOWN |
| Customer-arranged vehicle | NOT FOUND — spec requirement |
| Seller-arranged transport | NOT FOUND — spec requirement |

**No shipping zone data extracted in Phase 1.**

## Payment behavior (business spec vs legacy)

| Method | Legacy | Target (spec) |
|---|---|---|
| Online gateway | NOT FOUND | NextPay (primary) |
| Card-to-card | NOT FOUND | Required |
| Phone order | NOT FOUND | Required (09125199105) |
| Organizational invoice | NOT FOUND | Required |

## Cart features (legacy theme)

Woodmart enables:

- Quantity on product grid
- AJAX add to cart (theme)
- Wishlist (theme)

Server-side cart validation depth: **UNKNOWN** — assume standard WooCommerce.

## Security notes

- Open registration enabled — likely not intended for B2B store
- Compromised site — checkout may have been modified — **do not trust legacy checkout code**

## New platform requirements (from spec)

- Server-authoritative pricing/stock/totals
- Payment state machine
- Multiple conversion paths (online pay, card-to-card, phone, B2B)
- Mobile sticky checkout CTA

## Verification checklist (blocked)

- [ ] Enabled payment methods at checkout
- [ ] Shipping options shown
- [ ] Coupon/discount behavior
- [ ] Order confirmation email
- [ ] Failed payment handling
- [ ] Mobile checkout UX
