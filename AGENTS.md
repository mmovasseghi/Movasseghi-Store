# AGENTS.md — Movasseghi Store

Short operational constitution. Details live in `docs/` and `.cursor/rules/`.

## Repositories

| Repo | Role |
|---|---|
| `mmovasseghi/Movasseghi-Store` | **This repo** — new platform |
| `mmovasseghi/Movasseghi-Store-Legacy` | Read-only sanitized forensic export |

Legacy raw backup is **not** in Git. Never commit wp-config, SQL dumps, or secrets.

## Non-negotiables

1. Legacy is **read-only** — never modify Legacy repo content for migration
2. **Source > assumption** — CONFIRMED / INFERRED / UNKNOWN / NOT FOUND
3. **Content:** migrate → parity → SEO validation → release → then improve
4. **SEO assets protected** — URLs, slugs, titles, product data
5. **Mobile-first + performance** mandatory
6. **No fabricated** products, prices, claims, reviews
7. **No payment endpoint invention** — official docs only
8. **Token efficiency** — read `docs/audit/` not raw WordPress

## Context priority

```
1. docs/audit/* docs/knowledge/* docs/seo/*
2. src/* (implementation)
3. Legacy repo (if compact docs insufficient)
4. Raw backup (FORBIDDEN unless new forensic pass approved)
```

## Approval gates

| Gate | Trigger |
|---|---|
| Gate 1 | Forensics complete ✅ |
| Gate 2 | Architecture decision |
| Gate 3 | Design system + IA |
| Gate 4 | Irreversible production data migration |

## Phase status

- **Phase 1:** COMPLETE
- **Phase 1b:** GitHub + infra — IN PROGRESS
- **Phase 2+:** Blocked on Gate 2 unless routine docs/infra work

## Stack (baseline)

Next.js · TypeScript · Tailwind · shadcn/ui · Payload (candidate) · Playwright · Biome

Optional only when justified: PersianLabs, Motion, TanStack Query, Medusa

## Cloud execution

Prefer **Cursor Cloud Agent** with this repo. Use GitHub PRs for review. Deploy via GitHub Actions to Hetzner — not ad-hoc root SSH file copies.

## Rules

See `.cursor/rules/*.mdc` for scoped rules.
