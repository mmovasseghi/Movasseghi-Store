# Design System — فروشگاه موثقی

**Status:** Single source of truth (SSOT) · v2.0 · 2026-08-26  
**Direction:** [`design/DESIGN-DIRECTION.md`](design/DESIGN-DIRECTION.md)  
**Reference analysis:** [`design/REFERENCE-MATRIX.md`](design/REFERENCE-MATRIX.md)

---

## Brand personality

NATURAL · PREMIUM · TRUSTWORTHY · COMMERCIAL · MODERN

Evolution of legacy Woodmart green (`#428D42`) — warmer, deeper, more premium. **Not** generic eco cliché.

---

## Color tokens

Implemented in `src/app/(frontend)/globals.css` via `@theme`.

| Token | Hex | Usage |
|---|---|---|
| `brand-green` | `#2D6A4F` | Primary buttons, links, sale accent |
| `brand-green-light` | `#40916C` | Hover, active |
| `brand-aqua` | `#95D5B2` | Badges, highlights |
| `brand-aqua-pale` | `#D8F3DC` | Image wells, soft sections |
| `brand-off-white` | `#F8FAF9` | Page background |
| `brand-ink` | `#1B4332` | Headings, hero overlay |
| `brand-muted` | `#52796F` | Body secondary |
| `border` | `#E2E8E6` | Grid lines, inputs |

### Semantic

| Role | Color |
|---|---|
| success | `brand-green-light` |
| warning | `#E9C46A` |
| error | `#E76F51` |
| info | `brand-aqua` |

### Contrast

- Text on white: `brand-ink` / `brand-muted` — WCAG AA+
- Text on `brand-green`: white only
- Focus ring: 2px `brand-green` at 40% opacity

---

## Typography

| Role | Family | Size (mobile → desktop) | Weight |
|---|---|---|---|
| Display / H1 | Vazirmatn | 1.875rem → 3rem | 800 |
| H2 | Vazirmatn | 1.25rem → 1.5rem | 700 |
| H3 | Vazirmatn | 1rem → 1.125rem | 600 |
| Body | Vazirmatn | 1rem (16px min) | 400 |
| Small / meta | Vazirmatn | 0.875rem | 400 |
| Price | Vazirmatn | 0.875rem → 1rem | 700, `tabular-nums` |
| SKU / code | system-ui | 0.8125rem | 400 |

**Line height:** body 1.625 · headings 1.25  
**Legacy HTML:** `.legacy-product-content` in globals.css

---

## Spacing scale

Base unit **4px**. Tailwind scale: 1, 2, 3, 4, 5, 6, 8, 10, 12, 16, 20, 24.

| Context | Mobile | Desktop |
|---|---|---|
| Page horizontal padding | 16px (`px-4`) | 16px |
| Section vertical | 48px (`py-12`) | 48–64px |
| Card internal | 12px (`p-3`) | 16px (`p-4`) |
| Grid gap | 0 (bordered grid) | 0 |

Max content width: **72rem** (`max-w-6xl`) — legacy ~1220px equivalent.

---

## Radius

| Token | Value | Use |
|---|---|---|
| `rounded-lg` | 10px | Inputs, buttons |
| `rounded-xl` | 16px | Cards, sheets, CTAs |
| `rounded-full` | 9999px | Chips, badges |

Avoid `rounded-3xl` on product surfaces — breaks commercial grid aesthetic.

---

## Shadows & borders

- **Product grid:** 1px `border-border` between cells — **signature pattern**
- **Elevated:** `shadow-sm` default · `shadow-md` on hover (cards, sheet)
- No heavy drop shadows on catalog grid cells

---

## Components

### Button (`components/ui/Button.tsx`)

| Variant | Style |
|---|---|
| `primary` | `bg-brand-green text-white hover:bg-brand-green-light` |
| `secondary` | `border border-border bg-white hover:border-brand-green` |
| `ghost` | `text-brand-muted hover:text-brand-green hover:bg-brand-aqua-pale` |
| `inverse` | White on hero/dark bands |

