# 09 — Features

Feature map derived from theme, plugins, database, and filesystem.

| Feature | Legacy status | Evidence | New platform priority |
|---|---|---|---|
| Product catalog | ✅ Active | 95 published products | P0 |
| Category hierarchy | ✅ Active | 22 categories | P0 |
| Add to cart | ✅ Expected | WooCommerce pages | P0 |
| Checkout | ✅ Expected | `/checkout/` page | P0 |
| Customer accounts | ✅ Expected | `/my-account/`, registration enabled | P1 |
| Guest checkout | UNKNOWN | Not verified | P1 |
| Online payment gateway | ❓ Minimal DB config | Only PayPal settings key | P0 — business input |
| Card-to-card | NOT FOUND | — | P1 — spec requirement |
| Phone orders | NOT FOUND in UX | Business phone in spec only | P1 |
| Wholesale pricing | NOT FOUND | No plugin/tiers | P1 — spec requirement |
| MOQ | NOT FOUND | — | P1 |
| B2B / organizational invoice | NOT FOUND | — | P1 |
| Shipping: customer vehicle | NOT FOUND | Spec requirement | P1 |
| Shipping: seller transport | NOT FOUND | Spec requirement | P1 |
| Product search | ✅ Expected | Woodmart AJAX shop | P0 |
| Filters (category/attr) | ✅ Expected | Woodmart | P0 |
| Wishlist | ✅ Theme feature | Woodmart usermeta | P2 |
| Special offers carousel | ✅ Plugin | special-offer-woodmart | P2 |
| Blog / magazine | ✅ Active | `/mag/` + 209 posts | P2 — sanitize |
| SEO (Yoast) | ✅ Active | Indexables | P0 |
| XML sitemap | ✅ Expected | Yoast | P0 |
| Newsletter | ⚙️ Plugin present | Mailchimp for WP | P3 |
| Caching | ✅ Flying Press | Plugin dir | Replace |
| WAF | ✅ Wordfence | Plugin + wf* tables | Replace |
| Revolution Slider | ⚙️ DB remnants | rs tables | Do not migrate |
| Multi-language | ❌ Not detected | fa_IR locale only | RTL Persian first |
| Instagram feed | ❌ Empty token | Woodmart `insta_token` empty | P2 if needed |
| Google Maps | ❌ Empty key | Theme option | Optional |
| File manager | ⚠️ Present | wp-file-manager | **Remove** |
| User registration (public) | ⚠️ Open | `users_can_register=1` | **Disable** on new site |

## Commerce feature depth

### Implemented (legacy)

- Simple product model
- Category-based browsing
- AJAX shop UX (theme-level)
- Stock progress UI (theme)
- Persian RTL storefront

### Not evidenced (legacy)

- Variable products with attributes (minimal)
- Subscription / recurring
- Quote request workflow
- Distributor portal
- Multi-warehouse inventory
- Dynamic shipping quotes
- Invoice PDF generation

## Content features

| Feature | Notes |
|---|---|
| Elementor layouts | Homepage, product content blocks |
| CMS blocks (`?cms_block=` URLs) | Woodmart CMS block previews in indexables |
| FAQ structured pages | UNKNOWN |

## Admin features

Standard WordPress + WooCommerce admin. No custom admin panel detected.

## Feature recommendations for Movasseghi Store

**Must exceed legacy:**

- Secure checkout with server-side price validation
- Persian-aware search
- Mobile-first catalog/filter UX
- Clean SEO (no spam index pollution)
- B2B paths (wholesale, quote, org invoice) per business spec
- Payment abstraction (NextPay primary candidate)

**Do not replicate:**

- Open registration spam vector
- Casino spam blog
- WordPress plugin attack surface
