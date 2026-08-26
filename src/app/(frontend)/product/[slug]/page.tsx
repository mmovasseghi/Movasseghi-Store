import { notFound } from 'next/navigation'
import { LegacyProductContent } from '@/components/shop/LegacyProductContent'
import { ProductActions } from '@/components/shop/ProductActions'
import { ProductCard } from '@/components/shop/ProductCard'
import { ProductGallery } from '@/components/shop/ProductGallery'
import { ProductSpecs } from '@/components/shop/ProductSpecs'
import { ProductStickyBar } from '@/components/shop/ProductStickyBar'
import { ProductViewTracker } from '@/components/shop/ProductViewTracker'
import { Breadcrumbs } from '@/components/ui/Breadcrumbs'
import { productJsonLd } from '@/lib/jsonld'
import { getPayloadClient } from '@/lib/payload'
import { canonicalUrl } from '@/lib/site-url'
import {
  mediaUrl,
  productCardProps,
  productDisplayPrice,
  productGalleryImages,
} from '@/lib/products'
import type { Category, Product } from '@/payload-types'

export const dynamic = 'force-dynamic'

type Props = {
  params: Promise<{ slug: string }>
}

function categoryRef(cat: number | Category): Category | null {
  return typeof cat === 'object' ? cat : null
}

async function getRelatedProducts(
  product: Product,
  payload: Awaited<ReturnType<typeof getPayloadClient>>,
) {
  const firstCat = product.categories?.[0]
  const catId = typeof firstCat === 'object' ? firstCat?.id : firstCat
  if (!catId) return []

  const { docs } = await payload.find({
    collection: 'products',
    where: {
      and: [
        { status: { equals: 'published' } },
        { id: { not_equals: product.id } },
        { categories: { contains: catId } },
      ],
    },
    limit: 4,
    depth: 1,
  })
  return docs.map(productCardProps)
}

export async function generateMetadata({ params }: Props) {
  const { slug } = await params
  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'products',
      where: { slug: { equals: slug } },
      limit: 1,
    })
    const product = docs[0]
    if (!product) return { title: 'محصول' }
    return {
      title: product.seo?.title ?? product.name,
      description: product.seo?.description ?? product.shortDescription,
      alternates: { canonical: canonicalUrl(`/product/${product.slug}`) },
    }
  } catch {
    return { title: 'محصول' }
  }
}

export default async function ProductPage({ params }: Props) {
  const { slug } = await params

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'products',
      where: { slug: { equals: slug } },
      limit: 1,
      depth: 2,
    })
    const product = docs[0]
    if (!product || product.status !== 'published') notFound()

    const price = productDisplayPrice(product)
    const galleryImages = productGalleryImages(product)
    const featuredUrl = mediaUrl(product.featuredImage)
    const related = await getRelatedProducts(product, payload)

    const primaryCategory = product.categories?.map(categoryRef).find(Boolean)
    const breadcrumbItems = [
      { label: 'خانه', href: '/' },
      { label: 'فروشگاه', href: '/shop' },
      ...(primaryCategory
        ? [{ label: primaryCategory.name, href: `/shop/${primaryCategory.slug}` }]
        : []),
      { label: product.name },
    ]

    const jsonLd = productJsonLd(product)

    return (
      <>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
        <ProductViewTracker productId={String(product.id)} name={product.name} price={price} />
        <main className="mx-auto max-w-6xl px-4 pb-28 pt-6 md:pb-12 md:pt-8">
          <Breadcrumbs items={breadcrumbItems} className="mb-6" />

          <div className="grid gap-8 lg:grid-cols-2 lg:gap-12">
            <div>
              {galleryImages.length > 0 ? (
                <ProductGallery name={product.name} images={galleryImages} />
              ) : (
                <div className="flex aspect-square items-center justify-center rounded-xl border border-dashed border-border bg-brand-aqua-pale/50 text-brand-muted">
                  تصویر محصول در آرشیو legacy
                </div>
              )}
            </div>

            <div className="lg:sticky lg:top-20 lg:self-start">
              <div className="mb-2 flex flex-wrap gap-2">
                <span className="rounded-full bg-brand-aqua-pale px-3 py-0.5 text-xs font-medium text-brand-green">
                  گیاهی · آملون
                </span>
                {product.b2b?.b2bOnly && (
                  <span className="rounded-full bg-brand-ink/5 px-3 py-0.5 text-xs text-brand-muted">
                    عمده‌فروشی
                  </span>
                )}
              </div>
              <h1 className="text-2xl font-bold leading-tight text-brand-ink md:text-3xl">
                {product.name}
              </h1>
              {product.shortDescription && (
                <p className="mt-3 text-brand-muted leading-relaxed">{product.shortDescription}</p>
              )}

              <div className="mt-6">
                <ProductActions
                  productId={String(product.id)}
                  slug={product.slug}
                  name={product.name}
                  price={price}
                  regularPrice={product.regularPrice}
                  salePrice={product.salePrice}
                  imageUrl={featuredUrl}
                  stockQuantity={product.stockQuantity}
                />
              </div>

              <div className="mt-6">
                <ProductSpecs attributes={product.attributes} sku={product.sku} />
              </div>
            </div>
          </div>

          {product.legacyDescriptionHtml && (
            <section className="mt-12 border-t border-border pt-10 md:mt-16">
              <h2 className="mb-6 text-xl font-bold text-brand-ink">توضیحات کامل محصول</h2>
              <div className="rounded-xl border border-border bg-white p-4 md:p-8">
                <LegacyProductContent html={product.legacyDescriptionHtml} />
              </div>
            </section>
          )}

          {related.length > 0 && (
            <section className="mt-12 border-t border-border pt-10">
              <h2 className="mb-6 text-xl font-bold text-brand-ink">محصولات مرتبط</h2>
              <div className="grid grid-cols-2 gap-0 overflow-hidden rounded-xl border border-border sm:grid-cols-4">
                {related.map((p) => (
                  <ProductCard key={p.slug} {...p} variant="grid" />
                ))}
              </div>
            </section>
          )}

          <ProductStickyBar
            name={product.name}
            price={price}
            productId={String(product.id)}
            slug={product.slug}
            imageUrl={featuredUrl}
          />
        </main>
      </>
    )
  } catch {
    notFound()
  }
}
