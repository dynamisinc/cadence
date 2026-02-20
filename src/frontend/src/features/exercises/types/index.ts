/**
 * Exercise Feature Types
 *
 * TypeScript types for exercise CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Exercises.Models.DTOs
 */

import { ExerciseType, ExerciseStatus, DeliveryMode, TimelineMode } from '../../../types'

/**
 * Exercise DTO - Response from API
 */
export interface ExerciseDto {
  id: string
  name: string
  description: string | null
  exerciseType: ExerciseType
  status: ExerciseStatus
  isPracticeMode: boolean
  scheduledDate: string // DateOnly as ISO string (YYYY-MM-DD)
  startTime: string | null // TimeOnly as HH:MM:SS
  endTime: string | null // TimeOnly as HH:MM:SS
  timeZoneId: string
  location: string | null
  organizationId: string
  activeMselId: string | null
  // Timing configuration (CLK-01)
  deliveryMode: DeliveryMode
  timelineMode: TimelineMode
  timeScale: number | null
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
  createdBy: string // Guid as string - User who created the exercise
  // Status transition audit fields
  activatedAt: string | null // DateTime as ISO string
  activatedBy: string | null // Guid as string
  completedAt: string | null // DateTime as ISO string
  completedBy: string | null // Guid as string
  archivedAt: string | null // DateTime as ISO string
  archivedBy: string | null // Guid as string
  // Archive/delete tracking fields
  hasBeenPublished: boolean // True if exercise ever left Draft status
  previousStatus: ExerciseStatus | null // Status before archiving, used for restore
  // Exercise settings (S03-S05)
  clockMultiplier: number
  autoFireEnabled: boolean
  confirmFireInject: boolean
  confirmSkipInject: boolean
  confirmClockControl: boolean
  maxDuration: string | null // TimeSpan as HH:MM:SS or d.HH:MM:SS
  // Summary counts (for list views)
  injectCount: number
  firedInjectCount: number
}

/**
 * Request body for creating a new exercise
 */
export interface CreateExerciseRequest {
  name: string
  exerciseType: ExerciseType
  scheduledDate: string // YYYY-MM-DD format
  description?: string | null
  location?: string | null
  timeZoneId?: string
  isPracticeMode?: boolean
  deliveryMode: DeliveryMode
  timelineMode: TimelineMode
  clockMultiplier: number // Clock speed (1, 2, 5, 10, or 20)
  directorId?: string // Optional - user with Admin or Manager role to assign as Exercise Director
}

/**
 * Request body for updating an exercise
 */
export interface UpdateExerciseRequest {
  name: string
  exerciseType: ExerciseType
  scheduledDate: string // YYYY-MM-DD format
  description?: string | null
  location?: string | null
  timeZoneId?: string
  startTime?: string | null
  endTime?: string | null
  isPracticeMode?: boolean
  deliveryMode: DeliveryMode
  timelineMode: TimelineMode
  clockMultiplier: number // Clock speed (1, 2, 5, 10, or 20)
  directorId?: string // Optional - user with Admin or Manager role to assign as Exercise Director
}

/**
 * Request body for duplicating an exercise
 */
export interface DuplicateExerciseRequest {
  /** Name for the new exercise. Defaults to "Copy of {original name}". */
  name?: string
  /** Scheduled date for the new exercise. Defaults to the original date. */
  scheduledDate?: string // YYYY-MM-DD format
}

// =========================================================================
// MSEL Types
// =========================================================================

/**
 * MSEL list item DTO
 */
export interface MselDto {
  id: string
  name: string
  description: string | null
  version: number
  isActive: boolean
  exerciseId: string
  injectCount: number
  createdAt: string
  updatedAt: string
}

/**
 * MSEL Summary DTO - Detailed progress and metadata
 */
export interface MselSummaryDto {
  id: string
  name: string
  description: string | null
  version: number
  isActive: boolean
  exerciseId: string
  // Inject counts
  totalInjects: number
  draftCount: number
  releasedCount: number
  deferredCount: number
  completionPercentage: number
  // Related counts
  phaseCount: number
  objectiveCount: number
  // Last modification
  lastModifiedAt: string | null
  lastModifiedByName: string | null
  createdAt: string
  updatedAt: string
}

// =========================================================================
// Setup Progress Types
// =========================================================================

/**
 * Setup area DTO
 */
export interface SetupAreaDto {
  id: string
  name: string
  description: string
  isComplete: boolean
  weight: number
  currentCount: number
  requiredCount: number
  statusMessage: string
}

/**
 * Setup progress DTO
 */
export interface SetupProgressDto {
  overallPercentage: number
  isReadyToActivate: boolean
  areas: SetupAreaDto[]
}

// =========================================================================
// Delete Types
// =========================================================================

