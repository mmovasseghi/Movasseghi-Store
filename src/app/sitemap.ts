import { getPayloadClient } from '@/lib/payload'

export default async function sitemap() {
  const base = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000'

  const staticRoutes = ['', '/shop', '/b2b', '/about', '/cart'].map((path) => ({
    url: `${base}${path}`,
    lastModified: new Date(),
    changeFrequency: 'weekly' as const,
    priority: path === '' ? 1 : 0.8,
  }))

  try {
    const payload = await getPayloadClient()
    const { docs: products } = await payload.find({
      collection: 'products',
      where: { status: { equals: 'published' } },
      limit: 200,
    })
    const { docs: categories } = await payload.find({
      collection: 'categories',
      limit: 50,
    })

    return [
      ...staticRoutes,
      ...categories.map((c) => ({
        url: `${base}/shop/${c.slug}`,
        lastModified: new Date(),
        changeFrequency: 'weekly' as const,
        priority: 0.7,
      })),
      ...products.map((p) => ({
        url: `${base}/product/${p.slug}`,
        lastModified: new Date(),
        changeFrequency: 'weekly' as const,
        priority: 0.9,
      })),
    ]
  } catch {
    return staticRoutes
  }
}
