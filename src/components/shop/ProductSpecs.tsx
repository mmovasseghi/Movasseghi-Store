type ProductSpecsProps = {
  attributes?: {
    material?: string | null
    capacityMl?: number | null
    packSize?: string | null
    heatResistanceC?: number | null
  } | null
  sku?: string | null
}

const LABELS: Record<string, string> = {
  material: 'جنس',
  capacityMl: 'ظرفیت',
  packSize: 'بسته‌بندی',
  heatResistanceC: 'مقاومت حرارتی',
}

export function ProductSpecs({ attributes, sku }: ProductSpecsProps) {
  const rows: { label: string; value: string }[] = []

  if (sku) rows.push({ label: 'کد محصول', value: sku })
  if (attributes?.material) rows.push({ label: LABELS.material, value: attributes.material })
  if (attributes?.capacityMl)
    rows.push({ label: LABELS.capacityMl, value: `${attributes.capacityMl} ml` })
  if (attributes?.packSize) rows.push({ label: LABELS.packSize, value: attributes.packSize })
  if (attributes?.heatResistanceC)
    rows.push({ label: LABELS.heatResistanceC, value: `تا ${attributes.heatResistanceC}°C` })

  if (!rows.length) return null

  return (
    <div className="rounded-xl border border-border bg-white p-4">
      <h2 className="mb-3 text-sm font-semibold text-brand-ink">مشخصات کلیدی</h2>
      <dl className="divide-y divide-border">
        {rows.map((row) => (
          <div key={row.label} className="flex justify-between gap-4 py-2.5 text-sm">
            <dt className="text-brand-muted">{row.label}</dt>
            <dd className="font-medium text-brand-ink">{row.value}</dd>
          </div>
        ))}
      </dl>
    </div>
  )
}
