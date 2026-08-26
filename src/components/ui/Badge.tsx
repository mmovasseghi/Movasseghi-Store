import { cn } from '@/lib/utils'
import type { HTMLAttributes } from 'react'

type Variant = 'default' | 'sale' | 'b2b' | 'stock'

type Props = HTMLAttributes<HTMLSpanElement> & {
  variant?: Variant
}

const variants: Record<Variant, string> = {
  default: 'bg-brand-aqua-pale text-brand-ink',
  sale: 'bg-brand-green text-white',
  b2b: 'border border-brand-green/30 bg-white text-brand-green',
  stock: 'bg-brand-off-white text-brand-muted',
}

export function Badge({ className, variant = 'default', ...props }: Props) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
        variants[variant],
        className,
      )}
      {...props}
    />
  )
}
