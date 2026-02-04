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
  ApprovalNotificationDto,
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

// =============================================================================
// Approval Notifications (S08)
// =============================================================================

/**
 * Get approval notifications for the current user.
 */
export async function getApprovalNotifications(
  limit = 20,
  unreadOnly = false,
): Promise<ApprovalNotificationDto[]> {
  const response = await apiClient.get<ApprovalNotificationDto[]>(
    '/notifications/approval',
    { params: { limit, unreadOnly } },
  )
  return response.data
}

/**
 * Get unread approval notification count.
 */
export async function getApprovalUnreadCount(): Promise<number> {
  const response = await apiClient.get<{ count: number }>(
    '/notifications/approval/unread-count',
  )
  return response.data.count
}

/**
 * Mark an approval notification as read.
 */
export async function markApprovalAsRead(notificationId: string): Promise<void> {
  await apiClient.put(`/notifications/approval/${notificationId}/read`)
}

/**
 * Mark all approval notifications as read.
 */
export async function markAllApprovalAsRead(): Promise<void> {
  await apiClient.put('/notifications/approval/read-all')
}
