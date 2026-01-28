/**
 * ExerciseSettingsDialog Component
 *
 * Dialog for configuring exercise-level settings.
 * Includes clock mode, auto-fire, and confirmation preferences.
 * Settings save automatically on change.
 */

import { useState, useEffect, useCallback } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Box,
  Typography,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  Switch,
  Divider,
  Stack,
  Alert,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faGear,
  faClock,
  faPlay,
  faCircleCheck,
  faForward,
  faStopwatch,
} from '@fortawesome/free-solid-svg-icons'
import { CobraSecondaryButton } from '@/theme/styledComponents'
import { exerciseService } from '../services/exerciseService'
import type { ExerciseSettingsDto, UpdateExerciseSettingsRequest } from '../types'
import { CLOCK_MULTIPLIER_PRESETS } from '../types'

interface ExerciseSettingsDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Exercise ID to configure */
  exerciseId: string
  /** Exercise name for display */
  exerciseName: string
  /** Called when dialog is closed */
  onClose: () => void
  /** Called when settings are updated */
  onSettingsUpdated?: (settings: ExerciseSettingsDto) => void
}

/**
 * Section wrapper for consistent styling
 */
const SettingsSection = ({
  title,
  icon,
  children,
}: {
  title: string
  icon: typeof faClock
  children: React.ReactNode
}) => (
  <Box sx={{ py: 2 }}>
    <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
      <Box sx={{ color: 'text.secondary', width: 20, textAlign: 'center' }}>
        <FontAwesomeIcon icon={icon} />
      </Box>
      <Typography variant="subtitle1" fontWeight={600}>
        {title}
      </Typography>
    </Stack>
    {children}
  </Box>
)

/**
 * Switch item for confirmation settings
 */
const ConfirmationSwitch = ({
  label,
  description,
  icon,
  checked,
  onChange,
  disabled,
}: {
  label: string
  description: string
  icon: typeof faCircleCheck
  checked: boolean
  onChange: (checked: boolean) => void
  disabled?: boolean
}) => (
  <Box
    sx={{
      display: 'flex',
      alignItems: 'flex-start',
      justifyContent: 'space-between',
      py: 1.5,
      px: 1,
      borderRadius: 1,
      '&:hover': { bgcolor: 'action.hover' },
    }}
  >
    <Stack direction="row" spacing={1.5} alignItems="flex-start" sx={{ flex: 1 }}>
      <Box sx={{ color: 'text.secondary', width: 20, textAlign: 'center', mt: 0.25 }}>
        <FontAwesomeIcon icon={icon} size="sm" />
      </Box>
      <Box>
        <Typography variant="body2" fontWeight={500}>
          {label}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {description}
        </Typography>
      </Box>
    </Stack>
    <Switch
      checked={checked}
      onChange={e => onChange(e.target.checked)}
      disabled={disabled}
      size="small"
    />
  </Box>
)

/**
 * Dialog for exercise settings
 */
