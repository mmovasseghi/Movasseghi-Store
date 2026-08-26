import type { CartItem } from '@/commerce/cart'
import {
  validatedToCartItems,
  validateCartAgainstProducts,
} from '@/commerce/cart-validation'
import { getPayloadClient } from '@/lib/payload'
import { NextResponse } from 'next/server'

type Body = {
  items: Pick<CartItem, 'productId' | 'quantity'>[]
}

function parseBody(body: unknown): body is Body {
  if (!body || typeof body !== 'object') return false
  const b = body as Record<string, unknown>
  if (!Array.isArray(b.items) || b.items.length === 0) return false
  return b.items.every(
    (i) =>
      i &&
      typeof i === 'object' &&
      typeof (i as CartItem).productId === 'string' &&
      typeof (i as CartItem).quantity === 'number',
  )
}

export async function POST(request: Request) {
  let body: unknown
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ ok: false, error: 'Invalid JSON' }, { status: 400 })
  }

  if (!parseBody(body)) {
    return NextResponse.json({ ok: false, error: 'Invalid cart' }, { status: 400 })
  }

  try {
    const payload = await getPayloadClient()
    const ids = body.items.map((i) => Number(i.productId)).filter((n) => !Number.isNaN(n))

    const { docs } = await payload.find({
      collection: 'products',
      where: { id: { in: ids } },
      limit: ids.length,
      depth: 1,
    })

    const result = validateCartAgainstProducts(body.items, docs)
    if (!result.ok) {
      return NextResponse.json(result, { status: 400 })
    }

    return NextResponse.json({
      ok: true,
      items: validatedToCartItems(result.items),
      lines: result.items,
      subtotal: result.subtotal,
      warnings: result.warnings,
    })
  } catch (err) {
    console.error('Cart validate failed:', err)
    return NextResponse.json({ ok: false, error: 'Server error' }, { status: 500 })
  }
}
