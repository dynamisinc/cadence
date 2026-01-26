/**
 * NotificationToast Component
 *
 * Single toast notification display.
 */
import { Box, Typography, IconButton, Paper } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faXmark,
  faExclamationCircle,
  faPlay,
  faClock,
  faPause,
  faCheckCircle,
  faUserPlus,
  faEye,
  faBell,
} from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import type { Toast, NotificationType, NotificationPriority } from '../types'
import { getToastConfig } from '../hooks/useNotificationToast'

interface NotificationToastProps {
  toast: Toast
  onDismiss: (toastId: string) => void
  onMouseEnter: (toastId: string) => void
  onMouseLeave: (toastId: string, priority: NotificationPriority) => void
}

/**
 * Get icon for notification type.
 */
function getNotificationIcon(type: NotificationType) {
  switch (type) {
    case 'InjectReady':
      return faExclamationCircle
    case 'InjectFired':
      return faPlay
    case 'ClockStarted':
      return faClock
    case 'ClockPaused':
      return faPause
    case 'ExerciseCompleted':
      return faCheckCircle
    case 'AssignmentCreated':
      return faUserPlus
    case 'ObservationCreated':
      return faEye
    default:
      return faBell
  }
}

export function NotificationToast({
  toast,
  onDismiss,
  onMouseEnter,
  onMouseLeave,
}: NotificationToastProps) {
  const navigate = useNavigate()
  const { notification } = toast
  const config = getToastConfig(notification.priority)
  const icon = getNotificationIcon(notification.type)

  const handleClick = () => {
    if (notification.actionUrl) {
      navigate(notification.actionUrl)
      onDismiss(toast.id)
    }
  }

  return (
    <Paper
      elevation={6}
      onMouseEnter={() => onMouseEnter(toast.id)}
      onMouseLeave={() => onMouseLeave(toast.id, notification.priority)}
      onClick={notification.actionUrl ? handleClick : undefined}
      sx={{
        width: 360,
        p: 2,
        mb: 1,
        backgroundColor: config.backgroundColor,
        borderLeft: '4px solid ' + config.borderColor,
        cursor: notification.actionUrl ? 'pointer' : 'default',
        transition: 'transform 0.2s, opacity 0.2s',
        '&:hover': {
          transform: notification.actionUrl ? 'translateX(-4px)' : 'none',
        },
      }}
    >
      <Box display="flex" alignItems="flex-start" gap={1.5}>
        {/* Icon */}
        <FontAwesomeIcon
          icon={icon}
          style={{ color: config.borderColor, marginTop: 2 }}
        />

        {/* Content */}
        <Box flex={1}>
          <Typography variant="subtitle2" fontWeight="medium">
            {notification.title}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {notification.message}
          </Typography>
          {notification.actionUrl && (
            <Typography
              variant="caption"
              color="primary"
              sx={{ mt: 0.5, display: 'block' }}
            >
              Click to view
            </Typography>
          )}
        </Box>

        {/* Close Button */}
        <IconButton
          size="small"
          onClick={e => {
            e.stopPropagation()
            onDismiss(toast.id)
          }}
          sx={{ ml: 1 }}
        >
          <FontAwesomeIcon icon={faXmark} style={{ fontSize: '0.875rem' }} />
        </IconButton>
      </Box>
    </Paper>
  )
}
