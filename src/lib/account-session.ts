import { createHmac, timingSafeEqual } from 'node:crypto'

export const ACCOUNT_COOKIE = 'mov_account'
const TTL_MS = 30 * 24 * 60 * 60 * 1000

function sessionSecret(): string {
  const secret = process.env.PAYLOAD_SECRET
  if (!secret) throw new Error('PAYLOAD_SECRET is required')
  return secret
}

export function createAccountToken(phone: string): string {
  const exp = Date.now() + TTL_MS
  const body = `${phone}|${exp}`
  const sig = createHmac('sha256', sessionSecret()).update(body).digest('base64url')
  return `${body}|${sig}`
}

export function parseAccountToken(token: string | undefined | null): string | null {
  if (!token) return null
  const parts = token.split('|')
  if (parts.length !== 3) return null
  const [phone, expStr, sig] = parts
  const exp = Number(expStr)
  if (!phone || !Number.isFinite(exp) || exp < Date.now()) return null
  const body = `${phone}|${expStr}`
  const expected = createHmac('sha256', sessionSecret()).update(body).digest('base64url')
  if (sig.length !== expected.length) return null
  try {
    if (!timingSafeEqual(Buffer.from(sig), Buffer.from(expected))) return null
  } catch {
    return null
  }
  return phone
}
