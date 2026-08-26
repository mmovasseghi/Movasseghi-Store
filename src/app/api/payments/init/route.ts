import { paymentService, type PaymentProvider } from '@/commerce/payments/PaymentService'
import { getPayloadClient } from '@/lib/payload'
import { canonicalUrl } from '@/lib/site-url'
import { NextResponse } from 'next/server'

type InitBody = {
  orderId: string
  provider: PaymentProvider
}

const PROVIDERS: PaymentProvider[] = ['nextpay', 'zarinpal', 'balepay']

function providerConfigured(provider: PaymentProvider): boolean {
  switch (provider) {
    case 'nextpay':
      return Boolean(process.env.NEXTPAY_API_KEY)
    case 'zarinpal':
      return Boolean(process.env.ZARINPAL_MERCHANT_ID)
    case 'balepay':
      return Boolean(process.env.BALEPAY_API_KEY)
    default:
      return false
  }
}

function parseBody(body: unknown): body is InitBody {
  if (!body || typeof body !== 'object') return false
  const b = body as Record<string, unknown>
  return (
    typeof b.orderId === 'string' &&
    b.orderId.length >= 8 &&
    typeof b.provider === 'string' &&
    PROVIDERS.includes(b.provider as PaymentProvider)
  )
}

/** Initiate online payment — requires merchant credentials (approval gate). */
export async function POST(request: Request) {
  let body: unknown
  try {
    body = await request.json()
  } catch {
    return NextResponse.json({ error: 'Invalid JSON' }, { status: 400 })
  }

  if (!parseBody(body)) {
    return NextResponse.json({ error: 'Invalid request' }, { status: 400 })
  }

  if (!providerConfigured(body.provider)) {
    return NextResponse.json(
      { error: `Payment provider ${body.provider} is not configured` },
      { status: 503 },
    )
  }

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'orders',
      where: { orderNumber: { equals: body.orderId } },
      limit: 1,
    })
    const order = docs[0]
    if (!order) {
      return NextResponse.json({ error: 'Order not found' }, { status: 404 })
    }
    if (order.status !== 'pending') {
      return NextResponse.json({ error: 'Order is not payable' }, { status: 409 })
    }
    if (order.paymentMethod !== 'online') {
      return NextResponse.json({ error: 'Order payment method is not online' }, { status: 400 })
    }

    const amount = order.subtotal ?? 0
    if (amount <= 0) {
      return NextResponse.json({ error: 'Invalid order amount' }, { status: 400 })
    }

    const result = await paymentService.init(body.provider, {
      orderId: body.orderId,
      amount,
      currency: 'IRT',
      callbackUrl: canonicalUrl(`/api/payments/callback/${body.provider}`),
      customerMobile: order.customerPhone ?? undefined,
      description: `سفارش ${body.orderId}`,
    })

    if (!result.ok) {
      return NextResponse.json({ error: result.error }, { status: 502 })
    }

    return NextResponse.json({
      redirectUrl: result.redirectUrl,
      providerRef: result.providerRef,
    })
  } catch (err) {
    console.error('Payment init failed:', err)
    return NextResponse.json({ error: 'Server error' }, { status: 500 })
  }
}
