# Design System — فروشگاه موثقی

**Status:** Gate 3 baseline (2026-08-26)

## Brand

| Token | Value | Usage |
|---|---|---|
| `--brand-green` | `#2D6A4F` | Primary actions, accents |
| `--brand-green-light` | `#40916C` | Hover, links |
| `--brand-aqua` | `#95D5B2` | Soft backgrounds, badges |
| `--brand-aqua-pale` | `#D8F3DC` | Section backgrounds |
| `--brand-white` | `#FFFFFF` | Surfaces |
| `--brand-off-white` | `#F8FAF9` | Page background |
| `--brand-ink` | `#1B4332` | Headings |
| `--brand-muted` | `#52796F` | Secondary text |

## Semantic

| Token | Light |
|---|---|
| success | `#40916C` |
| warning | `#E9C46A` |
| error | `#E76F51` |
| border | `#E2E8E6` |

## Typography

| Role | Font | Notes |
|---|---|---|
| Persian body | `Vazirmatn` | Google Fonts, variable |
| Persian display | `Vazirmatn` | weight 700-800 |
| Latin fallback | `system-ui` | SKUs, codes |
| Base size mobile | `16px` | min for readability |
| Scale | 1.125 modular | mobile-first |

## Spacing

Base unit `4px`. Scale: 1, 2, 3, 4, 6, 8, 12, 16, 24, 32.

## Radius

| Token | Value |
|---|---|
| sm | 6px |
| md | 10px |
| lg | 16px |
| full | 9999px |

## Shadows

Subtle only — `shadow-sm`, `shadow-md` on cards/modals. No heavy drop shadows.

## Components (shadcn base)

- Button: primary green, secondary outline, ghost
- Card: white, border, rounded-lg
- Input: RTL, label above, error below
- Sheet: mobile cart, filters
- Dialog: quick view, B2B quote

## RTL rules

- `dir="rtl"` on `<html>`
- Logical properties: `ms-`, `me-`, `ps-`, `pe-`
- Icons that imply direction must flip (chevrons)
- Numbers/prices: `font-variant-numeric: tabular-nums`

## Motion

- Duration: 150ms (micro), 250ms (standard), 400ms (hero)
- Easing: `cubic-bezier(0.4, 0, 0.2, 1)`
- Respect `prefers-reduced-motion`
- No parallax, no particle systems

## Breakpoints

| Name | Min |
|---|---|
| sm | 640px |
| md | 768px |
| lg | 1024px |
| xl | 1280px |

Design mobile 360px first.

## Accessibility

- WCAG AA contrast minimum
- Focus ring: 2px `--brand-green`
- Touch targets: min 44×44px

## Information architecture

```
/                     Homepage
/shop                 Catalog
/shop/[category]      Category
/product/[slug]       Product
/cart                 Cart
/checkout             Checkout
/account              Account
/b2b                  B2B / wholesale inquiry
/mag                  Blog (optional, low priority)
/about, /contact      Trust pages
```

Primary SEO hub: `/shop/ظروف-یکبار-مصرف-آملون` (slug from legacy)
