import type { MetadataRoute } from 'next'
import { getSiteUrl, isStagingEnvironment } from '@/lib/site-url'

export default function robots(): MetadataRoute.Robots {
  if (isStagingEnvironment()) {
    return {
      rules: { userAgent: '*', disallow: '/' },
    }
  }

  return {
    rules: { userAgent: '*', allow: '/' },
    sitemap: `${getSiteUrl()}/sitemap.xml`,
  }
}
