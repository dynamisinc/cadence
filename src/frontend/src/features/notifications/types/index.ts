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

// =============================================================================
// Approval Notification Types (S08)
// =============================================================================

/**
 * Approval notification type enum values.
 * Matches backend ApprovalNotificationType enum.
 */
export type ApprovalNotificationType =
  | 'InjectSubmitted'
  | 'BatchSubmitted'
  | 'InjectApproved'
  | 'InjectRejected'
  | 'InjectReverted'

/**
 * DTO representing an approval workflow notification.
 * Matches backend ApprovalNotificationDto.
 */
export interface ApprovalNotificationDto {
  /** Unique identifier */
  id: string
  /** User who receives the notification (Exercise Director ID) */
  userId: string
  /** Related exercise ID */
  exerciseId: string
  /** Exercise name for display */
  exerciseName: string
  /** Related inject ID (null for batch notifications) */
  injectId: string | null
  /** Inject number for display (e.g., "#5") */
  injectNumber: string | null
  /** Notification type */
  type: ApprovalNotificationType
  /** Short title */
  title: string
  /** Detailed message */
  message: string
  /** JSON metadata for batch notifications */
  metadata: string | null
  /** User who triggered the notification (e.g., Controller who submitted) */
  triggeredByUserId: string | null
  /** Display name of the user who triggered */
  triggeredByName: string | null
  /** Whether read by user */
  isRead: boolean
  /** When read */
  readAt: string | null
  /** When created */
  createdAt: string
}

/**
 * Batch notification metadata (parsed from JSON metadata field)
 */
export interface BatchNotificationMetadata {
  injectIds: string[]
  injectNumbers: number[]
}

/**
 * Parse batch notification metadata
 */
export const parseBatchMetadata = (
  metadata: string | null,
): BatchNotificationMetadata | null => {
  if (!metadata) return null
  try {
    return JSON.parse(metadata) as BatchNotificationMetadata
  } catch {
    return null
  }
}
