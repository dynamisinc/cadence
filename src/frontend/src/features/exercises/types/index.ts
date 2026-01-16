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

// Re-export validation types
export type { CreateExerciseFormValues, UpdateExerciseFormValues } from './validation'
export { createExerciseSchema, updateExerciseSchema, EXERCISE_FIELD_LIMITS } from './validation'
