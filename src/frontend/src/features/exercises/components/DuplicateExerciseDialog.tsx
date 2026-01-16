import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
  Typography,
} from '@mui/material'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import type { ExerciseDto, DuplicateExerciseRequest } from '../types'
import { EXERCISE_FIELD_LIMITS } from '../types'

interface DuplicateExerciseDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** The exercise to duplicate */
  exercise: ExerciseDto | null
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when form is submitted */
  onSubmit: (request: DuplicateExerciseRequest) => Promise<void>
  /** Whether the form is currently submitting */
  isSubmitting?: boolean
}

interface FormValues {
  name: string
  scheduledDate: string
}

/**
 * Dialog for duplicating an exercise with optional new name and date
 */
export const DuplicateExerciseDialog = ({
  open,
  exercise,
  onClose,
  onSubmit,
  isSubmitting = false,
}: DuplicateExerciseDialogProps) => {
  const [values, setValues] = useState<FormValues>({
    name: '',
    scheduledDate: '',
  })
  const [errors, setErrors] = useState<Partial<Record<keyof FormValues, string>>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof FormValues, boolean>>>({})

  // Reset form when dialog opens or exercise changes
  useEffect(() => {
    if (open && exercise) {
      setValues({
        name: `Copy of ${exercise.name}`,
        scheduledDate: exercise.scheduledDate,
      })
      setErrors({})
      setTouched({})
    }
  }, [open, exercise])

  const handleChange =
    (field: keyof FormValues) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const value = e.target.value
      setValues(prev => ({ ...prev, [field]: value }))

      // Clear error when field is modified
      if (errors[field]) {
        setErrors(prev => ({ ...prev, [field]: undefined }))
      }
    }

  const handleBlur = (field: keyof FormValues) => () => {
    setTouched(prev => ({ ...prev, [field]: true }))
    validateField(field)
  }

  const validateField = (field: keyof FormValues): boolean => {
    let error: string | undefined

    switch (field) {
      case 'name':
        if (!values.name.trim()) {
          error = 'Name is required'
        } else if (values.name.length > EXERCISE_FIELD_LIMITS.name.max) {
          error = `Name must be ${EXERCISE_FIELD_LIMITS.name.max} characters or less`
        }
        break

      case 'scheduledDate':
        if (!values.scheduledDate) {
          error = 'Scheduled date is required'
        }
        break
    }

    setErrors(prev => ({ ...prev, [field]: error }))
    return !error
  }

  const validateForm = (): boolean => {
    const fieldsToValidate: (keyof FormValues)[] = ['name', 'scheduledDate']

    let isValid = true
    fieldsToValidate.forEach(field => {
      if (!validateField(field)) {
        isValid = false
      }
    })

    setTouched(
      fieldsToValidate.reduce(
        (acc, field) => ({ ...acc, [field]: true }),
        {},
      ),
    )

    return isValid
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) {
      return
    }

    await onSubmit({
      name: values.name.trim(),
      scheduledDate: values.scheduledDate,
    })
  }

  const getFieldError = (field: keyof FormValues) => {
    return touched[field] ? errors[field] : undefined
  }

  if (!exercise) {
    return null
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>Duplicate Exercise</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="body2" color="text.secondary">
              Create a copy of "{exercise.name}" with all its phases, objectives, and
              injects. The new exercise will start in Draft status.
            </Typography>

            <CobraTextField
              label="New Exercise Name"
              value={values.name}
              onChange={handleChange('name')}
              onBlur={handleBlur('name')}
              error={!!getFieldError('name')}
              helperText={
                getFieldError('name') ||
                `${values.name.length}/${EXERCISE_FIELD_LIMITS.name.max} characters`
              }
              required
              fullWidth
              autoFocus
            />

            <CobraTextField
              label="Scheduled Date"
              type="date"
              value={values.scheduledDate}
              onChange={handleChange('scheduledDate')}
              onBlur={handleBlur('scheduledDate')}
              error={!!getFieldError('scheduledDate')}
              helperText={getFieldError('scheduledDate')}
              required
              fullWidth
              slotProps={{
                inputLabel: { shrink: true },
              }}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={onClose} disabled={isSubmitting}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Duplicating...' : 'Duplicate Exercise'}
          </CobraPrimaryButton>
        </DialogActions>
      </form>
    </Dialog>
  )
}

export default DuplicateExerciseDialog
