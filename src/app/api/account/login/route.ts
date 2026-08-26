import { ACCOUNT_COOKIE, createAccountToken } from '@/lib/account-session'
import { getPayloadClient } from '@/lib/payload'
import { isValidMobile, normalizePhone } from '@/lib/phone'
import { cookies } from 'next/headers'
import { NextResponse } from 'next/server'

type LoginBody = {
  phone: string
  orderNumber: string
}

function validate(body: unknown): body is LoginBody {
  if (!body || typeof body !== 'object') return false
  const b = body as Record<string, unknown>
  return (
    typeof b.phone === 'string' &&
    isValidMobile(b.phone) &&
    typeof b.orderNumber === 'string' &&
    b.orderNumber.trim().length >= 8
  )
}

export async function POST(request: Request) {
  let body: unknown
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ error: 'Invalid JSON' }, { status: 400 })
  }

  if (!validate(body)) {
    return NextResponse.json({ error: 'اطلاعات ورود نامعتبر است' }, { status: 400 })
  }

  try {
    const payload = await getPayloadClient()
    const phone = normalizePhone(body.phone)
    const { docs } = await payload.find({
      collection: 'orders',
      where: {
        and: [
          { orderNumber: { equals: body.orderNumber.trim() } },
          { customerPhone: { equals: phone } },
        ],
      },
      limit: 1,
    })

    if (!docs[0]) {
      return NextResponse.json({ error: 'سفارش با این مشخصات یافت نشد' }, { status: 401 })
    }

    const token = createAccountToken(phone)
    const jar = await cookies()
    jar.set(ACCOUNT_COOKIE, token, {
      httpOnly: true,
      sameSite: 'lax',
      secure: process.env.NODE_ENV === 'production',
      path: '/',
      maxAge: 30 * 24 * 60 * 60,
    })

    return NextResponse.json({ ok: true })
  } catch (err) {
    console.error('Account login failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