Min height **44px** on mobile. Full width optional.

### Badge (`components/ui/Badge.tsx`)

| Variant | Use |
|---|---|
| `default` | Pack size, material |
| `sale` | Discount |
| `b2b` | Wholesale available |
| `stock` | Low/out of stock |

### Sheet (`components/ui/Sheet.tsx`)

- Slide from **start** (RTL: right edge visually)
- Backdrop `brand-ink/40`
- Used for **cart** quick view
- Focus trap via focus first element; Escape closes

### ProductCard (`components/shop/ProductCard.tsx`)

- Variant `grid` (bordered) · `card` (standalone)
- Image: aspect-square, `object-contain`, pale well
- Title: 2-line clamp
- Price: tabular, discount strikethrough
- Optional: pack badge, wholesale hint

### CategoryCard (`components/shop/CategoryCard.tsx`)

- Horizontal scroll on mobile
- Name + product count + chevron

### Navigation

- **Header:** sticky, logo, nav, cart trigger
- **MobileNav:** full-height drawer, 48px touch rows
- **Breadcrumbs:** product + category pages

### Forms (checkout)

- Label above field (Persian UX)
- Error below field
- Future: PersianLabs mobile/city copy-paste

### Legacy content

- `LegacyProductContent` — trusted HTML, scripts stripped

---

## Motion

CSS variables in globals:

| Token | Value |
|---|---|
| `--motion-fast` | 150ms |
| `--motion-base` | 250ms |
| `--motion-slow` | 400ms |
| `--motion-ease` | cubic-bezier(0.4, 0, 0.2, 1) |

Utilities: `.motion-reveal`, `.motion-safe-hover` — disabled under `prefers-reduced-motion`.

**Allowed:** fade-up sections, image scale on card hover, sheet slide, cart badge pulse once on add.

**Forbidden:** parallax, particles, infinite loops on LCP elements.

---

## Breakpoints

| Name | Min width | Design note |
|---|---|---|
| default | 0 | **Design here first** (360px) |
| `sm` | 640px | 2-col grid ok |
| `md` | 768px | Desktop nav visible |
| `lg` | 1024px | 3–4 col product grid |
| `xl` | 1280px | max-w-6xl centered |

---

## RTL rules

- `html[dir=rtl]` — project default
- Logical properties: `ms-`, `me-`, `ps-`, `pe-`, `start`, `end`
- Chevrons in nav flip meaning (use ←/→ appropriately in Persian UI copy)
- Sheet opens from `start` side
- Numbers/prices: `tabular-nums` + `fa-IR` locale

---

## Accessibility

- Touch targets ≥ **44×44px**
- Visible focus: `focus-visible:ring-2 focus-visible:ring-brand-green/40`
- Images: meaningful `alt` from product name / media sync
- Reduced motion: `@media (prefers-reduced-motion: reduce)`
- Sheet: `role=dialog`, `aria-modal`, labelled title

---

## Commerce patterns (legacy preserved)

| Pattern | Implementation |
|---|---|
| Bordered product grid | Shop, category, homepage featured |
| Category description before grid | `/shop/[category]` |
| Full product HTML | `legacyDescriptionHtml` |
| Phone order checkout | Default payment method |
| Trust badges | Footer enamad/samandehi |
| B2B MOQ/wholesale | Product attrs + `/b2b` |

---

## Dependencies policy

| Allowed | When |
|---|---|
| shadcn copy-paste | Dialog, Input, Select — as needed |
| PersianLabs copy-paste | Checkout Iranian fields only |
| **Forbidden** | npm install of Velora, Kairo, Easy UI, Cruip, nobruf templates |

---

## File map

```
src/app/(frontend)/globals.css   ← tokens + legacy HTML + motion
src/components/ui/               ← Button, Badge, Sheet
src/components/shop/             ← ProductCard, CategoryCard, CartSheet
src/components/layout/           ← Header, Footer, MobileNav
docs/design/                     ← REFERENCE-MATRIX, DESIGN-DIRECTION
docs/DESIGN-SYSTEM.md            ← this file (SSOT)
```
