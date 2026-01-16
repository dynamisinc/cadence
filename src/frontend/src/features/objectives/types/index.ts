/**
 * Objective Feature Types
 *
 * TypeScript types for objective CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Objectives.Models.DTOs
 */

/**
 * Objective DTO - Response from API
 */
export interface ObjectiveDto {
  id: string
  objectiveNumber: string
  name: string
  description: string | null
  exerciseId: string
  linkedInjectCount: number
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
}

/**
 * Lightweight DTO for selection dropdowns
 * Includes description for tooltip display
 */
export interface ObjectiveSummaryDto {
  id: string
  objectiveNumber: string
  name: string
  description: string | null
}

/**
 * Request body for creating a new objective
 */
export interface CreateObjectiveRequest {
  objectiveNumber?: string | null
  name: string
  description?: string | null
}

/**
 * Request body for updating an existing objective
 */
export interface UpdateObjectiveRequest {
  objectiveNumber: string
  name: string
  description?: string | null
}

/**
 * Form values for creating/editing an objective
 */
export interface ObjectiveFormValues {
  objectiveNumber: string
  name: string
  description: string
}

/**
 * Objective field limits for validation
 */
export const OBJECTIVE_FIELD_LIMITS = {
  objectiveNumber: { max: 10 },
  name: { min: 3, max: 200 },
  description: { max: 2000 },
}
