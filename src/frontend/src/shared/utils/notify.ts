/**
 * Smart toast notification wrapper with time-window deduplication.
 *
 * Wraps react-toastify to prevent duplicate toasts from stacking.
 * If a toast with the same key (explicit toastId or message text) was
 * shown within DEDUP_WINDOW_MS, the duplicate call is suppressed.
 *
 * Usage:
 * ```ts
 * import { notify } from '@/shared/utils/notify'
 * notify.success('Exercise created')
 * notify.error('Failed to save', { toastId: 'save-error' })
 * ```
 */

import { toast, type ToastOptions, type Id } from 'react-toastify'

/** How long (ms) to suppress duplicate toasts with the same key */
const DEDUP_WINDOW_MS = 3000

/** Max entries before we prune the map */
const MAX_ENTRIES = 100

const recentToasts = new Map<string, number>()

function pruneStaleEntries(now: number) {
  if (recentToasts.size <= MAX_ENTRIES) return
  for (const [key, timestamp] of recentToasts) {
    if (now - timestamp > DEDUP_WINDOW_MS) {
      recentToasts.delete(key)
    }
  }
}

type ToastMethod = (message: string, options?: ToastOptions) => Id

function dedupedToast(method: ToastMethod, message: string, options?: ToastOptions): Id | undefined {
  const key = options?.toastId?.toString() ?? message
  const now = Date.now()
  const lastShown = recentToasts.get(key)

  if (lastShown && now - lastShown < DEDUP_WINDOW_MS) {
    return undefined
  }

  recentToasts.set(key, now)
  pruneStaleEntries(now)

  return method(message, options)
}

export const notify = {
  success: (message: string, options?: ToastOptions) => dedupedToast(toast.success, message, options),
  error: (message: string, options?: ToastOptions) => dedupedToast(toast.error, message, options),
  warning: (message: string, options?: ToastOptions) => dedupedToast(toast.warning, message, options),
  info: (message: string, options?: ToastOptions) => dedupedToast(toast.info, message, options),
  dismiss: toast.dismiss,
}
