import { withPayload } from '@payloadcms/next/withPayload'
import type { NextConfig } from 'next'
import path from 'path'
import { fileURLToPath } from 'url'

const __filename = fileURLToPath(import.meta.url)
const dirname = path.dirname(__filename)

const nextConfig: NextConfig = {
  images: {
    formats: ['image/avif', 'image/webp'],
    localPatterns: [
      {
        pathname: '/api/media/file/**',
      },
    ],
  },
  async redirects() {
    return [
      { source: '/blog', destination: '/mag', permanent: true },
      { source: '/blog/:slug', destination: '/mag/:slug', permanent: true },
      {
        source: '/%d8%aa%d9%85%d8%a7%d8%b3-%d8%a8%d8%a7%d9%85%d8%a7',
        destination: '/contact',
        permanent: true,
      },
      {
        source: '/%d8%af%d8%b1%d8%a8%d8%a7%d8%b1%d9%87-%d9%85%d8%a7',
        destination: '/about',
        permanent: true,
      },
      {
        source: '/%d9%82%db%8c%d9%85%d8%aa-%d8%a2%d9%86%d9%84%d8%a7%db%8c%d9%86-%d9%85%d8%ad%d8%b5%d9%88%d9%84%d8%a7%d8%aa-%d8%a2%d9%85%d9%84%d9%88%d9%86',
        destination: '/pricing',
        permanent: true,
      },
    ]
  },
  async headers() {
    return [
      {
        source: '/(.*)',
        headers: [
          { key: 'X-Frame-Options', value: 'DENY' },
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
          { key: 'Permissions-Policy', value: 'camera=(), microphone=(), geolocation=()' },
        ],
      },
    ]
  },
  webpack: (webpackConfig) => {
    webpackConfig.resolve.extensionAlias = {
      '.cjs': ['.cts', '.cjs'],
      '.js': ['.ts', '.tsx', '.js', '.jsx'],
      '.mjs': ['.mts', '.mjs'],
    }

    return webpackConfig
  },
  turbopack: {
    root: path.resolve(dirname),
  },
}

export default withPayload(nextConfig, { devBundleServerPackages: false })
