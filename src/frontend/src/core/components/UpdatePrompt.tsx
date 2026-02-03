/**
 * UpdatePrompt Component
 *
 * Displays notifications for PWA updates and offline readiness.
 * Shows a snackbar when:
 * - The app is ready to work offline (first install)
 * - A new version is available (update waiting)
 *
 * The update notification includes:
 * - A summary of what's new (from bundled release notes)
 * - Link to full release notes (public /about page)
 * - Update and dismiss buttons
 */

import { useState } from 'react'
import { Snackbar, Alert, Button, Box, Typography, Collapse, List, ListItem, ListItemIcon, ListItemText, Link } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowsRotate, faCheck, faChevronDown, faChevronUp, faStar, faBug } from '@fortawesome/free-solid-svg-icons'
import { useServiceWorker } from '../../shared/hooks'
import { useReleaseNotes } from '../../features/version/hooks/useReleaseNotes'

const MAX_ITEMS_TO_SHOW = 3

export function UpdatePrompt() {
  const { needRefresh, offlineReady, updateServiceWorker, dismissNotification } = useServiceWorker()
  const { releaseNotes } = useReleaseNotes()
  const [expanded, setExpanded] = useState(false)

  // Get the latest release notes (most recent version)
  const latestRelease = releaseNotes[0]
  const hasChanges =
    latestRelease && (latestRelease.features.length > 0 || latestRelease.fixes.length > 0)

  // Combine features and fixes for display, limited to MAX_ITEMS_TO_SHOW
  const allChanges = latestRelease
    ? [
      ...latestRelease.features.map(f => ({ type: 'feature' as const, text: f })),
      ...latestRelease.fixes.map(f => ({ type: 'fix' as const, text: f })),
    ]
    : []
  const displayChanges = allChanges.slice(0, MAX_ITEMS_TO_SHOW)
  const _hasMoreChanges = allChanges.length > MAX_ITEMS_TO_SHOW

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

  // Show "update available" notification with release notes
  if (needRefresh) {
    return (
      <Snackbar
        open
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        sx={{ maxWidth: 450 }}
      >
        <Alert
          severity="info"
          icon={<FontAwesomeIcon icon={faArrowsRotate} />}
          sx={{
            alignItems: 'flex-start',
            '& .MuiAlert-message': { width: '100%' },
          }}
        >
          <Box sx={{ width: '100%' }}>
            <Typography variant="body2" fontWeight="medium">
              A new version of Cadence is available
            </Typography>

            {/* Show recent changes to give context, with link to full notes */}
            {hasChanges && (
              <>
                <Button
                  size="small"
                  onClick={() => setExpanded(!expanded)}
                  endIcon={<FontAwesomeIcon icon={expanded ? faChevronUp : faChevronDown} size="xs" />}
                  sx={{ mt: 0.5, mb: 0.5, p: 0, minWidth: 'auto', textTransform: 'none' }}
                >
                  {expanded ? 'Hide' : 'Recent changes'}
                </Button>

                <Collapse in={expanded}>
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                    Latest in v{latestRelease.version}:
                  </Typography>
                  <List dense disablePadding>
                    {displayChanges.map((change, index) => (
                      <ListItem key={index} disableGutters sx={{ py: 0.25 }}>
                        <ListItemIcon sx={{ minWidth: 24 }}>
                          <FontAwesomeIcon
                            icon={change.type === 'feature' ? faStar : faBug}
                            size="xs"
                            color={change.type === 'feature' ? '#4caf50' : '#ff9800'}
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={change.text}
                          primaryTypographyProps={{ variant: 'body2', fontSize: '0.8rem' }}
                        />
                      </ListItem>
                    ))}
                  </List>
                  <Link
                    href="/about"
                    sx={{ fontSize: '0.75rem', display: 'block', mt: 0.5 }}
                  >
                    View all release notes →
                  </Link>
                </Collapse>
              </>
            )}

            {/* Always show link to release notes if no changes to expand */}
            {!hasChanges && (
              <Link
                href="/about"
                sx={{ fontSize: '0.8rem', display: 'block', mt: 0.5 }}
              >
                View release notes →
              </Link>
            )}

            <Box sx={{ display: 'flex', gap: 1, mt: 1.5 }}>
              <Button
                color="inherit"
                size="small"
                variant="outlined"
                onClick={() => updateServiceWorker()}
                startIcon={<FontAwesomeIcon icon={faArrowsRotate} size="sm" />}
              >
                Update now
              </Button>
              <Button
                color="inherit"
                size="small"
                onClick={dismissNotification}
              >
                Later
              </Button>
            </Box>
          </Box>
        </Alert>
      </Snackbar>
    )
  }

  return null
}
