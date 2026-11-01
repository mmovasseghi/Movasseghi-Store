# Cycle: hero truncation + Stories inflight + Style/Layout

## Diagnosis correction (Q8T2c8aj lighthouse.json)

| Claim (old handoff) | Evidence |
|---------------------|----------|
| «task ~333ms از parse کردن 150KB CSS» | **رد.** `long-tasks.details.debugData.tasks[0]`: styleLayout **313.6ms**, Parse HTML/CSS کل main-thread فقط **15.9ms**. |
| TBT=0 یعنی کار سنگین نیست | **رد.** taskهای Style/Layout عمده قبل از FCP (1228ms / 1564ms) رخ داده‌اند. |

Local CDP trace (desktop cold, live `41a2e55`): before FCP **Layout ~121ms** vs **UpdateLayoutTree (Recalc Style) ~20ms** vs **ParseAuthorStyleSheet ~1.4ms**. Layout root = `#document`, dirtyObjects ≈1760, `partialLayout:false`.

## Fixes

1. **Hero desc:** remove mobile `line-clamp:2` / `max-height`; reserve `min-height: calc(3 * 1.62em)` so full copy stays visible (measured scrollH≈62px vs clamped 41px at 320/360/390).
2. **Stories:** share one `storiesLoadPromise` across boot / early intent / click; clear on failure for retry; modal images `loading=eager`.
3. **Style/Layout lever:** re-enable `content-visibility:auto` on below-fold home sections with measured `contain-intrinsic-size` (offers/cat/featured/segments/news/closing/faq/footer). Hypothesis: fewer full-document Layout dirty objects before FCP without FOUC. Success = lower pre-FCP Layout + stable CLS + intact first paint/interactions.

## Out of scope

preload / display-v2 rework; domain/CDN/new cert; CSS coverage deletion; animation removal.
