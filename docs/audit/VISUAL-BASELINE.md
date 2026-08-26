# Visual Baseline — Legacy vs New

**Purpose:** Establish LEGACY VISUAL BASELINE before further modernization.  
**Not pixel-perfect clone** — structural + DNA fidelity checklist.

Legend: ✅ parity · 🟡 partial · ❌ missing · ⏳ planned

---

## Global chrome

| Element | Legacy (Woodmart) | New (Movasseghi) | Status |
|---|---|---|---|
| Logo | WHB logo assets | `/brand/logo.png` | ✅ |
| Sticky header | `.whb-clone` | `sticky top-0` + blur | ✅ |
| Desktop nav | Horizontal RTL menu | Header links | ✅ |
| Mobile nav | Woodmart mobile menu | `MobileNav` drawer | ✅ |
| Search | Header search | `HeaderSearch` + `/shop?q=` | 🟡 no autocomplete yet |
| Cart | Dropdown mini-cart | `CartSheet` | ✅ |
| Footer trust | Widget area | enamad + samandehi | ✅ |
| Primary green | `#428D42` | `#2D6A4F` (evolved) | ✅ intentional |
| Body font | IranYekan 14px | Vazirmatn 16px | ✅ upgraded |
| Container | ~1220px | max-w-6xl | ✅ |

---

## Homepage `/`

| Element | Legacy | New | Status |
|---|---|---|---|
| Hero image | picMain-1 | `/brand/hero.png` | ✅ |
| Commercial H1 | Elementor | ظروف یکبار مصرف گیاهی آملون | ✅ |
| Category discovery | Blocks/carousel | CategoryCard scroll | ✅ |
| Featured products | Carousel/grid | Bordered grid | ✅ |
| Special offers | special-offer plugin | Not yet | ⏳ |
| B2B CTA band | Partial | Green CTA section | ✅ |
| SEO keyword chips | N/A | Commercial hub pills | ✅ new |

---

## Shop `/shop`

| Element | Legacy | New | Status |
|---|---|---|---|
| Product grid | Bordered | Bordered | ✅ |
| Category chips | Sidebar/filters | Top chips | ✅ |
| AJAX filters | Woodmart | Category pages + search | 🟡 |
| Sort | AJAX | Name sort | 🟡 |
| Pagination | AJAX scroll | Limit 48 | 🟡 |

---

## Category

| Element | Legacy | New | Status |
|---|---|---|---|
| H1 | Term name | Category name | ✅ |
| Description before grid | Theme option | whitespace-pre-line block | ✅ |
| Child categories | Term children | Chip links | ✅ |
| Product grid | Bordered | Bordered | ✅ |

---

## Product

| Element | Legacy | New | Status |
|---|---|---|---|
| Gallery + zoom | Woodmart | `ProductGallery` | ✅ |
| Price IRT | WC | `formatIrt` | ✅ |
| Add to cart | WC form | `ProductActions` | ✅ |
| Mobile sticky CTA | Theme | `ProductStickyBar` | ✅ |
| Full description HTML | post_content | `legacyDescriptionHtml` | ✅ |
| Yoast meta | DB | Payload seo fields | ✅ |
| Schema | Yoast | `productJsonLd` | ✅ |
| Related products | Theme | Related grid | ✅ |
| Hover image swap | Theme | Single image | 🟡 |

---

## Cart / checkout

| Element | Legacy | New | Status |
|---|---|---|---|
| Mini cart | Dropdown | Sheet | ✅ |
| Full cart page | WC template | `/cart` | ✅ |
| Phone order | Spec | Default checkout | ✅ |
| Card-to-card | Spec | Checkout option | ✅ |
| Online payment | Not live | Stub + API scaffold | ⏳ |

---

## Content pages

| Page | Legacy | New | Status |
|---|---|---|---|
| About | CMS page | `/about` + sync | ✅ |
| Contact | CMS page | `/contact` | ✅ |
| Pricing | CMS page | `/pricing` | ✅ |
| B2B | Spec | `/b2b` | ✅ |
| Blog | `/mag/` spam + 1 legit | Sanitized | ✅ |

---

## Responsive (360px baseline)

| Check | Legacy | New | Status |
|---|---|---|---|
| Touch targets | Often <44px | min-h-11 enforced | ✅ improved |
| Mobile product grid | 2 col | 2 col | ✅ |
| Mobile checkout | WC default | Single column | ✅ |
| Drawer nav | Theme | Full-height sheet | ✅ |

---

## Visual QA process (ongoing)

1. Build page → 2. Mobile 360px screenshot → 3. Desktop 1280px → 4. Compare this table → 5. Fix gaps → 6. Repeat

**Next gaps to close:** special offers strip, shop filters/sort, product gallery hover image, search autocomplete.
