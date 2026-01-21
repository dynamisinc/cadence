/**
 * useFireConfirmationPreference Hook
 *
 * Manages the "don't ask again" preference for fire confirmation dialog.
 * Uses sessionStorage for persistence within the browser session.
 * Automatically clears on new session (sessionStorage behavior).
 *
 * @module features/injects/hooks
 */

import { useState } from 'react'

const STORAGE_KEY = 'cadence_skipFireConfirmation'

/**
 * Hook to manage "don't ask again" preference for fire confirmation dialog
 *
 * @returns Tuple of [skipConfirmation, setSkipConfirmation]
 *
 * @example
 * const [skipConfirmation, setSkipConfirmation] = useFireConfirmationPreference()
 *
 * if (skipConfirmation) {
 *   // Fire immediately
 * } else {
 *   // Show confirmation dialog
 * }
 */
export const useFireConfirmationPreference = (): [boolean, (skip: boolean) => void] => {
  const [skipConfirmation, setSkipConfirmation] = useState<boolean>(() => {
    return sessionStorage.getItem(STORAGE_KEY) === 'true'
  })

  const setSkip = (skip: boolean): void => {
    setSkipConfirmation(skip)
    if (skip) {
      sessionStorage.setItem(STORAGE_KEY, 'true')
    } else {
      sessionStorage.removeItem(STORAGE_KEY)
    }
  }

  return [skipConfirmation, setSkip]
}
