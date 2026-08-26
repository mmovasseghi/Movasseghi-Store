import { getPayloadClient } from '@/lib/payload'
import { mediaUrl, productDisplayPrice } from '@/lib/products'
import { getSiteUrl } from '@/lib/site-url'
import type { Product } from '@/payload-types'

function escapeXml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function productItem(product: Product, base: string): string {
  const price = productDisplayPrice(product)
  const image =
    typeof product.featuredImage === 'object' && product.featuredImage
      ? mediaUrl(product.featuredImage)
      : null
  const imageUrl = image ? (image.startsWith('http') ? image : `${base}${image}`) : ''
  const link = `${base}/product/${product.slug}`
  const desc =
    product.seo?.description ?? product.shortDescription ?? product.name
  const availability =
    product.stockQuantity != null && product.stockQuantity > 0 ? 'in_stock' : 'preorder'

  return `<item>
  <g:id>${escapeXml(String(product.id))}</g:id>
  <g:title>${escapeXml(product.name)}</g:title>
  <g:description>${escapeXml(desc.slice(0, 5000))}</g:description>
  <g:link>${escapeXml(link)}</g:link>
  ${imageUrl ? `<g:image_link>${escapeXml(imageUrl)}</g:image_link>` : ''}
  ${price > 0 ? `<g:price>${price} IRR</g:price>` : ''}
  <g:availability>${availability}</g:availability>
  <g:condition>new</g:condition>
  <g:brand>موثقی</g:brand>
  ${product.sku ? `<g:mpn>${escapeXml(product.sku)}</g:mpn>` : ''}
</item>`
}

/** Google Merchant Center RSS 2.0 product feed */
export async function GET() {
  const base = getSiteUrl()
  let items = ''

  try {
    const payload = await getPayloadClient()
    const { docs } = await payload.find({
      collection: 'products',
      where: { status: { equals: 'published' } },
      limit: 200,
      depth: 1,
    })
    items = docs.map((p) => productItem(p, base)).join('\n')
  } catch {
    items = ''
  }

  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:g="http://base.google.com/ns/1.0">
  <channel>
    <title>فروشگاه موثقی</title>
    <link>${base}</link>
    <description>ظروف یکبار مصرف گیاهی آملون</description>
    ${items}
  </channel>
</rss>`

  return new Response(xml, {
    headers: {
      'Content-Type': 'application/xml; charset=utf-8',
      'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
    },
  })
}
