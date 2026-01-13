import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
} from '@mui/material'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import type { PhaseDto, PhaseFormValues } from '../types'
import { PHASE_FIELD_LIMITS } from '../types'

interface PhaseFormDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Phase to edit, or undefined for create */
  phase?: PhaseDto | null
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when form is submitted */
  onSubmit: (values: PhaseFormValues) => Promise<void>
  /** Whether the form is currently submitting */
  isSubmitting?: boolean
}

const INITIAL_VALUES: PhaseFormValues = {
  name: '',
  description: '',
}

/**
 * Dialog for creating or editing a phase
 */
export const PhaseFormDialog = ({
  open,
  phase,
  onClose,
  onSubmit,
  isSubmitting = false,
}: PhaseFormDialogProps) => {
  const isEditMode = !!phase

  const [values, setValues] = useState<PhaseFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<Partial<Record<keyof PhaseFormValues, string>>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof PhaseFormValues, boolean>>>({})

  // Reset form when dialog opens/closes or phase changes
  useEffect(() => {
    if (open) {
      if (phase) {
        setValues({
          name: phase.name,
          description: phase.description ?? '',
        })
      } else {
        setValues(INITIAL_VALUES)
      }
      setErrors({})
      setTouched({})
    }
  }, [open, phase])

  const handleChange = (field: keyof PhaseFormValues) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) => {
    const value = e.target.value
    setValues(prev => ({ ...prev, [field]: value }))

    // Clear error when field is modified
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: undefined }))
    }
  }

  const handleBlur = (field: keyof PhaseFormValues) => () => {
    setTouched(prev => ({ ...prev, [field]: true }))
    validateField(field)
  }

  const validateField = (field: keyof PhaseFormValues): boolean => {
    let error: string | undefined

    switch (field) {
      case 'name':
        if (!values.name.trim()) {
          error = 'Name is required'
        } else if (values.name.length < PHASE_FIELD_LIMITS.name.min) {
          error = `Name must be at least ${PHASE_FIELD_LIMITS.name.min} characters`
        } else if (values.name.length > PHASE_FIELD_LIMITS.name.max) {
          error = `Name must be ${PHASE_FIELD_LIMITS.name.max} characters or less`
        }
        break

      case 'description':
        if (values.description && values.description.length > PHASE_FIELD_LIMITS.description.max) {
          error = `Description must be ${PHASE_FIELD_LIMITS.description.max} characters or less`
        }
        break
    }

    setErrors(prev => ({ ...prev, [field]: error }))
    return !error
  }

  const validateForm = (): boolean => {
    const fieldsToValidate: (keyof PhaseFormValues)[] = ['name', 'description']

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
      description: values.description.trim(),
    })
  }

  const getFieldError = (field: keyof PhaseFormValues) => {
    return touched[field] ? errors[field] : undefined
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <form onSubmit={handleSubmit}>
        <DialogTitle>{isEditMode ? 'Edit Phase' : 'Add Phase'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <CobraTextField
              label="Phase Name"
              value={values.name}
              onChange={handleChange('name')}
              onBlur={handleBlur('name')}
              error={!!getFieldError('name')}
              helperText={
                getFieldError('name') ||
                `${values.name.length}/${PHASE_FIELD_LIMITS.name.max} characters`
              }
              required
              fullWidth
              autoFocus
              placeholder="e.g., Warning & Preparation"
            />

            <CobraTextField
              label="Description"
              value={values.description}
              onChange={handleChange('description')}
              onBlur={handleBlur('description')}
              error={!!getFieldError('description')}
              helperText={
                getFieldError('description') ||
                `Optional. ${values.description.length}/${PHASE_FIELD_LIMITS.description.max} characters`
              }
              fullWidth
              multiline
              rows={3}
              placeholder="Describe what happens during this phase..."
            />
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
                : 'Add Phase'}
          </CobraPrimaryButton>
        </DialogActions>
      </form>
    </Dialog>
  )
}

export default PhaseFormDialog
