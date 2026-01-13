/**
 * useUnsavedChangesWarning Hook
 *
 * Warns users when they try to navigate away with unsaved changes.
 *
 * Features:
 * - In-app navigation blocking via React Router's useBlocker
 * - Friendly confirmation dialog (not browser's native confirm)
 * - Browser beforeunload warning (for refreshes/closing tabs)
 *
 * Requires: createBrowserRouter (Data Mode) - not compatible with BrowserRouter
 *
 * Usage:
 * ```tsx
 * const { UnsavedChangesDialog } = useUnsavedChangesWarning(isDirty)
 *
 * return (
 *   <>
 *     <form>...</form>
 *     <UnsavedChangesDialog />
 *   </>
 * )
 * ```
 *
 * @param hasUnsavedChanges - Whether there are unsaved changes to warn about
 * @param options - Optional configuration for title, message, and button labels
 */

import { useEffect, useCallback, useMemo } from 'react'
import { useBlocker } from 'react-router-dom'
import type { Location } from 'react-router-dom'
import { ConfirmDialog } from '../components/ConfirmDialog'

export interface UnsavedChangesOptions {
  /** Dialog title */
  title?: string
  /** Dialog message */
  message?: string
  /** Label for the "leave" button */
  confirmLabel?: string
  /** Label for the "stay" button */
  cancelLabel?: string
}

const defaultOptions: Required<UnsavedChangesOptions> = {
  title: 'Unsaved Changes',
  message: 'You have unsaved changes. Are you sure you want to leave? Your changes will be lost.',
  confirmLabel: 'Leave',
  cancelLabel: 'Stay',
}

export const useUnsavedChangesWarning = (
  hasUnsavedChanges: boolean,
  options: UnsavedChangesOptions = {},
) => {
  const config = useMemo(
    () => ({ ...defaultOptions, ...options }),
    [options],
  )

  // Block in-app navigation when there are unsaved changes
  const blocker = useBlocker(
    useCallback(
      ({ currentLocation, nextLocation }: { currentLocation: Location; nextLocation: Location }) =>
        hasUnsavedChanges && currentLocation.pathname !== nextLocation.pathname,
      [hasUnsavedChanges],
    ),
  )

  const handleConfirm = useCallback(() => {
    if (blocker.state === 'blocked') {
      blocker.proceed()
    }
  }, [blocker])

  const handleCancel = useCallback(() => {
    if (blocker.state === 'blocked') {
      blocker.reset()
    }
  }, [blocker])

  // Handle browser refresh/close/external navigation
  // (Browser's native dialog is the only option for beforeunload)
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasUnsavedChanges) {
        e.preventDefault()
        e.returnValue = ''
        return ''
      }
    }

    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [hasUnsavedChanges])

  /**
   * Dialog component to render in your JSX.
   * Must be included for the navigation blocking dialog to appear.
   */
  const UnsavedChangesDialog = useCallback(
    () => (
      <ConfirmDialog
        open={blocker.state === 'blocked'}
        title={config.title}
        message={config.message}
        confirmLabel={config.confirmLabel}
        cancelLabel={config.cancelLabel}
        severity="warning"
        onConfirm={handleConfirm}
        onCancel={handleCancel}
      />
    ),
    [blocker.state, config, handleConfirm, handleCancel],
  )

  return {
    hasUnsavedChanges,
    blocker,
    /** Dialog component - must be rendered in JSX for navigation blocking */
    UnsavedChangesDialog,
  }
}

export default useUnsavedChangesWarning
