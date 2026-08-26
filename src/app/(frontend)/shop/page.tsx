import Link from 'next/link'
import { ProductCard } from '@/components/shop/ProductCard'
import { getPayloadClient } from '@/lib/payload'
import { productCardProps } from '@/lib/products'

export const dynamic = 'force-dynamic'

export const metadata = {
  title: 'فروشگاه',
  description: 'خرید ظروف یکبار مصرف گیاهی آملون — فروشگاه موثقی',
}

export default async function ShopPage() {
  let products: ReturnType<typeof productCardProps>[] = []
  let categories: { slug: string; name: string; productCount?: number | null }[] = []

  try {
    const payload = await getPayloadClient()
    const [productResult, categoryResult] = await Promise.all([
      payload.find({
        collection: 'products',
        where: { status: { equals: 'published' } },
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
      <h1 className="text-2xl font-bold text-brand-ink">فروشگاه</h1>
      <p className="mt-2 text-brand-muted">ظروف یکبار مصرف گیاهی آملون</p>

      {categories.length > 0 && (
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

      <div className="mt-8 grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
        {products.length > 0 ? (
          products.map((p) => <ProductCard key={p.slug} {...p} />)
        ) : (
          <p className="col-span-full rounded-lg border border-dashed border-border bg-white p-8 text-center text-brand-muted">
            محصولی یافت نشد. دیتابیس را راه‌اندازی کنید یا import را اجرا کنید.
          </p>
        )}
      </div>
    </main>
  )
}
