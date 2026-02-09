/**
 * InstallBanner Component
 *
 * Displays a banner prompting users to install the Cadence PWA.
 * Only shown when the app is installable (beforeinstallprompt event captured).
 * Dismiss is persisted to localStorage with a 90-day cooldown.
 * Re-shown on major version updates regardless of cooldown.
 * Follows COBRA styling guidelines.
 */

import { useState } from 'react'
import { Paper, Typography, IconButton, Box } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faDownload, faXmark } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '../../theme/styledComponents'
import { useInstallPrompt } from '../../shared/hooks'
import { shouldShowBanner, persistDismiss } from './installBannerUtils'

export function InstallBanner() {
  const { canInstall, promptInstall } = useInstallPrompt()
  const [dismissed, setDismissed] = useState(() => !shouldShowBanner())

  // Don't show if not installable or user dismissed
  if (!canInstall || dismissed) {
    return null
  }

  const handleDismiss = () => {
    persistDismiss()
    setDismissed(true)
  }

  const handleInstall = async () => {
    const accepted = await promptInstall()
    if (!accepted) {
      // User declined, dismiss the banner with cooldown
      handleDismiss()
    }
  }

  return (
    <Paper
      elevation={3}
      sx={{
        position: 'fixed',
        bottom: 16,
        left: 16,
        right: 16,
        p: 2,
        display: 'flex',
        alignItems: 'center',
        gap: 2,
        zIndex: 1100, // Above most content but below modals
        maxWidth: 500,
        mx: 'auto',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          width: 40,
          height: 40,
          borderRadius: '50%',
          bgcolor: 'buttonPrimary.main',
          color: 'buttonPrimary.contrastText',
          flexShrink: 0,
        }}
      >
        <FontAwesomeIcon icon={faDownload} />
      </Box>

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Typography variant="subtitle2" fontWeight={600}>
          Install Cadence
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Add to your device for offline access
        </Typography>
      </Box>

      <CobraPrimaryButton
        onClick={handleInstall}
        size="small"
        startIcon={<FontAwesomeIcon icon={faDownload} size="sm" />}
      >
        Install
      </CobraPrimaryButton>

      <IconButton
        size="small"
        onClick={handleDismiss}
        aria-label="Dismiss install banner"
        sx={{ ml: -1 }}
      >
        <FontAwesomeIcon icon={faXmark} />
      </IconButton>
    </Paper>
  )
}
