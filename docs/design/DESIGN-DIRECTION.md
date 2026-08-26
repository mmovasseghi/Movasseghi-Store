# Design Direction — Movasseghi Store

**Status:** APPROVED for implementation (2026-08-26)  
**SSOT for tokens/components:** [`../DESIGN-SYSTEM.md`](../DESIGN-SYSTEM.md)

---

## Identity statement

Movasseghi Store is **natural + premium + trustworthy + commercial + modern** — not a generic eco template, not a shadcn demo, not a Kairo clone.

Visual evolution of **ایریک/موثقی legacy** (green `#428D42` lineage → refined `#2D6A4F`), with **real product photos**, **real legacy HTML**, and **bordered catalog grid** as signature commerce pattern.

---

## Composition principles

1. **Legacy first** — content, images, SEO, grid DNA, category-desc-before-grid.
2. **Reference second** — borrow primitives (shadcn), motion discipline (Velora), typography rhythm (Cruip), e-com affordances (Kairo).
3. **One motion language** — subtle reveal, card hover, sheet slide; reduced-motion safe.
4. **Mobile defines hierarchy** — sticky buy bar, drawer nav, cart sheet, 360px baseline.
5. **No placeholder commerce** — zero stock photos, zero shortened product copy.

---

## Screen direction

### Header
- Sticky, white, 1px border, light backdrop blur.
- Mobile: hamburger → drawer (existing `MobileNav`).
- Desktop: shop · B2B · about · contact.
- Cart: **opens Sheet** (quick) + link to full cart.
- Search: compact on shop; header link/icon → shop search.

### Hero (home)
- Legacy `hero.png` background, ink gradient left-to-right (RTL).
- H1: primary SEO keyword (ظروف یکبار مصرف گیاهی آملون).
- CTAs: primary → main category; secondary B2B; tertiary phone.

### Product grid
- **Bordered unified grid** (legacy `products_bordered_grid`) — not isolated floating cards.
- Hover: subtle shadow + image scale (motion-safe).
- Show pack size badge when `attributes.packSize` set.
- Show «عمده» hint when wholesale price exists.

### Category
- Homepage: horizontal scroll **CategoryCard** with name + count.
- Category page: H1 + legacy description + child chips + grid.

### Product page
- Gallery (swipe, thumbs) → title/price/actions → specs accordion → **full legacy HTML** → related grid.
- Mobile sticky bar for add-to-cart (existing).

### Cart
- **Sheet** from start edge (RTL) for quick edit.
- Full `/cart` for review; large touch steppers.

### Checkout
- Single column mobile; phone order default.
- Future: PersianLabs mobile + city copy-paste (no npm package).

### B2B
- Cruip-style editorial blocks + MOQ/wholesale callouts on products.
- CTA band on homepage (existing).

### Admin
- Payload admin for catalog/orders; custom ops UI deferred.

---

## Anti-patterns (explicit reject)

- Excessive leaf/eco clip-art
- Glassmorphism cards everywhere
- Rounded-3xl SaaS product cards breaking grid
- Gradient heroes without product context
- Shortened AI product descriptions
- Importing Velora/Kairo/EasyUI as dependencies
- Desktop-first shrink

---

## Implementation phases

| Phase | Scope | Status |
|---|---|---|
| A | Design docs + tokens + UI primitives | This commit |
| B | Cart Sheet, enhanced ProductCard, CategoryCard | This commit |
| C | PersianLabs checkout fields | When checkout hardens |
| D | Admin ops dashboard | Post-launch |
| E | Image WebP derivatives | Infra |

---

## Quality bar (definition of done)

Design is **not** complete until:

- [ ] Visually clearly above legacy Woodmart
- [ ] Legacy grid + content preserved
- [ ] Mobile cart/checkout polished
- [ ] Motion coherent + reduced-motion
- [ ] DESIGN-SYSTEM.md matches code tokens
- [ ] No template collage appearance
