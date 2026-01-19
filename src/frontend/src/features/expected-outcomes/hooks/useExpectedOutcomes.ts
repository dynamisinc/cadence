/**
 * useExpectedOutcomes Hook
 *
 * React Query hooks for expected outcome CRUD operations.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { expectedOutcomeService } from '../services/expectedOutcomeService'
import type {
  CreateExpectedOutcomeRequest,
  UpdateExpectedOutcomeRequest,
  EvaluateExpectedOutcomeRequest,
  ReorderExpectedOutcomesRequest,
} from '../types'

const QUERY_KEY = 'expectedOutcomes'

/**
 * Hook to fetch all expected outcomes for an inject
 */
export const useExpectedOutcomes = (injectId: string | undefined) => {
  return useQuery({
    queryKey: [QUERY_KEY, injectId],
    queryFn: () => expectedOutcomeService.getOutcomes(injectId!),
    enabled: !!injectId,
  })
}

/**
 * Hook to fetch a single expected outcome
 */
export const useExpectedOutcome = (injectId: string | undefined, id: string | undefined) => {
  return useQuery({
    queryKey: [QUERY_KEY, injectId, id],
    queryFn: () => expectedOutcomeService.getOutcome(injectId!, id!),
    enabled: !!injectId && !!id,
  })
}

/**
 * Hook to create an expected outcome
 */
export const useCreateExpectedOutcome = (injectId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateExpectedOutcomeRequest) =>
      expectedOutcomeService.createOutcome(injectId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, injectId] })
    },
  })
}

/**
 * Hook to update an expected outcome
 */
export const useUpdateExpectedOutcome = (injectId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateExpectedOutcomeRequest }) =>
      expectedOutcomeService.updateOutcome(injectId, id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, injectId] })
    },
  })
}

/**
 * Hook to evaluate an expected outcome
 */
export const useEvaluateExpectedOutcome = (injectId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: EvaluateExpectedOutcomeRequest }) =>
      expectedOutcomeService.evaluateOutcome(injectId, id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, injectId] })
    },
  })
}

/**
 * Hook to reorder expected outcomes
 */
export const useReorderExpectedOutcomes = (injectId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: ReorderExpectedOutcomesRequest) =>
      expectedOutcomeService.reorderOutcomes(injectId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, injectId] })
    },
  })
}

/**
 * Hook to delete an expected outcome
 */
export const useDeleteExpectedOutcome = (injectId: string) => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => expectedOutcomeService.deleteOutcome(injectId, id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [QUERY_KEY, injectId] })
    },
  })
}
