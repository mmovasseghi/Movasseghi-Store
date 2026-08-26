'use client'

import { cn } from '@/lib/utils'
import { useEffect, useId } from 'react'

type Props = {
  open: boolean
  onClose: () => void
  title: string
  children: React.ReactNode
  side?: 'start' | 'end'
}

export function Sheet({ open, onClose, title, children, side = 'start' }: Props) {
  const titleId = useId()

  useEffect(() => {
    if (!open) return
    const prev = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => {
      document.body.style.overflow = prev
      window.removeEventListener('keydown', onKey)
    }
  }, [open, onClose])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-[70]">
      <button
        type="button"
        className="absolute inset-0 bg-brand-ink/40"
        aria-label="بستن"
        onClick={onClose}
      />
      <div
        role="dialog"
        aria-modal
        aria-labelledby={titleId}
        className={cn(
          'sheet-panel absolute inset-y-0 flex w-[min(100%,400px)] flex-col bg-white shadow-xl',
          side === 'start' ? 'start-0' : 'end-0',
        )}
      >
        <div className="flex items-center justify-between border-b border-border px-4 py-4">
          <h2 id={titleId} className="text-lg font-bold text-brand-ink">
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="flex h-10 w-10 items-center justify-center rounded-lg text-brand-muted hover:bg-brand-aqua-pale"
            aria-label="بستن"
          >
            ×
          </button>
        </div>
        <div className="flex-1 overflow-y-auto overscroll-contain">{children}</div>
      </div>
    </div>
  )
}
