import { AccountOrdersList } from '@/components/account/AccountOrdersList'
import Link from 'next/link'
import { ACCOUNT_COOKIE, parseAccountToken } from '@/lib/account-session'
import { canonicalUrl } from '@/lib/site-url'
import { cookies } from 'next/headers'
import { redirect } from 'next/navigation'

export const metadata = {
  title: 'سفارش‌های من',
  robots: { index: false, follow: false },
  alternates: { canonical: canonicalUrl('/account') },
}

export default async function AccountPage() {
  const jar = await cookies()
  const phone = parseAccountToken(jar.get(ACCOUNT_COOKIE)?.value)
  if (!phone) redirect('/account/login')

  return (
    <main className="mx-auto max-w-2xl px-4 py-8">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold text-brand-ink">سفارش‌های من</h1>
        <Link href="/b2b/quote" className="text-sm text-brand-green hover:underline">
          درخواست قیمت عمده
        </Link>
      </div>
      <div className="mt-6">
        <AccountOrdersList />
      </div>
    </main>
  )
}
