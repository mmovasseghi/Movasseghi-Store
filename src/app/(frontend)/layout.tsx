import type { Metadata } from 'next'
import { Vazirmatn } from 'next/font/google'
import { Footer } from '@/components/layout/Footer'
import { Header } from '@/components/layout/Header'
import { CartProvider } from '@/components/shop/CartProvider'
import { organizationJsonLd } from '@/lib/jsonld'
import './globals.css'

const vazirmatn = Vazirmatn({
  subsets: ['arabic'],
  variable: '--font-vazirmatn',
  display: 'swap',
})

export const metadata: Metadata = {
  title: {
    default: 'فروشگاه موثقی | ظروف یکبار مصرف گیاهی آملون',
    template: '%s | فروشگاه موثقی',
  },
  description:
    'خرید ظروف یکبار مصرف گیاهی آملون — مناسب رستوران، کافه و فست‌فود. ارسال سراسر ایران.',
  metadataBase: new URL(process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000'),
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const orgLd = organizationJsonLd()
  return (
    <html lang="fa" dir="rtl" className={vazirmatn.variable}>
      <head>
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(orgLd) }}
        />
      </head>
      <body className="flex min-h-screen flex-col">
        <CartProvider>
          <Header />
          <div className="flex-1">{children}</div>
          <Footer />
        </CartProvider>
      </body>
    </html>
  )
}
