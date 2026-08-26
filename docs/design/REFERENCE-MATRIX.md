# Reference Matrix — Movasseghi Store UI

**Purpose:** Map every major UI area to **Legacy (primary)**, **Zhaket/RTL market**, **Open Source**, and **final implementation**.  
**Rule:** References are **not** dependencies. Legacy Woodmart DNA + real product data have **highest priority**.

**Related:** [`../competition/ZHAKET-RTL-PATTERNS.md`](../competition/ZHAKET-RTL-PATTERNS.md) · [`../audit/THEME-RECONSTRUCTION.md`](../audit/THEME-RECONSTRUCTION.md)

**Legend:** ★★★ excellent fit · ★★ good · ★ partial · — weak / skip

---

## Architecture references (patterns only)

| Repo | Role | Movasseghi use |
|---|---|---|
| **Legacy Woodmart** | Visual + UX DNA | **PRIMARY** |
| Vercel Commerce | Storefront/SEO/perf architecture | Route + metadata patterns |
| Payload 3 | CMS + admin + ecommerce template | **Stack in use** |
| shadcn/ui | Accessible primitives | Button, Sheet, future Input |
| PersianLabs/ui | RTL forms | Checkout fields (copy-paste) |
| Saleor storefront | Cart/checkout/account UX | Reference only — FSL, no embed |
| next-shadcn-dashboard | Admin ops UI | Future custom dashboard |

---

## Market theme references (Zhaket / RTL)

| Area | Legacy | Market ref | OSS | Final |
|---|---|---|---|---|
| Header / mega nav | WHB ★★★ | ShopKadeh, Sabad ★★ | shadcn ★★ | Sticky Header + drawer |
| Mobile UX | Woodmart ★★ | DigiRado, ShopKadeh ★★★ | — | MobileNav + touch 44px |
| Product card | bordered grid ★★★ | Woostify ★★ | shadcn badges ★★ | ProductCard grid |
| Product page | full HTML ★★★ | Molla ★★ | Vercel commerce ★ | Gallery + legacy HTML |
| Filters | AJAX shop ★★ | ShopKadeh ★★★ | — | Category + search (filters ⏳) |
| Cart | mini-cart ★★ | DigiRado ★★★ | shadcn Sheet ★★★ | CartSheet |
| Checkout | phone ★ | Web Store ★★ | PersianLabs ★★ | Multi-path form |
| Account | WC account ★★ | Woostify ★★★ | Saleor ref ★ | ⏳ Phase 3 |
| Blog | /mag ★★ | Ecommax ★★ | — | /mag sanitized |
| Search | header ★★ | Liora ajax ★★★ | — | persianSearch + HeaderSearch |
| B2B | copy ★★ | Web Store ★★ | Cruip ★★ | /b2b + wholesale badges |
| Admin | WP admin ★ | — | Payload ★★★ | Payload + future ops UI |

---

## Summary selections

| Area | Primary | Secondary | Rejected |
|---|---|---|---|
| Header | Legacy DNA + shadcn composability | Kairo cart affordance | Velora heavy glass |
| Hero | Legacy imagery + Cruip hierarchy | Kairo CTA | Easy UI generic gradients |
| Product card | **Legacy bordered grid** | shadcn card tokens | Kairo floating cards |
| Category | Kairo merchandising | Legacy chips | nobruf SaaS blocks |
| Product page | **Legacy full HTML** | shadcn tabs/accordion | Template shortened layouts |
| Cart | shadcn Sheet pattern | Kairo mini-cart density | Full-page-only cart |
| Checkout | PersianLabs (future inputs) | shadcn forms | Multi-step wizard clone |
| B2B | Custom + Cruip editorial | Kairo CTA bands | Admin template |
| Admin | Payload native + shadcn tables | — | Kairo storefront admin |
| Motion | Velora subtle reveal | Legacy minimal | Easy UI animation soup |
| RTL | **PersianLabs** | Native logical CSS | LTR-first templates |

---

## Header

| Ref | Strengths | Weaknesses | Mobile | RTL | E-com | Verdict |
|---|---|---|---|---|---|---|
| **Legacy** | Logo, shop, search, cart, sticky WHB | Cluttered builder, slow | ★★ | ★★★ | ★★★ | **Keep IA** |
| shadcn | Composable nav, a11y, focus rings | Generic look if unstyled | ★★★ | ★★ | ★★ | **Primitives** |
| PersianLabs | RTL nav, logical spacing | No storefront header | ★★★ | ★★★ | ★ | Checkout only |
| Velora | Premium sticky, blur | Over-animated | ★★ | ★★ | ★★ | Blur/backdrop only |
| Kairo | Search + cart prominence | Template-specific | ★★★ | ★★ | ★★★ | Cart badge pattern |
| Easy UI | Variations | Inconsistent | ★★ | ★ | ★ | Skip |
| Cruip | Clean spacing | Marketing not shop | ★★ | ★ | ★ | Typography rhythm |
| nobruf | Responsive nav | Landing not catalog | ★★★ | ★★ | ★ | Mobile drawer ref |

