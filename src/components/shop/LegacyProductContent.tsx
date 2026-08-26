type Props = {
  html: string
}

/** Render verbatim legacy product HTML (trusted migration source). Strips scripts only. */
export function LegacyProductContent({ html }: Props) {
  const normalized = html.replace(/rn/g, '\n').replace(/<script[\s\S]*?<\/script>/gi, '')

  return (
    <article className="legacy-product-content" dangerouslySetInnerHTML={{ __html: normalized }} />
  )
}
