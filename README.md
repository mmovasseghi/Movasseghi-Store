# فروشگاه موثقی — Movasseghi Store

Modern ecommerce platform for **فروشگاه موثقی** — disposable plant-based food-service products.

ظروف یکبار مصرف گیاهی آملون

## Status

| Phase | State |
|---|---|
| Legacy forensics | ✅ Complete — `docs/audit/` |
| Gate 2 Architecture | ✅ Payload + custom commerce |
| Gate 3 Design system | ✅ `docs/DESIGN-SYSTEM.md` |
| App scaffold | ✅ Next.js 16 + Payload 3 + PostgreSQL |
| Staging server | ✅ Hetzner bootstrap — `91.107.181.79` |
| Content migration | 🔄 Pending — import from Legacy repo |

## Repositories

| Repo | Purpose |
|---|---|
| [Movasseghi-Store](https://github.com/mmovasseghi/Movasseghi-Store) | This project |
| [Movasseghi-Store-Legacy](https://github.com/mmovasseghi/Movasseghi-Store-Legacy) | Sanitized WordPress export |

## Local development

```bash
# 1. PostgreSQL
docker compose up postgres -d

# 2. Environment
cp .env.example .env
# Edit PAYLOAD_SECRET (openssl rand -hex 32)

# 3. Install & run
npm install
npm run dev
```

- Storefront: http://localhost:3000
- Admin: http://localhost:3000/admin

## Stack

Next.js 16 · TypeScript · Tailwind 4 · Payload CMS 3 · PostgreSQL · Biome

## Project layout

```
docs/           Audit, SEO, business rules
src/
  app/          Next.js routes (RTL storefront + Payload admin)
  collections/  Products, Categories, Pages, Media
  commerce/     Cart + PaymentService
  components/   UI
scripts/        Legacy extract, deploy, infra
```

## Deploy

- Staging: Hetzner `91.107.181.79` — `/var/www/movasseghi-staging`
- Workflow template: `docs/ci/deploy-staging.yml` (enable when GitHub workflow scope available)
- Manual: `scripts/deploy/staging.sh`

## Agents

Read `AGENTS.md` → `docs/audit/README.md` → `docs/EXECUTION-PLAN.md`

## Contact

- Phone: 09125199105
- Instagram: [@movasseghiStore](https://instagram.com/movasseghiStore)
