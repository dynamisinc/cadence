import { useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Stack,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  FormHelperText,
  Divider,
  Grid,
} from '@mui/material'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { InjectType, DeliveryMethod } from '../../../types'
import type {
  InjectDto,
  CreateInjectRequest,
  UpdateInjectRequest,
  InjectFormValues,
} from '../types'
import { INJECT_FIELD_LIMITS } from '../types'
import type { PhaseDto } from '../../phases/types'

interface InjectFormProps {
  /** Initial values for editing, or undefined for create */
  inject?: InjectDto | null
  /** Available phases for the dropdown */
  phases?: PhaseDto[]
  /** Called when form is submitted */
  onSubmit: (request: CreateInjectRequest | UpdateInjectRequest) => Promise<void>
  /** Called when cancel is clicked */
  onCancel: () => void
  /** Whether the form is currently submitting */
  isSubmitting?: boolean
}

const INITIAL_VALUES: InjectFormValues = {
  title: '',
  description: '',
  scheduledTime: '09:00',
  scenarioDay: '',
  scenarioTime: '',
  target: '',
  source: '',
  deliveryMethod: '',
  injectType: InjectType.Standard,
  expectedAction: '',
  controllerNotes: '',
  triggerCondition: '',
  phaseId: '',
}

/**
 * Form for creating or editing an inject
 *
 * Handles:
 * - All inject fields including dual time tracking
 * - Field validation with error messages
 * - Edit mode with pre-populated values
 */
