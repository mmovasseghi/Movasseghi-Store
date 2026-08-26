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
| Production | TBD domain | yes (after gates) |

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

- [ ] Create `deploy` user
- [ ] Install Node 22, Docker (optional), Nginx, Certbot
- [ ] Firewall: 22, 80, 443
- [ ] `/var/www/movasseghi-staging` directory
- [ ] Nginx staging vhost with noindex header
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
