import Link from 'next/link'
import { notFound } from 'next/navigation'
import { ProductCard } from '@/components/shop/ProductCard'
import { getPayloadClient } from '@/lib/payload'
import { productCardProps } from '@/lib/products'

type Props = {
  params: Promise<{ category: string }>
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
      title: cat.seo?.title ?? cat.name,
      description: cat.seo?.description ?? cat.description,
    }
  } catch {
    return { title: 'دسته‌بندی' }
  }
}

export default async function CategoryPage({ params }: Props) {
  const { category: slug } = await params
  let categoryName = slug
  let products: ReturnType<typeof productCardProps>[] = []

  try {
    const payload = await getPayloadClient()
    const { docs: cats } = await payload.find({
      collection: 'categories',
      where: { slug: { equals: slug } },
      limit: 1,
    })
    const category = cats[0]
    if (!category) notFound()
    categoryName = category.name

    const { docs } = await payload.find({
      collection: 'products',
      where: {
        and: [{ status: { equals: 'published' } }, { categories: { contains: category.id } }],
      },
      limit: 48,
      depth: 1,
    })
    products = docs.map(productCardProps)
  } catch {
    notFound()
  }

  return (
    <main className="mx-auto max-w-6xl px-4 py-8">
      <nav className="mb-4 text-sm text-brand-muted">
        <Link href="/shop" className="hover:text-brand-green">
          فروشگاه
        </Link>
        <span className="mx-2">/</span>
        <span>{categoryName}</span>
      </nav>
      <h1 className="text-2xl font-bold text-brand-ink">{categoryName}</h1>
      <div className="mt-8 grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
        {products.map((p) => (
          <ProductCard key={p.slug} {...p} />
        ))}
      </div>
      {products.length === 0 && (
        <p className="mt-8 text-center text-brand-muted">محصولی در این دسته ثبت نشده است.</p>
      )}
    </main>
  )
}