export const InjectForm = ({
  inject,
  phases = [],
  onSubmit,
  onCancel,
  isSubmitting = false,
}: InjectFormProps) => {
  const isEditMode = !!inject

  const [values, setValues] = useState<InjectFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<Partial<Record<keyof InjectFormValues, string>>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof InjectFormValues, boolean>>>({})

  // Initialize form values from inject when editing
  useEffect(() => {
    if (inject) {
      setValues({
        title: inject.title,
        description: inject.description,
        scheduledTime: inject.scheduledTime.substring(0, 5), // HH:MM
        scenarioDay: inject.scenarioDay?.toString() ?? '',
        scenarioTime: inject.scenarioTime?.substring(0, 5) ?? '',
        target: inject.target,
        source: inject.source ?? '',
        deliveryMethod: inject.deliveryMethod ?? '',
        injectType: inject.injectType,
        expectedAction: inject.expectedAction ?? '',
        controllerNotes: inject.controllerNotes ?? '',
        triggerCondition: inject.triggerCondition ?? '',
        phaseId: inject.phaseId ?? '',
      })
    }
  }, [inject])

  const handleChange = (field: keyof InjectFormValues) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement> | { target: { value: unknown } },
  ) => {
    const value = e.target.value as string
    setValues((prev) => ({ ...prev, [field]: value }))

    // Clear error when field is modified
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }))
    }
  }

  const handleBlur = (field: keyof InjectFormValues) => () => {
    setTouched((prev) => ({ ...prev, [field]: true }))
    validateField(field)
  }

  const validateField = (field: keyof InjectFormValues): boolean => {
    let error: string | undefined

    switch (field) {
      case 'title':
        if (!values.title.trim()) {
          error = 'Title is required'
        } else if (values.title.length < INJECT_FIELD_LIMITS.title.min) {
          error = `Title must be at least ${INJECT_FIELD_LIMITS.title.min} characters`
        } else if (values.title.length > INJECT_FIELD_LIMITS.title.max) {
          error = `Title must be ${INJECT_FIELD_LIMITS.title.max} characters or less`
        }
        break

      case 'description':
        if (!values.description.trim()) {
          error = 'Description is required'
        } else if (values.description.length > INJECT_FIELD_LIMITS.description.max) {
          error = `Description must be ${INJECT_FIELD_LIMITS.description.max} characters or less`
        }
        break

      case 'target':
        if (!values.target.trim()) {
          error = 'Target is required'
        } else if (values.target.length > INJECT_FIELD_LIMITS.target.max) {
          error = `Target must be ${INJECT_FIELD_LIMITS.target.max} characters or less`
        }
        break

      case 'scheduledTime':
        if (!values.scheduledTime) {
          error = 'Scheduled time is required'
        }
        break

      case 'scenarioDay':
        if (values.scenarioDay) {
          const day = parseInt(values.scenarioDay, 10)
          if (isNaN(day) || day < INJECT_FIELD_LIMITS.scenarioDay.min || day > INJECT_FIELD_LIMITS.scenarioDay.max) {
            error = `Day must be between ${INJECT_FIELD_LIMITS.scenarioDay.min} and ${INJECT_FIELD_LIMITS.scenarioDay.max}`
          }
        }
        // Also check if scenario time is provided without day
        if (values.scenarioTime && !values.scenarioDay) {
          error = 'Day is required when time is provided'
        }
        break

      case 'source':
        if (values.source && values.source.length > INJECT_FIELD_LIMITS.source.max) {
          error = `Source must be ${INJECT_FIELD_LIMITS.source.max} characters or less`
        }
        break

      case 'expectedAction':
        if (values.expectedAction && values.expectedAction.length > INJECT_FIELD_LIMITS.expectedAction.max) {
          error = `Expected action must be ${INJECT_FIELD_LIMITS.expectedAction.max} characters or less`
        }
        break

      case 'controllerNotes':
        if (values.controllerNotes && values.controllerNotes.length > INJECT_FIELD_LIMITS.controllerNotes.max) {
          error = `Controller notes must be ${INJECT_FIELD_LIMITS.controllerNotes.max} characters or less`
        }
        break

      case 'triggerCondition':
        if (values.triggerCondition && values.triggerCondition.length > INJECT_FIELD_LIMITS.triggerCondition.max) {
          error = `Trigger condition must be ${INJECT_FIELD_LIMITS.triggerCondition.max} characters or less`
        }
        break
    }

    setErrors((prev) => ({ ...prev, [field]: error }))
    return !error
  }

  const validateForm = (): boolean => {
    const fieldsToValidate: (keyof InjectFormValues)[] = [
      'title',
      'description',
      'target',
      'scheduledTime',
      'scenarioDay',
      'source',
      'expectedAction',
      'controllerNotes',
      'triggerCondition',
    ]

    let isValid = true
    fieldsToValidate.forEach((field) => {
      if (!validateField(field)) {
        isValid = false
      }
    })

    // Mark all fields as touched
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

    const request: CreateInjectRequest | UpdateInjectRequest = {
      title: values.title.trim(),
      description: values.description.trim(),
      scheduledTime: `${values.scheduledTime}:00`, // Add seconds
      scenarioDay: values.scenarioDay ? parseInt(values.scenarioDay, 10) : null,
      scenarioTime: values.scenarioTime ? `${values.scenarioTime}:00` : null,
      target: values.target.trim(),
      source: values.source.trim() || null,
      deliveryMethod: (values.deliveryMethod as DeliveryMethod) || null,
      injectType: values.injectType,
      expectedAction: values.expectedAction.trim() || null,
      controllerNotes: values.controllerNotes.trim() || null,
      triggerCondition: values.triggerCondition.trim() || null,
      phaseId: values.phaseId || null,
    }

    await onSubmit(request)
  }

  const getFieldError = (field: keyof InjectFormValues) => {
    return touched[field] ? errors[field] : undefined
  }

  return (
    <Box component="form" onSubmit={handleSubmit}>
      <Stack spacing={3}>
        {/* Title */}
        <CobraTextField
          label="Title"
          value={values.title}
          onChange={handleChange('title')}
          onBlur={handleBlur('title')}
          error={!!getFieldError('title')}
          helperText={getFieldError('title') || `Brief description (${values.title.length}/${INJECT_FIELD_LIMITS.title.max})`}
          required
          fullWidth
          placeholder="e.g., County issues mandatory evacuation order"
        />

        {/* Time Section */}
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            TIME
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <CobraTextField
                label="Scheduled Time"
                type="time"
                value={values.scheduledTime}
                onChange={handleChange('scheduledTime')}
                onBlur={handleBlur('scheduledTime')}
                error={!!getFieldError('scheduledTime')}
                helperText={getFieldError('scheduledTime') || 'When to deliver (wall clock)'}
                required
                fullWidth
                slotProps={{
                  inputLabel: { shrink: true },
                }}
              />
            </Grid>
            <Grid size={{ xs: 6, sm: 3 }}>
              <CobraTextField
                label="Scenario Day"
                type="number"
                value={values.scenarioDay}
                onChange={handleChange('scenarioDay')}
                onBlur={handleBlur('scenarioDay')}
                error={!!getFieldError('scenarioDay')}
                helperText={getFieldError('scenarioDay') || 'Day 1, 2, 3...'}
                fullWidth
                slotProps={{
                  input: { inputProps: { min: 1, max: 99 } },
                }}
              />
            </Grid>
            <Grid size={{ xs: 6, sm: 3 }}>
              <CobraTextField
                label="Scenario Time"
                type="time"
                value={values.scenarioTime}
                onChange={handleChange('scenarioTime')}
                fullWidth
                helperText="Story time"
                slotProps={{
                  inputLabel: { shrink: true },
                }}
              />
            </Grid>
          </Grid>
        </Box>

        {/* Targeting Section */}
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            TARGETING
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 4 }}>
              <CobraTextField
                label="From (Source)"
                value={values.source}
                onChange={handleChange('source')}
                onBlur={handleBlur('source')}
                error={!!getFieldError('source')}
                helperText={getFieldError('source') || 'Simulated sender'}
                fullWidth
                placeholder="e.g., County Emergency Manager"
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <CobraTextField
                label="To (Target)"
                value={values.target}
                onChange={handleChange('target')}
                onBlur={handleBlur('target')}
                error={!!getFieldError('target')}
                helperText={getFieldError('target')}
                required
                fullWidth
                placeholder="e.g., EOC Director"
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Delivery Method</InputLabel>
                <Select
                  value={values.deliveryMethod}
                  onChange={handleChange('deliveryMethod')}
                  label="Delivery Method"
                >
                  <MenuItem value="">
                    <em>Not specified</em>
                  </MenuItem>
                  <MenuItem value={DeliveryMethod.Verbal}>Verbal</MenuItem>
                  <MenuItem value={DeliveryMethod.Phone}>Phone Call</MenuItem>
                  <MenuItem value={DeliveryMethod.Email}>Email</MenuItem>
                  <MenuItem value={DeliveryMethod.Radio}>Radio</MenuItem>
                  <MenuItem value={DeliveryMethod.Written}>Written Document</MenuItem>
                  <MenuItem value={DeliveryMethod.Simulation}>Simulation</MenuItem>
                  <MenuItem value={DeliveryMethod.Other}>Other</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </Box>

        {/* Content Section */}
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            CONTENT
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <Stack spacing={2}>
            <CobraTextField
              label="Description"
              value={values.description}
              onChange={handleChange('description')}
              onBlur={handleBlur('description')}
              error={!!getFieldError('description')}
              helperText={getFieldError('description') || `Full inject content (${values.description.length}/${INJECT_FIELD_LIMITS.description.max})`}
              required
              fullWidth
              multiline
              rows={4}
              placeholder="Describe what happens in this inject..."
            />

            <CobraTextField
              label="Expected Action"
              value={values.expectedAction}
              onChange={handleChange('expectedAction')}
              onBlur={handleBlur('expectedAction')}
              error={!!getFieldError('expectedAction')}
              helperText={getFieldError('expectedAction') || 'What players should do in response'}
              fullWidth
              multiline
              rows={3}
              placeholder="e.g., Acknowledge order, activate evacuation plan..."
            />
          </Stack>
        </Box>

        {/* Organization Section */}
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            ORGANIZATION
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Inject Type</InputLabel>
                <Select
                  value={values.injectType}
                  onChange={handleChange('injectType')}
                  label="Inject Type"
                >
                  <MenuItem value={InjectType.Standard}>Standard</MenuItem>
                  <MenuItem value={InjectType.Contingency}>Contingency</MenuItem>
                  <MenuItem value={InjectType.Adaptive}>Adaptive</MenuItem>
                  <MenuItem value={InjectType.Complexity}>Complexity</MenuItem>
                </Select>
                <FormHelperText>
                  Standard for normal delivery, others for conditional use
                </FormHelperText>
              </FormControl>
            </Grid>
            <Grid size={{ xs: 12, sm: 6 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Phase</InputLabel>
                <Select
                  value={values.phaseId}
                  onChange={handleChange('phaseId')}
                  label="Phase"
                >
                  <MenuItem value="">
                    <em>Not assigned</em>
                  </MenuItem>
                  {phases.map((phase) => (
                    <MenuItem key={phase.id} value={phase.id}>
                      {phase.name}
                    </MenuItem>
                  ))}
                </Select>
                <FormHelperText>
                  Exercise phase for grouping
                </FormHelperText>
              </FormControl>
            </Grid>
          </Grid>

          {(values.injectType === InjectType.Contingency ||
            values.injectType === InjectType.Adaptive ||
            values.injectType === InjectType.Complexity) && (
            <Box sx={{ mt: 2 }}>
              <CobraTextField
                label="Trigger Condition"
                value={values.triggerCondition}
                onChange={handleChange('triggerCondition')}
                onBlur={handleBlur('triggerCondition')}
                error={!!getFieldError('triggerCondition')}
                helperText={getFieldError('triggerCondition') || 'When to fire this inject'}
                fullWidth
                multiline
                rows={2}
                placeholder="e.g., Use if players are ahead of schedule or evacuation discussion is too smooth"
              />
            </Box>
          )}
        </Box>

        {/* Controller Notes */}
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            CONTROLLER NOTES (Internal)
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <CobraTextField
            label="Controller Notes"
            value={values.controllerNotes}
            onChange={handleChange('controllerNotes')}
            onBlur={handleBlur('controllerNotes')}
            error={!!getFieldError('controllerNotes')}
            helperText={getFieldError('controllerNotes') || 'Private guidance for the person delivering this inject'}
            fullWidth
            multiline
            rows={3}
            placeholder="e.g., Deliver with urgency. Have evacuation zone map ready."
          />
        </Box>

        {/* Form Actions */}
        <Stack direction="row" spacing={2} justifyContent="flex-end">
          <CobraSecondaryButton onClick={onCancel} disabled={isSubmitting}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton type="submit" disabled={isSubmitting}>
            {isSubmitting
              ? isEditMode
                ? 'Saving...'
                : 'Creating...'
              : isEditMode
                ? 'Save Changes'
                : 'Create Inject'}
          </CobraPrimaryButton>
        </Stack>
      </Stack>
    </Box>
  )
}

export default InjectForm
