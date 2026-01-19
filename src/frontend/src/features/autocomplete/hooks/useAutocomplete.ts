/**
 * useAutocomplete Hooks
 *
 * React Query hooks for autocomplete suggestions.
 */

import { useQuery } from '@tanstack/react-query'
import { autocompleteService } from '../services/autocompleteService'

const QUERY_KEY = 'autocomplete'
const STALE_TIME = 60 * 1000 // 1 minute

/**
 * Hook to fetch track suggestions
 */
export const useTrackSuggestions = (
  exerciseId: string | undefined,
  filter?: string,
  limit = 20
) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'tracks', exerciseId, filter, limit],
    queryFn: () => autocompleteService.getTrackSuggestions(exerciseId!, filter, limit),
    enabled: !!exerciseId,
    staleTime: STALE_TIME,
  })
}

/**
 * Hook to fetch target suggestions
 */
export const useTargetSuggestions = (
  exerciseId: string | undefined,
  filter?: string,
  limit = 20
) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'targets', exerciseId, filter, limit],
    queryFn: () => autocompleteService.getTargetSuggestions(exerciseId!, filter, limit),
    enabled: !!exerciseId,
    staleTime: STALE_TIME,
  })
}

/**
 * Hook to fetch source suggestions
 */
export const useSourceSuggestions = (
  exerciseId: string | undefined,
  filter?: string,
  limit = 20
) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'sources', exerciseId, filter, limit],
    queryFn: () => autocompleteService.getSourceSuggestions(exerciseId!, filter, limit),
    enabled: !!exerciseId,
    staleTime: STALE_TIME,
  })
}

/**
 * Hook to fetch location name suggestions
 */
export const useLocationNameSuggestions = (
  exerciseId: string | undefined,
  filter?: string,
  limit = 20
) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'locationNames', exerciseId, filter, limit],
    queryFn: () => autocompleteService.getLocationNameSuggestions(exerciseId!, filter, limit),
    enabled: !!exerciseId,
    staleTime: STALE_TIME,
  })
}

/**
 * Hook to fetch location type suggestions
 */
export const useLocationTypeSuggestions = (
  exerciseId: string | undefined,
  filter?: string,
  limit = 20
) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'locationTypes', exerciseId, filter, limit],
    queryFn: () => autocompleteService.getLocationTypeSuggestions(exerciseId!, filter, limit),
    enabled: !!exerciseId,
    staleTime: STALE_TIME,
  })
}

/**
 * Hook to fetch responsible controller suggestions
 */
export const useResponsibleControllerSuggestions = (
  exerciseId: string | undefined,
  filter?: string,
  limit = 20
) => {
  return useQuery({
    queryKey: [QUERY_KEY, 'responsibleControllers', exerciseId, filter, limit],
    queryFn: () => autocompleteService.getResponsibleControllerSuggestions(exerciseId!, filter, limit),
    enabled: !!exerciseId,
    staleTime: STALE_TIME,
  })
}
