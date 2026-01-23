/**
 * Autocomplete Service
 *
 * API client for autocomplete suggestions.
 */

import api from '@/core/services/api'

const BASE_URL = '/autocomplete/exercises'

/**
 * Get track suggestions for an exercise
 */
export const getTrackSuggestions = async (
  exerciseId: string,
  filter?: string,
  limit = 20,
): Promise<string[]> => {
  const params = new URLSearchParams()
  if (filter) params.append('filter', filter)
  params.append('limit', limit.toString())

  const response = await api.get<string[]>(`${BASE_URL}/${exerciseId}/tracks?${params}`)
  return response.data
}

/**
 * Get target suggestions for an exercise
 */
export const getTargetSuggestions = async (
  exerciseId: string,
  filter?: string,
  limit = 20,
): Promise<string[]> => {
  const params = new URLSearchParams()
  if (filter) params.append('filter', filter)
  params.append('limit', limit.toString())

  const response = await api.get<string[]>(`${BASE_URL}/${exerciseId}/targets?${params}`)
  return response.data
}

/**
 * Get source suggestions for an exercise
 */
export const getSourceSuggestions = async (
  exerciseId: string,
  filter?: string,
  limit = 20,
): Promise<string[]> => {
  const params = new URLSearchParams()
  if (filter) params.append('filter', filter)
  params.append('limit', limit.toString())

  const response = await api.get<string[]>(`${BASE_URL}/${exerciseId}/sources?${params}`)
  return response.data
}

/**
 * Get location name suggestions for an exercise
 */
export const getLocationNameSuggestions = async (
  exerciseId: string,
  filter?: string,
  limit = 20,
): Promise<string[]> => {
  const params = new URLSearchParams()
  if (filter) params.append('filter', filter)
  params.append('limit', limit.toString())

  const response = await api.get<string[]>(`${BASE_URL}/${exerciseId}/location-names?${params}`)
  return response.data
}

/**
 * Get location type suggestions for an exercise
 */
export const getLocationTypeSuggestions = async (
  exerciseId: string,
  filter?: string,
  limit = 20,
): Promise<string[]> => {
  const params = new URLSearchParams()
  if (filter) params.append('filter', filter)
  params.append('limit', limit.toString())

  const response = await api.get<string[]>(`${BASE_URL}/${exerciseId}/location-types?${params}`)
  return response.data
}

/**
 * Get responsible controller suggestions for an exercise
 */
export const getResponsibleControllerSuggestions = async (
  exerciseId: string,
  filter?: string,
  limit = 20,
): Promise<string[]> => {
  const params = new URLSearchParams()
  if (filter) params.append('filter', filter)
  params.append('limit', limit.toString())

  const response = await api.get<string[]>(`${BASE_URL}/${exerciseId}/responsible-controllers?${params}`)
  return response.data
}

export const autocompleteService = {
  getTrackSuggestions,
  getTargetSuggestions,
  getSourceSuggestions,
  getLocationNameSuggestions,
  getLocationTypeSuggestions,
  getResponsibleControllerSuggestions,
}

export default autocompleteService
