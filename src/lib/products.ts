export type ProductRecord = {
  slug: string
  name: string
  regularPrice: number
  salePrice?: number | null
  featuredImage?: { url?: string | null } | number | null
  status?: string
  sku?: string | null
  shortDescription?: string | null
  seo?: { title?: string | null; description?: string | null } | null
  attributes?: {
    material?: string | null
    capacityMl?: number | null
  } | null
}

export type MediaRecord = {
  url?: string | null
}

export function mediaUrl(media: number | MediaRecord | null | undefined): string | null {
  if (!media || typeof media === 'number') return null
  return media.url ?? null
}

export function productDisplayPrice(
  product: Pick<ProductRecord, 'regularPrice' | 'salePrice'>,
): number {
  if (product.salePrice && product.salePrice > 0 && product.salePrice < product.regularPrice) {
    return product.salePrice
  }
  return product.regularPrice
}

export function productCardProps(product: ProductRecord) {
  const image =
    typeof product.featuredImage === 'object' && product.featuredImage
      ? mediaUrl(product.featuredImage)
      : null

  return {
    slug: product.slug,
    name: product.name,
    price: product.regularPrice,
    salePrice: product.salePrice,
    imageUrl: image,
  }
}