export const ExerciseSettingsDialog = ({
  open,
  exerciseId,
  exerciseName,
  onClose,
  onSettingsUpdated,
}: ExerciseSettingsDialogProps) => {
  const [settings, setSettings] = useState<ExerciseSettingsDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Fetch settings when dialog opens
  useEffect(() => {
    if (!open || !exerciseId) {
      return
    }

    const fetchSettings = async () => {
      try {
        setIsLoading(true)
        setError(null)
        const data = await exerciseService.getSettings(exerciseId)
        setSettings(data)
      } catch (err) {
        console.error('Failed to fetch exercise settings:', err)
        setError('Failed to load settings. Please try again.')
      } finally {
        setIsLoading(false)
      }
    }

    fetchSettings()
  }, [open, exerciseId])

  // Update a single setting
  const updateSetting = useCallback(
    async (update: UpdateExerciseSettingsRequest) => {
      if (!settings) return

      // Optimistic update
      const previousSettings = { ...settings }
      setSettings({ ...settings, ...update })
      setIsSaving(true)
      setError(null)

      try {
        const updated = await exerciseService.updateSettings(exerciseId, update)
        setSettings(updated)
        onSettingsUpdated?.(updated)
      } catch (err) {
        console.error('Failed to update exercise settings:', err)
        setError('Failed to save setting. Please try again.')
        // Revert on error
        setSettings(previousSettings)
      } finally {
        setIsSaving(false)
      }
    },
    [exerciseId, settings, onSettingsUpdated],
  )

  const handleClockMultiplierChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const value = Number(event.target.value)
    updateSetting({ clockMultiplier: value })
  }

  const handleAutoFireChange = (checked: boolean) => {
    updateSetting({ autoFireEnabled: checked })
  }

  const handleConfirmFireInjectChange = (checked: boolean) => {
    updateSetting({ confirmFireInject: checked })
  }

  const handleConfirmSkipInjectChange = (checked: boolean) => {
    updateSetting({ confirmSkipInject: checked })
  }

  const handleConfirmClockControlChange = (checked: boolean) => {
    updateSetting({ confirmClockControl: checked })
  }

  // Show loading state
  if (isLoading) {
    return (
      <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
        <DialogContent>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'center',
              alignItems: 'center',
              py: 4,
            }}
          >
            <CircularProgress size={32} />
          </Box>
        </DialogContent>
      </Dialog>
    )
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="exercise-settings-dialog-title"
    >
      <DialogTitle
        id="exercise-settings-dialog-title"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 24,
          }}
        >
          <FontAwesomeIcon icon={faGear} />
        </Box>
        <Box>
          <Typography variant="h6" component="span">
            Exercise Settings
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {exerciseName}
          </Typography>
        </Box>
      </DialogTitle>

      <DialogContent dividers>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* Clock Multiplier Section */}
        <SettingsSection title="Clock Speed" icon={faClock}>
          <FormControl component="fieldset" fullWidth>
            <FormLabel sx={{ mb: 1 }}>
              Scenario time runs faster than wall clock time
            </FormLabel>
            <RadioGroup
              value={settings?.clockMultiplier ?? 1}
              onChange={handleClockMultiplierChange}
              sx={{ pl: 1 }}
            >
              {CLOCK_MULTIPLIER_PRESETS.map(preset => (
                <FormControlLabel
                  key={preset.value}
                  value={preset.value}
                  control={<Radio size="small" disabled={isSaving} />}
                  label={
                    <Stack spacing={0}>
                      <span>{preset.label}</span>
                      {preset.value > 1 && (
                        <Typography variant="caption" color="text.secondary">
                          1 minute wall clock = {preset.value} minutes scenario time
                        </Typography>
                      )}
                    </Stack>
                  }
                />
              ))}
            </RadioGroup>
          </FormControl>
        </SettingsSection>

        <Divider />

        {/* Auto-Fire Section */}
        <SettingsSection title="Auto-Fire" icon={faPlay}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'flex-start',
              justifyContent: 'space-between',
              p: 1.5,
              bgcolor: settings?.autoFireEnabled ? 'success.50' : 'grey.50',
              borderRadius: 1,
              border: 1,
              borderColor: settings?.autoFireEnabled ? 'success.200' : 'grey.200',
            }}
          >
            <Box sx={{ flex: 1 }}>
              <Typography variant="body2" fontWeight={500}>
                Automatically fire injects when scheduled
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {settings?.autoFireEnabled
                  ? 'Injects will be fired automatically at their scheduled time'
                  : 'Controllers must manually fire each inject'}
              </Typography>
            </Box>
            <Switch
              checked={settings?.autoFireEnabled ?? false}
              onChange={e => handleAutoFireChange(e.target.checked)}
              disabled={isSaving}
            />
          </Box>
        </SettingsSection>

        <Divider />

        {/* Confirmation Dialogs Section */}
        <SettingsSection title="Confirmation Dialogs" icon={faCircleCheck}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2, pl: 1 }}>
            Show confirmation dialog before these actions
          </Typography>

          <ConfirmationSwitch
            label="Fire Inject"
            description="Confirm before delivering an inject to players"
            icon={faPlay}
            checked={settings?.confirmFireInject ?? true}
            onChange={handleConfirmFireInjectChange}
            disabled={isSaving}
          />

          <ConfirmationSwitch
            label="Skip Inject"
            description="Confirm before skipping an inject"
            icon={faForward}
            checked={settings?.confirmSkipInject ?? true}
            onChange={handleConfirmSkipInjectChange}
            disabled={isSaving}
          />

          <ConfirmationSwitch
            label="Clock Control"
            description="Confirm before starting, pausing, or stopping the clock"
            icon={faStopwatch}
            checked={settings?.confirmClockControl ?? true}
            onChange={handleConfirmClockControlChange}
            disabled={isSaving}
          />
        </SettingsSection>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2 }}>
        <Typography variant="caption" color="text.secondary" sx={{ flex: 1 }}>
          {isSaving ? 'Saving...' : 'Settings save automatically'}
        </Typography>
        <CobraSecondaryButton onClick={onClose}>Done</CobraSecondaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default ExerciseSettingsDialog
