import { ACCOUNT_COOKIE, parseAccountToken } from '@/lib/account-session'
import { getPayloadClient } from '@/lib/payload'
import { cookies } from 'next/headers'
import { NextResponse } from 'next/server'

export async function GET() {
  const jar = await cookies()
  const phone = parseAccountToken(jar.get(ACCOUNT_COOKIE)?.value)
  if (!phone) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
  }

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'orders',
      where: { customerPhone: { equals: phone } },
      sort: '-createdAt',
      limit: 50,
    })

    return NextResponse.json({
      orders: docs.map((o) => ({
        orderNumber: o.orderNumber,
        status: o.status,
        subtotal: o.subtotal,
        createdAt: o.createdAt,
        paymentMethod: o.paymentMethod,
        shippingMethod: o.shippingMethod,
      })),
    })
  } catch (err) {
    console.error('Account orders failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
