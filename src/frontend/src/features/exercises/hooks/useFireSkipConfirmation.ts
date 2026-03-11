/**
 * useFireSkipConfirmation
 *
 * Manages the fire/skip confirmation dialog state machine for the exercise conduct view.
 * Handles optional "are you sure?" confirmations before firing or skipping injects,
 * with localStorage-persisted "don't ask again" flags per exercise.
 *
 * @module features/exercises
 */

import { useState, useCallback } from 'react'
import type { InjectDto } from '../../injects/types'

interface UseFireSkipConfirmationParams {
  /** The exercise ID, used to key localStorage flags */
  exerciseId: string | undefined
  /** Whether the exercise settings require fire confirmation */
  confirmFireInject: boolean | undefined
  /** Whether the exercise settings require skip confirmation */
  confirmSkipInject: boolean | undefined
  /** Mutation function to fire an inject */
  fireInject: (injectId: string) => Promise<unknown>
  /** Mutation function to skip an inject */
  skipInject: (injectId: string, request: { reason: string }) => Promise<unknown>
  /** The full list of injects (to look up inject by id) */
  injects: InjectDto[]
}

export interface UseFireSkipConfirmationReturn {
  // State
  fireConfirmInject: InjectDto | null
  skipConfirmInject: InjectDto | null
  pendingSkipInjectId: string | null
  skipFireConfirmation: boolean
  skipSkipConfirmation: boolean

  // Fire handlers
  handleFireWithConfirmation: (injectId: string) => Promise<void>
  handleFireConfirmed: () => Promise<void>
  handleFireCancelled: () => void

  // Skip handlers
  handleSkipWithConfirmation: (injectId: string, request: { reason: string }) => Promise<void>
  handleSkipPreConfirmation: (injectId: string) => boolean | null
  handleSkipConfirmProceed: () => void
  handleSkipConfirmCancelled: () => void
  handlePendingSkipClear: () => void

  // "Don't ask again" handlers
  handleSkipFireConfirmation: () => void
  handleSkipSkipConfirmation: () => void
}

/**
 * Manages the fire/skip confirmation dialog state machine.
 *
 * Fire flow:
 *   1. handleFireWithConfirmation(injectId) → if confirmFireInject && !skipFireConfirmation, sets fireConfirmInject
 *   2. User clicks confirm → handleFireConfirmed() fires the inject and clears dialog
 *   3. User clicks cancel → handleFireCancelled() clears dialog
 *
 * Skip pre-confirmation flow (the "are you sure?" step before the reason dialog):
 *   1. handleSkipPreConfirmation(injectId) → returns true if confirmation needed (sets skipConfirmInject)
 *   2. User clicks proceed → handleSkipConfirmProceed() sets pendingSkipInjectId, which triggers reason dialog
 *   3. User provides reason → handleSkipWithConfirmation() forwards to skipInject
 *   4. User clicks cancel → handleSkipConfirmCancelled() clears both dialog + pending
 */
export const useFireSkipConfirmation = ({
  exerciseId,
  confirmFireInject,
  confirmSkipInject,
  fireInject,
  skipInject,
  injects,
}: UseFireSkipConfirmationParams): UseFireSkipConfirmationReturn => {
  // Confirmation dialog state
  const [fireConfirmInject, setFireConfirmInject] = useState<InjectDto | null>(null)
  const [skipConfirmInject, setSkipConfirmInject] = useState<InjectDto | null>(null)
  const [pendingSkipInjectId, setPendingSkipInjectId] = useState<string | null>(null)

  // Storage key helper
  const getStorageKey = useCallback(
    (type: string) => `cadence:skipConfirmation:${exerciseId}:${type}`,
    [exerciseId],
  )

  // "Don't ask again" flags — initialized from localStorage
  const [skipFireConfirmation, setSkipFireConfirmation] = useState(() => {
    if (!exerciseId) return false
    return localStorage.getItem(`cadence:skipConfirmation:${exerciseId}:fire`) === 'true'
  })
  const [skipSkipConfirmation, setSkipSkipConfirmation] = useState(() => {
    if (!exerciseId) return false
    return localStorage.getItem(`cadence:skipConfirmation:${exerciseId}:skip`) === 'true'
  })

  // Persist "don't ask again" choices to localStorage
  const handleSkipFireConfirmation = useCallback(() => {
    setSkipFireConfirmation(true)
    if (exerciseId) {
      localStorage.setItem(getStorageKey('fire'), 'true')
    }
  }, [exerciseId, getStorageKey])

  const handleSkipSkipConfirmation = useCallback(() => {
    setSkipSkipConfirmation(true)
    if (exerciseId) {
      localStorage.setItem(getStorageKey('skip'), 'true')
    }
  }, [exerciseId, getStorageKey])

  // =========================================================================
  // Fire handlers
  // =========================================================================

  const handleFireWithConfirmation = useCallback(
    async (injectId: string) => {
      const inject = injects.find(i => i.id === injectId)
      if (!inject) return

      if (confirmFireInject && !skipFireConfirmation) {
        setFireConfirmInject(inject)
      } else {
        await fireInject(injectId)
      }
    },
    [injects, confirmFireInject, skipFireConfirmation, fireInject],
  )

  const handleFireConfirmed = useCallback(async () => {
    if (fireConfirmInject) {
      await fireInject(fireConfirmInject.id)
      setFireConfirmInject(null)
    }
  }, [fireConfirmInject, fireInject])

  const handleFireCancelled = useCallback(() => {
    setFireConfirmInject(null)
  }, [])

  // =========================================================================
  // Skip handlers
  // =========================================================================

  // Called AFTER the reason is provided — forwards directly to skipInject
  const handleSkipWithConfirmation = useCallback(
    async (injectId: string, request: { reason: string }) => {
      await skipInject(injectId, request)
    },
    [skipInject],
  )

  // The pre-confirmation step (before reason dialog)
  // Returns true if a confirmation dialog was shown, false/null if none needed
  const handleSkipPreConfirmation = useCallback(
    (injectId: string): boolean | null => {
      const inject = injects.find(i => i.id === injectId)
      if (!inject) return null

      if (confirmSkipInject && !skipSkipConfirmation) {
        setSkipConfirmInject(inject)
        return true // Indicates confirmation is needed
      }
      return false // No confirmation needed, proceed to reason dialog
    },
    [injects, confirmSkipInject, skipSkipConfirmation],
  )

  const handleSkipConfirmProceed = useCallback(() => {
    // User confirmed they want to skip — now they need to provide a reason.
    // Set pendingSkipInjectId to trigger the reason dialog in the consume component.
    if (skipConfirmInject?.id) {
      setPendingSkipInjectId(skipConfirmInject.id)
    }
    setSkipConfirmInject(null)
  }, [skipConfirmInject])

  const handleSkipConfirmCancelled = useCallback(() => {
    setSkipConfirmInject(null)
    setPendingSkipInjectId(null)
  }, [])

  const handlePendingSkipClear = useCallback(() => {
    setPendingSkipInjectId(null)
  }, [])

  return {
    // State
    fireConfirmInject,
    skipConfirmInject,
    pendingSkipInjectId,
    skipFireConfirmation,
    skipSkipConfirmation,

    // Fire handlers
    handleFireWithConfirmation,
    handleFireConfirmed,
    handleFireCancelled,

    // Skip handlers
    handleSkipWithConfirmation,
    handleSkipPreConfirmation,
    handleSkipConfirmProceed,
    handleSkipConfirmCancelled,
    handlePendingSkipClear,

    // "Don't ask again" handlers
    handleSkipFireConfirmation,
    handleSkipSkipConfirmation,
  }
}
