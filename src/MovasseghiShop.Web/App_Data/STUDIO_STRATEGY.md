# موثقی Studio — Master Strategy

> Phases 1–4 implemented in codebase.

## Phase 1 — DONE
- SeoProfile, PillarKeyword, GrowthAction, SeoEngineService, publish gate ≥95

## Phase 2 — DONE
- Pillar pages, Rank Radar (manual + API), sitemap, Bulk Auto-Fix, product FAQ from SeoProfile

## Phase 3 — DONE (Content + Page Builder)
- [x] HomeSection manager (`/Admin/StudioContent/Home`)
- [x] Navigation builder (`/Admin/StudioContent/Nav`)
- [x] Content atoms — promo, cart-empty, catalog-empty (`/Admin/StudioContent/Atoms`)
- [x] TipTap editor for CMS pages (`/Admin/StudioContent/Pages`)
- [x] Storefront wired: hero, trust, nav, footer, empty states

## Phase 4 — DONE (Growth Engine full)
- [x] SerpAPI rank sync (`SerpApi:ApiKey`, `SerpApi:AutoSync`)
- [x] Competitor watch (`/Admin/StudioContent/Competitors`)
- [x] Weekly AI report — rule-based (`/Admin/StudioContent/Reports`)
- [x] Authority checklist (`/Admin/StudioContent/Authority`)
- [x] Background jobs: rank sync (24h), weekly report (Monday)

## Config (appsettings.json)
```json
"SiteSettings": { "PublicBaseUrl": "https://your-domain.com" },
"SerpApi": { "ApiKey": "", "AutoSync": false },
"GrowthEngine": { "AutoWeeklyReport": true }
```

## Operator URLs
| URL | Purpose |
|-----|---------|
| `/Admin/Studio` | Command Center |
| `/Admin/Studio/RankRadar` | Rank + SerpAPI sync |
| `/Admin/StudioContent/Home` | Homepage sections |
| `/Admin/StudioContent/Pages` | CMS + TipTap |
| `/Admin/StudioContent/Nav` | Menus |
| `/Admin/StudioContent/Reports` | Weekly growth report |
