/**
 * Notification Types
 *
 * Types for the Notifications feature.
 */

/**
 * Notification type enum values.
 */
export type NotificationType =
  | 'InjectReady'
  | 'InjectFired'
  | 'ClockStarted'
  | 'ClockPaused'
  | 'ExerciseCompleted'
  | 'AssignmentCreated'
  | 'ObservationCreated'
  | 'ExerciseStatusChanged'
  | 'System'

/**
 * Notification priority levels.
 */
export type NotificationPriority = 'Low' | 'Medium' | 'High'

/**
 * DTO representing a notification.
 */
export interface NotificationDto {
  /** Unique identifier */
  id: string
  /** Notification type */
  type: NotificationType
  /** Priority level */
  priority: NotificationPriority
  /** Short title */
  title: string
  /** Detailed message */
  message: string
  /** URL to navigate to when clicked */
  actionUrl: string | null
  /** Related entity type */
  relatedEntityType: string | null
  /** Related entity ID */
  relatedEntityId: string | null
  /** Whether read by user */
  isRead: boolean
  /** When created */
  createdAt: string
  /** When read */
  readAt: string | null
}

/**
 * Response containing notifications with counts.
 */
export interface NotificationsResponse {
  /** List of notifications */
  items: NotificationDto[]
  /** Total count */
  totalCount: number
  /** Unread count */
  unreadCount: number
}

/**
 * Unread count response.
 */
export interface UnreadCountResponse {
  unreadCount: number
}

/**
 * Mark all read response.
 */
export interface MarkAllReadResponse {
  markedCount: number
}

/**
 * Toast configuration based on priority.
 */
export interface ToastConfig {
  /** Show toast for this priority */
  showToast: boolean
  /** Auto-dismiss duration in ms (null = never) */
  autoDismissMs: number | null
  /** Background color */
  backgroundColor: string
  /** Border color */
  borderColor: string
}

/**
 * Toast state for the provider.
 */
export interface Toast {
  /** Unique toast ID */
  id: string
  /** The notification data */
  notification: NotificationDto
  /** When the toast was created */
  createdAt: Date
}
