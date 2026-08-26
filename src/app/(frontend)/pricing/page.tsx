import Link from 'next/link'
import { formatIrt } from '@/commerce/cart'
import { getPayloadClient } from '@/lib/payload'
import { productDisplayPrice } from '@/lib/products'

export const dynamic = 'force-dynamic'

export const metadata = {
  title: 'قیمت آنلاین محصولات آملون',
  description:
    'لیست قیمت به‌روز ظروف یکبار مصرف گیاهی آملون — تمامی قیمت‌های نمایش‌داده‌شده آخرین قیمت‌های اعلام‌شده هستند.',
}

type Props = {
  searchParams: Promise<{ cat?: string }>
}

export default async function PricingPage({ searchParams }: Props) {
  const { cat: catSlug } = await searchParams

  let categories: { id: number; slug: string; name: string }[] = []
  let products: {
    name: string
    slug: string
    price: number
    categoryNames: string[]
  }[] = []

  try {
    const payload = await getPayloadClient()
    const [catRes, prodRes] = await Promise.all([
      payload.find({ collection: 'categories', limit: 50, sort: 'sortOrder' }),
      payload.find({
        collection: 'products',
        where: { status: { equals: 'published' } },
        limit: 200,
        sort: 'name',
        depth: 1,
      }),
    ])

    categories = catRes.docs.map((c) => ({ id: c.id, slug: c.slug, name: c.name }))

    const activeCat = catSlug ? categories.find((c) => c.slug === catSlug) : null

    for (const p of prodRes.docs) {
      const catDocs = (Array.isArray(p.categories) ? p.categories : []).flatMap((c) =>
        typeof c === 'object' && c !== null && 'slug' in c && 'name' in c ? [c] : [],
      )
      if (activeCat && !catDocs.some((c) => c.slug === activeCat.slug)) continue
      products.push({
        name: p.name,
        slug: p.slug,
        price: productDisplayPrice(p),
        categoryNames: catDocs.map((c) => c.name),
      })
    }
  } catch {
    // empty state
  }

  return (
    <main className="mx-auto max-w-6xl px-4 py-8">
      <nav className="mb-4 text-sm text-brand-muted">
        <Link href="/shop" className="hover:text-brand-green">
          فروشگاه
        </Link>
        <span className="mx-2">/</span>
        <span>لیست قیمت</span>
      </nav>

      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">قیمت آنلاین محصولات آملون</h1>
      <p className="mt-3 max-w-3xl leading-relaxed text-brand-muted">
        تمامی قیمت‌های نمایش‌داده‌شده در سایت، آخرین قیمت‌های اعلام‌شده هستند. برای سفارش عمده یا
        تخفیف ویژه با{' '}
        <a href="tel:09125199105" className="text-brand-green hover:underline">
          ۰۹۱۲۵۱۹۹۱۰۵
        </a>{' '}
        تماس بگیرید.
      </p>

      {categories.length > 0 && (
        <div className="mt-6 flex flex-wrap gap-2">
          <Link
            href="/pricing"
            className={`rounded-full border px-4 py-1.5 text-sm ${!catSlug ? 'border-brand-green bg-brand-green/10 text-brand-green' : 'border-border bg-white hover:border-brand-green'}`}
          >
            همه
          </Link>
          {categories.map((c) => (
            <Link
              key={c.slug}
              href={`/pricing?cat=${encodeURIComponent(c.slug)}`}
              className={`rounded-full border px-4 py-1.5 text-sm ${catSlug === c.slug ? 'border-brand-green bg-brand-green/10 text-brand-green' : 'border-border bg-white hover:border-brand-green'}`}
            >
              {c.name}
            </Link>
          ))}
        </div>
      )}

      <div className="mt-8 overflow-x-auto rounded-xl border border-border bg-white">
        <table className="w-full min-w-[480px] text-sm">
          <thead>
            <tr className="border-b border-border bg-brand-off-white text-start">
              <th className="px-4 py-3 font-semibold text-brand-ink">محصول</th>
              <th className="hidden px-4 py-3 font-semibold text-brand-ink sm:table-cell">دسته</th>
              <th className="px-4 py-3 font-semibold text-brand-ink">قیمت</th>
            </tr>
          </thead>
          <tbody>
            {products.map((p) => (
              <tr key={p.slug} className="border-b border-border last:border-0">
                <td className="px-4 py-3">
                  <Link href={`/product/${p.slug}`} className="font-medium text-brand-ink hover:text-brand-green">
                    {p.name}
                  </Link>
                </td>
                <td className="hidden px-4 py-3 text-brand-muted sm:table-cell">
                  {p.categoryNames.join('، ') || '—'}
                </td>
                <td className="px-4 py-3 font-medium text-brand-green">
                  {p.price > 0 ? formatIrt(p.price) : 'تماس بگیرید'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {products.length === 0 && (
          <p className="p-8 text-center text-brand-muted">محصولی برای نمایش یافت نشد.</p>
        )}
      </div>
    </main>
  )
}
