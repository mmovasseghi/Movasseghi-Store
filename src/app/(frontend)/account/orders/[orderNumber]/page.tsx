import { formatIrt } from '@/commerce/cart'
import Link from 'next/link'
import { notFound, redirect } from 'next/navigation'
import { ACCOUNT_COOKIE, parseAccountToken } from '@/lib/account-session'
import { getPayloadClient } from '@/lib/payload'
import { canonicalUrl } from '@/lib/site-url'
import { cookies } from 'next/headers'

const STATUS_LABEL: Record<string, string> = {
  pending: 'در انتظار تأیید',
  confirmed: 'تأیید شده',
  cancelled: 'لغو شده',
}

const PAYMENT_LABEL: Record<string, string> = {
  phone: 'تلفنی',
  card_to_card: 'کارت به کارت',
  online: 'آنلاین',
}

const SHIPPING_LABEL: Record<string, string> = {
  seller: 'ارسال فروشنده',
  customer: 'خودروی مشتری',
}

type Props = {
  params: Promise<{ orderNumber: string }>
}

export async function generateMetadata({ params }: Props) {
  const { orderNumber } = await params
  return {
    title: `سفارش ${orderNumber}`,
    robots: { index: false, follow: false },
    alternates: { canonical: canonicalUrl(`/account/orders/${orderNumber}`) },
  }
}

export default async function AccountOrderDetailPage({ params }: Props) {
  const jar = await cookies()
  const phone = parseAccountToken(jar.get(ACCOUNT_COOKIE)?.value)
  if (!phone) redirect('/account/login')

  const { orderNumber } = await params
  const decoded = decodeURIComponent(orderNumber)

  let order: {
    orderNumber: string
    status: string
    subtotal: number
    paymentMethod: string
    shippingMethod: string
    customerName: string
    note?: string | null
    createdAt: string
    items: { name?: string; quantity?: number }[]
  } | null = null

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'orders',
      where: {
        and: [{ orderNumber: { equals: decoded } }, { customerPhone: { equals: phone } }],
      },
      limit: 1,
    })
    const doc = docs[0]
    if (!doc) notFound()
    order = {
      orderNumber: doc.orderNumber,
      status: doc.status ?? 'pending',
      subtotal: doc.subtotal,
      paymentMethod: doc.paymentMethod,
      shippingMethod: doc.shippingMethod,
      customerName: doc.customerName,
      note: doc.note,
      createdAt: doc.createdAt,
      items: Array.isArray(doc.items)
        ? (doc.items as { name?: string; quantity?: number }[])
        : [],
    }
  } catch {
    notFound()
  }

  return (
    <main className="mx-auto max-w-2xl px-4 py-8">
      <nav className="mb-4 text-sm text-brand-muted">
        <Link href="/account" className="hover:text-brand-green">
          سفارش‌های من
        </Link>
        <span className="mx-2">/</span>
        <span>{order.orderNumber}</span>
      </nav>
      <h1 className="text-2xl font-bold text-brand-ink">{order.orderNumber}</h1>
      <p className="mt-1 text-sm text-brand-muted">
        {new Date(order.createdAt).toLocaleString('fa-IR')}
      </p>

      <div className="mt-6 space-y-4 rounded-xl border border-border bg-white p-6">
        <p>
          <span className="text-brand-muted">وضعیت: </span>
          {STATUS_LABEL[order.status] ?? order.status}
        </p>
        <p>
          <span className="text-brand-muted">مشتری: </span>
          {order.customerName}
        </p>
        <p>
          <span className="text-brand-muted">پرداخت: </span>
          {PAYMENT_LABEL[order.paymentMethod] ?? order.paymentMethod}
        </p>
        <p>
          <span className="text-brand-muted">ارسال: </span>
          {SHIPPING_LABEL[order.shippingMethod] ?? order.shippingMethod}
        </p>
        <p>
          <span className="text-brand-muted">جمع: </span>
          <strong>{formatIrt(order.subtotal)}</strong>
        </p>
        {order.note && (
          <p className="text-sm text-brand-muted whitespace-pre-line">{order.note}</p>
        )}
        <ul className="border-t border-border pt-4 text-sm text-brand-muted">
          {order.items.map((item, i) => (
            <li key={i}>
              {item.name ?? '—'} × {item.quantity ?? 1}
            </li>
          ))}
        </ul>
      </div>
    </main>
  )
}
