/**
 * UserSettingsDialog Component
 *
 * Dialog for user display and behavior preferences.
 * Includes theme, display density, and time format settings.
 * Settings save automatically on change (no explicit save button).
 */

import { useState } from 'react'
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
  Divider,
  Stack,
  Alert,
  CircularProgress,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faGear,
  faSun,
  faMoon,
  faDesktop,
  faExpand,
  faCompress,
  faClock,
  faRotateLeft,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraSecondaryButton,
  CobraLinkButton,
} from '@/theme/styledComponents'
import { useUserPreferences } from '../contexts/UserPreferencesContext'
import { getCurrentTimeFormatted } from '../utils/timeFormat'
import type { ThemePreference, DisplayDensity, TimeFormat } from '../types'

interface UserSettingsDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Called when dialog is closed */
  onClose: () => void
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
  icon: typeof faSun
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
 * Dialog for user preferences
 */
export const UserSettingsDialog = ({ open, onClose }: UserSettingsDialogProps) => {
  const {
    preferences,
    isLoading,
    error,
    setTheme,
    setDisplayDensity,
    setTimeFormat,
    resetPreferences,
  } = useUserPreferences()

  const [isResetting, setIsResetting] = useState(false)

  const handleThemeChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    try {
      await setTheme(event.target.value as ThemePreference)
    } catch {
      // Error is handled by context
    }
  }

  const handleDensityChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    try {
      await setDisplayDensity(event.target.value as DisplayDensity)
    } catch {
      // Error is handled by context
    }
  }

  const handleTimeFormatChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    try {
      await setTimeFormat(event.target.value as TimeFormat)
    } catch {
      // Error is handled by context
    }
  }

  const handleReset = async () => {
    setIsResetting(true)
    try {
      await resetPreferences()
    } catch {
      // Error is handled by context
    } finally {
      setIsResetting(false)
    }
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

  const currentTheme = preferences?.theme ?? 'System'
  const currentDensity = preferences?.displayDensity ?? 'Comfortable'
  const currentTimeFormat = preferences?.timeFormat ?? 'TwentyFourHour'

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
      aria-labelledby="settings-dialog-title"
    >
      <DialogTitle
        id="settings-dialog-title"
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
        User Settings
      </DialogTitle>

      <DialogContent dividers>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {/* Theme Section */}
        <SettingsSection title="Theme" icon={faSun}>
          <FormControl component="fieldset" fullWidth>
            <FormLabel sx={{ mb: 1 }}>Choose your preferred appearance</FormLabel>
            <RadioGroup
              value={currentTheme}
              onChange={handleThemeChange}
              sx={{ pl: 1 }}
            >
              <FormControlLabel
                value="Light"
                control={<Radio size="small" />}
                label={
                  <Stack direction="row" spacing={1} alignItems="center">
                    <FontAwesomeIcon icon={faSun} />
                    <span>Light</span>
                  </Stack>
                }
              />
              <FormControlLabel
                value="Dark"
                control={<Radio size="small" />}
                label={
                  <Stack direction="row" spacing={1} alignItems="center">
                    <FontAwesomeIcon icon={faMoon} />
                    <span>Dark</span>
                  </Stack>
                }
              />
              <FormControlLabel
                value="System"
                control={<Radio size="small" />}
                label={
                  <Stack direction="row" spacing={1} alignItems="center">
                    <FontAwesomeIcon icon={faDesktop} />
                    <span>System (follow OS preference)</span>
                  </Stack>
                }
              />
            </RadioGroup>
          </FormControl>
        </SettingsSection>

        <Divider />

        {/* Display Density Section */}
        <SettingsSection title="Display Density" icon={faExpand}>
          <FormControl component="fieldset" fullWidth>
            <FormLabel sx={{ mb: 1 }}>Adjust spacing throughout the app</FormLabel>
            <RadioGroup
              value={currentDensity}
              onChange={handleDensityChange}
              sx={{ pl: 1 }}
            >
              <FormControlLabel
                value="Comfortable"
                control={<Radio size="small" />}
                label={
                  <Stack direction="row" spacing={1} alignItems="center">
                    <FontAwesomeIcon icon={faExpand} />
                    <span>Comfortable (Default)</span>
                  </Stack>
                }
              />
              <FormControlLabel
                value="Compact"
                control={<Radio size="small" />}
                label={
                  <Stack direction="row" spacing={1} alignItems="center">
                    <FontAwesomeIcon icon={faCompress} />
                    <span>Compact (More information density)</span>
                  </Stack>
                }
              />
            </RadioGroup>
          </FormControl>
        </SettingsSection>

        <Divider />

        {/* Time Format Section */}
        <SettingsSection title="Time Format" icon={faClock}>
          <FormControl component="fieldset" fullWidth>
            <FormLabel sx={{ mb: 1 }}>
              Choose how times are displayed. Current time:{' '}
              <strong>{getCurrentTimeFormatted(currentTimeFormat)}</strong>
            </FormLabel>
            <RadioGroup
              value={currentTimeFormat}
              onChange={handleTimeFormatChange}
              sx={{ pl: 1 }}
            >
              <FormControlLabel
                value="TwentyFourHour"
                control={<Radio size="small" />}
                label={
                  <Stack spacing={0}>
                    <span>24-hour (Military time)</span>
                    <Typography variant="caption" color="text.secondary">
                      Example: 14:30 — Standard in emergency management
                    </Typography>
                  </Stack>
                }
              />
              <FormControlLabel
                value="TwelveHour"
                control={<Radio size="small" />}
                label={
                  <Stack spacing={0}>
                    <span>12-hour (AM/PM)</span>
                    <Typography variant="caption" color="text.secondary">
                      Example: 2:30 PM
                    </Typography>
                  </Stack>
                }
              />
            </RadioGroup>
          </FormControl>
        </SettingsSection>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2, justifyContent: 'space-between' }}>
        <CobraLinkButton
          onClick={handleReset}
          disabled={isResetting}
          startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
        >
          {isResetting ? 'Resetting...' : 'Reset to Defaults'}
        </CobraLinkButton>
        <CobraSecondaryButton onClick={onClose}>Done</CobraSecondaryButton>
      </DialogActions>
    </Dialog>
  )
}

export default UserSettingsDialog
