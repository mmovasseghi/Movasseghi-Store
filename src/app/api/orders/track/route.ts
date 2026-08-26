import { getPayloadClient } from '@/lib/payload'
import { isValidMobile, normalizePhone } from '@/lib/phone'
import { NextResponse } from 'next/server'

type TrackBody = {
  orderNumber: string
  phone: string
}

function validate(body: unknown): body is TrackBody {
  if (!body || typeof body !== 'object') return false
  const b = body as Record<string, unknown>
  return (
    typeof b.orderNumber === 'string' &&
    b.orderNumber.length >= 8 &&
    typeof b.phone === 'string' &&
    isValidMobile(b.phone)
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
    return NextResponse.json({ error: 'Invalid tracking data' }, { status: 400 })
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

    const order = docs[0]
    if (!order) {
      return NextResponse.json({ error: 'سفارش یافت نشد' }, { status: 404 })
    }

    const items = Array.isArray(order.items)
      ? (order.items as { name?: string; quantity?: number }[])
      : []

    return NextResponse.json({
      orderNumber: order.orderNumber,
      status: order.status,
      subtotal: order.subtotal,
      paymentMethod: order.paymentMethod,
      shippingMethod: order.shippingMethod,
      createdAt: order.createdAt,
      items: items.map((i) => ({ name: i.name ?? '—', quantity: i.quantity ?? 1 })),
    })
  } catch (err) {
    console.error('Order track failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
