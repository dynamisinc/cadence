/**
 * Expected Outcomes Feature Types
 *
 * TypeScript types for expected outcome CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.ExpectedOutcomes.Models.DTOs
 */

/**
 * ExpectedOutcome DTO - Response from API
 */
export interface ExpectedOutcomeDto {
  id: string
  injectId: string
  description: string
  sortOrder: number
  wasAchieved: boolean | null
  evaluatorNotes: string | null
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
}

/**
 * Request body for creating a new expected outcome
 */
export interface CreateExpectedOutcomeRequest {
  description: string
  sortOrder?: number | null
}

/**
 * Request body for updating an expected outcome
 */
export interface UpdateExpectedOutcomeRequest {
  description: string
}

/**
 * Request body for evaluating an expected outcome
 */
export interface EvaluateExpectedOutcomeRequest {
  wasAchieved: boolean | null
  evaluatorNotes?: string | null
}

/**
 * Request body for reordering expected outcomes
 */
export interface ReorderExpectedOutcomesRequest {
  outcomeIds: string[]
}

/**
 * Field limits for validation
 */
export const EXPECTED_OUTCOME_FIELD_LIMITS = {
  description: { min: 1, max: 1000 },
  evaluatorNotes: { max: 2000 },
}

/**
 * Achievement status display helpers
 */
export const getAchievementStatusLabel = (wasAchieved: boolean | null): string => {
  if (wasAchieved === null) return 'Not Evaluated'
  return wasAchieved ? 'Achieved' : 'Not Achieved'
}

export const getAchievementStatusColor = (wasAchieved: boolean | null): 'default' | 'success' | 'error' => {
  if (wasAchieved === null) return 'default'
  return wasAchieved ? 'success' : 'error'
}
