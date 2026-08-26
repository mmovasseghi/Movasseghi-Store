type Props = {
  html: string
}

/** Render verbatim legacy product HTML (trusted migration source). Strips scripts only. */
export function LegacyProductContent({ html }: Props) {
  const normalized = html.replace(/rn/g, '\n').replace(/<script[\s\S]*?<\/script>/gi, '')

  return (
    <article
      className="prose prose-neutral max-w-none prose-headings:text-brand-ink prose-p:text-brand-muted prose-a:text-brand-green rtl:prose-p:text-right"
      // Legacy HTML is the authoritative product content from WordPress export
      dangerouslySetInnerHTML={{ __html: normalized }}
    />
  )
}
