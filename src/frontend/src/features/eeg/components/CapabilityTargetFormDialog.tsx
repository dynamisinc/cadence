/**
 * CapabilityTargetFormDialog Component
 *
 * Modal dialog for creating or editing a Capability Target.
 * Allows selecting a capability and defining a performance threshold.
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
  Autocomplete,
  TextField,
  Box,
  Tooltip,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faXmark, faCircleInfo } from '@fortawesome/free-solid-svg-icons'
import {
  CobraTextField,
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import type { CapabilityDto } from '@/features/capabilities/types'
import type {
  CapabilityTargetDto,
  CreateCapabilityTargetRequest,
  UpdateCapabilityTargetRequest,
} from '../types'
import { CAPABILITY_TARGET_FIELD_LIMITS } from '../constants'

interface CapabilityTargetFormDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Capability target to edit (null for create mode) */
  target?: CapabilityTargetDto | null
  /** Available capabilities for selection */
  capabilities: CapabilityDto[]
  /** Loading state for capabilities */
  capabilitiesLoading?: boolean
  /** Called when dialog should close */
  onClose: () => void
  /** Called when save is clicked for creating */
  onCreate?: (request: CreateCapabilityTargetRequest) => Promise<void>
  /** Called when save is clicked for updating */
  onUpdate?: (id: string, request: UpdateCapabilityTargetRequest) => Promise<void>
}

/**
 * Dialog for creating or editing Capability Targets
 */
export const CapabilityTargetFormDialog: FC<CapabilityTargetFormDialogProps> = ({
  open,
  target,
  capabilities,
  capabilitiesLoading = false,
  onClose,
  onCreate,
  onUpdate,
}) => {
  const isEditMode = !!target

  // Form state
  const [capabilityId, setCapabilityId] = useState('')
  const [targetDescription, setTargetDescription] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [touched, setTouched] = useState<Record<string, boolean>>({})

  // Initialize form when target changes or dialog opens
  useEffect(() => {
    if (open) {
      if (target) {
        setCapabilityId(target.capabilityId)
        setTargetDescription(target.targetDescription)
      } else {
        setCapabilityId('')
        setTargetDescription('')
      }
      setError(null)
      setIsLoading(false)
      setTouched({})
    }
  }, [target, open])

  const validateForm = (): boolean => {
    if (!capabilityId) {
      setError('Please select a capability')
      return false
    }

    if (!targetDescription.trim()) {
      setError('Target description is required')
      return false
    }

    if (targetDescription.trim().length < CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.min) {
      setError(
        `Target description must be at least ${CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.min} characters`,
      )
      return false
    }

    if (targetDescription.length > CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.max) {
      setError(
        `Target description must be ${CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.max} characters or less`,
      )
      return false
    }

    return true
  }

  const handleSave = async () => {
    setTouched({ capabilityId: true, targetDescription: true })

    if (!validateForm()) {
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      if (isEditMode && target && onUpdate) {
        const request: UpdateCapabilityTargetRequest = {
          capabilityId,
          targetDescription: targetDescription.trim(),
        }
        await onUpdate(target.id, request)
      } else if (onCreate) {
        const request: CreateCapabilityTargetRequest = {
          capabilityId,
          targetDescription: targetDescription.trim(),
        }
        await onCreate(request)
      }
      onClose()
    } catch (err: unknown) {
      const axiosError = err as { response?: { data?: { message?: string } } }
      setError(axiosError.response?.data?.message || 'Failed to save capability target')
      setIsLoading(false)
    }
  }

  // Get selected capability for display
  const selectedCapability = capabilities.find(c => c.id === capabilityId)

  // Validation helpers
  const descriptionLength = targetDescription.length
  const isDescriptionValid =
    descriptionLength >= CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.min &&
    descriptionLength <= CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.max
  const canSave = capabilityId && isDescriptionValid && !isLoading

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        {isEditMode ? 'Edit Capability Target' : 'Add Capability Target'}
      </DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}>
            <Autocomplete
              sx={{ flex: 1 }}
              options={capabilities.filter(c => c.isActive)}
              value={selectedCapability ?? null}
              onChange={(_event, newValue) => {
                setCapabilityId(newValue?.id ?? '')
                setTouched(prev => ({ ...prev, capabilityId: true }))
                if (error) setError(null)
              }}
              getOptionLabel={option => option.name}
              groupBy={option => option.category || 'Uncategorized'}
              loading={capabilitiesLoading}
              disabled={capabilitiesLoading}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              renderOption={(props, option) => {
                const { key, ...otherProps } = props
                return (
                  <Box component="li" key={key} {...otherProps}>
                    <Box>
                      <Typography variant="body1">{option.name}</Typography>
                      {option.description && (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {option.description.length > 80
                            ? `${option.description.substring(0, 80)}...`
                            : option.description}
                        </Typography>
                      )}
                    </Box>
                  </Box>
                )
              }}
              renderInput={params => (
                <TextField
                  {...params}
                  label="Capability"
                  required
                  error={touched.capabilityId && !capabilityId}
                  helperText={
                    capabilitiesLoading
                      ? 'Loading capabilities...'
                      : 'Search and select the organizational capability this target measures'
                  }
                  placeholder="Type to search capabilities..."
                />
              )}
            />
            {selectedCapability?.description && (
              <Tooltip
                title={selectedCapability.description}
                arrow
                placement="right"
                slotProps={{
                  tooltip: {
                    sx: { maxWidth: 350 },
                  },
                }}
              >
                <IconButton size="small" sx={{ mt: 1 }} aria-label="View capability description">
                  <FontAwesomeIcon icon={faCircleInfo} />
                </IconButton>
              </Tooltip>
            )}
          </Box>

          <CobraTextField
            label="Target Description"
            value={targetDescription}
            onChange={e => {
              setTargetDescription(e.target.value)
              if (error) setError(null)
            }}
            onBlur={() => setTouched(prev => ({ ...prev, targetDescription: true }))}
            fullWidth
            required
            multiline
            rows={3}
            placeholder='e.g., "Establish interoperable communications within 30 minutes of EOC activation"'
            helperText={
              touched.targetDescription && !isDescriptionValid
                ? `${descriptionLength}/${CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.max} - Must be ${CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.min}-${CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.max} characters`
                : `${descriptionLength}/${CAPABILITY_TARGET_FIELD_LIMITS.targetDescription.max} - Describe the measurable performance threshold`
            }
            error={touched.targetDescription && !targetDescription.trim()}
          />

          <Typography variant="body2" color="text.secondary">
            <strong>Examples of good target descriptions:</strong>
          </Typography>
          <Typography variant="body2" color="text.secondary" component="ul" sx={{ mt: 0, pl: 2 }}>
            <li>&quot;Activate EOC within 60 minutes of notification&quot;</li>
            <li>&quot;Complete damage assessment of critical facilities within 4 hours&quot;</li>
            <li>&quot;Issue public alert within 15 minutes of decision&quot;</li>
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
          {isLoading ? 'Saving...' : isEditMode ? 'Save' : 'Add Target'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default CapabilityTargetFormDialog
