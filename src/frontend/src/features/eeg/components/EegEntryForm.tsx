/**
 * EegEntryForm Component
 *
 * Form for recording structured EEG entries against Critical Tasks.
 * Used by evaluators during exercise conduct.
 */

import { useState, useEffect, useMemo } from 'react'
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
  Paper,
  Alert,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faXmark, faLightbulb } from '@fortawesome/free-solid-svg-icons'
import { toast } from 'react-toastify'
import { format, parseISO } from 'date-fns'

import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { PerformanceRatingSelector } from './PerformanceRatingSelector'
import { useCapabilityTargets } from '../hooks/useCapabilityTargets'
import { useCriticalTasks } from '../hooks/useCriticalTasks'
import { useEegEntries, useEegEntriesByTask } from '../hooks/useEegEntries'
import type {
  PerformanceRating,
  CreateEegEntryRequest,
  UpdateEegEntryRequest,
  EegEntryDto,
} from '../types'

interface InjectOption {
  id: string
  injectNumber: number
  title: string
  scheduledTime: string
}

interface EegEntryFormProps {
  /** Exercise ID */
  exerciseId: string
  /** Entry to edit (when in edit mode) */
  editEntry?: EegEntryDto | null
  /** Pre-selected triggering inject (when opened from inject's Assess button) */
  triggeringInject?: InjectOption | null
  /** Critical tasks linked to the triggering inject (for smart defaults) */
  linkedCriticalTaskIds?: string[]
  /** Available injects for the triggering inject selector */
  availableInjects?: InjectOption[]
  /** Current exercise time display string */
  exerciseTime?: string
  /** Pre-selected capability target ID (from coverage dashboard) */
  preSelectedCapabilityTargetId?: string
  /** Pre-selected critical task ID (from coverage dashboard) */
  preSelectedTaskId?: string
  /** Called when form should close */
  onClose?: () => void
  /** Called after successful save */
  onSaved?: () => void
}

interface FormValues {
  capabilityTargetId: string
  criticalTaskId: string
  observationText: string
  rating: PerformanceRating | null
  triggeringInjectId: string
  observedAt: string // ISO string for datetime-local input
}

// Helper to get current datetime in local format for datetime-local input
const getCurrentLocalDatetime = () => {
  const now = new Date()
  // Format as YYYY-MM-DDTHH:mm (required format for datetime-local input)
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  const hours = String(now.getHours()).padStart(2, '0')
  const minutes = String(now.getMinutes()).padStart(2, '0')
  return `${year}-${month}-${day}T${hours}:${minutes}`
}

