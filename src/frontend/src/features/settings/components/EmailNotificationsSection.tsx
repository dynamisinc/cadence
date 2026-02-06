/**
 * EmailNotificationsSection Component
 *
 * Renders email notification preference toggles grouped by required/optional.
 * Fetches preferences from the API and saves changes optimistically.
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
 * Single preference row with toggle switch
 */
const PreferenceRow = ({
  pref,
  onToggle,
  isUpdating,
}: {
  pref: EmailPreferenceDto
  onToggle: (category: string, isEnabled: boolean) => void
  isUpdating: boolean
}) => (
  <Stack
    direction="row"
    alignItems="center"
    justifyContent="space-between"
    sx={{ py: 1 }}
  >
    <Box sx={{ flex: 1 }}>
      <Stack direction="row" spacing={1} alignItems="center">
        <Typography variant="body2" fontWeight={500}>
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
  </Stack>
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
        <Box sx={{ mb: 2 }}>
          <Typography
            variant="caption"
            color="text.secondary"
            fontWeight={600}
            sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}
          >
            Always On
          </Typography>
          {requiredPrefs.map(pref => (
            <PreferenceRow
              key={pref.category}
              pref={pref}
              onToggle={handleToggle}
              isUpdating={updatingCategory === pref.category}
            />
          ))}
        </Box>
      )}

      {/* Optional notifications */}
      {optionalPrefs.length > 0 && (
        <Box>
          <Typography
            variant="caption"
            color="text.secondary"
            fontWeight={600}
            sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}
          >
            Customizable
          </Typography>
          {optionalPrefs.map(pref => (
            <PreferenceRow
              key={pref.category}
              pref={pref}
              onToggle={handleToggle}
              isUpdating={updatingCategory === pref.category}
            />
          ))}
        </Box>
      )}
    </Box>
  )
}

export default EmailNotificationsSection
