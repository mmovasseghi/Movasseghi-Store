import { paymentService, type PaymentProvider } from '@/commerce/payments/PaymentService'
import { getPayloadClient } from '@/lib/payload'
import { canonicalUrl } from '@/lib/site-url'
import { NextResponse } from 'next/server'

type RouteParams = { params: Promise<{ provider: string }> }

const PROVIDERS: PaymentProvider[] = ['nextpay', 'zarinpal', 'balepay']

function isProvider(value: string): value is PaymentProvider {
  return PROVIDERS.includes(value as PaymentProvider)
}

/** Payment gateway callback — server-side verify only. */
export async function GET(request: Request, { params }: RouteParams) {
  const { provider } = await params
  if (!isProvider(provider)) {
    return NextResponse.redirect(canonicalUrl('/checkout?error=unknown_provider'))
  }

  const url = new URL(request.url)
  const queryParams: Record<string, string> = {}
  url.searchParams.forEach((value, key) => {
    queryParams[key] = value
  })

  const verify = await paymentService.verify(provider, queryParams)
  if (!verify.ok) {
    return NextResponse.redirect(
      canonicalUrl(`/checkout?error=payment_failed&order=${queryParams.order ?? ''}`),
    )
  }

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'orders',
      where: { orderNumber: { equals: verify.orderId } },
      limit: 1,
    })
    const order = docs[0]

    if (order && order.status === 'pending') {
      const expected = order.subtotal ?? 0
      if (verify.paidAmount > 0 && Math.abs(verify.paidAmount - expected) > 1) {
        await payload.update({
          collection: 'orders',
          id: order.id,
          data: {
            status: 'cancelled',
            note: [order.note, `Amount mismatch: ${verify.paidAmount} vs ${expected}`].filter(Boolean).join('\n'),
          },
        })
        return NextResponse.redirect(
          canonicalUrl(`/checkout?error=amount_mismatch&order=${verify.orderId}`),
        )
      }

      await payload.update({
        collection: 'orders',
        id: order.id,
        data: {
          status: 'confirmed',
          note: [order.note, `Payment ref: ${verify.refId}`].filter(Boolean).join('\n'),
        },
      })
    }
  } catch (err) {
    console.error('Payment callback order update failed:', err)
  }

  return NextResponse.redirect(canonicalUrl(`/checkout/success?order=${verify.orderId}&paid=1`))
}
