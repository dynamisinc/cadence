/**
 * Expected Outcome Service
 *
 * API client for expected outcome CRUD operations.
 */

import api from '@/core/services/api'
import type {
  ExpectedOutcomeDto,
  CreateExpectedOutcomeRequest,
  UpdateExpectedOutcomeRequest,
  EvaluateExpectedOutcomeRequest,
  ReorderExpectedOutcomesRequest,
} from '../types'

const BASE_URL = '/api/injects'

/**
 * Get all expected outcomes for an inject
 */
export const getOutcomes = async (injectId: string): Promise<ExpectedOutcomeDto[]> => {
  const response = await api.get<ExpectedOutcomeDto[]>(`${BASE_URL}/${injectId}/outcomes`)
  return response.data
}

/**
 * Get a single expected outcome by ID
 */
export const getOutcome = async (injectId: string, id: string): Promise<ExpectedOutcomeDto> => {
  const response = await api.get<ExpectedOutcomeDto>(`${BASE_URL}/${injectId}/outcomes/${id}`)
  return response.data
}

/**
 * Create a new expected outcome
 */
export const createOutcome = async (
  injectId: string,
  request: CreateExpectedOutcomeRequest,
): Promise<ExpectedOutcomeDto> => {
  const response = await api.post<ExpectedOutcomeDto>(`${BASE_URL}/${injectId}/outcomes`, request)
  return response.data
}

/**
 * Update an expected outcome's description
 */
export const updateOutcome = async (
  injectId: string,
  id: string,
  request: UpdateExpectedOutcomeRequest,
): Promise<ExpectedOutcomeDto> => {
  const response = await api.put<ExpectedOutcomeDto>(`${BASE_URL}/${injectId}/outcomes/${id}`, request)
  return response.data
}

/**
 * Evaluate an expected outcome (set WasAchieved and EvaluatorNotes)
 */
export const evaluateOutcome = async (
  injectId: string,
  id: string,
  request: EvaluateExpectedOutcomeRequest,
): Promise<ExpectedOutcomeDto> => {
  const response = await api.post<ExpectedOutcomeDto>(
    `${BASE_URL}/${injectId}/outcomes/${id}/evaluate`,
    request,
  )
  return response.data
}

/**
 * Reorder expected outcomes for an inject
 */
export const reorderOutcomes = async (
  injectId: string,
  request: ReorderExpectedOutcomesRequest,
): Promise<void> => {
  await api.post(`${BASE_URL}/${injectId}/outcomes/reorder`, request)
}

/**
 * Delete an expected outcome
 */
export const deleteOutcome = async (injectId: string, id: string): Promise<void> => {
  await api.delete(`${BASE_URL}/${injectId}/outcomes/${id}`)
}

export const expectedOutcomeService = {
  getOutcomes,
  getOutcome,
  createOutcome,
  updateOutcome,
  evaluateOutcome,
  reorderOutcomes,
  deleteOutcome,
}

export default expectedOutcomeService
