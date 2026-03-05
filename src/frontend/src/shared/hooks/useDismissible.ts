/**
 * useDismissible Hook
 *
 * Track dismissed UI elements via localStorage.
 * Returns { isDismissed, dismiss, reset } for a given key.
 * Used to remember when users dismiss orientation panels or help sections.
 */

import { useState, useCallback } from 'react'

const STORAGE_PREFIX = 'cadence:dismissed:'

export interface UseDismissibleReturn {
  isDismissed: boolean
  dismiss: () => void
  reset: () => void
}

function readStorage(key: string): boolean {
  try {
    return localStorage.getItem(STORAGE_PREFIX + key) === 'true'
  } catch {
    return false
  }
}

function writeStorage(key: string, value: boolean): void {
  try {
    if (value) {
      localStorage.setItem(STORAGE_PREFIX + key, 'true')
    } else {
      localStorage.removeItem(STORAGE_PREFIX + key)
    }
  } catch {
    // localStorage unavailable (e.g., private browsing)
  }
}

export function useDismissible(key: string): UseDismissibleReturn {
  const [isDismissed, setIsDismissed] = useState(() => readStorage(key))

  const dismiss = useCallback(() => {
    writeStorage(key, true)
    setIsDismissed(true)
  }, [key])

  const reset = useCallback(() => {
    writeStorage(key, false)
    setIsDismissed(false)
  }, [key])

  return { isDismissed, dismiss, reset }
}
