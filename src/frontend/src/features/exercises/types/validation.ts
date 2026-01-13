/**
 * Exercise Form Validation Schemas
 *
 * Zod schemas for form validation with React Hook Form.
 */

import { z } from 'zod'
import { ExerciseType } from '../../../types'

/** Maximum lengths for exercise fields */
export const EXERCISE_FIELD_LIMITS = {
  name: 200,
  description: 4000,
  location: 500,
} as const

/**
 * Schema for creating a new exercise
 */
export const createExerciseSchema = z.object({
  name: z
    .string()
    .min(1, 'Exercise name is required')
    .max(EXERCISE_FIELD_LIMITS.name, `Name must be ${EXERCISE_FIELD_LIMITS.name} characters or less`),
  exerciseType: z.nativeEnum(ExerciseType, {
    error: 'Exercise type is required',
  }),
  scheduledDate: z
    .string()
    .min(1, 'Scheduled date is required'),
  description: z
    .string()
    .max(EXERCISE_FIELD_LIMITS.description, `Description must be ${EXERCISE_FIELD_LIMITS.description} characters or less`)
    .optional()
    .or(z.literal('')),
  location: z
    .string()
    .max(EXERCISE_FIELD_LIMITS.location, `Location must be ${EXERCISE_FIELD_LIMITS.location} characters or less`)
    .optional()
    .or(z.literal('')),
  startTime: z.string().optional().or(z.literal('')),
  endTime: z.string().optional().or(z.literal('')),
}).refine(
  (data) => {
    // If both times are provided, end must be after start
    if (data.startTime && data.endTime) {
      return data.endTime > data.startTime
    }
    return true
  },
  {
    message: 'End time must be after start time',
    path: ['endTime'],
  },
)

/**
 * Schema for updating an existing exercise
 */
export const updateExerciseSchema = createExerciseSchema

/** Type for create exercise form values */
export type CreateExerciseFormValues = z.infer<typeof createExerciseSchema>

/** Type for update exercise form values */
export type UpdateExerciseFormValues = z.infer<typeof updateExerciseSchema>
