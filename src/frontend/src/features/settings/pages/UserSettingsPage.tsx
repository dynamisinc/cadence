/**
 * UserSettingsPage
 *
 * Full page view for user settings (accessible from sidebar).
 * Displays user preferences for theme, display density, and time format.
 */

import { useState } from 'react'
import {
  Box,
  Paper,
  Typography,
  FormControl,
  FormLabel,
  RadioGroup,
  FormControlLabel,
  Radio,
  Divider,
  Alert,
  CircularProgress,
  Grid,
  useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
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
  faHome,
} from '@fortawesome/free-solid-svg-icons'
import { CobraLinkButton } from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import { useUserPreferences } from '../contexts/UserPreferencesContext'
import { getCurrentTimeFormatted } from '../utils/timeFormat'
import { useBreadcrumbs } from '@/core/contexts'
import { VersionInfoCard } from '@/features/version'
import { EmailNotificationsSection } from '../components/EmailNotificationsSection'
import type { ThemePreference, DisplayDensity, TimeFormat } from '../types'
import { PageHeader } from '@/shared/components'

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
 * User Settings Page
 */
export const UserSettingsPage = () => {
  const {
    preferences,
    isLoading,
    error,
    setTheme,
    setDisplayDensity,
    setTimeFormat,
    resetPreferences,
  } = useUserPreferences()
  const theme = useTheme()
  const isWideScreen = useMediaQuery(theme.breakpoints.up('md'))

  const [isResetting, setIsResetting] = useState(false)

  // Set breadcrumbs
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'My Preferences' },
  ])

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

  const currentTheme = preferences?.theme ?? 'System'
  const currentDensity = preferences?.displayDensity ?? 'Comfortable'
  const currentTimeFormat = preferences?.timeFormat ?? 'TwentyFourHour'

  return (
    <Box sx={{ padding: CobraStyles.Padding.MainWindow }}>
      <PageHeader
        title="My Preferences"
        icon={faGear}
        subtitle="Customize your experience with display and behavior preferences."
      />

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Responsive grid: two columns on md+ for Theme & Display Density,
          full width for Time Format */}
      <Grid container spacing={2} alignItems="stretch">
        {/* Theme — half width on md+, full width on smaller */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
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
          </Paper>
        </Grid>

        {/* Display Density — half width on md+, full width on smaller */}
        <Grid size={{ xs: 12, md: 6 }}>
          <Paper sx={{ p: 3, height: '100%' }}>
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
          </Paper>
        </Grid>

        {/* Time Format — full width on all screen sizes */}
        <Grid size={12}>
          <Paper sx={{ p: 3 }}>
            <SettingsSection title="Time Format" icon={faClock}>
              {/* On wide screens, display time format options side-by-side */}
              {isWideScreen ? (
                <FormControl component="fieldset" fullWidth>
                  <FormLabel sx={{ mb: 1 }}>
                    Choose how times are displayed. Current time:{' '}
                    <strong>{getCurrentTimeFormatted(currentTimeFormat)}</strong>
                  </FormLabel>
                  <RadioGroup
                    value={currentTimeFormat}
                    onChange={handleTimeFormatChange}
                    row
                    sx={{ pl: 1, gap: 4 }}
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
              ) : (
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
              )}
            </SettingsSection>

            <Divider sx={{ my: 2 }} />

            {/* Reset Button */}
            <Box sx={{ display: 'flex', justifyContent: 'flex-start' }}>
              <CobraLinkButton
                onClick={handleReset}
                disabled={isResetting}
                startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
              >
                {isResetting ? 'Resetting...' : 'Reset to Defaults'}
              </CobraLinkButton>
            </Box>
          </Paper>
        </Grid>

        {/* Email Notifications */}
        <Grid size={12}>
          <Paper sx={{ p: 3 }}>
            <EmailNotificationsSection />
          </Paper>
        </Grid>

        {/* Version Information */}
        <Grid size={12}>
          <VersionInfoCard />
        </Grid>
      </Grid>
    </Box>
  )
}

export default UserSettingsPage
