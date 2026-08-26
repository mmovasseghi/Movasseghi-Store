import Link from 'next/link'
import { canonicalUrl } from '@/lib/site-url'

type Props = {
  title: string
  description?: string
  canonicalPath: string
  children: React.ReactNode
}

export function LegalPage({ title, description, canonicalPath, children }: Props) {
  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <nav className="mb-4 text-sm text-brand-muted">
        <Link href="/" className="hover:text-brand-green">
          خانه
        </Link>
        <span className="mx-2">/</span>
        <span>{title}</span>
      </nav>
      <h1 className="text-2xl font-bold text-brand-ink md:text-3xl">{title}</h1>
      {description && <p className="mt-3 text-brand-muted">{description}</p>}
      <div className="prose-legal mt-8 space-y-4 text-sm leading-relaxed text-brand-muted">{children}</div>
    </main>
  )
}

export function legalMetadata(title: string, path: string, description: string) {
  return {
    title,
    description,
    alternates: { canonical: canonicalUrl(path) },
  }
}
