# RESPONSIVE-AUDIT — Legacy Breakpoint Behavior

**Status:** PARTIAL — theme CSS analyzed; live mobile verification UNKNOWN (site HTTP 500)

---

## Legacy approach

Woodmart is **desktop-first** with responsive CSS overrides. RTL handled via `style-rtl.css`.

| Breakpoint area | Legacy behavior (INFERRED from CSS) |
|---|---|
| `< 768px` | Mobile header (`wd-header-mobile-nav`), stacked product grid |
| `768–1024px` | Tablet — often 2-column product grid |
| `> 1024px` | Full nav, multi-column grid (3-4 cols) |
| Sticky header | `.whb-clone` duplicate header on scroll |

---

## Mobile requirements (new platform — HARD)

| Screen | Must excel |
|---|---|
| Homepage | Hero, categories, CTA, phone |
| Shop | 2-col grid, filter sheet, sort |
| Product | Gallery swipe, sticky add-to-cart/call |
| Cart | Full-width line items, checkout CTA |
| Checkout | Single-column steps |
| B2B | Form + phone |

---

## Legacy mobile gaps

| Issue | Evidence | Fix |
|---|---|---|
| Hover-dependent product info | `base_hover_content: excerpt` | Show excerpt always on touch |
| Qty on product grid | Theme option on | Hide on `< md` |
| Small touch targets | Woodmart defaults | Min 44px per project rules |
| Heavy JS | Elementor + Woodmart | Next.js SSR + minimal client JS |

---

## Product grid responsive (target)

| Viewport | Columns |
|---|---:|
| Mobile | 2 |
| Tablet | 3 |
| Desktop | 4 |
| Wide | 4-5 |

Preserve **bordered grid** at all breakpoints.

---

## Typography scale (new — mobile-first)

See `docs/DESIGN-SYSTEM.md`:

- Body: 16px mobile (up from legacy 14px — readability)
- Product title: 18px mobile / 20px desktop
- Price: prominent tabular nums

---

## Testing checklist

- [ ] iPhone SE viewport — shop + product + cart
- [ ] Android Chrome — RTL layout
- [ ] Tablet landscape — grid + gallery
- [ ] `prefers-reduced-motion` — no essential info in motion only

---

## Related

- `docs/audit/VISUAL-DESIGN-AUDIT.md`
- `.cursor/rules/50-mobile.mdc`
