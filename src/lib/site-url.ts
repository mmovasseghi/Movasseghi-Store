/** Site URL helpers for SEO canonicals and environment detection. */

export function getSiteUrl(): string {
  return (process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000').replace(/\/$/, '')
}

export function canonicalUrl(path: string): string {
  const normalized = path.startsWith('/') ? path : `/${path}`
  return `${getSiteUrl()}${normalized}`
}

/** Staging / IP-only hosts must not be indexed. */
export function isStagingEnvironment(): boolean {
  if (process.env.STAGING === 'true') return true
  const host = getSiteUrl().replace(/^https?:\/\//, '').split(':')[0] ?? ''
  return /^\d{1,3}(\.\d{1,3}){3}$/.test(host) || host.endsWith('.local')
}

export function stagingRobots(): { index: false; follow: false } {
  return { index: false, follow: false }
}
