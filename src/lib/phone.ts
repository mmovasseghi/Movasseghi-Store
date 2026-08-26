/** Normalize Iranian mobile to 09xxxxxxxxx Latin digits. */
export function normalizePhone(value: string): string {
  const persian = '۰۱۲۳۴۵۶۷۸۹'
  const arabic = '٠١٢٣٤٥٦٧٨٩'
  const latin = value
    .replace(/[۰-۹]/g, (d) => String(persian.indexOf(d)))
    .replace(/[٠-٩]/g, (d) => String(arabic.indexOf(d)))
  return latin.replace(/\D/g, '')
}

export function isValidMobile(phone: string): boolean {
  return /^09\d{9}$/.test(normalizePhone(phone))
}
