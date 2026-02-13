import api from '@/core/services/api'
import type {
  OrganizationSuggestionDto,
  CreateSuggestionRequest,
  UpdateSuggestionRequest,
  BulkCreateSuggestionsRequest,
  BulkCreateSuggestionsResult,
} from '../types'

const BASE_URL = '/organizations/current/suggestions'

export const getSuggestions = async (
  fieldName: string,
  includeInactive = true,
): Promise<OrganizationSuggestionDto[]> => {
  const params = new URLSearchParams()
  params.append('fieldName', fieldName)
  params.append('includeInactive', includeInactive.toString())
  const response = await api.get<OrganizationSuggestionDto[]>(`${BASE_URL}?${params}`)
  return response.data
}

export const createSuggestion = async (
  request: CreateSuggestionRequest,
): Promise<OrganizationSuggestionDto> => {
  const response = await api.post<OrganizationSuggestionDto>(BASE_URL, request)
  return response.data
}

export const updateSuggestion = async (
  id: string,
  request: UpdateSuggestionRequest,
): Promise<OrganizationSuggestionDto> => {
  const response = await api.put<OrganizationSuggestionDto>(`${BASE_URL}/${id}`, request)
  return response.data
}

export const deleteSuggestion = async (id: string): Promise<void> => {
  await api.delete(`${BASE_URL}/${id}`)
}

export const bulkCreateSuggestions = async (
  request: BulkCreateSuggestionsRequest,
): Promise<BulkCreateSuggestionsResult> => {
  const response = await api.post<BulkCreateSuggestionsResult>(`${BASE_URL}/bulk`, request)
  return response.data
}

export const reorderSuggestions = async (
  fieldName: string,
  orderedIds: string[],
): Promise<void> => {
  await api.put(`${BASE_URL}/reorder?fieldName=${fieldName}`, orderedIds)
}

export const suggestionManagementService = {
  getSuggestions,
  createSuggestion,
  updateSuggestion,
  deleteSuggestion,
  bulkCreateSuggestions,
  reorderSuggestions,
}
