/**
 * useConfirmDialog Hook
 *
 * Provides an imperative API for showing confirmation dialogs.
 * Returns a component to render and a function to trigger confirmations.
 *
 * Usage:
 * ```tsx
 * const { confirm, ConfirmDialogComponent } = useConfirmDialog()
 *
 * const handleDelete = async () => {
 *   const confirmed = await confirm({
 *     title: 'Delete item?',
 *     message: 'This action cannot be undone.',
 *     severity: 'danger',
 *     confirmLabel: 'Delete',
 *   })
 *   if (confirmed) {
 *     // perform delete
 *   }
 * }
 *
 * return (
 *   <>
 *     <button onClick={handleDelete}>Delete</button>
 *     <ConfirmDialogComponent />
 *   </>
 * )
 * ```
 */

import { useState, useCallback, useRef } from 'react'
import { ConfirmDialog, type ConfirmDialogSeverity } from '../components/ConfirmDialog'

export interface ConfirmOptions {
  /** Dialog title */
  title: string
  /** Dialog message/description */
  message: string
  /** Label for the confirm button */
  confirmLabel?: string
  /** Label for the cancel button */
  cancelLabel?: string
  /** Severity level affects icon and confirm button style */
  severity?: ConfirmDialogSeverity
}

interface DialogState extends ConfirmOptions {
  open: boolean
}

const defaultState: DialogState = {
  open: false,
  title: '',
  message: '',
  confirmLabel: 'Confirm',
  cancelLabel: 'Cancel',
  severity: 'warning',
}

/**
 * Hook for imperative confirmation dialogs
 */
export const useConfirmDialog = () => {
  const [state, setState] = useState<DialogState>(defaultState)
  const resolveRef = useRef<((value: boolean) => void) | null>(null)

  /**
   * Show a confirmation dialog and return a promise that resolves
   * to true if confirmed, false if cancelled.
   */
  const confirm = useCallback((options: ConfirmOptions): Promise<boolean> => {
    return new Promise(resolve => {
      resolveRef.current = resolve
      setState({
        open: true,
        ...options,
      })
    })
  }, [])

  const handleConfirm = useCallback(() => {
    setState(defaultState)
    resolveRef.current?.(true)
    resolveRef.current = null
  }, [])

  const handleCancel = useCallback(() => {
    setState(defaultState)
    resolveRef.current?.(false)
    resolveRef.current = null
  }, [])

  /**
   * Component to render in your JSX tree.
   * Must be included for the dialog to appear.
   */
  const ConfirmDialogComponent = useCallback(
    () => (
      <ConfirmDialog
        open={state.open}
        title={state.title}
        message={state.message}
        confirmLabel={state.confirmLabel}
        cancelLabel={state.cancelLabel}
        severity={state.severity}
        onConfirm={handleConfirm}
        onCancel={handleCancel}
      />
    ),
    [state, handleConfirm, handleCancel],
  )

  return {
    /** Function to trigger confirmation dialog */
    confirm,
    /** Component to render - must be included in JSX */
    ConfirmDialogComponent,
  }
}

export default useConfirmDialog
