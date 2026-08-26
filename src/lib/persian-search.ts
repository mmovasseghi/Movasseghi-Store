/**
 * Persian text normalization for search and matching.
 */

const PERSIAN_DIGITS = '۰۱۲۳۴۵۶۷۸۹'
const ARABIC_DIGITS = '٠١٢٣٤٥٦٧٨٩'

export function toLatinDigits(value: string): string {
  return value
    .replace(/[۰-۹]/g, (d) => String(PERSIAN_DIGITS.indexOf(d)))
    .replace(/[٠-٩]/g, (d) => String(ARABIC_DIGITS.indexOf(d)))
}

/** Normalize ی/ي, ک/ك, digits, ZWNJ, whitespace for search. */
export function normalizePersianText(input: string): string {
  return toLatinDigits(input)
    .replace(/[\u064A\u0649]/g, 'ی')
    .replace(/\u0643/g, 'ک')
    .replace(/\u200c/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .toLowerCase()
}

export function persianSearchMatch(haystack: string, needle: string): boolean {
  const n = normalizePersianText(needle)
  if (!n) return true
  const h = normalizePersianText(haystack)
  return h.includes(n)
}
