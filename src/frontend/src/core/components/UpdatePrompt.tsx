/**
 * UpdatePrompt Component
 *
 * Displays notifications for PWA updates and offline readiness.
 * Shows a snackbar when:
 * - The app is ready to work offline (first install)
 * - A new version is available (update waiting)
 */

import { Snackbar, Alert, Button, Box } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowsRotate, faCheck, faXmark } from '@fortawesome/free-solid-svg-icons'
import { useServiceWorker } from '../../shared/hooks'

export function UpdatePrompt() {
  const { needRefresh, offlineReady, updateServiceWorker, dismissNotification } = useServiceWorker()

  // Show "offline ready" notification
  if (offlineReady) {
    return (
      <Snackbar
        open
        autoHideDuration={6000}
        onClose={dismissNotification}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity="success"
          onClose={dismissNotification}
          icon={<FontAwesomeIcon icon={faCheck} />}
          sx={{ alignItems: 'center' }}
        >
          Cadence is ready to work offline
        </Alert>
      </Snackbar>
    )
  }

  // Show "update available" notification
  if (needRefresh) {
    return (
      <Snackbar
        open
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity="info"
          icon={<FontAwesomeIcon icon={faArrowsRotate} />}
          sx={{ alignItems: 'center' }}
          action={
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                color="inherit"
                size="small"
                onClick={() => updateServiceWorker()}
                startIcon={<FontAwesomeIcon icon={faArrowsRotate} size="sm" />}
              >
                Update
              </Button>
              <Button
                color="inherit"
                size="small"
                onClick={dismissNotification}
                startIcon={<FontAwesomeIcon icon={faXmark} size="sm" />}
              >
                Later
              </Button>
            </Box>
          }
        >
          A new version of Cadence is available
        </Alert>
      </Snackbar>
    )
  }

  return null
}
