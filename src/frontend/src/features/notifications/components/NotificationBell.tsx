/**
 * NotificationBell Component
 *
 * Bell icon with unread count badge and dropdown.
 */
import { useState, useRef } from 'react'
import { Badge, Popover } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faBell } from '@fortawesome/free-solid-svg-icons'
import {
  useNotifications,
  useUnreadCount,
  useMarkAsRead,
  useMarkAllAsRead,
} from '../hooks/useNotifications'
import { NotificationDropdown } from './NotificationDropdown'
import { CobraIconButton } from '@/theme/styledComponents'

export function NotificationBell() {
  const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)

  const { data: unreadCount = 0 } = useUnreadCount()
  const {
    data: notificationsData,
    isLoading,
    isError,
  } = useNotifications(10)
  const markAsReadMutation = useMarkAsRead()
  const markAllAsReadMutation = useMarkAllAsRead()

  const isOpen = Boolean(anchorEl)

  const handleClick = () => {
    setAnchorEl(buttonRef.current)
  }

  const handleClose = () => {
    setAnchorEl(null)
  }

  const handleMarkAsRead = (notificationId: string) => {
    markAsReadMutation.mutate(notificationId)
  }

  const handleMarkAllAsRead = () => {
    markAllAsReadMutation.mutate()
  }

  // Format badge content
  const badgeContent = unreadCount > 99 ? '99+' : unreadCount

  return (
    <>
      <CobraIconButton
        ref={buttonRef}
        onClick={handleClick}
        aria-label="notifications"
        aria-haspopup="true"
        aria-expanded={isOpen}
        sx={{ color: 'inherit' }}
      >
        <Badge
          badgeContent={badgeContent}
          color="error"
          invisible={unreadCount === 0}
          max={99}
        >
          <FontAwesomeIcon icon={faBell} />
        </Badge>
      </CobraIconButton>

      <Popover
        open={isOpen}
        anchorEl={anchorEl}
        onClose={handleClose}
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
            elevation: 8,
            sx: { mt: 1, borderRadius: 2 },
          },
        }}
      >
        <NotificationDropdown
          notifications={notificationsData?.items || []}
          isLoading={isLoading}
          isError={isError}
          onMarkAsRead={handleMarkAsRead}
          onMarkAllAsRead={handleMarkAllAsRead}
          onClose={handleClose}
        />
      </Popover>
    </>
  )
}
