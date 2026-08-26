import { productDisplayPrice, productGalleryImages } from '@/lib/products'
import type { Category, Product } from '@/payload-types'

const SITE = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000'

export function organizationJsonLd() {
  return {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    name: 'فروشگاه موثقی',
    url: SITE,
    telephone: '+989125199105',
    sameAs: ['https://instagram.com/movasseghiStore'],
  }
}

export function categoryJsonLd(
  category: Category,
  products: { slug: string; name: string }[],
) {
  return {
    '@context': 'https://schema.org',
    '@type': 'CollectionPage',
    name: category.name,
    description: category.seo?.description ?? category.description,
    url: `${SITE}/shop/${category.slug}`,
    mainEntity: {
      '@type': 'ItemList',
      itemListElement: products.slice(0, 20).map((p, i) => ({
        '@type': 'ListItem',
        position: i + 1,
        url: `${SITE}/product/${p.slug}`,
        name: p.name,
      })),
    },
  }
}

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
