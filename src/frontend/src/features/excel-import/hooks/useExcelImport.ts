/**
 * useExcelImport Hooks
 *
 * React Query hooks for Excel import operations.
 */

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { excelImportService } from '../services/excelImportService'
import type {
  SelectWorksheetRequest,
  ConfigureMappingsRequest,
  ExecuteImportRequest,
  UpdateRowsRequest,
  ValidationResult,
} from '../types'

const QUERY_KEY = 'excelImport'

/**
 * Hook to upload and analyze an Excel file
 */
export const useUploadFile = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => excelImportService.uploadFile(file),
    onSuccess: data => {
      // Cache the session state
      queryClient.setQueryData([QUERY_KEY, 'session', data.sessionId], data)
    },
  })
}

/**
 * Hook to get import session state
 */
export const useSessionState = (sessionId: string | undefined) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'session', sessionId],
    queryFn: () => excelImportService.getSessionState(sessionId!),
    enabled: !!sessionId,
    staleTime: 60 * 1000, // 1 minute
  })
}

/**
 * Hook to select a worksheet
 */
export const useSelectWorksheet = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: SelectWorksheetRequest) => excelImportService.selectWorksheet(request),
    onSuccess: data => {
      // Cache the worksheet selection result
      queryClient.setQueryData(
        [QUERY_KEY, 'worksheet', data.sessionId],
        data,
      )
    },
  })
}

/**
 * Hook to get suggested mappings
 */
export const useSuggestedMappings = (sessionId: string | undefined) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'mappings', sessionId],
    queryFn: () => excelImportService.getSuggestedMappings(sessionId!),
    enabled: !!sessionId,
  })
}

/**
 * Hook to validate import data
 */
export const useValidateImport = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: ConfigureMappingsRequest) => excelImportService.validateImport(request),
    onSuccess: data => {
      // Cache the validation result
      queryClient.setQueryData(
        [QUERY_KEY, 'validation', data.sessionId],
        data,
      )
    },
  })
}

/**
 * Hook to execute the import
 */
export const useExecuteImport = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: ExecuteImportRequest) => excelImportService.executeImport(request),
    onSuccess: (_, variables) => {
      // Invalidate injects cache for the exercise using the correct key structure
      // injectKeys.all(exerciseId) = ['exercises', exerciseId, 'injects']
      queryClient.invalidateQueries({ queryKey: ['exercises', variables.exerciseId, 'injects'] })
      // Also invalidate MSEL data which displays injects in grouped form
      queryClient.invalidateQueries({ queryKey: ['exercises', variables.exerciseId, 'msel'] })
    },
  })
}

/**
 * Hook to update rows and re-validate (for auto-fix and inline editing)
 */
export const useUpdateRows = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateRowsRequest) => excelImportService.updateRows(request),
    onSuccess: data => {
      // Merge updated rows into the cached validation result
      queryClient.setQueryData<ValidationResult>(
        [QUERY_KEY, 'validation', data.sessionId],
        old => {
          if (!old) return old
          const rowMap = new Map(old.rows.map(r => [r.rowNumber, r]))
          for (const updated of data.updatedRows) {
            rowMap.set(updated.rowNumber, updated)
          }
          return {
            ...old,
            totalRows: data.totalRows,
            validRows: data.validRows,
            errorRows: data.errorRows,
            warningRows: data.warningRows,
            rows: Array.from(rowMap.values()).sort((a, b) => a.rowNumber - b.rowNumber),
          }
        },
      )
    },
  })
}

/**
 * Hook to cancel an import session
 */
export const useCancelImport = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (sessionId: string) => excelImportService.cancelImport(sessionId),
    onSuccess: (_, sessionId) => {
      // Clear all cached data for this session
      queryClient.removeQueries({ queryKey: [QUERY_KEY, 'session', sessionId] })
      queryClient.removeQueries({ queryKey: [QUERY_KEY, 'worksheet', sessionId] })
      queryClient.removeQueries({ queryKey: [QUERY_KEY, 'mappings', sessionId] })
      queryClient.removeQueries({ queryKey: [QUERY_KEY, 'validation', sessionId] })
    },
  })
}
