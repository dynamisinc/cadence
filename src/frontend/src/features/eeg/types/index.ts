/**
 * EEG (Exercise Evaluation Guide) Feature Types
 *
 * TypeScript types for capability targets, critical tasks, and EEG entries.
 * Matches backend DTOs in Cadence.Core.Features.Eeg.Models.DTOs
 */

// =============================================================================
// Capability Target Types
// =============================================================================

/**
 * Summary DTO for capability in capability target responses
 */
export interface CapabilitySummaryDto {
  id: string
  name: string
  category: string | null
}

/**
 * Full capability target DTO
 */
export interface CapabilityTargetDto {
  id: string
  exerciseId: string
  capabilityId: string
  capability: CapabilitySummaryDto
  targetDescription: string
  sortOrder: number
  criticalTaskCount: number
  createdAt: string
  updatedAt: string
}

/**
 * Request DTO for creating a capability target
 */
export interface CreateCapabilityTargetRequest {
  capabilityId: string
  targetDescription: string
  sortOrder?: number
}

/**
 * Request DTO for updating a capability target
 */
export interface UpdateCapabilityTargetRequest {
  targetDescription: string
  sortOrder?: number
}

/**
 * Response for capability target list
 */
export interface CapabilityTargetListResponse {
  items: CapabilityTargetDto[]
  totalCount: number
}

// =============================================================================
// Critical Task Types
// =============================================================================

/**
 * Full critical task DTO
 */
export interface CriticalTaskDto {
  id: string
  capabilityTargetId: string
  taskDescription: string
  standard: string | null
  sortOrder: number
  linkedInjectCount: number
  eegEntryCount: number
  createdAt: string
  updatedAt: string
}

/**
 * Request DTO for creating a critical task
 */
export interface CreateCriticalTaskRequest {
  taskDescription: string
  standard?: string | null
  sortOrder?: number
}

/**
 * Request DTO for updating a critical task
 */
export interface UpdateCriticalTaskRequest {
  taskDescription: string
  standard?: string | null
  sortOrder?: number
}

/**
 * Response for critical task list
 */
export interface CriticalTaskListResponse {
  items: CriticalTaskDto[]
  totalCount: number
}

/**
 * Request DTO for setting linked injects
 */
export interface SetLinkedInjectsRequest {
  injectIds: string[]
}

/**
 * Request DTO for setting linked critical tasks on an inject
 */
export interface SetLinkedCriticalTasksRequest {
  criticalTaskIds: string[]
}

// =============================================================================
// EEG Entry Types
// =============================================================================

/**
 * HSEEP P/S/M/U performance rating
 */
export enum PerformanceRating {
  Performed = 'Performed',
  SomeChallenges = 'SomeChallenges',
  MajorChallenges = 'MajorChallenges',
  UnableToPerform = 'UnableToPerform',
}

/**
 * Display strings for performance ratings
 */
export const PERFORMANCE_RATING_LABELS: Record<PerformanceRating, string> = {
  [PerformanceRating.Performed]: 'P - Performed without Challenges',
  [PerformanceRating.SomeChallenges]: 'S - Performed with Some Challenges',
  [PerformanceRating.MajorChallenges]: 'M - Performed with Major Challenges',
  [PerformanceRating.UnableToPerform]: 'U - Unable to be Performed',
}

/**
 * Short display strings for performance ratings
 */
export const PERFORMANCE_RATING_SHORT_LABELS: Record<PerformanceRating, string> = {
  [PerformanceRating.Performed]: 'P',
  [PerformanceRating.SomeChallenges]: 'S',
  [PerformanceRating.MajorChallenges]: 'M',
  [PerformanceRating.UnableToPerform]: 'U',
}

/**
 * Colors for performance ratings
 */
export const PERFORMANCE_RATING_COLORS: Record<PerformanceRating, string> = {
  [PerformanceRating.Performed]: '#4caf50', // Green
  [PerformanceRating.SomeChallenges]: '#ff9800', // Orange
  [PerformanceRating.MajorChallenges]: '#f44336', // Red
  [PerformanceRating.UnableToPerform]: '#9e9e9e', // Grey
}

/**
 * HSEEP detailed descriptions for ratings
 */
export const PERFORMANCE_RATING_DESCRIPTIONS: Record<PerformanceRating, string> = {
  [PerformanceRating.Performed]:
    'The targets and critical tasks associated with the capability were completed in a manner that achieved the objective(s) and did not negatively impact the performance of other activities.',
  [PerformanceRating.SomeChallenges]:
    'The targets and critical tasks were completed successfully, achieving the objective(s). However, opportunities to enhance effectiveness and/or efficiency were identified.',
  [PerformanceRating.MajorChallenges]:
    'The targets and critical tasks were completed in a manner that achieved the objective(s); however, the completion negatively impacted the performance of other activities or was not conducted in accordance with applicable plans.',
  [PerformanceRating.UnableToPerform]:
    'The targets and critical tasks were not performed in a manner that achieved the objective(s).',
}

/**
 * Summary DTO for critical task in EEG entry responses
 */
export interface CriticalTaskSummaryDto {
  id: string
  taskDescription: string
  capabilityTargetId: string
  capabilityTargetDescription: string
  capabilityName: string
}

/**
 * Summary DTO for inject in EEG entry responses
 */
export interface InjectSummaryDto {
  id: string
  injectNumber: number
  title: string
}

/**
 * Full EEG entry DTO
 */
export interface EegEntryDto {
  id: string
  criticalTaskId: string
  criticalTask: CriticalTaskSummaryDto
  observationText: string
  rating: PerformanceRating
  ratingDisplay: string
  observedAt: string
  recordedAt: string
  evaluatorId: string
  evaluatorName: string | null
  triggeringInjectId: string | null
  triggeringInject: InjectSummaryDto | null
  createdAt: string
  updatedAt: string
}

/**
 * Request DTO for creating an EEG entry
 */
export interface CreateEegEntryRequest {
  criticalTaskId: string
  observationText: string
  rating: PerformanceRating
  observedAt?: string
  triggeringInjectId?: string | null
}

/**
 * Request DTO for updating an EEG entry
 */
export interface UpdateEegEntryRequest {
  observationText: string
  rating: PerformanceRating
  observedAt?: string
  triggeringInjectId?: string | null
}

/**
 * Response for EEG entry list
 */
export interface EegEntryListResponse {
  items: EegEntryDto[]
  totalCount: number
}

// =============================================================================
// EEG Coverage Types
// =============================================================================

/**
 * Task rating summary for coverage display
 */
export interface TaskRatingDto {
  taskId: string
  taskDescription: string
  latestRating: PerformanceRating | null
}

/**
 * Capability target coverage for dashboard
 */
export interface CapabilityTargetCoverageDto {
  id: string
  targetDescription: string
  capabilityName: string
  totalTasks: number
  evaluatedTasks: number
  taskRatings: TaskRatingDto[]
}

/**
 * Unevaluated task for dashboard
 */
export interface UnevaluatedTaskDto {
  taskId: string
  taskDescription: string
  capabilityTargetId: string
  capabilityTargetDescription: string
}

/**
 * EEG coverage statistics
 */
export interface EegCoverageDto {
  totalTasks: number
  evaluatedTasks: number
  coveragePercentage: number
  ratingDistribution: Record<PerformanceRating, number>
  byCapabilityTarget: CapabilityTargetCoverageDto[]
  unevaluatedTasks: UnevaluatedTaskDto[]
}
