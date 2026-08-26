import { CheckoutForm } from '@/components/shop/CheckoutForm'

export const metadata = {
  title: 'تسویه حساب',
  description: 'ثبت سفارش ظروف یکبار مصرف گیاهی — فروشگاه موثقی',
}

export default function CheckoutPage() {
  return (
    <main className="mx-auto max-w-6xl px-4 py-8">
      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">تسویه حساب</h1>
      <p className="mt-2 text-brand-muted">سفارش تلفنی، کارت به کارت، یا پرداخت آنلاین (به‌زودی)</p>
      <CheckoutForm />
    </main>
  )
}
