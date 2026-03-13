/**
 * NotificationItem Component
 *
 * Displays a single notification in the dropdown.
 */
import { Box, Typography, ListItemButton, ListItemIcon, ListItemText } from '@mui/material'
import { useTheme } from '@mui/material/styles'
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
import { formatDate } from '@/shared/utils/dateUtils'

interface NotificationItemProps {
  notification: NotificationDto
  onClick: (notification: NotificationDto) => void
}

/**
 * Get icon for notification type.
 */
function getNotificationIcon(
  type: NotificationType,
  palette: {
    semantic: { warning: string; success: string; info: string; purple: string; cyan: string }
    neutral: { 600: string }
  },
) {
  switch (type) {
    case 'InjectReady':
      return { icon: faExclamationCircle, color: palette.semantic.warning }
    case 'InjectFired':
      return { icon: faPlay, color: palette.semantic.success }
    case 'ClockStarted':
      return { icon: faClock, color: palette.semantic.info }
    case 'ClockPaused':
      return { icon: faPause, color: palette.semantic.warning }
    case 'ExerciseCompleted':
      return { icon: faCheckCircle, color: palette.semantic.success }
    case 'AssignmentCreated':
      return { icon: faUserPlus, color: palette.semantic.purple }
    case 'ObservationCreated':
      return { icon: faEye, color: palette.semantic.cyan }
    case 'ExerciseStatusChanged':
      return { icon: faCircle, color: palette.semantic.info }
    default:
      return { icon: faBell, color: palette.neutral[600] }
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
  return formatDate(dateString)
}

export function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const theme = useTheme()
  const { icon, color } = getNotificationIcon(notification.type, theme.palette)

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