/**
 * Reasons why an exercise can be deleted.
 */
export type DeleteEligibilityReason = 'NeverPublished' | 'Archived'

/**
 * Reasons why an exercise cannot be deleted.
 */
export type CannotDeleteReason = 'MustArchiveFirst' | 'NotAuthorized' | 'NotFound'

/**
 * Summary of data that would be deleted with an exercise.
 */
export interface DeleteDataSummary {
  injectCount: number
  phaseCount: number
  observationCount: number
  participantCount: number
  expectedOutcomeCount: number
  objectiveCount: number
  mselCount: number
}

/**
 * Response from the delete summary endpoint.
 */
export interface DeleteSummaryResponse {
  exerciseId: string
  exerciseName: string
  canDelete: boolean
  deleteReason: DeleteEligibilityReason | null
  cannotDeleteReason: CannotDeleteReason | null
  summary: DeleteDataSummary
}

// =========================================================================
// Exercise Participant Types (S14)
// =========================================================================

/**
 * Exercise participant DTO
 */
export interface ExerciseParticipantDto {
  participantId: string
  userId: string
  displayName: string
  email: string
  exerciseRole: string
  systemRole: string
  effectiveRole: string
  addedAt: string
  addedBy: string | null
}

/**
 * Request to add a participant to an exercise
 */
export interface AddParticipantRequest {
  userId: string
  role: string
}

/**
 * Request to update a participant's role
 */
export interface UpdateParticipantRoleRequest {
  role: string
}

/**
 * Response from GET /api/exercises/{exerciseId}/participants
 */
export interface ParticipantsListResponse {
  participants: ExerciseParticipantDto[]
}

// =========================================================================
// Exercise Settings Types (S03-S05)
// =========================================================================

/**
 * Exercise settings DTO - Settings-only view of an exercise
 */
export interface ExerciseSettingsDto {
  clockMultiplier: number
  autoFireEnabled: boolean
  confirmFireInject: boolean
  confirmSkipInject: boolean
  confirmClockControl: boolean
  maxDuration: string | null // TimeSpan as HH:MM:SS or d.HH:MM:SS
}

/**
 * Request to update exercise settings
 */
export interface UpdateExerciseSettingsRequest {
  clockMultiplier?: number
  autoFireEnabled?: boolean
  confirmFireInject?: boolean
  confirmSkipInject?: boolean
  confirmClockControl?: boolean
  maxDuration?: string // TimeSpan as HH:MM:SS format
}

/**
 * Clock multiplier preset options
 */
export const CLOCK_MULTIPLIER_PRESETS = [
  { value: 1, label: '1x (Real-time)' },
  { value: 2, label: '2x' },
  { value: 5, label: '5x' },
  { value: 10, label: '10x' },
  { value: 20, label: '20x (Max)' },
] as const

// =========================================================================
// Exercise Capabilities Types (S04)
// =========================================================================

/**
 * Request to set target capabilities for an exercise
 */
export interface SetExerciseCapabilitiesRequest {
  capabilityIds: string[]
}

/**
 * Exercise capability coverage summary
 */
export interface ExerciseCapabilitySummaryDto {
  targetCount: number
  evaluatedCount: number
  coveragePercentage: number | null
}

// =========================================================================
// Approval Settings Types (S00-S09)
// =========================================================================

import { ApprovalPolicy, SelfApprovalPolicy } from '../../../types'

/**
 * Approval settings DTO - Returns exercise approval configuration
 */
export interface ApprovalSettingsDto {
  requireInjectApproval: boolean
  approvalPolicyOverridden: boolean
  approvalOverrideReason: string | null
  approvalOverriddenById: string | null
  approvalOverriddenAt: string | null
  organizationPolicy: ApprovalPolicy
  selfApprovalPolicy: SelfApprovalPolicy
}

/**
 * Request to update exercise approval settings
 */
export interface UpdateApprovalSettingsRequest {
  requireInjectApproval: boolean
  isOverride?: boolean
  overrideReason?: string | null
}

/**
 * Approval queue status summary (S06: Approval Queue View)
 */
export interface ApprovalStatusDto {
  totalInjects: number
  approvedCount: number
  pendingApprovalCount: number
  draftCount: number
  approvalPercentage: number
  allApproved: boolean
}

/**
 * Validation result for publishing an exercise (S07: Go-Live Gate)
 */
export interface PublishValidationResult {
  canPublish: boolean
  draftCount: number
  submittedCount: number
  totalUnapprovedCount: number
  warnings: string[]
  errors: string[]
}

// Re-export validation types
export type { CreateExerciseFormValues, UpdateExerciseFormValues } from './validation'
export { createExerciseSchema, updateExerciseSchema, EXERCISE_FIELD_LIMITS } from './validation'