// Helper to convert UTC ISO string to local datetime-local format
const utcToLocalDatetimeInput = (utcStr: string) => {
  const date = parseISO(utcStr.endsWith('Z') ? utcStr : `${utcStr}Z`)
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${year}-${month}-${day}T${hours}:${minutes}`
}

// Helper to convert local datetime-local value to ISO string for API
const localDatetimeInputToIso = (localStr: string) => {
  // Parse the local datetime string and convert to ISO
  const date = new Date(localStr)
  return date.toISOString()
}

const INITIAL_VALUES: FormValues = {
  capabilityTargetId: '',
  criticalTaskId: '',
  observationText: '',
  rating: null,
  triggeringInjectId: '',
  observedAt: '', // Will be set on component mount
}

const OBSERVATION_MIN_LENGTH = 10
const OBSERVATION_MAX_LENGTH = 2000

/**
 * EEG Entry form for evaluators to record structured observations.
 *
 * Features:
 * - Cascading selectors (Capability Target -> Critical Tasks)
 * - P/S/M/U rating selection with HSEEP descriptions
 * - Optional triggering inject linkage
 * - Quick entry mode (Save & Continue)
 * - Smart defaults from linked inject tasks
 */
export const EegEntryForm = ({
  exerciseId,
  editEntry,
  triggeringInject,
  linkedCriticalTaskIds = [],
  availableInjects = [],
  exerciseTime,
  preSelectedCapabilityTargetId,
  preSelectedTaskId,
  onClose,
  onSaved,
}: EegEntryFormProps) => {
  // Determine if in edit mode
  const isEditMode = !!editEntry

  // Initialize form values - use edit entry values if in edit mode
  const [values, setValues] = useState<FormValues>(() => {
    if (editEntry) {
      return {
        capabilityTargetId: editEntry.criticalTask.capabilityTargetId,
        criticalTaskId: editEntry.criticalTaskId,
        observationText: editEntry.observationText,
        rating: editEntry.rating,
        triggeringInjectId: editEntry.triggeringInjectId ?? '',
        observedAt: utcToLocalDatetimeInput(editEntry.observedAt),
      }
    }
    return {
      ...INITIAL_VALUES,
      triggeringInjectId: triggeringInject?.id ?? '',
      capabilityTargetId: preSelectedCapabilityTargetId ?? '',
      observedAt: getCurrentLocalDatetime(),
    }
  })
  const [errors, setErrors] = useState<Partial<Record<keyof FormValues, string>>>({})
  const [touched, setTouched] = useState<Partial<Record<keyof FormValues, boolean>>>({})

  // Fetch capability targets for the exercise
  const { capabilityTargets, loading: loadingTargets } = useCapabilityTargets(exerciseId)

  // Fetch critical tasks for selected capability target
  const { criticalTasks, loading: loadingTasks } =
    useCriticalTasks(exerciseId, values.capabilityTargetId)

  // EEG entry mutations - use task-level hook for updates
  const { createEntry, isCreating } = useEegEntries(exerciseId)
  const { updateEegEntry, isUpdating } = useEegEntriesByTask(exerciseId, editEntry?.criticalTaskId ?? '')

  // Selected critical task details
  const selectedTask = useMemo(
    () => criticalTasks.find(t => t.id === values.criticalTaskId),
    [criticalTasks, values.criticalTaskId],
  )

  // Get linked tasks (smart defaults when opened from inject)
  const linkedTasks = useMemo(() => {
    if (linkedCriticalTaskIds.length === 0) return []
    return criticalTasks.filter(t => linkedCriticalTaskIds.includes(t.id))
  }, [criticalTasks, linkedCriticalTaskIds])

  // Handle triggering inject change
  useEffect(() => {
    if (triggeringInject) {
      setValues(prev => ({
        ...prev,
        triggeringInjectId: triggeringInject.id,
      }))
    }
  }, [triggeringInject])

  // Handle pre-selected task from coverage dashboard
  useEffect(() => {
    if (preSelectedTaskId && criticalTasks.length > 0) {
      // Check if the pre-selected task is in the loaded critical tasks
      const taskExists = criticalTasks.some(t => t.id === preSelectedTaskId)
      if (taskExists) {
        setValues(prev => ({
          ...prev,
          criticalTaskId: preSelectedTaskId,
        }))
      }
    }
  }, [preSelectedTaskId, criticalTasks])

  // When capability target changes, reset critical task (only in create mode)
  useEffect(() => {
    if (!isEditMode) {
      setValues(prev => ({
        ...prev,
        criticalTaskId: '',
      }))
    }
  }, [values.capabilityTargetId, isEditMode])

  const handleChange = (field: keyof FormValues) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement> | { target: { value: unknown } },
  ) => {
    const value = e.target.value as string
    setValues(prev => ({ ...prev, [field]: value }))

    // Clear error when field is modified
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: undefined }))
    }
  }

  const handleRatingChange = (rating: PerformanceRating | null) => {
    setValues(prev => ({ ...prev, rating }))
    if (errors.rating) {
      setErrors(prev => ({ ...prev, rating: undefined }))
    }
  }

  const handleBlur = (field: keyof FormValues) => () => {
    setTouched(prev => ({ ...prev, [field]: true }))
    validateField(field)
  }

  const validateField = (field: keyof FormValues): boolean => {
    let error: string | undefined

    switch (field) {
      case 'criticalTaskId':
        if (!values.criticalTaskId) {
          error = 'Critical Task is required'
        }
        break

      case 'observationText':
        if (!values.observationText.trim()) {
          error = 'Observation is required'
        } else if (values.observationText.length > OBSERVATION_MAX_LENGTH) {
          error = `Observation must be ${OBSERVATION_MAX_LENGTH} characters or less`
        }
        break

      case 'rating':
        if (!values.rating) {
          error = 'Performance rating is required'
        }
        break
    }

    setErrors(prev => ({ ...prev, [field]: error }))
    return !error
  }

  const validateForm = (): { isValid: boolean; errorMessages: string[] } => {
    const fieldsToValidate: (keyof FormValues)[] = ['criticalTaskId', 'observationText', 'rating']
    const errorMessages: string[] = []

    fieldsToValidate.forEach(field => {
      if (!validateField(field)) {
        const error = errors[field]
        if (error) errorMessages.push(error)
      }
    })

    // Also validate required fields directly
    if (!values.criticalTaskId) errorMessages.push('Critical Task is required')
    if (!values.observationText.trim()) errorMessages.push('Observation is required')
    if (!values.rating) errorMessages.push('Performance rating is required')

    setTouched(
      fieldsToValidate.reduce((acc, field) => ({ ...acc, [field]: true }), {}),
    )

    return { isValid: errorMessages.length === 0, errorMessages: [...new Set(errorMessages)] }
  }

  const handleSubmit = async (continueEntry: boolean = false) => {
    const { isValid, errorMessages } = validateForm()
    if (!isValid) {
      toast.error(errorMessages[0] || 'Please fix the validation errors')
      return
    }

    // Show warning for short observations
    if (values.observationText.length < OBSERVATION_MIN_LENGTH) {
      toast.warn('Consider adding more detail to your observation')
    }

    try {
      if (isEditMode && editEntry) {
        // Update existing entry
        const updateRequest: UpdateEegEntryRequest = {
          observationText: values.observationText.trim(),
          rating: values.rating!,
          triggeringInjectId: values.triggeringInjectId || null,
          observedAt: values.observedAt ? localDatetimeInputToIso(values.observedAt) : undefined,
        }
        await updateEegEntry(editEntry.id, updateRequest)
        toast.success('EEG entry updated')
        onSaved?.()
        onClose?.()
      } else {
        // Create new entry
        const request: CreateEegEntryRequest = {
          criticalTaskId: values.criticalTaskId,
          observationText: values.observationText.trim(),
          rating: values.rating!,
          triggeringInjectId: values.triggeringInjectId || null,
          observedAt: values.observedAt ? localDatetimeInputToIso(values.observedAt) : undefined,
        }
        await createEntry(values.criticalTaskId, request)
        toast.success('EEG entry saved')

        if (continueEntry) {
          // Reset form but keep capability target and triggering inject, reset time to now
          setValues(prev => ({
            ...INITIAL_VALUES,
            capabilityTargetId: prev.capabilityTargetId,
            triggeringInjectId: prev.triggeringInjectId,
            observedAt: getCurrentLocalDatetime(),
          }))
          setErrors({})
          setTouched({})
        } else {
          onSaved?.()
          onClose?.()
        }
      }
    } catch {
      // Error is handled by the hook
    }
  }

  const handleCancel = () => {
    const hasContent =
      values.criticalTaskId ||
      values.observationText.trim() ||
      values.rating

    if (hasContent) {
      // TODO: Show discard confirmation dialog
      onClose?.()
    } else {
      onClose?.()
    }
  }

  const getFieldError = (field: keyof FormValues) => {
    return touched[field] ? errors[field] : undefined
  }

  return (
    <Paper sx={{ p: 3, position: 'relative' }}>
      {/* Header */}
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" mb={2}>
        <Box>
          <Typography variant="h6">{isEditMode ? 'Edit EEG Entry' : '+ EEG Entry'}</Typography>
          {isEditMode && editEntry && (
            <Typography variant="body2" color="text.secondary">
              Originally recorded: {format(parseISO(editEntry.recordedAt.endsWith('Z') ? editEntry.recordedAt : `${editEntry.recordedAt}Z`), 'h:mm a')} by{' '}
              {editEntry.evaluatorName ?? 'Unknown'}
            </Typography>
          )}
          {!isEditMode && triggeringInject && (
            <Typography variant="body2" color="text.secondary">
              Assessing: INJ-{triggeringInject.injectNumber.toString().padStart(3, '0')} -{' '}
              {triggeringInject.title}
            </Typography>
          )}
        </Box>
        <Stack direction="row" alignItems="center" spacing={2}>
          {exerciseTime && (
            <Typography variant="body2" color="text.secondary">
              Exercise Time: {exerciseTime}
            </Typography>
          )}
          {onClose && (
            <IconButton onClick={handleCancel} size="small">
              <FontAwesomeIcon icon={faXmark} />
            </IconButton>
          )}
        </Stack>
      </Stack>

      <Divider sx={{ mb: 3 }} />

      {/* Linked Tasks Hint */}
      {linkedTasks.length > 0 && (
        <Alert
          severity="info"
          icon={<FontAwesomeIcon icon={faLightbulb} />}
          sx={{ mb: 3 }}
        >
          <Typography variant="subtitle2">
            This inject tests these Critical Tasks:
          </Typography>
          <Box component="ul" sx={{ m: 0, pl: 2 }}>
            {linkedTasks.map(task => (
              <li key={task.id}>
                <Typography variant="body2">{task.taskDescription}</Typography>
              </li>
            ))}
          </Box>
        </Alert>
      )}

      <Stack spacing={3}>
        {/* Capability Target Selector */}
        <FormControl fullWidth size="small" error={!!getFieldError('capabilityTargetId')}>
          <InputLabel>Capability Target *</InputLabel>
          <Select
            value={values.capabilityTargetId}
            onChange={handleChange('capabilityTargetId')}
            onBlur={handleBlur('capabilityTargetId')}
            label="Capability Target *"
            disabled={loadingTargets || isEditMode}
          >
            <MenuItem value="">
              <em>Select a capability target...</em>
            </MenuItem>
            {capabilityTargets.map(target => (
              <MenuItem key={target.id} value={target.id}>
                {target.capability.name}
              </MenuItem>
            ))}
          </Select>
          {values.capabilityTargetId && (
            <FormHelperText sx={{ color: 'text.secondary' }}>
              {capabilityTargets.find(t => t.id === values.capabilityTargetId)?.targetDescription}
            </FormHelperText>
          )}
        </FormControl>

        {/* Critical Task Selector */}
        <FormControl
          fullWidth
          size="small"
          error={!!getFieldError('criticalTaskId')}
          disabled={!values.capabilityTargetId || isEditMode}
        >
          <InputLabel>Critical Task *</InputLabel>
          <Select
            value={values.criticalTaskId}
            onChange={handleChange('criticalTaskId')}
            onBlur={handleBlur('criticalTaskId')}
            label="Critical Task *"
            disabled={!values.capabilityTargetId || loadingTasks || isEditMode}
          >
            <MenuItem value="">
              <em>Select a critical task...</em>
            </MenuItem>
            {criticalTasks.map(task => (
              <MenuItem key={task.id} value={task.id}>
                {task.taskDescription}
              </MenuItem>
            ))}
          </Select>
          {getFieldError('criticalTaskId') && (
            <FormHelperText error>{getFieldError('criticalTaskId')}</FormHelperText>
          )}
          {selectedTask?.standard && (
            <FormHelperText sx={{ color: 'text.secondary' }}>
              Standard: {selectedTask.standard}
            </FormHelperText>
          )}
        </FormControl>

        {/* Observation Text */}
        <CobraTextField
          label="Observation *"
          value={values.observationText}
          onChange={handleChange('observationText')}
          onBlur={handleBlur('observationText')}
          error={!!getFieldError('observationText')}
          helperText={
            getFieldError('observationText') ||
            `Document what you observed (${values.observationText.length}/${OBSERVATION_MAX_LENGTH})`
          }
          required
          fullWidth
          multiline
          rows={4}
          placeholder="Describe what you observed during task performance..."
        />

        {/* Performance Rating */}
        <PerformanceRatingSelector
          value={values.rating}
          onChange={handleRatingChange}
          error={touched.rating && !values.rating}
          helperText={getFieldError('rating')}
        />

        {/* Observed At Time */}
        <CobraTextField
          label="Observed At"
          type="datetime-local"
          value={values.observedAt}
          onChange={handleChange('observedAt')}
          fullWidth
          helperText="When did you observe this behavior? Defaults to current time."
          slotProps={{
            inputLabel: { shrink: true },
          }}
        />

        {/* Triggering Inject (Optional) */}
        {availableInjects.length > 0 && (
          <FormControl fullWidth size="small">
            <InputLabel>Triggered by Inject (optional)</InputLabel>
            <Select
              value={values.triggeringInjectId}
              onChange={handleChange('triggeringInjectId')}
              label="Triggered by Inject (optional)"
            >
              <MenuItem value="">
                <em>None</em>
              </MenuItem>
              {availableInjects.map(inject => (
                <MenuItem key={inject.id} value={inject.id}>
                  INJ-{inject.injectNumber.toString().padStart(3, '0')}: {inject.title}
                </MenuItem>
              ))}
            </Select>
            <FormHelperText>
              Link this entry to the inject that prompted the observed behavior
            </FormHelperText>
          </FormControl>
        )}

        {/* Form Actions */}
        <Stack direction="row" spacing={2} justifyContent="flex-end">
          <CobraSecondaryButton onClick={handleCancel} disabled={isCreating || isUpdating}>
            Cancel
          </CobraSecondaryButton>
          {!isEditMode && (
            <CobraSecondaryButton
              onClick={() => handleSubmit(true)}
              disabled={isCreating}
            >
              Save & Continue
            </CobraSecondaryButton>
          )}
          <CobraPrimaryButton
            onClick={() => handleSubmit(false)}
            disabled={isCreating || isUpdating}
          >
            {isCreating || isUpdating
              ? 'Saving...'
              : isEditMode
                ? 'Save Changes'
                : 'Save'}
          </CobraPrimaryButton>
        </Stack>
      </Stack>
    </Paper>
  )
}

export default EegEntryForm
