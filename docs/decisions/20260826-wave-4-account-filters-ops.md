# Wave 4 — Account, shop filters, admin ops

**Date:** 2026-08-26

## Decisions

### Customer account without public registration

- **Policy:** AGENTS.md + PAGE-DNA — no open registration (legacy had 7,779 spam accounts).
- **Decision:** Phone + order-number verification → signed httpOnly cookie (30 days) → list all orders for that phone.
- **Routes:** `/account/login`, `/account`, `/account/orders/[orderNumber]`
- **API:** `POST /api/account/login|logout`, `GET /api/account/orders`

### Shop attribute filters

- **Decision:** Client-side filter on published catalog (~95 SKUs) for `material` and `packSize` query params.
- **UI:** Extended `ShopToolbar` with selects; options derived via `extractFilterOptions()`.

### Admin ops dashboard

- **Decision:** Payload `beforeDashboard` component showing pending orders, new quotes, published product count.
- **API:** `GET /api/admin/ops-summary` (Payload auth required).

## Shipped

| Feature | Path |
|---|---|
| Account login | `/account/login`, `POST /api/account/login` |
| Order history | `/account`, `GET /api/account/orders` |
| Order detail | `/account/orders/[orderNumber]` |
| Shop filters | `/shop?material=&pack=` |
| Ops dashboard | Payload admin home |
| Deploy fix | `.gitattributes` for `*.sh` LF |

## Next

Phase 7 payments (blocked on merchant credentials), shop capacity/heat filters if data populated, account quotes view for B2B users.
