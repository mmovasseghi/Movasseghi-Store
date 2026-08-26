# Design Direction — Movasseghi Store

**Status:** v3.0 · 2026-08-26  
**SSOT tokens:** [`../DESIGN-SYSTEM.md`](../DESIGN-SYSTEM.md)  
**Pages:** [`PAGE-INVENTORY.md`](PAGE-INVENTORY.md)  
**Legacy reconstruction:** [`../audit/THEME-RECONSTRUCTION.md`](../audit/THEME-RECONSTRUCTION.md)

---

## Strategy stack (user-defined)

```
LEGACY WORDPRESS (Woodmart + WC + Elementor)
        → LEGACY DNA MODEL
        → + Zhaket/RTL market patterns
        → + Open Source primitives (shadcn, Payload, Vercel patterns)
        → MOVASSEGHI DESIGN SYSTEM
        → Storefront · Admin · Content · Commerce
```

**Never:** clone ShopKadeh, Molla, shadcn demo, or generic AI ecommerce.

---

## Reconstruction-first pipeline

| Phase | Action | Status |
|---|---|---|
| **A** | Theme forensics (Woodmart settings, CSS, templates, pipeline) | ✅ `docs/audit/THEME-*` |
| **B** | Visual baseline vs new storefront | ✅ `VISUAL-BASELINE.md` |
| **C** | Token mapping Legacy → Design System | ✅ `DESIGN-SYSTEM.md` |
| **D** | Modernize (mobile, premium, performance) | 🔄 Ongoing |

---

## Identity statement

Movasseghi Store = **same brand recognition as legacy ایریک/موثقی**, implemented in modern code, then made **more beautiful, mobile-first, and performant**.

Visual anchors:

- Real legacy hero + product photos
- Bordered catalog grid (Woodmart signature)
- Green commercial palette (evolved from `#428D42`)
- Full product HTML preserved
- Phone + B2B + trust badges

---

## Composition principles

1. **Legacy first** — reconstruct DNA before inventing layouts
2. **Market second** — borrow proven Persian ecommerce UX (drawer cart, mobile nav)
3. **OSS third** — shadcn/Payload for implementation quality
4. **One motion language** — subtle, reduced-motion safe
5. **Mobile defines hierarchy** — 360px baseline
6. **No placeholder commerce** — real content/images only

---

## Screen direction (summary)

| Screen | Direction |
|---|---|
| Header | Sticky, logo, desktop search, cart sheet trigger |
| Home | Legacy hero + SEO chips + categories + bordered featured grid |
| Shop | Search, category chips, bordered grid |
| Category | H1 + legacy description + child chips + grid |
| Product | Gallery, actions, specs, **full legacy HTML**, sticky mobile bar |
| Cart | Sheet + full page, server validation |
| Checkout | Phone default, card-to-card, online when credentialed |
| B2B | Editorial + phone + quote path (expand) |
| Admin | Payload native → custom ops UI later |

Detail per area: [`REFERENCE-MATRIX.md`](REFERENCE-MATRIX.md)

---

## Anti-patterns

- Generic Tailwind/shadcn starter as visual foundation
- Floating SaaS product cards breaking grid
- Shortened product copy
- Stock/AI product images
- Elementor port to React
- Installing reference themes as npm deps

---

## Quality gate

A page ships when:

- [ ] Matches legacy DNA checklist in `VISUAL-BASELINE.md`
- [ ] Mobile 360px intentionally designed
- [ ] Real product content where applicable
- [ ] Performance budget respected
- [ ] Recognizable as **Movasseghi**, not a template

---

## Next implementation targets

1. Custom 404 + error states
2. Shop filters/sort (Woodmart AJAX parity)
3. Special offers / featured strip (legacy plugin equivalent)
4. Account + order tracking (Woostify/Web Store patterns)
5. B2B quote form
