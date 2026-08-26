import Link from 'next/link'
import { Suspense } from 'react'
import { ProductCard } from '@/components/shop/ProductCard'
import { ShopSearch } from '@/components/shop/ShopSearch'
import { ShopToolbar } from '@/components/shop/ShopToolbar'
import { getPayloadClient } from '@/lib/payload'
import { persianSearchMatch } from '@/lib/persian-search'
import {
  isOnSale,
  productCardProps,
  sortProductCards,
  type ProductCardData,
} from '@/lib/products'
import {
  extractFilterOptions,
  hasActiveShopFilters,
  matchesShopFilters,
} from '@/lib/shop-filters'
import { canonicalUrl } from '@/lib/site-url'

export const dynamic = 'force-dynamic'

export const metadata = {
  title: 'فروشگاه',
  description: 'خرید ظروف یکبار مصرف گیاهی آملون — فروشگاه موثقی',
  alternates: { canonical: canonicalUrl('/shop') },
}

type Props = {
  searchParams: Promise<{ q?: string; sort?: string; sale?: string; material?: string; pack?: string }>
}

export default async function ShopPage({ searchParams }: Props) {
  const { q, sort = 'name', sale, material, pack } = await searchParams
  const query = q?.trim() ?? ''
  const saleOnly = sale === '1'
  const materialFilter = material?.trim() ?? ''
  const packFilter = pack?.trim() ?? ''
  const sortKey = sort === 'price-asc' || sort === 'price-desc' ? sort : 'name'
  const filtersActive = hasActiveShopFilters({ q: query, sale, material: materialFilter, pack: packFilter })

  let products: ProductCardData[] = []
  let categories: { slug: string; name: string; productCount?: number | null }[] = []
  let filterOptions = { materials: [] as string[], packSizes: [] as string[] }

  try {
    const payload = await getPayloadClient()
    const needsWideFetch = Boolean(query || saleOnly || materialFilter || packFilter)

    const [productResult, categoryResult] = await Promise.all([
      payload.find({
        collection: 'products',
        where: { status: { equals: 'published' } },
        limit: needsWideFetch ? 200 : 200,
        sort: 'name',
        depth: 1,
      }),
      payload.find({
        collection: 'categories',
        limit: 50,
        sort: 'sortOrder',
      }),
    ])

    filterOptions = extractFilterOptions(productResult.docs)

    let filtered = productResult.docs
    if (query) {
      filtered = filtered.filter(
        (p) =>
          persianSearchMatch(p.name, query) ||
          (p.sku ? persianSearchMatch(p.sku, query) : false),
      )
    }
    if (saleOnly) {
      filtered = filtered.filter((p) => isOnSale(p))
    }
    if (materialFilter || packFilter) {
      filtered = filtered.filter((p) =>
        matchesShopFilters(p, materialFilter || undefined, packFilter || undefined),
      )
    }

    products = sortProductCards(filtered.map(productCardProps), sortKey).slice(0, 48)
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

      <Suspense fallback={null}>
        <ShopToolbar filterOptions={filterOptions} />
      </Suspense>

      {filtersActive && (
        <p className="mt-4 text-sm text-brand-muted">
          {query && <>نتایج «{query}» — </>}
          {saleOnly && <>فقط تخفیف‌دار — </>}
          {materialFilter && <>جنس: {materialFilter} — </>}
          {packFilter && <>بسته: {packFilter} — </>}
          {products.length} محصول
          {' · '}
          <Link href="/shop" className="text-brand-green hover:underline">
            پاک کردن فیلتر
          </Link>
        </p>
      )}

      {categories.length > 0 && !filtersActive && (
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
            {query || saleOnly || materialFilter || packFilter ? 'محصولی یافت نشد.' : 'محصولی یافت نشد.'}
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
