import { CheckoutSuccess } from '@/components/shop/CheckoutSuccess'

export const metadata = {
  title: 'سفارش ثبت شد',
  robots: { index: false, follow: false },
}

export default function CheckoutSuccessPage() {
  return (
    <main className="mx-auto max-w-2xl px-4 py-12">
      <CheckoutSuccess />
    </main>
  )
}
