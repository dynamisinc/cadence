/**
 * useParticipantImport Hook
 *
 * Manages the bulk participant import flow:
 * 1. Upload file → Parse and validate
 * 2. Preview → Classify rows (Assign/Update/Invite/Error)
 * 3. Confirm → Execute import operations
 * 4. Results → Show outcomes per row
 *
 * @module features/exercises/hooks
 */

import { useState, useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { bulkImportService } from '../services/bulkImportService'
import type {
  ImportFlowStep,
  FileParseResult,
  ImportPreviewResult,
  BulkImportResult,
} from '../types/bulkImport'

/**
 * Hook for managing bulk participant import flow
 *
 * @param exerciseId - Exercise ID
 * @returns Import flow state and actions
 */
export function useParticipantImport(exerciseId: string) {
  const queryClient = useQueryClient()

  // Flow state
  const [step, setStep] = useState<ImportFlowStep>('idle')
  const [parseResult, setParseResult] = useState<FileParseResult | null>(null)
  const [previewResult, setPreviewResult] = useState<ImportPreviewResult | null>(null)
  const [importResult, setImportResult] = useState<BulkImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  /**
   * Reset all state and return to idle
   */
  const reset = useCallback(() => {
    setStep('idle')
    setParseResult(null)
    setPreviewResult(null)
    setImportResult(null)
    setError(null)
    setIsLoading(false)
  }, [])

  /**
   * Upload and parse a participant file
   *
   * On success, automatically advances to preview step.
   * On failure, returns to idle with error message.
   *
   * @param file - CSV or Excel file to upload
   */
  const uploadFile = useCallback(
    async (file: File) => {
      setIsLoading(true)
      setError(null)
      setStep('uploading')

      try {
        // Upload and parse
        const result = await bulkImportService.uploadFile(exerciseId, file)
        setParseResult(result)

        if (result.isValid) {
          // Auto-advance to preview
          const preview = await bulkImportService.getPreview(exerciseId, result.sessionId)
          setPreviewResult(preview)
          setStep('preview')
        } else {
          // Parse failed - show errors
          setStep('idle')
          setError(result.errors.join('; '))
        }
      } catch (err: unknown) {
        setStep('idle')
        const message = err instanceof Error ? err.message : 'Upload failed'
        setError(message)
      } finally {
        setIsLoading(false)
      }
    },
    [exerciseId],
  )

  /**
   * Confirm and execute the import
   *
   * Processes all valid rows from preview.
   * On success, advances to results step and invalidates participant queries.
   * On failure, returns to preview with error message.
   */
  const confirmImport = useCallback(async () => {
    if (!previewResult) {
      setError('No preview result available')
      return
    }

    setIsLoading(true)
    setError(null)
    setStep('confirming')

    try {
      const result = await bulkImportService.confirmImport(exerciseId, previewResult.sessionId)
      setImportResult(result)
      setStep('results')

      // Invalidate participant queries to refresh UI
      await queryClient.invalidateQueries({ queryKey: ['participants', exerciseId] })
      await queryClient.invalidateQueries({ queryKey: ['exercises', exerciseId] })
      await queryClient.invalidateQueries({
        queryKey: ['pendingAssignments', exerciseId],
      })
    } catch (err: unknown) {
      setStep('preview')
      const message = err instanceof Error ? err.message : 'Import failed'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [exerciseId, previewResult, queryClient])

  /**
   * Cancel preview and return to upload step
   */
  const goBackToUpload = useCallback(() => {
    setStep('idle')
    setParseResult(null)
    setPreviewResult(null)
    setError(null)
  }, [])

  return {
    // State
    step,
    parseResult,
    previewResult,
    importResult,
    error,
    isLoading,

    // Actions
    uploadFile,
    confirmImport,
    goBackToUpload,
    reset,
  }
}
