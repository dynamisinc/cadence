import { useEffect } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Stack,
  MenuItem,
  Box,
  FormControl,
  InputLabel,
  Select,
  FormHelperText,
} from '@mui/material'
import { CobraTextField, CobraPrimaryButton, CobraLinkButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { ExerciseType } from '../../../types'
import { getExerciseTypeFullName } from '../../../theme/cobraTheme'
import { createExerciseSchema, EXERCISE_FIELD_LIMITS, type CreateExerciseFormValues } from '../types/validation'
import type { ExerciseDto } from '../types'

interface ExerciseFormProps {
  exercise?: ExerciseDto | null
  onSubmit: (values: CreateExerciseFormValues) => Promise<void>
  onCancel: () => void
  isSubmitting?: boolean
  /** Fields that should be disabled (e.g., for Active exercises) */
  disabledFields?: (keyof CreateExerciseFormValues)[]
  /** Callback when form dirty state changes */
  onDirtyChange?: (isDirty: boolean) => void
}

const EXERCISE_TYPES: ExerciseType[] = [
  ExerciseType.TTX,
  ExerciseType.FE,
  ExerciseType.FSE,
  ExerciseType.CAX,
  ExerciseType.Hybrid,
]

/**
 * Reusable form component for creating and editing exercises
 *
 * Uses React Hook Form with Zod validation per S01/S02:
 * - Name is required, max 200 characters
 * - Exercise Type is required
 * - Scheduled Date is required
 * - End Time must be after Start Time (if both provided)
 */
export const ExerciseForm = ({
  exercise,
  onSubmit,
  onCancel,
  isSubmitting = false,
  disabledFields = [],
  onDirtyChange,
}: ExerciseFormProps) => {
  const isEdit = !!exercise

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty },
  } = useForm<CreateExerciseFormValues>({
    resolver: zodResolver(createExerciseSchema),
    defaultValues: {
      name: '',
      exerciseType: '' as ExerciseType,
      scheduledDate: '',
      description: '',
      location: '',
      startTime: '',
      endTime: '',
    },
    mode: 'onBlur',
  })

  // Watch name for character count
  const nameValue = watch('name')

  // Notify parent of dirty state changes
  useEffect(() => {
    onDirtyChange?.(isDirty)
  }, [isDirty, onDirtyChange])

  // Reset form with exercise data when editing
  useEffect(() => {
    if (exercise) {
      reset({
        name: exercise.name,
        exerciseType: exercise.exerciseType,
        scheduledDate: exercise.scheduledDate,
        description: exercise.description ?? '',
        location: exercise.location ?? '',
        startTime: exercise.startTime ?? '',
        endTime: exercise.endTime ?? '',
      })
    }
  }, [exercise, reset])

  const isFieldDisabled = (field: keyof CreateExerciseFormValues) =>
    disabledFields.includes(field)

  const onFormSubmit = handleSubmit(async (data) => {
    await onSubmit(data)
  })

  return (
    <form onSubmit={onFormSubmit}>
      <Stack spacing={CobraStyles.Spacing.FormFields}>
        {/* Name Field */}
        <Box>
          <Controller
            name="name"
            control={control}
            render={({ field }) => (
              <CobraTextField
                {...field}
                label="Name"
                error={!!errors.name}
                helperText={
                  errors.name?.message ||
                  `${nameValue?.length ?? 0}/${EXERCISE_FIELD_LIMITS.name}`
                }
                fullWidth
                required
                autoFocus={!isEdit}
                disabled={isFieldDisabled('name')}
                inputProps={{ maxLength: EXERCISE_FIELD_LIMITS.name }}
              />
            )}
          />
        </Box>

        {/* Exercise Type */}
        <Controller
          name="exerciseType"
          control={control}
          render={({ field }) => (
            <FormControl
              fullWidth
              size="small"
              required
              error={!!errors.exerciseType}
              disabled={isFieldDisabled('exerciseType')}
            >
              <InputLabel id="exercise-type-label">Exercise Type</InputLabel>
              <Select
                {...field}
                labelId="exercise-type-label"
                label="Exercise Type"
              >
                {EXERCISE_TYPES.map(type => (
                  <MenuItem key={type} value={type}>
                    {type} - {getExerciseTypeFullName(type)}
                  </MenuItem>
                ))}
              </Select>
              {errors.exerciseType && (
                <FormHelperText>{errors.exerciseType.message}</FormHelperText>
              )}
              {isFieldDisabled('exerciseType') && (
                <FormHelperText>Cannot modify during active exercise</FormHelperText>
              )}
            </FormControl>
          )}
        />

        {/* Scheduled Date */}
        <Controller
          name="scheduledDate"
          control={control}
          render={({ field }) => (
            <CobraTextField
              {...field}
              label="Scheduled Date"
              type="date"
              error={!!errors.scheduledDate}
              helperText={
                errors.scheduledDate?.message ||
                (isFieldDisabled('scheduledDate')
                  ? 'Cannot modify during active exercise'
                  : undefined)
              }
              fullWidth
              required
              disabled={isFieldDisabled('scheduledDate')}
              slotProps={{
                inputLabel: { shrink: true },
              }}
            />
          )}
        />

        {/* Start/End Time (row) */}
        <Stack direction="row" spacing={2}>
          <Controller
            name="startTime"
            control={control}
            render={({ field }) => (
              <CobraTextField
                {...field}
                label="Start Time"
                type="time"
                fullWidth
                disabled={isFieldDisabled('startTime')}
                slotProps={{
                  inputLabel: { shrink: true },
                }}
              />
            )}
          />
          <Controller
            name="endTime"
            control={control}
            render={({ field }) => (
              <CobraTextField
                {...field}
                label="End Time"
                type="time"
                error={!!errors.endTime}
                helperText={errors.endTime?.message}
                fullWidth
                disabled={isFieldDisabled('endTime')}
                slotProps={{
                  inputLabel: { shrink: true },
                }}
              />
            )}
          />
        </Stack>

        {/* Description */}
        <Controller
          name="description"
          control={control}
          render={({ field }) => (
            <CobraTextField
              {...field}
              label="Description"
              error={!!errors.description}
              helperText={errors.description?.message}
              fullWidth
              multiline
              rows={4}
              disabled={isFieldDisabled('description')}
            />
          )}
        />

        {/* Location */}
        <Controller
          name="location"
          control={control}
          render={({ field }) => (
            <CobraTextField
              {...field}
              label="Location"
              error={!!errors.location}
              helperText={errors.location?.message}
              fullWidth
              disabled={isFieldDisabled('location')}
            />
          )}
        />

        {/* Form Actions */}
        <Stack direction="row" justifyContent="flex-end" spacing={2} pt={2}>
          <CobraLinkButton onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </CobraLinkButton>
          <CobraPrimaryButton type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? 'Saving...'
              : isEdit
                ? 'Save Changes'
                : 'Create Exercise'}
          </CobraPrimaryButton>
        </Stack>
      </Stack>
    </form>
  )
}

export default ExerciseForm
