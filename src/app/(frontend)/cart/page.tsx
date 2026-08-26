import Link from 'next/link'
import { emptyCart, formatIrt } from '@/commerce/cart'

export const metadata = {
  title: 'سبد خرید',
}

export default function CartPage() {
  const cart = emptyCart()

  return (
    <main className="mx-auto max-w-3xl px-4 py-8">
      <h1 className="text-2xl font-bold text-brand-ink">سبد خرید</h1>
      {cart.items.length === 0 ? (
        <div className="mt-8 rounded-lg border border-border bg-white p-8 text-center">
          <p className="text-brand-muted">سبد خرید شما خالی است.</p>
          <Link
            href="/shop"
            className="mt-4 inline-block rounded-lg bg-brand-green px-6 py-2 text-white hover:bg-brand-green-light"
          >
            رفتن به فروشگاه
          </Link>
        </div>
      ) : (
        <p className="mt-4">{formatIrt(0)}</p>
      )}
    </main>
  )
}
