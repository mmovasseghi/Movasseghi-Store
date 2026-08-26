# UX-AUDIT — Legacy User Flows & Patterns

**Status:** COMPLETE (inferred from theme + WooCommerce) / PARTIAL (live behavior)

---

## Primary user journeys

### Retail buyer (B2C)

```
Search / category browse → product page → add to cart → checkout → payment
                                    ↘ phone order (09125199105)
```

### Business buyer (B2B)

```
Category (فله) → product specs → phone / quote request → invoice
```

---

## Navigation model (CONFIRMED)

| Pattern | Legacy | Assessment |
|---|---|---|
| Top nav | Horizontal categories + pages | Works desktop; mobile needs drawer |
| Category tree | 3 levels (آملون → فله/شیرینک → product type) | **Strong** — preserve IA |
| Breadcrumbs | WooCommerce + Yoast | **Keep** for SEO |
| Footer links | Theme widgets | Standard — rebuild with trust signals |
| Search | Header search | **Improve** — Persian normalization |

---

## Catalog UX (Woodmart)

| Feature | Setting | UX impact |
|---|---|---|
| AJAX shop | On | Faster filtering — good |
| Bordered grid | On | Visual separation — distinctive |
| Category desc before grid | On | SEO + context — **keep** |
| Hover excerpt | On | Desktop only — hide on touch |
| Qty on grid | On | Clutter on mobile — **disable mobile** |
| Quick view | Available | Redundant with good product page |

---

## Product page UX

| Element | Legacy | Recommendation |
|---|---|---|
| Gallery | Image + thumbs | Swipe gallery + zoom (implemented) |
| Price | IRT, "تماس" when 0 | Keep — most prices were 0 in DB |
| Long content | Below fold HTML | Accordion sections on mobile |
| Trust | Warranty/heat claims in copy | Preserve in content |
| CTA | Add to cart + implicit phone | Sticky mobile CTA bar |

---

## Cart & checkout UX

| Area | Legacy state | Gap |
|---|---|---|
| Cart | WooCommerce standard | Not verified live |
| Checkout | WooCommerce | No IR gateway found |
| Guest checkout | Likely enabled | Confirm before rebuild |
| Shipping | Tehran default | Extract zones — UNKNOWN |
| Phone order | Not formalized in UI | **Add prominent CTA** |

---

## Forms

| Form | Legacy | New |
|---|---|---|
| Contact | Contact page | Rebuild with validation |
| Registration | Open — abused | **Disable** public reg |
| Checkout | WC forms | Multi-step mobile-first |

---

## Conversion patterns worth keeping

1. Category descriptions set buying context before grid
2. Product pages are **long-form commercial** — not minimal
3. Green primary CTA color association with eco brand
4. Phone as fallback when price is "call for quote"

---

## UX weaknesses to fix

1. Mobile filter experience (Woodmart drawer — UNKNOWN quality)
2. Registration spam vector
3. Elementor page weight
4. Inconsistent sticky elements on mobile
5. No dedicated B2B quote flow

---

## Related

- `docs/audit/PAGE-DNA.md`
- `docs/audit/RESPONSIVE-AUDIT.md`
- `docs/competition/ux-gaps.md`
