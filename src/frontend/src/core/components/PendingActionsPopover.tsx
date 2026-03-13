/**
 * PendingActionsPopover Component
 *
 * Displays a list of pending offline actions with details and a force sync button.
 * Shown when clicking on the ConnectionStatusIndicator.
 */

import React, { useEffect, useState } from 'react'
import {
  Popover,
  Box,
  Typography,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Divider,
  CircularProgress,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faBolt,
  faForward,
  faRotateLeft,
  faPlus,
  faPen,
  faTrash,
  faSync,
  faCheck,
  faExclamationTriangle,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { cobraTheme } from '@/theme/cobraTheme'
import { formatDate } from '@/shared/utils/dateUtils'
import { getPendingActions, type PendingAction, type PendingActionType } from '../offline/db'
import { useOfflineSyncContext } from '../contexts/OfflineSyncContext'

interface PendingActionsPopoverProps {
  anchorEl: HTMLElement | null
  open: boolean
  onClose: () => void
}

interface ActionConfig {
  icon: typeof faBolt
  label: string
  color: string
}

const actionConfigs: Record<PendingActionType, ActionConfig> = {
  FIRE_INJECT: {
    icon: faBolt,
    label: 'Fire Inject',
    color: cobraTheme.palette.semantic.warningAmber,
  },
  SKIP_INJECT: {
    icon: faForward,
    label: 'Skip Inject',
    color: cobraTheme.palette.neutral[500],
  },
  RESET_INJECT: {
    icon: faRotateLeft,
    label: 'Reset Inject',
    color: cobraTheme.palette.semantic.info,
  },
  CREATE_OBSERVATION: {
    icon: faPlus,
    label: 'Create Observation',
    color: cobraTheme.palette.semantic.success,
  },
  UPDATE_OBSERVATION: {
    icon: faPen,
    label: 'Update Observation',
    color: cobraTheme.palette.semantic.purple,
  },
  DELETE_OBSERVATION: {
    icon: faTrash,
    label: 'Delete Observation',
    color: cobraTheme.palette.semantic.error,
  },
  UPLOAD_PHOTO: {
    icon: faPlus,
    label: 'Upload Photo',
    color: cobraTheme.palette.semantic.success,
  },
  QUICK_PHOTO: {
    icon: faPlus,
    label: 'Quick Photo',
    color: cobraTheme.palette.semantic.success,
  },
  UPDATE_PHOTO: {
    icon: faPen,
    label: 'Update Photo',
    color: cobraTheme.palette.semantic.purple,
  },
  DELETE_PHOTO: {
    icon: faTrash,
    label: 'Delete Photo',
    color: cobraTheme.palette.semantic.error,
  },
}

const getActionDescription = (action: PendingAction): string => {
  const payload = action.payload as Record<string, unknown>

  switch (action.type) {
    case 'FIRE_INJECT':
    case 'SKIP_INJECT':
    case 'RESET_INJECT':
      return `Inject #${payload.injectNumber || payload.injectId || 'Unknown'}`
    case 'CREATE_OBSERVATION':
    case 'UPDATE_OBSERVATION':
      return `"${((payload.content as string) || '').substring(0, 30)}${((payload.content as string) || '').length > 30 ? '...' : ''}"`
    case 'DELETE_OBSERVATION':
      return `Observation ${payload.observationId || 'Unknown'}`
    case 'UPLOAD_PHOTO':
    case 'QUICK_PHOTO':
      return `Photo ${payload.tempId || payload.tempPhotoId || 'pending'}`
    case 'UPDATE_PHOTO':
    case 'DELETE_PHOTO':
      return `Photo ${payload.photoId || 'Unknown'}`
    default:
      return 'Unknown action'
  }
}

const formatTimestamp = (date: Date): string => {
  const now = new Date()
  const diff = now.getTime() - new Date(date).getTime()
  const minutes = Math.floor(diff / 60000)
  const hours = Math.floor(diff / 3600000)

  if (minutes < 1) return 'Just now'
  if (minutes < 60) return `${minutes}m ago`
  if (hours < 24) return `${hours}h ago`
  return formatDate(new Date(date).toISOString())
}

export const PendingActionsPopover: React.FC<PendingActionsPopoverProps> = ({
  anchorEl,
  open,
  onClose,
}) => {
  const theme = useTheme()
  const [actions, setActions] = useState<PendingAction[]>([])
  const [loading, setLoading] = useState(true)
  const { manualSync, isSyncing, syncStatus } = useOfflineSyncContext()

  // Load pending actions when popover opens
  useEffect(() => {
    if (open) {
      setLoading(true)
      getPendingActions()
        .then(setActions)
        .finally(() => setLoading(false))
    }
  }, [open])

  // Refresh actions after sync completes
  useEffect(() => {
    if (!isSyncing && syncStatus !== 'syncing' && open) {
      getPendingActions().then(setActions)
    }
  }, [isSyncing, syncStatus, open])

  const handleForceSync = async () => {
    await manualSync()
    // Refresh the list after sync
    const updatedActions = await getPendingActions()
    setActions(updatedActions)
    // Close popover if all actions synced successfully
    if (updatedActions.length === 0) {
      onClose()
    }
  }

  const pendingActions = actions.filter(a => a.status === 'pending' || a.status === 'syncing')
  const failedActions = actions.filter(a => a.status === 'failed')

  return (
    <Popover
      open={open}
      anchorEl={anchorEl}
      onClose={onClose}
      anchorOrigin={{
        vertical: 'bottom',
        horizontal: 'right',
      }}
      transformOrigin={{
        vertical: 'top',
        horizontal: 'right',
      }}
      slotProps={{
        paper: {
          sx: {
            width: 360,
            maxHeight: 480,
            mt: 1,
          },
        },
      }}
    >
      <Box sx={{ p: 2 }}>
        <Typography variant="subtitle1" fontWeight={600} gutterBottom>
          Pending Changes
        </Typography>

        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
            <CircularProgress size={24} />
          </Box>
        ) : actions.length === 0 ? (
          <Box sx={{ py: 2, textAlign: 'center' }}>
            <FontAwesomeIcon
              icon={faCheck}
              style={{ color: theme.palette.semantic.success, fontSize: 24, marginBottom: 8 }}
            />
            <Typography variant="body2" color="text.secondary">
              All changes synced
            </Typography>
          </Box>
        ) : (
          <>
            {/* Pending Actions */}
            {pendingActions.length > 0 && (
              <>
                <Typography
                  variant="caption"
                  color="text.secondary"
                  sx={{ display: 'block', mb: 1 }}
                >
                  {pendingActions.length} pending action{pendingActions.length !== 1 ? 's' : ''}
                </Typography>
                <List dense disablePadding>
                  {pendingActions.map(action => {
                    const config = actionConfigs[action.type]
                    return (
                      <ListItem
                        key={action.id}
                        sx={{
                          px: 1,
                          py: 0.5,
                          bgcolor: action.status === 'syncing' ? 'action.hover' : 'transparent',
                          borderRadius: 1,
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 32 }}>
                          {action.status === 'syncing' ? (
                            <CircularProgress size={16} />
                          ) : (
                            <FontAwesomeIcon icon={config.icon} style={{ color: config.color }} />
                          )}
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Typography variant="body2" noWrap>
                              {config.label}
                            </Typography>
                          }
                          secondary={
                            <Typography variant="caption" color="text.secondary" noWrap>
                              {getActionDescription(action)} - {formatTimestamp(action.timestamp)}
                            </Typography>
                          }
                        />
                      </ListItem>
                    )
                  })}
                </List>
              </>
            )}

            {/* Failed Actions */}
            {failedActions.length > 0 && (
              <>
                {pendingActions.length > 0 && <Divider sx={{ my: 1 }} />}
                <Typography
                  variant="caption"
                  color="error"
                  sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 1 }}
                >
                  <FontAwesomeIcon icon={faExclamationTriangle} />
                  {failedActions.length} failed action{failedActions.length !== 1 ? 's' : ''}
                </Typography>
                <List dense disablePadding>
                  {failedActions.map(action => {
                    const config = actionConfigs[action.type]
                    return (
                      <ListItem
                        key={action.id}
                        sx={{
                          px: 1,
                          py: 0.5,
                          bgcolor: 'rgba(239, 68, 68, 0.1)',
                          borderRadius: 1,
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 32 }}>
                          <FontAwesomeIcon
                            icon={config.icon}
                            style={{ color: theme.palette.semantic.error, opacity: 0.7 }}
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={
                            <Typography variant="body2" noWrap>
                              {config.label}
                            </Typography>
                          }
                          secondary={
                            <Typography variant="caption" color="error" noWrap>
                              {action.error || 'Sync failed'}
                            </Typography>
                          }
                        />
                      </ListItem>
                    )
                  })}
                </List>
              </>
            )}

            {/* Force Sync Button */}
            <Divider sx={{ my: 2 }} />
            <CobraPrimaryButton
              fullWidth
              onClick={handleForceSync}
              disabled={isSyncing || pendingActions.length === 0}
              startIcon={
                isSyncing ? (
                  <CircularProgress size={16} color="inherit" />
                ) : (
                  <FontAwesomeIcon icon={faSync} />
                )
              }
            >
              {isSyncing ? 'Syncing...' : 'Sync Now'}
            </CobraPrimaryButton>
          </>
        )}
      </Box>
    </Popover>
  )
}

export default PendingActionsPopover
