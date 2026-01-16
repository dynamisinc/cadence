/**
 * Exercise Feature Types
 *
 * TypeScript types for exercise CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Exercises.Models.DTOs
 */

import { ExerciseType, ExerciseStatus } from '../../../types'

/**
 * Exercise DTO - Response from API
 */
export interface ExerciseDto {
  id: string
  name: string
  description: string | null
  exerciseType: ExerciseType
  status: ExerciseStatus
  isPracticeMode: boolean
  scheduledDate: string // DateOnly as ISO string (YYYY-MM-DD)
  startTime: string | null // TimeOnly as HH:MM:SS
  endTime: string | null // TimeOnly as HH:MM:SS
  timeZoneId: string
  location: string | null
  organizationId: string
  activeMselId: string | null
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
  // Status transition audit fields
  activatedAt: string | null // DateTime as ISO string
  activatedBy: string | null // Guid as string
  completedAt: string | null // DateTime as ISO string
  completedBy: string | null // Guid as string
  archivedAt: string | null // DateTime as ISO string
  archivedBy: string | null // Guid as string
}

/**
 * Request body for creating a new exercise
 */
export interface CreateExerciseRequest {
  name: string
  exerciseType: ExerciseType
  scheduledDate: string // YYYY-MM-DD format
  description?: string | null
  location?: string | null
  timeZoneId?: string
  isPracticeMode?: boolean
}

/**
 * Request body for updating an exercise
 */
export interface UpdateExerciseRequest {
  name: string
  exerciseType: ExerciseType
  scheduledDate: string // YYYY-MM-DD format
  description?: string | null
  location?: string | null
  timeZoneId?: string
  startTime?: string | null
  endTime?: string | null
  isPracticeMode?: boolean
}

/**
 * Request body for duplicating an exercise
 */
export interface DuplicateExerciseRequest {
  /** Name for the new exercise. Defaults to "Copy of {original name}". */
  name?: string
  /** Scheduled date for the new exercise. Defaults to the original date. */
  scheduledDate?: string // YYYY-MM-DD format
}

// =========================================================================
// MSEL Types
// =========================================================================

/**
 * MSEL list item DTO
 */
export interface MselDto {
  id: string
  name: string
  description: string | null
  version: number
  isActive: boolean
  exerciseId: string
  injectCount: number
  createdAt: string
  updatedAt: string
}

/**
 * MSEL Summary DTO - Detailed progress and metadata
 */
export interface MselSummaryDto {
  id: string
  name: string
  description: string | null
  version: number
  isActive: boolean
  exerciseId: string
  // Inject counts
  totalInjects: number
  pendingCount: number
  firedCount: number
  skippedCount: number
  completionPercentage: number
  // Related counts
  phaseCount: number
  objectiveCount: number
  // Last modification
  lastModifiedAt: string | null
  lastModifiedByName: string | null
  createdAt: string
  updatedAt: string
}

// =========================================================================
// Setup Progress Types
// =========================================================================

/**
 * Setup area DTO
 */
export interface SetupAreaDto {
  id: string
  name: string
  description: string
  isComplete: boolean
  weight: number
  currentCount: number
  requiredCount: number
  statusMessage: string
}

/**
 * Setup progress DTO
 */
export interface SetupProgressDto {
  overallPercentage: number
  isReadyToActivate: boolean
  areas: SetupAreaDto[]
}

// Re-export validation types
export type { CreateExerciseFormValues, UpdateExerciseFormValues } from './validation'
export { createExerciseSchema, updateExerciseSchema, EXERCISE_FIELD_LIMITS } from './validation'
