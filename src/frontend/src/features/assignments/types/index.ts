/**
 * Assignment Types
 *
 * Types for the My Assignments feature.
 */

/**
 * User's assignment to an exercise with role and status information.
 */
export interface AssignmentDto {
  /** Exercise unique identifier */
  exerciseId: string
  /** Exercise name */
  exerciseName: string
  /** User's HSEEP role in this exercise */
  role: string
  /** Current exercise status */
  exerciseStatus: string
  /** Exercise type (TTX, FE, FSE, etc.) */
  exerciseType: string
  /** Scheduled date for the exercise */
  scheduledDate: string
  /** Scheduled start time (if set) */
  startTime: string | null
  /** Current state of the exercise clock */
  clockState: string | null
  /** Total elapsed time in seconds */
  elapsedSeconds: number | null
  /** When the exercise was completed */
  completedAt: string | null
  /** When the user was assigned */
  assignedAt: string
  /** Total number of injects */
  totalInjects: number
  /** Number of fired injects */
  firedInjects: number
  /** Number of injects ready to fire */
  readyInjects: number
  /** Exercise location */
  location: string | null
  /** Exercise time zone */
  timeZoneId: string
  /** Name of the organization this exercise belongs to */
  organizationName: string
}

/**
 * Response containing assignments grouped by status.
 */
export interface MyAssignmentsResponse {
  /** Exercises currently in conduct */
  active: AssignmentDto[]
  /** Exercises scheduled for the future */
  upcoming: AssignmentDto[]
  /** Exercises that have finished */
  completed: AssignmentDto[]
}

/**
 * Assignment section type for grouping.
 */
export type AssignmentSectionType = 'active' | 'upcoming' | 'completed'

/**
 * Props for the AssignmentSection component.
 */
export interface AssignmentSectionProps {
  title: string
  type: AssignmentSectionType
  assignments: AssignmentDto[]
  isLoading?: boolean
  emptyMessage?: string
  showOrganization?: boolean
}
