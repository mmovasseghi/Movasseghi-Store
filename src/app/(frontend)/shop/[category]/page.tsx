import Link from 'next/link'
import { Suspense } from 'react'
import { notFound } from 'next/navigation'
import { ProductCard } from '@/components/shop/ProductCard'
import { ShopToolbar } from '@/components/shop/ShopToolbar'
import { getPayloadClient } from '@/lib/payload'
import { categoryJsonLd } from '@/lib/jsonld'
import { productCardProps, sortProductCards } from '@/lib/products'
import {
  extractFilterOptions,
  filterPublishedProducts,
  hasActiveShopFilters,
} from '@/lib/shop-filters'
import { canonicalUrl } from '@/lib/site-url'
import type { Category } from '@/payload-types'

export const dynamic = 'force-dynamic'

type Props = {
  params: Promise<{ category: string }>
  searchParams: Promise<{ sort?: string; sale?: string; material?: string; pack?: string }>
}

export async function generateMetadata({ params }: Props) {
  const { category: slug } = await params
  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'categories',
      where: { slug: { equals: slug } },
      limit: 1,
    })
    const cat = docs[0]
    if (!cat) return { title: 'دسته‌بندی' }
    return {
      title: cat.seo?.title ?? `${cat.name} | فروشگاه موثقی`,
      description: cat.seo?.description ?? cat.description?.replace(/rn/g, ' ').slice(0, 160),
      alternates: { canonical: canonicalUrl(`/shop/${cat.slug}`) },
    }
  } catch {
    return { title: 'دسته‌بندی' }
  }
}

export default async function CategoryPage({ params, searchParams }: Props) {
  const { category: slug } = await params
  const { sort = 'name', sale, material, pack } = await searchParams
  const sortKey = sort === 'price-asc' || sort === 'price-desc' ? sort : 'name'
  const materialFilter = material?.trim() ?? ''
  const packFilter = pack?.trim() ?? ''
  const filtersActive = hasActiveShopFilters({ sale, material: materialFilter, pack: packFilter })
  const categoryPath = `/shop/${slug}`

  let category: Category | null = null
  let childCategories: { slug: string; name: string }[] = []
  let products: ReturnType<typeof productCardProps>[] = []
  let filterOptions = { materials: [] as string[], packSizes: [] as string[] }

  try {
    const payload = await getPayloadClient()
    const { docs: cats } = await payload.find({
      collection: 'categories',
      where: { slug: { equals: slug } },
      limit: 1,
      depth: 1,
    })
    category = cats[0] ?? null
    if (!category) notFound()

    const { docs: children } = await payload.find({
      collection: 'categories',
      where: { parent: { equals: category.id } },
      limit: 20,
      sort: 'sortOrder',
    })
    childCategories = children.map((c) => ({ slug: c.slug, name: c.name }))

    const { docs } = await payload.find({
      collection: 'products',
      where: {
        and: [{ status: { equals: 'published' } }, { categories: { contains: category.id } }],
      },
      limit: 200,
      depth: 1,
    })
    filterOptions = extractFilterOptions(docs)
    const filtered = filterPublishedProducts(docs, { sale, material: materialFilter, pack: packFilter })
    products = sortProductCards(filtered.map(productCardProps), sortKey)
  } catch {
    notFound()
  }

  const categoryDescription = category.description
  const jsonLd = categoryJsonLd(category, products)

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <main className="mx-auto max-w-6xl px-4 py-8">
        <nav className="mb-4 text-sm text-brand-muted">
          <Link href="/shop" className="hover:text-brand-green">
            فروشگاه
          </Link>
          <span className="mx-2">/</span>
          <span>{category.name}</span>
        </nav>
        <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">{category.name}</h1>
        {categoryDescription && (
          <div className="mt-4 max-w-3xl leading-relaxed text-brand-muted whitespace-pre-line">
            {categoryDescription.replace(/rn/g, '\n')}
          </div>
        )}

        {childCategories.length > 0 && (
          <div className="mt-6 flex flex-wrap gap-2">
            {childCategories.map((c) => (
              <Link
                key={c.slug}
                href={`/shop/${c.slug}`}
                className="rounded-full border border-border bg-white px-4 py-1.5 text-sm hover:border-brand-green hover:text-brand-green"
              >
                {c.name}
              </Link>
            ))}
          </div>
        )}

        <Suspense fallback={null}>
          <ShopToolbar filterOptions={filterOptions} basePath={categoryPath} />
        </Suspense>

        {filtersActive && (
          <p className="mt-4 text-sm text-brand-muted">
            {sale === '1' && <>فقط تخفیف‌دار — </>}
            {materialFilter && <>جنس: {materialFilter} — </>}
            {packFilter && <>بسته: {packFilter} — </>}
            {products.length} محصول ·{' '}
            <Link href={categoryPath} className="text-brand-green hover:underline">
              پاک کردن فیلتر
            </Link>
          </p>
        )}

        <div className="mt-8 grid grid-cols-2 divide-x divide-y divide-border overflow-hidden rounded-xl border border-border md:grid-cols-3 lg:grid-cols-4">
          {products.map((p) => (
            <ProductCard key={p.slug} {...p} />
          ))}
        </div>
        {products.length === 0 && (
          <p className="mt-8 text-center text-brand-muted">محصولی در این دسته ثبت نشده است.</p>
        )}

        <p className="mt-8 text-center text-sm">
          <Link href="/pricing" className="text-brand-green hover:underline">
            لیست قیمت این دسته
          </Link>
        </p>
      </main>
    </>
  )
}
