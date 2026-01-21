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
import type { ObjectiveDto, ObjectiveFormValues } from '../types'
import { OBJECTIVE_FIELD_LIMITS } from '../types'

interface ObjectiveFormDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Objective to edit, or undefined for create */
  objective?: ObjectiveDto | null
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when form is submitted */
  onSubmit: (values: ObjectiveFormValues) => Promise<void>
  /** Whether the form is currently submitting */
  isSubmitting?: boolean
}

const INITIAL_VALUES: ObjectiveFormValues = {
  objectiveNumber: '',
  name: '',
  description: '',
}

/**
 * Dialog for creating or editing an objective
 */
export const ObjectiveFormDialog = ({
  open,
  objective,
  onClose,
  onSubmit,
  isSubmitting = false,
}: ObjectiveFormDialogProps) => {
  const isEditMode = !!objective

  const [values, setValues] = useState<ObjectiveFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<Partial<Record<keyof ObjectiveFormValues, string>>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof ObjectiveFormValues, boolean>>>({})

  // Reset form when dialog opens/closes or objective changes
  useEffect(() => {
    if (open) {
      if (objective) {
        setValues({
          objectiveNumber: objective.objectiveNumber,
          name: objective.name,
          description: objective.description ?? '',
        })
      } else {
        setValues(INITIAL_VALUES)
      }
      setErrors({})
      setTouched({})
    }
  }, [open, objective])

  const handleChange = (field: keyof ObjectiveFormValues) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const value = e.target.value
    setValues(prev => ({ ...prev, [field]: value }))

    // Clear error when field is modified
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: undefined }))
    }
  }

  const handleBlur = (field: keyof ObjectiveFormValues) => () => {
    setTouched(prev => ({ ...prev, [field]: true }))
    validateField(field)
  }

  const validateField = (field: keyof ObjectiveFormValues): boolean => {
    let error: string | undefined

    switch (field) {
      case 'objectiveNumber':
        if (values.objectiveNumber) {
          const max = OBJECTIVE_FIELD_LIMITS.objectiveNumber.max
          if (values.objectiveNumber.length > max) {
            error = `Objective number must be ${max} characters or less`
          }
        }
        break

      case 'name':
        if (!values.name.trim()) {
          error = 'Name is required'
        } else if (values.name.length < OBJECTIVE_FIELD_LIMITS.name.min) {
          error = `Name must be at least ${OBJECTIVE_FIELD_LIMITS.name.min} characters`
        } else if (values.name.length > OBJECTIVE_FIELD_LIMITS.name.max) {
          error = `Name must be ${OBJECTIVE_FIELD_LIMITS.name.max} characters or less`
        }
        break

      case 'description':
        if (values.description) {
          const max = OBJECTIVE_FIELD_LIMITS.description.max
          if (values.description.length > max) {
            error = `Description must be ${max} characters or less`
          }
        }
        break
    }

    setErrors(prev => ({ ...prev, [field]: error }))
    return !error
  }

  const validateForm = (): boolean => {
    const fieldsToValidate: (keyof ObjectiveFormValues)[] = ['objectiveNumber', 'name', 'description']

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
      objectiveNumber: values.objectiveNumber.trim(),
      name: values.name.trim(),
      description: values.description.trim(),
    })
  }

  const getFieldError = (field: keyof ObjectiveFormValues) => {
    return touched[field] ? errors[field] : undefined
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>{isEditMode ? 'Edit Objective' : 'Add Objective'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <CobraTextField
              label="Objective Number"
              value={values.objectiveNumber}
              onChange={handleChange('objectiveNumber')}
              onBlur={handleBlur('objectiveNumber')}
              error={!!getFieldError('objectiveNumber')}
              helperText={
                getFieldError('objectiveNumber') ||
                (isEditMode ? 'Required' : 'Optional - auto-assigned if blank')
              }
              fullWidth
              placeholder="e.g., 1, 1.1, A"
              required={isEditMode}
            />

            <CobraTextField
              label="Objective Name"
              value={values.name}
              onChange={handleChange('name')}
              onBlur={handleBlur('name')}
              error={!!getFieldError('name')}
              helperText={
                getFieldError('name') ||
                `${values.name.length}/${OBJECTIVE_FIELD_LIMITS.name.max} characters`
              }
              required
              fullWidth
              autoFocus
              placeholder="e.g., Demonstrate EOC activation procedures"
            />

            <CobraTextField
              label="Description"
              value={values.description}
              onChange={handleChange('description')}
              onBlur={handleBlur('description')}
              error={!!getFieldError('description')}
              helperText={
                getFieldError('description') ||
                `Optional. ${values.description.length}/${OBJECTIVE_FIELD_LIMITS.description.max}`
              }
              fullWidth
              multiline
              rows={4}
              placeholder="Describe what this objective aims to test or demonstrate..."
            />

            <Typography variant="caption" color="text.secondary">
              Per HSEEP, objectives should be SMART (Specific, Measurable,
              Achievable, Relevant, Time-bound).
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={onClose} disabled={isSubmitting}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? isEditMode
                ? 'Saving...'
                : 'Creating...'
              : isEditMode
                ? 'Save'
                : 'Add Objective'}
          </CobraPrimaryButton>
        </DialogActions>
      </form>
    </Dialog>
  )
}

export default ObjectiveFormDialog
