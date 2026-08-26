import { notFound } from 'next/navigation'
import { ProductGallery } from '@/components/shop/ProductGallery'
import { formatIrt } from '@/commerce/cart'
import { getPayloadClient } from '@/lib/payload'
import { productDisplayPrice, productGalleryImages } from '@/lib/products'
import { formatPrice } from '@/lib/utils'

export const dynamic = 'force-dynamic'

type Props = {
  params: Promise<{ slug: string }>
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

    return (
      <main className="mx-auto max-w-6xl px-4 py-8">
        <div className="grid gap-8 md:grid-cols-2">
          {galleryImages.length > 0 ? (
            <ProductGallery name={product.name} images={galleryImages} />
          ) : (
            <div className="relative flex aspect-square items-center justify-center rounded-lg border border-dashed border-border bg-brand-aqua-pale text-brand-muted">
              تصویر محصول — در حال انتقال از آرشیو legacy
            </div>
          )}
          <div>
            <h1 className="text-2xl font-bold text-brand-ink">{product.name}</h1>
            {product.sku && <p className="mt-1 text-sm text-brand-muted">کد: {product.sku}</p>}
            <p className="mt-4 text-2xl font-bold tabular-nums text-brand-green">
              {price > 0 ? formatIrt(price) : 'تماس برای قیمت'}
            </p>
            {product.salePrice && product.salePrice < product.regularPrice && (
              <p className="text-sm text-brand-muted line-through tabular-nums">
                {formatPrice(product.regularPrice)} تومان
              </p>
            )}
            {product.shortDescription && (
              <p className="mt-4 text-brand-muted">{product.shortDescription}</p>
            )}
            <div className="mt-6 flex flex-wrap gap-3">
              <button
                type="button"
                className="rounded-lg bg-brand-green px-6 py-3 font-medium text-white hover:bg-brand-green-light"
              >
                افزودن به سبد — به‌زودی
              </button>
              <a
                href="tel:09125199105"
                className="rounded-lg border border-brand-green px-6 py-3 font-medium text-brand-green"
              >
                تماس برای سفارش
              </a>
            </div>
            {product.attributes && (
              <dl className="mt-8 grid grid-cols-2 gap-3 text-sm">
                {product.attributes.material && (
                  <>
                    <dt className="text-brand-muted">جنس</dt>
                    <dd>{product.attributes.material}</dd>
                  </>
                )}
                {product.attributes.capacityMl && (
                  <>
                    <dt className="text-brand-muted">ظرفیت</dt>
                    <dd>{product.attributes.capacityMl} ml</dd>
                  </>
                )}
              </dl>
            )}
          </div>
        </div>
      </main>
    )
  } catch {
    notFound()
  }
}
