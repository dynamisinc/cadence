/**
 * CriticalTaskFormDialog Component
 *
 * Modal dialog for creating or editing a Critical Task.
 * Allows defining task description and optional standard reference.
 */

import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  Stack,
  Typography,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faXmark } from '@fortawesome/free-solid-svg-icons'
import {
  CobraTextField,
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import type {
  CriticalTaskDto,
  CreateCriticalTaskRequest,
  UpdateCriticalTaskRequest,
} from '../types'

/** Field limits for critical task form */
export const CRITICAL_TASK_FIELD_LIMITS = {
  taskDescription: { min: 5, max: 500 },
  standard: { max: 500 },
}

interface CriticalTaskFormDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Critical task to edit (null for create mode) */
  task?: CriticalTaskDto | null
  /** Parent capability target name for display */
  capabilityTargetName?: string
  /** Called when dialog should close */
  onClose: () => void
  /** Called when save is clicked for creating */
  onCreate?: (request: CreateCriticalTaskRequest) => Promise<void>
  /** Called when save is clicked for updating */
  onUpdate?: (id: string, request: UpdateCriticalTaskRequest) => Promise<void>
}

/**
 * Dialog for creating or editing Critical Tasks
 */
export const CriticalTaskFormDialog: FC<CriticalTaskFormDialogProps> = ({
  open,
  task,
  capabilityTargetName,
  onClose,
  onCreate,
  onUpdate,
}) => {
  const isEditMode = !!task

  // Form state
  const [taskDescription, setTaskDescription] = useState('')
  const [standard, setStandard] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [touched, setTouched] = useState<Record<string, boolean>>({})

  // Initialize form when task changes or dialog opens
  useEffect(() => {
    if (open) {
      if (task) {
        setTaskDescription(task.taskDescription)
        setStandard(task.standard || '')
      } else {
        setTaskDescription('')
        setStandard('')
      }
      setError(null)
      setIsLoading(false)
      setTouched({})
    }
  }, [task, open])

  const validateForm = (): boolean => {
    if (!taskDescription.trim()) {
      setError('Task description is required')
      return false
    }

    if (taskDescription.trim().length < CRITICAL_TASK_FIELD_LIMITS.taskDescription.min) {
      setError(
        `Task description must be at least ${CRITICAL_TASK_FIELD_LIMITS.taskDescription.min} characters`,
      )
      return false
    }

    if (taskDescription.length > CRITICAL_TASK_FIELD_LIMITS.taskDescription.max) {
      setError(
        `Task description must be ${CRITICAL_TASK_FIELD_LIMITS.taskDescription.max} characters or less`,
      )
      return false
    }

    if (standard && standard.length > CRITICAL_TASK_FIELD_LIMITS.standard.max) {
      setError(
        `Standard must be ${CRITICAL_TASK_FIELD_LIMITS.standard.max} characters or less`,
      )
      return false
    }

    return true
  }

  const handleSave = async () => {
    setTouched({ taskDescription: true, standard: true })

    if (!validateForm()) {
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      if (isEditMode && task && onUpdate) {
        const request: UpdateCriticalTaskRequest = {
          taskDescription: taskDescription.trim(),
          standard: standard.trim() || null,
        }
        await onUpdate(task.id, request)
      } else if (onCreate) {
        const request: CreateCriticalTaskRequest = {
          taskDescription: taskDescription.trim(),
          standard: standard.trim() || null,
        }
        await onCreate(request)
      }
      onClose()
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { message?: string } } }
      setError(axiosError.response?.data?.message || 'Failed to save critical task')
      setIsLoading(false)
    }
  }

  // Validation helpers
  const descriptionLength = taskDescription.length
  const isDescriptionValid =
    descriptionLength >= CRITICAL_TASK_FIELD_LIMITS.taskDescription.min &&
    descriptionLength <= CRITICAL_TASK_FIELD_LIMITS.taskDescription.max
  const canSave = isDescriptionValid && !isLoading

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {isEditMode ? 'Edit Critical Task' : 'Add Critical Task'}
      </DialogTitle>
      {capabilityTargetName && (
        <Typography variant="body2" color="text.secondary" sx={{ px: 3, mt: -1 }}>
          Capability Target: {capabilityTargetName}
        </Typography>
      )}
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          <CobraTextField
            label="Task Description"
            value={taskDescription}
            onChange={e => {
              setTaskDescription(e.target.value)
              if (error) setError(null)
            }}
            onBlur={() => setTouched(prev => ({ ...prev, taskDescription: true }))}
            fullWidth
            required
            multiline
            rows={2}
            placeholder="e.g., Activate emergency communication plan"
            helperText={
              touched.taskDescription && !isDescriptionValid
                ? `${descriptionLength}/${CRITICAL_TASK_FIELD_LIMITS.taskDescription.max} - Must be ${CRITICAL_TASK_FIELD_LIMITS.taskDescription.min}-${CRITICAL_TASK_FIELD_LIMITS.taskDescription.max} characters`
                : `${descriptionLength}/${CRITICAL_TASK_FIELD_LIMITS.taskDescription.max} - What specific action should be observed?`
            }
            error={touched.taskDescription && !taskDescription.trim()}
            autoFocus
          />

          <CobraTextField
            label="Standard (optional)"
            value={standard}
            onChange={e => {
              setStandard(e.target.value)
              if (error) setError(null)
            }}
            onBlur={() => setTouched(prev => ({ ...prev, standard: true }))}
            fullWidth
            multiline
            rows={2}
            placeholder="e.g., Per SOP 5.2, using emergency notification system within 10 minutes"
            helperText={`${standard.length}/${CRITICAL_TASK_FIELD_LIMITS.standard.max} - Reference the plan, SOP, or standard that defines how this task should be performed`}
          />

          <Typography variant="body2" color="text.secondary">
            <strong>Examples of good task descriptions:</strong>
          </Typography>
          <Typography variant="body2" color="text.secondary" component="ul" sx={{ mt: 0, pl: 2 }}>
            <li>&quot;Issue EOC activation notification to all stakeholders&quot;</li>
            <li>&quot;Establish unified command structure&quot;</li>
            <li>&quot;Complete initial damage assessment report&quot;</li>
          </Typography>
        </Stack>
      </DialogContent>
      <DialogActions>
        <CobraSecondaryButton onClick={onClose} disabled={isLoading}>
          <FontAwesomeIcon icon={faXmark} style={{ marginRight: 8 }} />
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton onClick={handleSave} disabled={!canSave}>
          <FontAwesomeIcon icon={faSave} style={{ marginRight: 8 }} />
          {isLoading ? 'Saving...' : isEditMode ? 'Save' : 'Add Task'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default CriticalTaskFormDialog
