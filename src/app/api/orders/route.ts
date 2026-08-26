import type { CartItem } from '@/commerce/cart'
import { getPayloadClient } from '@/lib/payload'
import { NextResponse } from 'next/server'

type OrderBody = {
  orderId: string
  name: string
  phone: string
  note?: string
  payment: 'online' | 'card_to_card' | 'phone'
  shipping: 'customer' | 'seller'
  items: CartItem[]
  subtotal: number
}

function toLatinDigits(value: string): string {
  const persian = '۰۱۲۳۴۵۶۷۸۹'
  const arabic = '٠١٢٣٤٥٦٧٨٩'
  return value.replace(/[۰-۹]/g, (d) => String(persian.indexOf(d))).replace(/[٠-٩]/g, (d) => String(arabic.indexOf(d)))
}

function validate(body: unknown): body is OrderBody {
  if (!body || typeof body !== 'object') return false
  const b = body as Record<string, unknown>
  const phone = toLatinDigits(String(b.phone ?? '')).replace(/\D/g, '')
  return (
    typeof b.orderId === 'string' &&
    b.orderId.length >= 8 &&
    typeof b.name === 'string' &&
    b.name.trim().length >= 2 &&
    typeof b.phone === 'string' &&
    /^09\d{9}$/.test(phone) &&
    Array.isArray(b.items) &&
    b.items.length > 0 &&
    typeof b.subtotal === 'number' &&
    b.subtotal >= 0 &&
    ['online', 'card_to_card', 'phone'].includes(b.payment as string) &&
    ['customer', 'seller'].includes(b.shipping as string)
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
    return NextResponse.json({ error: 'Invalid order data' }, { status: 400 })
  }

  try {
    const payload = await getPayloadClient()
    const existing = await payload.find({
      collection: 'orders',
      where: { orderNumber: { equals: body.orderId } },
      limit: 1,
    })
    if (existing.docs.length > 0) {
      return NextResponse.json({ orderId: body.orderId, saved: true })
    }

    await payload.create({
      collection: 'orders',
      data: {
        orderNumber: body.orderId,
        customerName: body.name.trim(),
        customerPhone: toLatinDigits(body.phone).replace(/\D/g, ''),
        note: body.note?.trim() || undefined,
        paymentMethod: body.payment,
        shippingMethod: body.shipping,
        items: body.items,
        subtotal: body.subtotal,
        status: 'pending',
      },
    })

    return NextResponse.json({ orderId: body.orderId, saved: true })
  } catch (err) {
    console.error('Order save failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