**Selected:** Sticky white header, logo start, desktop inline nav, mobile drawer (existing), **cart opens Sheet**, search routes to `/shop?q=`.

---

## Hero

| Ref | Strengths | Weaknesses | Mobile | Animation | Verdict |
|---|---|---|---|---|---|
| **Legacy** | Real `picMain-1`, commercial H1 | Elementor bloat UNKNOWN layout | ★★ | ★ | **Real hero image** |
| Velora | Section reveal, depth | Risk LCP | ★★ | ★★★ | Subtle gradient overlay only |
| Kairo | Product-forward hero, dual CTA | Generic stock | ★★★ | ★★ | CTA grouping |
| Cruip | Proportion, headline scale | Not product-specific | ★★★ | ★ | Type scale |
| Easy UI | Many hero variants | Template noise | ★★ | ★★ | Skip |
| nobruf | Benefit bullets | SaaS tone | ★★★ | ★ | Trust grid ref |

**Selected:** Full-bleed **legacy hero** + ink gradient (Cruip hierarchy) + 3 CTAs (shop, B2B, phone). No parallax.

---

## Product card

| Ref | Strengths | Weaknesses | Mobile | Verdict |
|---|---|---|---|---|
| **Legacy** | Bordered grid, price, hover info | Dated typography | ★★★ | **Keep grid** |
| shadcn | Card, badge, button variants | Default rounded SaaS | ★★★ | Badges/buttons |
| Kairo | Sale badges, quick add | Breaks bordered grid | ★★★ | Badge language |
| Velora | Hover lift | CLS risk | ★★ | Light shadow on hover |
| Cruip | Price typography | — | ★★★ | Tabular nums |

**Selected:** Bordered grid cell, square image, 2-line title, price + optional pack/MOQ badge, wholesale hint, touch ≥44px.

---

## Category

| Ref | Strengths | Weaknesses | Verdict |
|---|---|---|---|
| **Legacy** | Chips + desc before grid | No rich cards | Chips on shop |
| Kairo | Category tiles with counts | Needs real images | **CategoryCard** with count |
| nobruf | Icon benefits | Not e-com | Skip |

**Selected:** Homepage **CategoryCard** row + shop chip bar; category page keeps SEO description before grid (legacy).

---

## Product page

| Ref | Strengths | Weaknesses | Verdict |
|---|---|---|---|
| **Legacy** | Full HTML SEO, gallery, specs | Tab overload | **legacyDescriptionHtml verbatim** |
| shadcn | Accordion specs, dialogs | — | Specs accordion mobile |
| Kairo | Gallery + sticky buy box | — | Sticky bar (done) |
| Templates | Polished layout | Fake content | **Never shorten copy** |

**Selected:** Gallery + actions + specs + **full legacy HTML** + related grid. Non-negotiable.

---

## Cart & checkout

| Ref | Strengths | Weaknesses | Verdict |
|---|---|---|---|
| shadcn | Sheet, form fields | — | **Cart Sheet** |
| Kairo | Mini-cart list density | — | Line item layout |
| PersianLabs | Mobile number, city | Not installed yet | Phase: checkout fields |
| Legacy | Phone order, card-to-card | No gateway | **Preserve flows** |

**Selected:** Sheet cart (mobile-first) + full `/cart` page; checkout keeps phone/card/online stub.

---

## B2B

| Ref | Strengths | Verdict |
|---|---|---|
| Cruip | Editorial sections, CTA bands | Page structure |
| Legacy | MOQ/wholesale in copy | Show wholesale on card when set |
| Kairo | Commercial CTAs | CTA band on homepage |

**Selected:** Dedicated `/b2b` + wholesale price on product card when `b2b.wholesalePrice` exists.

---

## Admin

| Ref | Verdict |
|---|---|
| Payload CMS | **Use native admin** — custom ops UI later |
| shadcn | Tables/filters for future custom dashboards |
| Store templates | **Do not copy** |

---

## Motion

| Ref | Pattern | Adopt? |
|---|---|---|
| Velora | Section fade-up | Yes — CSS only, `motion-safe` |
| Legacy | Minimal | Default baseline |
| Easy UI | Many presets | No |
| a11y | `prefers-reduced-motion` | **Required** |

**Coherent system:** 150ms micro, 250ms standard, 400ms hero; no parallax/particles.

---

## RTL & Persian UX

| Ref | Use |
|---|---|
| **PersianLabs** | Jalali, mobile input, city — install **copy-paste at checkout** when needed |
| Project | `dir=rtl`, logical properties, Vazirmatn |
| Legacy | fa_IR proven |

---

## Performance & maintainability

- **Do not npm-install** reference repos.
- Copy-paste shadcn/PersianLabs **only** when a primitive is needed (Sheet, Input).
- Prefer existing Tailwind tokens in `globals.css`.
- One component per concern; extend `ProductCard` don't fork templates.
