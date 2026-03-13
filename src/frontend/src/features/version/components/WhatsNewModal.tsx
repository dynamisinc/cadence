import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Typography,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Box,
  Divider,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faStar, faBug, faRocket } from '@fortawesome/free-solid-svg-icons'
import { appVersion } from '@/config/version'
import { useReleaseNotes, getReleaseNotesForVersion } from '../hooks/useReleaseNotes'

interface WhatsNewModalProps {
  open: boolean;
  onDismiss: () => void;
  onViewAllNotes?: () => void;
}

/**
 * Modal displayed when user opens app after a version update.
 * Shows features and fixes for the current version.
 */
export function WhatsNewModal({ open, onDismiss, onViewAllNotes }: WhatsNewModalProps) {
  const theme = useTheme()
  const { releaseNotes } = useReleaseNotes()
  const currentNotes = getReleaseNotesForVersion(releaseNotes, appVersion.version)

  const handleViewAllNotes = () => {
    onDismiss()
    onViewAllNotes?.()
  }

  return (
    <Dialog
      open={open}
      onClose={onDismiss}
      maxWidth="sm"
      fullWidth
      aria-labelledby="whats-new-title"
    >
      <DialogTitle id="whats-new-title">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faRocket} color={theme.palette.semantic.warning} />
          <Typography variant="h6" component="span">
            What's New in Cadence
          </Typography>
        </Box>
        <Typography variant="subtitle2" color="text.secondary" component="span" sx={{ display: 'block' }}>
          Version {appVersion.version}
        </Typography>
      </DialogTitle>

      <Divider />

      <DialogContent>
        {currentNotes ? (
          <>
            {currentNotes.features.length > 0 && (
              <Box sx={{ mb: 2 }}>
                <Typography variant="subtitle1" fontWeight="medium" gutterBottom>
                  Features
                </Typography>
                <List dense disablePadding>
                  {currentNotes.features.map((feature, index) => (
                    <ListItem key={index} disableGutters>
                      <ListItemIcon sx={{ minWidth: 32 }}>
                        <FontAwesomeIcon icon={faStar} size="sm" color={theme.palette.semantic.success} />
                      </ListItemIcon>
                      <ListItemText primary={feature} />
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}

            {currentNotes.fixes.length > 0 && (
              <Box>
                <Typography variant="subtitle1" fontWeight="medium" gutterBottom>
                  Fixes
                </Typography>
                <List dense disablePadding>
                  {currentNotes.fixes.map((fix, index) => (
                    <ListItem key={index} disableGutters>
                      <ListItemIcon sx={{ minWidth: 32 }}>
                        <FontAwesomeIcon icon={faBug} size="sm" color={theme.palette.semantic.warning} />
                      </ListItemIcon>
                      <ListItemText primary={fix} />
                    </ListItem>
                  ))}
                </List>
              </Box>
            )}
          </>
        ) : (
          <Typography color="text.secondary">
            Updated to version {appVersion.version}
          </Typography>
        )}
      </DialogContent>

      <Divider />

      <DialogActions sx={{ justifyContent: 'space-between', px: 3, py: 2 }}>
        <Button onClick={handleViewAllNotes} color="inherit">
          View all release notes
        </Button>
        <Button onClick={onDismiss} variant="contained" autoFocus>
          Got it
        </Button>
      </DialogActions>
    </Dialog>
  )
}
