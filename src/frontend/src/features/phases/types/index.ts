/**
 * Phase Feature Types
 *
 * TypeScript types for phase CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Phases.Models.DTOs
 */

/**
 * Phase DTO - Response from API
 */
export interface PhaseDto {
  id: string
  name: string
  description: string | null
  sequence: number
  startTime: string | null // TimeOnly as HH:MM:SS
  endTime: string | null // TimeOnly as HH:MM:SS
  exerciseId: string
  injectCount: number
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
}

/**
 * Request body for creating a new phase
 */
export interface CreatePhaseRequest {
  name: string
  description?: string | null
  startTime?: string | null // HH:MM:SS format
  endTime?: string | null // HH:MM:SS format
}

/**
 * Request body for updating an existing phase
 */
export interface UpdatePhaseRequest {
  name: string
  description?: string | null
  startTime?: string | null // HH:MM:SS format
  endTime?: string | null // HH:MM:SS format
}

/**
 * Request body for reordering phases
 */
export interface ReorderPhasesRequest {
  phaseIds: string[]
}

/**
 * Form values for creating/editing a phase
 */
export interface PhaseFormValues {
  name: string
  description: string
}

/**
 * Phase field limits for validation
 */
export const PHASE_FIELD_LIMITS = {
  name: { min: 3, max: 100 },
  description: { max: 500 },
}
