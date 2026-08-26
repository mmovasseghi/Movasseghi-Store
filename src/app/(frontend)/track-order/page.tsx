import { TrackOrderForm } from '@/components/shop/TrackOrderForm'
import Link from 'next/link'
import { canonicalUrl } from '@/lib/site-url'

export const metadata = {
  title: 'پیگیری سفارش',
  description: 'پیگیری وضعیت سفارش با کد سفارش و شماره موبایل',
  alternates: { canonical: canonicalUrl('/track-order') },
  robots: { index: false, follow: false },
}

export default function TrackOrderPage() {
  return (
    <main className="mx-auto max-w-lg px-4 py-10">
      <h1 className="text-2xl font-bold text-brand-ink">پیگیری سفارش</h1>
      <p className="mt-2 text-sm text-brand-muted">
        کد سفارش (مثلاً ORD-…) و همان شماره موبایلی که هنگام ثبت وارد کرده‌اید را وارد کنید.
      </p>
      <TrackOrderForm />
      <p className="mt-6 text-center text-sm text-brand-muted">
        سفارش جدید؟{' '}
        <Link href="/shop" className="text-brand-green hover:underline">
          فروشگاه
        </Link>
      </p>
    </main>
  )
}
