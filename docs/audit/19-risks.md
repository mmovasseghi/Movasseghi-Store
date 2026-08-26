# 19 — Risks

Risk register for legacy → Movasseghi Store migration.

| ID | Risk | Likelihood | Impact | Severity | Mitigation |
|---|---|---|---|---|---|
| R1 | Legacy malware in codebase | High | Critical | **CRITICAL** | Data-only migration; no PHP deploy; malware scan media |
| R2 | SEO authority loss on domain change | High | High | **HIGH** | 301 map; GSC change-of-address; preserve slugs |
| R3 | Spam URL index pollution migrates | Medium | High | **HIGH** | 410 spam URLs; clean sitemap; disallow in robots during cleanup |
| R4 | Fake user/PII import (7,779 spam accounts) | High | Medium | **HIGH** | Whitelist migration users; GDPR-style exclude |
| R5 | Unverified environmental claims | Medium | Medium | **MEDIUM** | SOURCE_REQUIRED; legal review before marketing changes |
| R6 | Payment config unknown | High | High | **HIGH** | Business workshop; implement from spec + provider docs |
| R7 | Live site behavior unknown (HTTP 500) | High | Medium | **MEDIUM** | Staging parity QA; customer UAT |
| R8 | Missing orders/customer history | Medium | Medium | **MEDIUM** | Confirm with business if any WC orders expected |
| R9 | Credential leak from backup | Medium | Critical | **HIGH** | Rotate DB/salts; never commit wp-config |
| R10 | Open registration re-abused | Medium | Medium | **MEDIUM** | Disable public registration default |
| R11 | Price/stock mismatch at launch | Medium | High | **HIGH** | Server-side validation; manual price audit |
| R12 | Incomplete media migration | Medium | High | **MEDIUM** | Attachment audit script; broken image CI check |
| R13 | Canonical host split (www/non-www) | High | Medium | **MEDIUM** | Enforce single host + 301 |
| R14 | Competitor SERP pressure | High | Medium | **MEDIUM** | Commercial category UX; see competition docs |
| R15 | Wholesale/B2B gap vs spec | High | High | **HIGH** | Design B2B flows in Gate 3 IA |
| R16 | Iranian payment integration complexity | Medium | High | **MEDIUM** | PaymentService abstraction; test matrix |
| R17 | False product specs from AI rewrite | Low | High | **HIGH** | Content governance — migrate first |
| R18 | Staging indexed by Google | Medium | High | **MEDIUM** | noindex + auth on staging |
| R19 | Backup incomplete (uploads) | Low | High | **MEDIUM** | Verify attachment files exist in tar |
| R20 | Brand confusion Ayrik → Movasseghi | Medium | Medium | **MEDIUM** | Clear brand transition messaging |

## Security incident summary

The legacy site exhibits classic **WordPress compromise**:

- Malware plugin (`HelloDollyV2_ixwz`)
- Mass subscriber registration spam (~7,779 accounts)
- Casino content injection (~150+ URLs)
- `wp-file-manager` present

Treat entire legacy runtime as **untrusted**.

## Release blockers (from spec)

Migration release must not proceed if:

- Important product/category URLs disappear without redirects
- Product schema broken
- Spam content re-published
- Staging publicly indexable

## Risk review cadence

- Re-evaluate after Gate 2 (architecture)
- Re-evaluate after clean dataset extraction
- Re-evaluate before production data migration (Gate 4)
