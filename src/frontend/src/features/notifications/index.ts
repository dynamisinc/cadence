/**
 * Notifications Feature
 *
 * Real-time notifications with bell indicator and toasts.
 */

// Components
export { NotificationBell } from './components/NotificationBell'
export { NotificationDropdown } from './components/NotificationDropdown'
export { NotificationItem } from './components/NotificationItem'
export { NotificationToast } from './components/NotificationToast'
export { NotificationToastProvider, useToast } from './components/NotificationToastProvider'

// Hooks
export {
  useNotifications,
  useUnreadCount,
  useMarkAsRead,
  useMarkAllAsRead,
  addNotificationToCache,
  NOTIFICATIONS_QUERY_KEY,
  UNREAD_COUNT_QUERY_KEY,
} from './hooks/useNotifications'
export { useNotificationSignalR } from './hooks/useNotificationSignalR'
export { useNotificationToast, getToastConfig } from './hooks/useNotificationToast'

// Types
export type {
  NotificationDto,
  NotificationsResponse,
  NotificationType,
  NotificationPriority,
  Toast,
  ToastConfig,
} from './types'
