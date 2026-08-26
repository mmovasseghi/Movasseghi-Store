import Image from 'next/image'
import { notFound } from 'next/navigation'
import { formatIrt } from '@/commerce/cart'
import { getPayloadClient } from '@/lib/payload'
import { mediaUrl, productDisplayPrice } from '@/lib/products'
import { formatPrice } from '@/lib/utils'

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
    const imageUrl = mediaUrl(product.featuredImage)

    return (
      <main className="mx-auto max-w-6xl px-4 py-8">
        <div className="grid gap-8 md:grid-cols-2">
          <div className="relative aspect-square overflow-hidden rounded-lg border border-border bg-brand-aqua-pale">
            {imageUrl ? (
              <Image src={imageUrl} alt={product.name} fill className="object-cover" priority />
            ) : (
              <div className="flex h-full items-center justify-center text-brand-muted">
                بدون تصویر
              </div>
            )}
          </div>
          <div>
            <h1 className="text-2xl font-bold text-brand-ink">{product.name}</h1>
            {product.sku && <p className="mt-1 text-sm text-brand-muted">کد: {product.sku}</p>}
            <p className="mt-4 text-2xl font-bold tabular-nums text-brand-green">
              {formatIrt(price)}
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
