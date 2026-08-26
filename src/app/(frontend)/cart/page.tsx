import { CartView } from '@/components/shop/CartView'

export const metadata = {
  title: 'سبد خرید',
  robots: { index: false, follow: false },
}

export default function CartPage() {
  return (
    <main className="mx-auto max-w-3xl px-4 py-8">
      <h1 className="text-2xl font-bold text-brand-ink">سبد خرید</h1>
      <CartView />
    </main>
  )
}
