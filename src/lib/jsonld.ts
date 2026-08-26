import { productDisplayPrice, productGalleryImages } from '@/lib/products'
import type { Product } from '@/payload-types'

const SITE = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000'

export function productJsonLd(product: Product) {
  const price = productDisplayPrice(product)
  const images = productGalleryImages(product).map((i) =>
    i.url.startsWith('http') ? i.url : `${SITE}${i.url}`,
  )

  return {
    '@context': 'https://schema.org',
    '@type': 'Product',
    name: product.name,
    description: product.seo?.description ?? product.shortDescription ?? product.name,
    sku: product.sku ?? undefined,
    image: images.length ? images : undefined,
    offers: {
      '@type': 'Offer',
      url: `${SITE}/product/${product.slug}`,
      priceCurrency: 'IRR',
      price: price > 0 ? price : undefined,
      availability:
        product.stockQuantity && product.stockQuantity > 0
          ? 'https://schema.org/InStock'
          : 'https://schema.org/PreOrder',
      seller: {
        '@type': 'Organization',
        name: 'فروشگاه موثقی',
      },
    },
  }
}
