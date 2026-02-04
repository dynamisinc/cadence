/**
 * ExerciseSettingsPage
 *
 * Full page view for exercise settings (accessible from sidebar).
 * Displays exercise-specific settings like clock speed, auto-fire, and confirmations.
 */

import { useState, useEffect, useCallback } from 'react'
import { useParams } from 'react-router-dom'
import {
  Box,
  Paper,
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
  Grid,
  useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faGear,
  faClock,
  faPlay,
  faCircleCheck,
  faForward,
  faStopwatch,
  faHome,
  faShieldHalved,
} from '@fortawesome/free-solid-svg-icons'
import CobraStyles from '@/theme/CobraStyles'
import { useBreadcrumbs } from '@/core/contexts'
import { useExercise } from '../hooks'
import { ExerciseApprovalToggle } from '../components/ExerciseApprovalToggle'
import { exerciseService } from '../services/exerciseService'
import type { ExerciseSettingsDto, UpdateExerciseSettingsRequest } from '../types'
import { CLOCK_MULTIPLIER_PRESETS } from '../types'

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
 * Exercise Settings Page
 */
export const ExerciseSettingsPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId)
  const theme = useTheme()
  const isWideScreen = useMediaQuery(theme.breakpoints.up('md'))

  const [settings, setSettings] = useState<ExerciseSettingsDto | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'Settings' },
      ]
      : undefined,
  )

  // Fetch settings when page loads
  useEffect(() => {
    if (!exerciseId) {
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
  }, [exerciseId])

  // Update a single setting
  const updateSetting = useCallback(
    async (update: UpdateExerciseSettingsRequest) => {
      if (!settings || !exerciseId) return

      // Optimistic update
      const previousSettings = { ...settings }
      setSettings({ ...settings, ...update })
      setIsSaving(true)
      setError(null)

      try {
        const updated = await exerciseService.updateSettings(exerciseId, update)
        setSettings(updated)
      } catch (err) {
        console.error('Failed to update exercise settings:', err)
        setError('Failed to save setting. Please try again.')
        // Revert on error
        setSettings(previousSettings)
      } finally {
        setIsSaving(false)
      }
    },
    [exerciseId, settings],
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
  if (isLoading || exerciseLoading) {
    return (
      <Box
        sx={{
          padding: CobraStyles.Padding.MainWindow,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: 400,
        }}
      >
        <CircularProgress size={48} />
      </Box>
    )
  }

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      {/* Header */}
      <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 3 }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'primary.main',
            fontSize: 32,
          }}
        >
          <FontAwesomeIcon icon={faGear} />
        </Box>
        <Box>
          <Typography variant="h4" fontWeight={600}>
            Exercise Settings
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {exercise?.name} — Configure clock speed, auto-fire, and confirmation dialogs.
          </Typography>
        </Box>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Responsive grid: two columns on md+ for Clock Speed & Auto-Fire,
          full width for Confirmation Dialogs */}
      <Grid container spacing={2} alignItems="stretch">
        {/* Clock Speed — half width on md+, full width on smaller */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
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
          </Paper>
        </Grid>

        {/* Auto-Fire — half width on md+, full width on smaller */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
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
          </Paper>
        </Grid>

        {/* Inject Approval — full width */}
        {exerciseId && (
          <Grid size={12}>
            <Paper sx={{ p: 3 }}>
              <SettingsSection title="Inject Approval" icon={faShieldHalved}>
                <ExerciseApprovalToggle exerciseId={exerciseId} />
              </SettingsSection>
            </Paper>
          </Grid>
        )}

        {/* Confirmation Dialogs — full width on all screen sizes */}
        <Grid size={12}>
          <Paper sx={{ p: 3 }}>
            <SettingsSection title="Confirmation Dialogs" icon={faCircleCheck}>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 2, pl: 1 }}>
                Show confirmation dialog before these actions
              </Typography>

              {/* On wide screens, display confirmation switches in a two-column layout */}
              {isWideScreen ? (
                <Grid container spacing={1} alignItems="stretch">
                  <Grid size={6}>
                    <ConfirmationSwitch
                      label="Fire Inject"
                      description="Confirm before delivering an inject to players"
                      icon={faPlay}
                      checked={settings?.confirmFireInject ?? true}
                      onChange={handleConfirmFireInjectChange}
                      disabled={isSaving}
                    />
                  </Grid>
                  <Grid size={6}>
                    <ConfirmationSwitch
                      label="Skip Inject"
                      description="Confirm before skipping an inject"
                      icon={faForward}
                      checked={settings?.confirmSkipInject ?? true}
                      onChange={handleConfirmSkipInjectChange}
                      disabled={isSaving}
                    />
                  </Grid>
                  <Grid size={12}>
                    <ConfirmationSwitch
                      label="Clock Control"
                      description="Confirm before starting, pausing, or stopping the clock"
                      icon={faStopwatch}
                      checked={settings?.confirmClockControl ?? true}
                      onChange={handleConfirmClockControlChange}
                      disabled={isSaving}
                    />
                  </Grid>
                </Grid>
              ) : (
                <>
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
                </>
              )}
            </SettingsSection>

            <Divider sx={{ my: 2 }} />

            {/* Save status */}
            <Typography variant="caption" color="text.secondary">
              {isSaving ? 'Saving...' : 'Settings save automatically'}
            </Typography>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

export default ExerciseSettingsPage
