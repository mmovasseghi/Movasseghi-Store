import { AccountLoginForm } from '@/components/account/AccountLoginForm'
import Link from 'next/link'
import { canonicalUrl } from '@/lib/site-url'

export const metadata = {
  title: 'ورود به حساب',
  robots: { index: false, follow: false },
  alternates: { canonical: canonicalUrl('/account/login') },
}

export default function AccountLoginPage() {
  return (
    <main className="mx-auto max-w-lg px-4 py-8">
      <h1 className="text-2xl font-bold text-brand-ink">حساب کاربری</h1>
      <p className="mt-2 text-sm text-brand-muted">
        یا{' '}
        <Link href="/track-order" className="text-brand-green hover:underline">
          پیگیری یک سفارش
        </Link>{' '}
        بدون ورود
      </p>
      <div className="mt-6">
        <AccountLoginForm />
      </div>
    </main>
  )
}
