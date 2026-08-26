# CUSTOM-CODE-AUDIT — Legacy Customizations

**Canonical detail:** [16-custom-code.md](./16-custom-code.md)

## Findings (CONFIRMED)

| Item | Status |
|---|---|
| Child theme | NOT FOUND |
| Custom PHP in theme | Minimal — Woodmart standard |
| special-offer-woodmart plugin | Custom carousel — review for behavior |
| DB custom CSS | Primary color, IranYekan fonts |
| Elementor page JSON | Homepage/product layouts — not exported |

## Malware

Do not import any PHP from legacy plugins except audited special-offer if needed for reference.

## New platform

All commerce logic in `src/commerce/`, `src/collections/` — no WordPress PHP port.
