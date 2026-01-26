/**
 * Notification API Service
 *
 * API calls for the Notifications feature.
 */
import { apiClient } from '@/core/services/api'
import type {
  NotificationsResponse,
  UnreadCountResponse,
  MarkAllReadResponse,
} from '../types'

/**
 * Get notifications for the current user.
 */
export async function getNotifications(
  limit = 10,
  offset = 0,
): Promise<NotificationsResponse> {
  const response = await apiClient.get<NotificationsResponse>('/notifications', {
    params: { limit, offset },
  })
  return response.data
}

/**
 * Get unread notification count.
 */
export async function getUnreadCount(): Promise<number> {
  const response = await apiClient.get<UnreadCountResponse>(
    '/notifications/unread-count',
  )
  return response.data.unreadCount
}

/**
 * Mark a notification as read.
 */
export async function markAsRead(notificationId: string): Promise<void> {
  await apiClient.post('/notifications/' + notificationId + '/read')
}

/**
 * Mark all notifications as read.
 */
export async function markAllAsRead(): Promise<number> {
  const response = await apiClient.post<MarkAllReadResponse>(
    '/notifications/read-all',
  )
  return response.data.markedCount
}
