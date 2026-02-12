import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Box,
  Typography,
  Stack,
  CircularProgress,
  Alert,
  IconButton,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSave, faEnvelope, faXmark } from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '@/theme/styledComponents'
import { useSystemSettings, useUpdateSystemSettings } from '../hooks/useSystemSettings'
import { notify } from '@/shared/utils/notify'

export const SystemSettingsAdmin: FC = () => {
  const { data: settings, isLoading, error } = useSystemSettings()
  const updateSettings = useUpdateSystemSettings()

  const [supportAddress, setSupportAddress] = useState('')
  const [defaultSenderName, setDefaultSenderName] = useState('')
  const [hasChanges, setHasChanges] = useState(false)

  // Populate form when settings load
  useEffect(() => {
    if (settings) {
      setSupportAddress(settings.supportAddress ?? '')
      setDefaultSenderName(settings.defaultSenderName ?? '')
      setHasChanges(false)
    }
  }, [settings])

  // Track changes
  useEffect(() => {
    if (settings) {
      const changed =
        supportAddress !== (settings.supportAddress ?? '') ||
        defaultSenderName !== (settings.defaultSenderName ?? '')
      setHasChanges(changed)
    }
  }, [supportAddress, defaultSenderName, settings])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    try {
      await updateSettings.mutateAsync({
        supportAddress: supportAddress.trim() || null,
        defaultSenderAddress: null,
        defaultSenderName: defaultSenderName.trim() || null,
      })
      notify.success('System settings updated')
      setHasChanges(false)
    } catch (err: unknown) {
      console.error('[SystemSettingsAdmin] Failed to update:', err)
      const errorMessage = err instanceof Error ? err.message : 'Failed to update settings'
      notify.error(errorMessage)
    }
  }

  const handleReset = () => {
    if (settings) {
      setSupportAddress(settings.supportAddress ?? '')
      setDefaultSenderName(settings.defaultSenderName ?? '')
      setHasChanges(false)
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress size={32} />
      </Box>
    )
  }

  if (error) {
    return (
      <Alert severity="error">
        {error instanceof Error ? error.message : 'Failed to load system settings'}
      </Alert>
    )
  }

  return (
    <Box>
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 3 }}>
        <FontAwesomeIcon icon={faEnvelope} />
        <Box>
          <Typography variant="h6">Email Configuration</Typography>
          <Typography variant="caption" color="text.secondary">
            Override default email settings. Leave blank to use deployment defaults.
          </Typography>
        </Box>
      </Stack>

      <form onSubmit={handleSubmit}>
        <Stack spacing={2.5} sx={{ maxWidth: 480 }}>
          <CobraTextField
            label="Support Email Address"
            value={supportAddress}
            onChange={e => setSupportAddress(e.target.value)}
            fullWidth
            type="email"
            placeholder={settings?.effectiveSupportAddress}
            helperText={
              supportAddress
                ? 'Overriding deployment default'
                : `Default: ${settings?.effectiveSupportAddress ?? 'not configured'}`
            }
            slotProps={{
              input: {
                endAdornment: supportAddress ? (
                  <Tooltip title="Clear override">
                    <IconButton size="small" onClick={() => setSupportAddress('')}>
                      <FontAwesomeIcon icon={faXmark} size="sm" />
                    </IconButton>
                  </Tooltip>
                ) : undefined,
              },
            }}
          />

          <CobraTextField
            label="Default Sender Name"
            value={defaultSenderName}
            onChange={e => setDefaultSenderName(e.target.value)}
            fullWidth
            placeholder={settings?.effectiveDefaultSenderName}
            helperText={
              defaultSenderName
                ? 'Overriding deployment default'
                : `Default: ${settings?.effectiveDefaultSenderName ?? 'not configured'}`
            }
            slotProps={{
              input: {
                endAdornment: defaultSenderName ? (
                  <Tooltip title="Clear override">
                    <IconButton size="small" onClick={() => setDefaultSenderName('')}>
                      <FontAwesomeIcon icon={faXmark} size="sm" />
                    </IconButton>
                  </Tooltip>
                ) : undefined,
              },
            }}
          />

          {settings?.updatedAt && (
            <Typography variant="caption" color="text.secondary">
              Last updated: {new Date(settings.updatedAt).toLocaleString()}
              {settings.updatedBy && ` by ${settings.updatedBy}`}
            </Typography>
          )}

          <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', pt: 1 }}>
            <CobraSecondaryButton
              onClick={handleReset}
              disabled={!hasChanges || updateSettings.isPending}
            >
              Reset
            </CobraSecondaryButton>
            <CobraPrimaryButton
              type="submit"
              disabled={!hasChanges || updateSettings.isPending}
              startIcon={
                updateSettings.isPending ? (
                  <CircularProgress size={16} color="inherit" />
                ) : (
                  <FontAwesomeIcon icon={faSave} />
                )
              }
            >
              {updateSettings.isPending ? 'Saving...' : 'Save Changes'}
            </CobraPrimaryButton>
          </Box>
        </Stack>
      </form>
    </Box>
  )
}

export default SystemSettingsAdmin
