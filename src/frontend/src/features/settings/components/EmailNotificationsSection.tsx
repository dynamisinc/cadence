/**
 * EmailNotificationsSection Component
 *
 * Renders email notification preference toggles grouped by required/optional.
 * Fetches preferences from the API and saves changes optimistically.
 * Uses 2-column grid layout on medium+ screens for better space utilization.
 */

import { useState, useEffect, useCallback } from 'react'
import {
  Box,
  Typography,
  Switch,
  Stack,
  Alert,
  CircularProgress,
  Chip,
  Grid,
  Paper,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEnvelope } from '@fortawesome/free-solid-svg-icons'
import { emailPreferencesService } from '../services/emailPreferencesService'
import type { EmailPreferenceDto } from '../types'

/**
 * Section wrapper consistent with other settings sections
 */
const SectionHeader = () => (
  <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }}>
    <Box sx={{ color: 'text.secondary', width: 20, textAlign: 'center' }}>
      <FontAwesomeIcon icon={faEnvelope} />
    </Box>
    <Typography variant="subtitle1" fontWeight={600}>
      Email Notifications
    </Typography>
  </Stack>
)

/**
 * Single preference card with toggle switch
 * Displayed as a compact card within a grid layout
 */
const PreferenceCard = ({
  pref,
  onToggle,
  isUpdating,
}: {
  pref: EmailPreferenceDto
  onToggle: (category: string, isEnabled: boolean) => void
  isUpdating: boolean
}) => (
  <Paper
    variant="outlined"
    sx={{
      px: 2,
      py: 1.5,
      height: '100%',
      display: 'flex',
      alignItems: 'center',
      gap: 2,
      transition: 'box-shadow 0.2s',
      '&:hover': {
        boxShadow: 1,
      },
    }}
  >
    <Box sx={{ minWidth: 0 }}>
      <Stack direction="row" spacing={1} alignItems="center">
        <Typography variant="body2" fontWeight={500} noWrap>
          {pref.displayName}
        </Typography>
        {pref.isMandatory && (
          <Chip
            label="Required"
            size="small"
            color="default"
            sx={{ height: 20, fontSize: '0.65rem' }}
          />
        )}
      </Stack>
      <Typography variant="caption" color="text.secondary">
        {pref.description}
      </Typography>
    </Box>
    <Switch
      checked={pref.isEnabled}
      onChange={() => onToggle(pref.category, !pref.isEnabled)}
      disabled={pref.isMandatory || isUpdating}
      size="small"
      inputProps={{
        'aria-label': `${pref.displayName} email notifications`,
      }}
    />
  </Paper>
)

export const EmailNotificationsSection = () => {
  const [preferences, setPreferences] = useState<EmailPreferenceDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [updatingCategory, setUpdatingCategory] = useState<string | null>(null)

  const fetchPreferences = useCallback(async () => {
    try {
      setError(null)
      const response = await emailPreferencesService.getPreferences()
      setPreferences(response.preferences)
    } catch {
      setError('Failed to load email preferences')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchPreferences()
  }, [fetchPreferences])

  const handleToggle = async (category: string, isEnabled: boolean) => {
    // Optimistic update
    const previousPrefs = [...preferences]
    setPreferences(prev =>
      prev.map(p => (p.category === category ? { ...p, isEnabled } : p)),
    )
    setUpdatingCategory(category)

    try {
      const response = await emailPreferencesService.updatePreference({
        category,
        isEnabled,
      })
      setPreferences(response.preferences)
    } catch {
      // Revert on error
      setPreferences(previousPrefs)
      setError('Failed to update preference. Please try again.')
    } finally {
      setUpdatingCategory(null)
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ py: 2 }}>
        <SectionHeader />
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
          <CircularProgress size={24} />
        </Box>
      </Box>
    )
  }

  const requiredPrefs = preferences.filter(p => p.isMandatory)
  const optionalPrefs = preferences.filter(p => !p.isMandatory)

  return (
    <Box sx={{ py: 2 }}>
      <SectionHeader />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Required notifications */}
      {requiredPrefs.length > 0 && (
        <Box sx={{ mb: 3 }}>
          <Typography
            variant="caption"
            color="text.secondary"
            fontWeight={600}
            sx={{ textTransform: 'uppercase', letterSpacing: 0.5, mb: 1.5, display: 'block' }}
          >
            Required — cannot be disabled
          </Typography>
          <Grid container spacing={2}>
            {requiredPrefs.map(pref => (
              <Grid key={pref.category} size={{ xs: 12, md: 6 }}>
                <PreferenceCard
                  pref={pref}
                  onToggle={handleToggle}
                  isUpdating={updatingCategory === pref.category}
                />
              </Grid>
            ))}
          </Grid>
        </Box>
      )}

      {/* Optional notifications */}
      {optionalPrefs.length > 0 && (
        <Box>
          <Typography
            variant="caption"
            color="text.secondary"
            fontWeight={600}
            sx={{ textTransform: 'uppercase', letterSpacing: 0.5, mb: 1.5, display: 'block' }}
          >
            Optional
          </Typography>
          <Grid container spacing={2}>
            {optionalPrefs.map(pref => (
              <Grid key={pref.category} size={{ xs: 12, md: 6 }}>
                <PreferenceCard
                  pref={pref}
                  onToggle={handleToggle}
                  isUpdating={updatingCategory === pref.category}
                />
              </Grid>
            ))}
          </Grid>
        </Box>
      )}
    </Box>
  )
}

export default EmailNotificationsSection
