import { cn } from '@/lib/utils'
import type { ButtonHTMLAttributes } from 'react'

type Variant = 'primary' | 'secondary' | 'ghost' | 'inverse'

type Props = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: Variant
  fullWidth?: boolean
}

const variants: Record<Variant, string> = {
  primary:
    'bg-brand-green text-white hover:bg-brand-green-light focus-visible:ring-brand-green/40',
  secondary:
    'border border-border bg-white text-brand-ink hover:border-brand-green hover:text-brand-green',
  ghost: 'text-brand-muted hover:bg-brand-aqua-pale hover:text-brand-green',
  inverse: 'bg-white text-brand-green hover:bg-brand-aqua-pale',
}

export function Button({
  className,
  variant = 'primary',
  fullWidth,
  type = 'button',
  ...props
}: Props) {
  return (
    <button
      type={type}
      className={cn(
        'inline-flex min-h-11 items-center justify-center rounded-xl px-5 text-sm font-semibold transition-colors',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
        'disabled:pointer-events-none disabled:opacity-50',
        variants[variant],
        fullWidth && 'w-full',
        className,
      )}
      {...props}
    />
  )
}
