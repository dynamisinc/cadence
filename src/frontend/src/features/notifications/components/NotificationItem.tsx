/**
 * NotificationItem Component
 *
 * Displays a single notification in the dropdown.
 */
import { Box, Typography, ListItemButton, ListItemIcon, ListItemText } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faExclamationCircle,
  faPlay,
  faClock,
  faPause,
  faCheckCircle,
  faUserPlus,
  faEye,
  faCircle,
  faBell,
} from '@fortawesome/free-solid-svg-icons'
import type { NotificationDto, NotificationType } from '../types'

interface NotificationItemProps {
  notification: NotificationDto
  onClick: (notification: NotificationDto) => void
}

/**
 * Get icon for notification type.
 */
function getNotificationIcon(type: NotificationType) {
  switch (type) {
    case 'InjectReady':
      return { icon: faExclamationCircle, color: '#ff9800' }
    case 'InjectFired':
      return { icon: faPlay, color: '#4caf50' }
    case 'ClockStarted':
      return { icon: faClock, color: '#2196f3' }
    case 'ClockPaused':
      return { icon: faPause, color: '#ff9800' }
    case 'ExerciseCompleted':
      return { icon: faCheckCircle, color: '#4caf50' }
    case 'AssignmentCreated':
      return { icon: faUserPlus, color: '#9c27b0' }
    case 'ObservationCreated':
      return { icon: faEye, color: '#00bcd4' }
    case 'ExerciseStatusChanged':
      return { icon: faCircle, color: '#2196f3' }
    default:
      return { icon: faBell, color: '#666' }
  }
}

/**
 * Format relative time (e.g., "2m ago", "1h ago").
 */
function formatTimeAgo(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHour = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHour / 24)

  if (diffSec < 60) return 'Just now'
  if (diffMin < 60) return diffMin + 'm ago'
  if (diffHour < 24) return diffHour + 'h ago'
  if (diffDay < 7) return diffDay + 'd ago'
  return date.toLocaleDateString()
}

export function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const { icon, color } = getNotificationIcon(notification.type)

  return (
    <ListItemButton
      onClick={() => onClick(notification)}
      sx={{
        py: 1.5,
        px: 2,
        backgroundColor: notification.isRead ? 'transparent' : 'action.hover',
        '&:hover': {
          backgroundColor: 'action.selected',
        },
      }}
    >
      <ListItemIcon sx={{ minWidth: 40 }}>
        <FontAwesomeIcon icon={icon} style={{ color, fontSize: '1rem' }} />
      </ListItemIcon>
      <ListItemText
        primary={
          <Typography
            variant="body2"
            fontWeight={notification.isRead ? 'normal' : 'medium'}
            noWrap
          >
            {notification.title}
          </Typography>
        }
        secondary={
          <Box display="flex" alignItems="center" gap={1}>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                flex: 1,
              }}
            >
              {notification.message}
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0 }}>
              {formatTimeAgo(notification.createdAt)}
            </Typography>
          </Box>
        }
      />
      {/* Unread indicator */}
      {!notification.isRead && (
        <Box
          sx={{
            width: 8,
            height: 8,
            borderRadius: '50%',
            backgroundColor: 'primary.main',
            ml: 1,
          }}
        />
      )}
    </ListItemButton>
  )
}
