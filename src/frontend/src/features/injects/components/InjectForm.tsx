import { useState, useEffect, useRef } from 'react'
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
  Autocomplete,
  Chip,
} from '@mui/material'
import { toast } from 'react-toastify'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { InjectType, DeliveryMethod, TriggerType } from '../../../types'
import type {
  InjectDto,
  CreateInjectRequest,
  UpdateInjectRequest,
  InjectFormValues,
} from '../types'
import { INJECT_FIELD_LIMITS } from '../types'
import type { PhaseDto } from '../../phases/types'
import { useObjectiveSummaries } from '../../objectives/hooks/useObjectives'
import type { ObjectiveSummaryDto } from '../../objectives/types'
import { useDeliveryMethods } from '../../delivery-methods'
import { ExpectedOutcomesList } from '../../expected-outcomes'

interface InjectFormProps {
  /** Exercise ID for loading objectives */
  exerciseId: string
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
  deliveryTime: '',
  scenarioDay: '',
  scenarioTime: '',
  target: '',
  source: '',
  deliveryMethod: '',
  deliveryMethodId: '',
  deliveryMethodOther: '',
  injectType: InjectType.Standard,
  expectedAction: '',
  controllerNotes: '',
  triggerCondition: '',
  phaseId: '',
  objectiveIds: [],
  // Phase G fields
  sourceReference: '',
  priority: '',
  triggerType: TriggerType.Manual,
  responsibleController: '',
  locationName: '',
  locationType: '',
  track: '',
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
  exerciseId,
  inject,
  phases = [],
  onSubmit,
  onCancel,
  isSubmitting = false,
}: InjectFormProps) => {
  const isEditMode = !!inject
  const { summaries: objectives } = useObjectiveSummaries(exerciseId)
  const { data: deliveryMethods = [] } = useDeliveryMethods()

  const [values, setValues] = useState<InjectFormValues>(INITIAL_VALUES)
  const [errors, setErrors] = useState<Partial<Record<keyof InjectFormValues, string>>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof InjectFormValues, boolean>>>({})

  // Find if selected delivery method is "Other"
  const selectedDeliveryMethod = deliveryMethods.find(dm => dm.id === values.deliveryMethodId)
  const showOtherInput = selectedDeliveryMethod?.isOther ?? false

  // Initialize form values from inject when editing
  useEffect(() => {
    if (inject) {
      setValues({
        title: inject.title,
        description: inject.description,
        scheduledTime: inject.scheduledTime.substring(0, 5), // HH:MM
        deliveryTime: inject.deliveryTime ?? '',
        scenarioDay: inject.scenarioDay?.toString() ?? '',
        scenarioTime: inject.scenarioTime?.substring(0, 5) ?? '',
        target: inject.target,
        source: inject.source ?? '',
        deliveryMethod: inject.deliveryMethod ?? '',
        deliveryMethodId: inject.deliveryMethodId ?? '',
        deliveryMethodOther: inject.deliveryMethodOther ?? '',
        injectType: inject.injectType,
        expectedAction: inject.expectedAction ?? '',
        controllerNotes: inject.controllerNotes ?? '',
        triggerCondition: inject.triggerCondition ?? '',
        phaseId: inject.phaseId ?? '',
        objectiveIds: inject.objectiveIds ?? [],
        // Phase G fields
        sourceReference: inject.sourceReference ?? '',
        priority: inject.priority?.toString() ?? '',
        triggerType: inject.triggerType ?? TriggerType.Manual,
        responsibleController: inject.responsibleController ?? '',
        locationName: inject.locationName ?? '',
        locationType: inject.locationType ?? '',
        track: inject.track ?? '',
      })
    }
  }, [inject])

  const handleChange = (field: keyof InjectFormValues) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement> | { target: { value: unknown } },
  ) => {
    const value = e.target.value as string
    setValues(prev => ({ ...prev, [field]: value }))

    // Clear error when field is modified
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: undefined }))
    }
  }

  const handleBlur = (field: keyof InjectFormValues) => () => {
    setTouched(prev => ({ ...prev, [field]: true }))
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
          const minDay = INJECT_FIELD_LIMITS.scenarioDay.min
          const maxDay = INJECT_FIELD_LIMITS.scenarioDay.max
          if (isNaN(day) || day < minDay || day > maxDay) {
            error = `Day must be between ${minDay} and ${maxDay}`
          }
        }
        // Also check if scenario time is provided without day
        if (values.scenarioTime && !values.scenarioDay) {
          error = 'Day is required when time is provided'
        }
        break

      case 'source':
        if (values.source && values.source.length > INJECT_FIELD_LIMITS.source.max) {
          error = `Source must be ${INJECT_FIELD_LIMITS.source.max} chars or less`
        }
        break

      case 'expectedAction':
        if (values.expectedAction) {
          const maxLen = INJECT_FIELD_LIMITS.expectedAction.max
          if (values.expectedAction.length > maxLen) {
            error = `Expected action must be ${maxLen} characters or less`
          }
        }
        break

      case 'controllerNotes':
        if (values.controllerNotes) {
          const maxLen = INJECT_FIELD_LIMITS.controllerNotes.max
          if (values.controllerNotes.length > maxLen) {
            error = `Controller notes must be ${maxLen} characters or less`
          }
        }
        break

      case 'triggerCondition':
        if (values.triggerCondition) {
          const maxLen = INJECT_FIELD_LIMITS.triggerCondition.max
          if (values.triggerCondition.length > maxLen) {
            error = `Trigger condition must be ${maxLen} characters or less`
          }
        }
        break
    }

    setErrors(prev => ({ ...prev, [field]: error }))
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
    fieldsToValidate.forEach(field => {
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
      deliveryMethodId: values.deliveryMethodId || null,
      deliveryMethodOther: values.deliveryMethodOther.trim() || null,
      injectType: values.injectType,
      expectedAction: values.expectedAction.trim() || null,
      controllerNotes: values.controllerNotes.trim() || null,
      triggerCondition: values.triggerCondition.trim() || null,
      phaseId: values.phaseId || null,
      objectiveIds: values.objectiveIds.length > 0 ? values.objectiveIds : null,
      // Phase G fields
      sourceReference: values.sourceReference.trim() || null,
      priority: values.priority ? parseInt(values.priority, 10) : null,
      triggerType: values.triggerType,
      responsibleController: values.responsibleController.trim() || null,
      locationName: values.locationName.trim() || null,
      locationType: values.locationType.trim() || null,
      track: values.track.trim() || null,
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
                  value={values.deliveryMethodId}
                  onChange={handleChange('deliveryMethodId')}
                  label="Delivery Method"
                >
                  <MenuItem value="">
                    <em>Not specified</em>
                  </MenuItem>
                  {deliveryMethods.map(dm => (
                    <MenuItem key={dm.id} value={dm.id}>
                      {dm.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Grid>
            {showOtherInput && (
              <Grid size={{ xs: 12 }}>
                <CobraTextField
                  label="Specify Delivery Method"
                  value={values.deliveryMethodOther}
                  onChange={handleChange('deliveryMethodOther')}
                  fullWidth
                  placeholder="e.g., Messenger, Fax, etc."
                  helperText={`Custom delivery method (${values.deliveryMethodOther.length}/${INJECT_FIELD_LIMITS.deliveryMethodOther.max})`}
                />
              </Grid>
            )}
          </Grid>
        </Box>

        {/* Location & Track Section (Phase G) */}
        <Box>
          <Typography variant="subtitle2" color="text.secondary" gutterBottom>
            LOCATION & TRACK
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, sm: 4 }}>
              <CobraTextField
                label="Location Name"
                value={values.locationName}
                onChange={handleChange('locationName')}
                fullWidth
                placeholder="e.g., Main EOC, Stadium A"
                helperText="Where this inject takes place"
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <CobraTextField
                label="Location Type"
                value={values.locationType}
                onChange={handleChange('locationType')}
                fullWidth
                placeholder="e.g., EOC, Hospital, Field"
                helperText="Category of location"
              />
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <CobraTextField
                label="Track"
                value={values.track}
                onChange={handleChange('track')}
                fullWidth
                placeholder="e.g., LAFD, LAPD, EOC"
                helperText="Agency grouping"
              />
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
            <Grid size={{ xs: 12, sm: 4 }}>
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
            <Grid size={{ xs: 12, sm: 4 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Trigger Type</InputLabel>
                <Select
                  value={values.triggerType}
                  onChange={handleChange('triggerType')}
                  label="Trigger Type"
                >
                  <MenuItem value={TriggerType.Manual}>Manual</MenuItem>
                  <MenuItem value={TriggerType.Scheduled}>Scheduled</MenuItem>
                  <MenuItem value={TriggerType.Conditional}>Conditional</MenuItem>
                </Select>
                <FormHelperText>
                  How this inject is triggered
                </FormHelperText>
              </FormControl>
            </Grid>
            <Grid size={{ xs: 12, sm: 4 }}>
              <FormControl fullWidth size="small">
                <InputLabel>Priority</InputLabel>
                <Select
                  value={values.priority}
                  onChange={handleChange('priority')}
                  label="Priority"
                >
                  <MenuItem value="">
                    <em>Not set</em>
                  </MenuItem>
                  <MenuItem value="1">1 - Critical</MenuItem>
                  <MenuItem value="2">2 - High</MenuItem>
                  <MenuItem value="3">3 - Medium</MenuItem>
                  <MenuItem value="4">4 - Low</MenuItem>
                  <MenuItem value="5">5 - Informational</MenuItem>
                </Select>
                <FormHelperText>
                  Importance level
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
                  {phases.map(phase => (
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
            <Grid size={{ xs: 12, sm: 6 }}>
              <CobraTextField
                label="Responsible Controller"
                value={values.responsibleController}
                onChange={handleChange('responsibleController')}
                fullWidth
                placeholder="e.g., John Smith, Fire Lead"
                helperText="Who is responsible for firing this inject"
              />
            </Grid>
            <Grid size={{ xs: 12 }}>
              <Autocomplete
                multiple
                size="small"
                options={objectives}
                getOptionLabel={(option: ObjectiveSummaryDto) =>
                  `${option.objectiveNumber}. ${option.name}`
                }
                value={objectives.filter(obj => values.objectiveIds.includes(obj.id))}
                onChange={(_, newValue) => {
                  setValues(prev => ({
                    ...prev,
                    objectiveIds: newValue.map(obj => obj.id),
                  }))
                }}
                isOptionEqualToValue={(option, value) => option.id === value.id}
                renderInput={params => (
                  <CobraTextField
                    {...params}
                    label="Linked Objectives"
                    placeholder={values.objectiveIds.length === 0 ? 'Select objectives...' : ''}
                    helperText="Objectives this inject tests"
                  />
                )}
                renderTags={(value, getTagProps) =>
                  value.map((option, index) => {
                    const { key, ...tagProps } = getTagProps({ index })
                    return (
                      <Chip
                        key={key}
                        label={`${option.objectiveNumber}. ${option.name}`}
                        size="small"
                        {...tagProps}
                      />
                    )
                  })
                }
                noOptionsText="No objectives defined for this exercise"
              />
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

        {/* Expected Outcomes (only in edit mode) */}
        {isEditMode && inject && (
          <ExpectedOutcomesList injectId={inject.id} isEditable={true} />
        )}

        {/* Import Reference (Phase G) */}
        {isEditMode && values.sourceReference && (
          <Box>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              IMPORT REFERENCE
            </Typography>
            <Divider sx={{ mb: 2 }} />

            <CobraTextField
              label="Source Reference"
              value={values.sourceReference}
              onChange={handleChange('sourceReference')}
              fullWidth
              disabled
              helperText="Original ID from imported file (read-only)"
            />
          </Box>
        )}

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
