import { ACCOUNT_COOKIE } from '@/lib/account-session'
import { cookies } from 'next/headers'
import { NextResponse } from 'next/server'

export async function POST() {
  const jar = await cookies()
  jar.delete(ACCOUNT_COOKIE)
  return NextResponse.json({ ok: true })
}
