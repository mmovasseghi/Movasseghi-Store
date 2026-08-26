import Link from 'next/link'
import { Suspense } from 'react'
import type { Where } from 'payload'
import { ProductCard } from '@/components/shop/ProductCard'
import { ShopSearch } from '@/components/shop/ShopSearch'
import { getPayloadClient } from '@/lib/payload'
import { productCardProps } from '@/lib/products'

export const dynamic = 'force-dynamic'

export const metadata = {
  title: 'فروشگاه',
  description: 'خرید ظروف یکبار مصرف گیاهی آملون — فروشگاه موثقی',
}

type Props = {
  searchParams: Promise<{ q?: string }>
}

export default async function ShopPage({ searchParams }: Props) {
  const { q } = await searchParams
  const query = q?.trim() ?? ''

  let products: ReturnType<typeof productCardProps>[] = []
  let categories: { slug: string; name: string; productCount?: number | null }[] = []

  try {
    const payload = await getPayloadClient()
    const productWhere = (query
      ? {
          and: [
            { status: { equals: 'published' as const } },
            { name: { contains: query } },
          ],
        }
      : { status: { equals: 'published' as const } }) as Where

    const [productResult, categoryResult] = await Promise.all([
      payload.find({
        collection: 'products',
        where: productWhere,
        limit: 48,
        sort: 'name',
        depth: 1,
      }),
      payload.find({
        collection: 'categories',
        limit: 50,
        sort: 'sortOrder',
      }),
    ])
    products = productResult.docs.map(productCardProps)
    categories = categoryResult.docs.map((c) => ({
      slug: c.slug,
      name: c.name,
      productCount: c.productCount,
    }))
  } catch {
    // graceful empty state
  }

  return (
    <main className="mx-auto max-w-6xl px-4 py-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-brand-ink">فروشگاه</h1>
          <p className="mt-2 text-brand-muted">ظروف یکبار مصرف گیاهی آملون</p>
        </div>
        <Suspense fallback={null}>
          <ShopSearch />
        </Suspense>
      </div>

      {query && (
        <p className="mt-4 text-sm text-brand-muted">
          نتایج جستجو برای «{query}» — {products.length} محصول
          {products.length > 0 && (
            <>
              {' '}
              ·{' '}
              <Link href="/shop" className="text-brand-green hover:underline">
                پاک کردن
              </Link>
            </>
          )}
        </p>
      )}

      {categories.length > 0 && !query && (
        <div className="mt-6 flex flex-wrap gap-2">
          {categories.map((cat) => (
            <Link
              key={cat.slug}
              href={`/shop/${cat.slug}`}
              className="rounded-full border border-border bg-white px-4 py-1.5 text-sm hover:border-brand-green hover:text-brand-green"
            >
              {cat.name}
              {cat.productCount != null && (
                <span className="ms-1 text-brand-muted">({cat.productCount})</span>
              )}
            </Link>
          ))}
        </div>
      )}

      <div className="mt-8 grid grid-cols-2 divide-x divide-y divide-border overflow-hidden rounded-xl border border-border md:grid-cols-3 lg:grid-cols-4">
        {products.length > 0 ? (
          products.map((p) => <ProductCard key={p.slug} {...p} />)
        ) : (
          <p className="col-span-full rounded-lg border border-dashed border-border bg-white p-8 text-center text-brand-muted">
            {query ? 'محصولی با این نام یافت نشد.' : 'محصولی یافت نشد.'}
          </p>
        )}
      </div>

      <p className="mt-6 text-center text-sm">
        <Link href="/pricing" className="text-brand-green hover:underline">
          مشاهده لیست قیمت کامل
        </Link>
      </p>
    </main>
  )
}
