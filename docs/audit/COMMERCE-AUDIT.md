# COMMERCE-AUDIT — WooCommerce Behavior

**Canonical detail:** [15-forms-and-checkout.md](./15-forms-and-checkout.md) · [17-data-model.md](./17-data-model.md)

## Catalog (CONFIRMED)

- 95 simple products
- Hierarchical categories (22)
- Featured image + gallery on subset
- Prices mostly 0 in DB → "تماس برای قیمت" UX

## Cart / checkout (INFERRED)

- Standard WooCommerce cart/checkout pages exist
- Guest checkout: likely enabled — VERIFY on restore
- Orders in backup: minimal / empty — not behavior-tested

## Payment (NOT FOUND)

- No NextPay/ZarinPal plugin in backup
- PayPal keys present — likely unused for IR market

## New platform architecture

```
Cart → Checkout → Order → PaymentService → Provider callback → verified status
```

See `src/commerce/` stubs.

## B2B

- Copy targets restaurants/catering in product descriptions
- Dedicated B2B page + quote flow — **new implementation**
