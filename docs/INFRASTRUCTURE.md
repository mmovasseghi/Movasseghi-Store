# Infrastructure

## Server

| Item | Value |
|---|---|
| Provider | Hetzner |
| IP | `91.107.181.79` |
| OS | Linux (Ubuntu expected) |

## Access policy

- **Deploy user:** `deploy` (non-root)
- **Root:** setup only — not for running app
- **Auth:** SSH keys in GitHub Secrets (`STAGING_SSH_KEY`)
- **Never** commit passwords or private keys

## Environments

| Env | Host | Indexable |
|---|---|---|
| Staging | IP or staging subdomain | **no** (robots + noindex) |
| Production | http://91.107.181.79 (domain TBD) | yes |

## Deploy path

```
git push → GitHub Actions → build → SSH deploy user → staging path
```

Workflow: `.github/workflows/deploy-staging.yml` (enabled after app bootstrap)

## Secrets (GitHub Actions)

| Secret | Purpose |
|---|---|
| `STAGING_SSH_KEY` | Deploy private key |
| `STAGING_HOST` | 91.107.181.79 |
| `STAGING_USER` | deploy |
| `DATABASE_URL` | Postgres connection |
| Payment keys | NextPay/ZarinPal — staging vs prod separate |

## Bootstrap checklist

- [x] Run `scripts/infra/hetzner-bootstrap.sh` as root (2026-08-26)
- [x] Node 22 + Nginx + UFW + fail2ban installed
- [x] PostgreSQL 14 + PM2 (2026-08-26 release)
- [x] **Release live** at http://91.107.181.79/ (Next.js + Payload)
- [x] 95 products + 22 categories imported from legacy
- [ ] Add deploy user SSH public key (currently root deploy)
- [ ] TLS + domain (when purchased)
- [ ] GitHub Actions deploy key authorized

## Staging URL (temporary)

Until domain purchased:

```
http://91.107.181.79/
```

Add `X-Robots-Tag: noindex` at Nginx layer.

## Backups

Production: daily DB + media, retention 14d, restore test monthly.

See `docs/DISASTER-RECOVERY.md` (create at Phase 9).
