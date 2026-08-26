export type ProductRecord = {
  slug: string
  name: string
  regularPrice: number
  salePrice?: number | null
  featuredImage?: { url?: string | null; alt?: string | null } | number | null
  status?: string
  sku?: string | null
  shortDescription?: string | null
  stockQuantity?: number | null
  seo?: { title?: string | null; description?: string | null } | null
  attributes?: {
    material?: string | null
    capacityMl?: number | null
    packSize?: string | null
  } | null
  b2b?: {
    wholesalePrice?: number | null
    moq?: number | null
    b2bOnly?: boolean | null
  } | null
}

export type MediaRecord = {
  url?: string | null
  alt?: string | null
}

export function mediaUrl(media: number | MediaRecord | null | undefined): string | null {
  if (!media || typeof media === 'number') return null
  return media.url ?? null
}

export function productGalleryImages(
  product: ProductRecord & {
    gallery?: Array<{ image?: number | MediaRecord | null } | null> | null
  },
): { url: string; alt: string }[] {
  const images: { url: string; alt: string }[] = []
  const featured = mediaUrl(product.featuredImage)
  if (featured) {
    const alt =
      typeof product.featuredImage === 'object' && product.featuredImage?.alt
        ? String(product.featuredImage.alt)
        : product.name
    images.push({ url: featured, alt })
  }
  for (const item of product.gallery ?? []) {
    if (!item?.image) continue
    const url = mediaUrl(item.image)
    if (!url) continue
    const alt =
      typeof item.image === 'object' && item.image.alt ? String(item.image.alt) : product.name
    if (!images.some((i) => i.url === url)) images.push({ url, alt })
  }
  return images
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
    shortDescription: product.shortDescription ?? null,
    price: product.regularPrice,
    salePrice: product.salePrice,
    imageUrl: image,
    packSize: product.attributes?.packSize ?? null,
    wholesalePrice: product.b2b?.wholesalePrice ?? null,
    inStock: (product.stockQuantity ?? 0) > 0,
    hasDiscount:
      !!product.salePrice &&
      product.salePrice > 0 &&
      product.salePrice < product.regularPrice,
  }
}
