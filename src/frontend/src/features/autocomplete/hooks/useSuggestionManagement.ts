import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { suggestionManagementService } from '../services/suggestionManagementService'
import type {
  CreateSuggestionRequest,
  UpdateSuggestionRequest,
  BulkCreateSuggestionsRequest,
  SuggestionFieldName,
} from '../types'

const QUERY_KEY = 'org-suggestions'

/**
 * Fetch all managed suggestions for a field in the current organization.
 */
export const useFieldSuggestions = (fieldName: SuggestionFieldName | null) => {
  return useQuery({
    queryKey: [QUERY_KEY, fieldName],
    queryFn: () => suggestionManagementService.getSuggestions(fieldName!, true),
    enabled: !!fieldName,
  })
}

/**
 * Create a single suggestion.
 */
export const useCreateSuggestion = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateSuggestionRequest) =>
      suggestionManagementService.createSuggestion(request),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, variables.fieldName] })
      queryClient.invalidateQueries({ queryKey: ['autocomplete'] })
      notify.success('Suggestion added')
    },
    onError: (error: Error & { response?: { data?: { message?: string } } }) => {
      const message = error.response?.data?.message || error.message || 'Failed to add suggestion'
      notify.error(message)
    },
  })
}

/**
 * Update an existing suggestion.
 */
export const useUpdateSuggestion = (fieldName: SuggestionFieldName) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateSuggestionRequest }) =>
      suggestionManagementService.updateSuggestion(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, fieldName] })
      queryClient.invalidateQueries({ queryKey: ['autocomplete'] })
      notify.success('Suggestion updated')
    },
    onError: (error: Error & { response?: { data?: { message?: string } } }) => {
      const message =
        error.response?.data?.message || error.message || 'Failed to update suggestion'
      notify.error(message)
    },
  })
}

/**
 * Delete a suggestion.
 */
export const useDeleteSuggestion = (fieldName: SuggestionFieldName) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => suggestionManagementService.deleteSuggestion(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, fieldName] })
      queryClient.invalidateQueries({ queryKey: ['autocomplete'] })
      notify.success('Suggestion removed')
    },
    onError: () => {
      notify.error('Failed to remove suggestion')
    },
  })
}

/**
 * Bulk create suggestions from a list of values.
 */
export const useBulkCreateSuggestions = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: BulkCreateSuggestionsRequest) =>
      suggestionManagementService.bulkCreateSuggestions(request),
    onSuccess: (result, variables) => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, variables.fieldName] })
      queryClient.invalidateQueries({ queryKey: ['autocomplete'] })
      if (result.created > 0) {
        const msg =
          result.skippedDuplicates > 0
            ? `Added ${result.created} suggestions (${result.skippedDuplicates} duplicates skipped)`
            : `Added ${result.created} suggestions`
        notify.success(msg)
      } else {
        notify.info('All values already exist')
      }
    },
    onError: (error: Error & { response?: { data?: { message?: string } } }) => {
      const message =
        error.response?.data?.message || error.message || 'Failed to import suggestions'
      notify.error(message)
    },
  })
}
