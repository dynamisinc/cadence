/**
 * NotificationDropdown Component
 *
 * Dropdown menu showing recent notifications.
 */
import { Box, Typography, Divider, List, CircularProgress, Alert } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faInbox } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import type { NotificationDto } from '../types'
import { NotificationItem } from './NotificationItem'
import { CobraLinkButton } from '@/theme/styledComponents'

interface NotificationDropdownProps {
  notifications: NotificationDto[]
  isLoading: boolean
  isError: boolean
  onMarkAsRead: (notificationId: string) => void
  onMarkAllAsRead: () => void
  onClose: () => void
}

export function NotificationDropdown({
  notifications,
  isLoading,
  isError,
  onMarkAsRead,
  onMarkAllAsRead,
  onClose,
}: NotificationDropdownProps) {
  const navigate = useNavigate()

  const handleNotificationClick = (notification: NotificationDto) => {
    // Mark as read
    if (!notification.isRead) {
      onMarkAsRead(notification.id)
    }

    // Navigate if action URL exists
    if (notification.actionUrl) {
      navigate(notification.actionUrl)
      onClose()
    }
  }

  const unreadCount = notifications.filter(n => !n.isRead).length

  return (
    <Box sx={{ width: 360, maxHeight: 480, overflow: 'hidden' }}>
      {/* Header */}
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems="center"
        px={2}
        py={1.5}
      >
        <Typography variant="subtitle1" fontWeight="medium">
          Notifications
        </Typography>
        {unreadCount > 0 && (
          <CobraLinkButton size="small" onClick={onMarkAllAsRead}>
            Mark all as read
          </CobraLinkButton>
        )}
      </Box>
      <Divider />

      {/* Loading State */}
      {isLoading && (
        <Box display="flex" justifyContent="center" py={4}>
          <CircularProgress size={24} />
        </Box>
      )}

      {/* Error State */}
      {isError && (
        <Box px={2} py={2}>
          <Alert severity="error">Failed to load notifications</Alert>
        </Box>
      )}

      {/* Empty State */}
      {!isLoading && !isError && notifications.length === 0 && (
        <Box
          display="flex"
          flexDirection="column"
          alignItems="center"
          py={4}
          color="text.secondary"
        >
          <FontAwesomeIcon
            icon={faInbox}
            style={{ fontSize: '2rem', marginBottom: '0.5rem', color: '#ccc' }}
          />
          <Typography variant="body2">No notifications</Typography>
        </Box>
      )}

      {/* Notification List */}
      {!isLoading && !isError && notifications.length > 0 && (
        <List disablePadding sx={{ maxHeight: 400, overflow: 'auto' }}>
          {notifications.map(notification => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              onClick={handleNotificationClick}
            />
          ))}
        </List>
      )}
    </Box>
  )
}
