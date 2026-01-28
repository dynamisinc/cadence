/**
 * Metrics Feature Types
 *
 * TypeScript types for exercise metrics (S01, S02, S03, S04).
 * Matches backend DTOs in Cadence.Core.Features.Metrics.Models.DTOs
 */

import type { ExerciseClockState, InjectStatus } from '../../../types'

// =============================================================================
// S01: Exercise Progress Dashboard Types (Real-time conduct metrics)
// =============================================================================

/**
 * Real-time exercise progress data for conduct view.
 * Used by Controllers/Directors to monitor exercise status at a glance.
 */
export interface ExerciseProgressDto {
  /** Total number of injects in the active MSEL. */
  totalInjects: number
  /** Number of injects that have been fired. */
  firedCount: number
  /** Number of injects that have been skipped. */
  skippedCount: number
  /** Number of injects still pending (including Ready). */
  pendingCount: number
  /** Number of injects in Ready status (ready to fire). */
  readyCount: number
  /** Progress percentage: (fired + skipped) / total * 100. */
  progressPercentage: number
  /** Total number of observations recorded. */
  observationCount: number
  /** Current exercise clock elapsed time (ISO 8601 duration string). */
  elapsedTime: string
  /** Current clock status (Stopped, Running, Paused). */
  clockStatus: ExerciseClockState
  /** Current phase name (based on most recently fired inject or first pending). */
  currentPhaseName: string | null
  /** Next 3 upcoming injects (pending, ordered by sequence). */
  nextInjects: UpcomingInjectDto[]
  /** P/S/M/U rating counts for quick observation summary. */
  ratingCounts: RatingCountsDto
}

/**
 * Upcoming inject information for progress display.
 */
export interface UpcomingInjectDto {
  id: string
  injectNumber: number
  title: string
  scheduledTime: string // TimeOnly as HH:mm:ss
  deliveryTime: string | null // TimeSpan as HH:mm:ss or null
  phaseName: string | null
  status: InjectStatus
}

/**
 * Quick P/S/M/U counts for progress dashboard.
 */
export interface RatingCountsDto {
  performed: number
  satisfactory: number
  marginal: number
  unsatisfactory: number
  unrated: number
}

// =============================================================================
// S02: Exercise Inject Summary Types (AAR metrics)
// =============================================================================

/**
 * Comprehensive inject delivery statistics for after-action review.
 */
export interface InjectSummaryDto {
  /** Total inject count in the MSEL. */
  totalCount: number
  /** Number of injects that were fired. */
  firedCount: number
  /** Number of injects that were skipped. */
  skippedCount: number
  /** Number of injects not executed (still pending when exercise ended). */
  notExecutedCount: number
  /** Percentage of injects that were fired. */
  firedPercentage: number
  /** Percentage of injects that were skipped. */
  skippedPercentage: number
  /** Percentage of injects not executed. */
  notExecutedPercentage: number
  /** On-time rate: percentage of fired injects delivered within tolerance. */
  onTimeRate: number | null
  /** Number of injects delivered on time. */
  onTimeCount: number
  /** Average timing variance from scheduled time (ISO 8601 duration). */
  averageVariance: string | null
  /** Earliest variance (most early delivery). */
  earliestVariance: TimingVarianceDto | null
  /** Latest variance (most late delivery). */
  latestVariance: TimingVarianceDto | null
  /** Inject statistics broken down by phase. */
  byPhase: PhaseInjectSummaryDto[]
  /** Inject statistics broken down by controller who fired them. */
  byController: ControllerInjectSummaryDto[]
  /** List of skipped injects with reasons. */
  skippedInjects: SkippedInjectDto[]
}

/**
 * Timing variance information for a specific inject.
 */
export interface TimingVarianceDto {
  injectId: string
  injectNumber: number
  title: string
  variance: string // ISO 8601 duration
}

/**
 * Inject statistics for a specific phase.
 */
export interface PhaseInjectSummaryDto {
  phaseId: string | null
  phaseName: string
  sequence: number
  totalCount: number
  firedCount: number
  skippedCount: number
  notExecutedCount: number
  onTimeRate: number | null
}

/**
 * Inject statistics for a specific controller.
 */
export interface ControllerInjectSummaryDto {
  controllerId: string | null
  controllerName: string
  firedCount: number
  averageVariance: string | null // ISO 8601 duration
  onTimeRate: number | null
}

/**
 * Information about a skipped inject.
 */
export interface SkippedInjectDto {
  id: string
  injectNumber: number
  title: string
  phaseName: string | null
  skipReason: string | null
  skippedAt: string | null // ISO datetime
  skippedByName: string | null
}

// =============================================================================
// S03: Exercise Observation Summary Types (AAR metrics)
// =============================================================================

/**
 * Comprehensive observation statistics for after-action review.
 */
export interface ObservationSummaryDto {
  /** Total number of observations recorded. */
  totalCount: number
  /** P/S/M/U rating distribution. */
  ratingDistribution: RatingDistributionDto
  /** Coverage rate: percentage of objectives with at least one observation. */
  coverageRate: number | null
  /** Number of objectives that have at least one observation. */
  objectivesCovered: number
  /** Total number of objectives defined for the exercise. */
  totalObjectives: number
  /** List of objectives without any observations (gaps). */
  uncoveredObjectives: UncoveredObjectiveDto[]
  /** Observation statistics broken down by evaluator. */
  byEvaluator: EvaluatorSummaryDto[]
  /** Observation statistics broken down by phase. */
  byPhase: PhaseObservationSummaryDto[]
  /** Number of observations linked to an inject. */
  linkedToInjectCount: number
  /** Number of observations linked to an objective. */
  linkedToObjectiveCount: number
  /** Number of observations not linked to any inject or objective. */
  unlinkedCount: number
}

/**
 * P/S/M/U rating distribution with counts and percentages.
 */
export interface RatingDistributionDto {
  performedCount: number
  performedPercentage: number
  satisfactoryCount: number
  satisfactoryPercentage: number
  marginalCount: number
  marginalPercentage: number
  unsatisfactoryCount: number
  unsatisfactoryPercentage: number
  unratedCount: number
  unratedPercentage: number
  /** Average rating: P=1, S=2, M=3, U=4. Lower is better. */
  averageRating: number | null
}

/**
 * Objective without any observations (coverage gap).
 */
export interface UncoveredObjectiveDto {
  id: string
  objectiveNumber: string
  name: string
}

/**
 * Observation statistics for a specific evaluator.
 */
export interface EvaluatorSummaryDto {
  evaluatorId: string | null
  evaluatorName: string
  observationCount: number
  /** Average rating: P=1, S=2, M=3, U=4. */
  averageRating: number | null
  /** Rating distribution for this evaluator. */
  ratingCounts: RatingCountsDto
}

/**
 * Observation statistics for a specific phase.
 */
export interface PhaseObservationSummaryDto {
  phaseId: string | null
  phaseName: string
  sequence: number
  observationCount: number
  ratingCounts: RatingCountsDto
}

// =============================================================================
// S04: Exercise Timeline Summary Types (AAR metrics)
// =============================================================================

/**
 * Comprehensive timeline and duration analysis for after-action review.
 */
export interface TimelineSummaryDto {
  /** Planned exercise duration based on start/end times. */
  plannedDuration: string | null
  /** Actual exercise duration (total elapsed time on clock). */
  actualDuration: string
  /** Variance from planned duration (positive = ran longer than planned). */
  durationVariance: string | null
  /** When the exercise was first started (clock started). */
  startedAt: string | null
  /** When the exercise was ended (clock stopped or exercise completed). */
  endedAt: string | null
  /** Total wall-clock time from first start to final stop. */
  wallClockDuration: string | null
  /** Number of times the exercise was paused. */
  pauseCount: number
  /** Total time spent paused. */
  totalPauseTime: string
  /** Average pause duration. */
  averagePauseDuration: string | null
  /** Longest single pause duration. */
  longestPauseDuration: string | null
  /** Individual pause events with details. */
  pauseEvents: PauseEventDto[]
  /** Timeline of all clock events (start, pause, stop). */
  clockEvents: ClockEventDto[]
  /** Phase timing analysis. */
  phaseTimings: PhaseTimingDto[]
  /** Inject pacing metrics. */
  injectPacing: InjectPacingDto
}

/**
 * Details of a single pause event.
 */
export interface PauseEventDto {
  /** When the pause started. */
  pausedAt: string
  /** When the pause ended (clock resumed). Null if still paused. */
  resumedAt: string | null
  /** Duration of this pause. */
  duration: string
  /** Elapsed exercise time when pause started. */
  elapsedAtPause: string
  /** User who initiated the pause. */
  pausedByName: string | null
  /** User who resumed the clock. */
  resumedByName: string | null
  /** Reason for the pause (if provided). */
  notes: string | null
}

/**
 * A single clock event in the timeline.
 */
export interface ClockEventDto {
  /** Type of event (Started, Paused, Stopped). */
  eventType: string
  /** When the event occurred. */
  occurredAt: string
  /** Elapsed exercise time when event occurred. */
  elapsedTime: string
  /** User who triggered the event. */
  userName: string | null
  /** Notes associated with the event. */
  notes: string | null
}

/**
 * Timing analysis for a specific phase.
 */
export interface PhaseTimingDto {
  phaseId: string | null
  phaseName: string
  sequence: number
  /** When the first inject in this phase was fired. */
  startedAt: string | null
  /** When the last inject in this phase was fired. */
  endedAt: string | null
  /** Time spent in this phase (last - first inject fire time). */
  duration: string | null
  /** Number of injects fired in this phase. */
  injectsFired: number
  /** Elapsed exercise time at first inject fire. */
  elapsedAtStart: string | null
  /** Elapsed exercise time at last inject fire. */
  elapsedAtEnd: string | null
}

/**
 * Inject pacing analysis for the exercise.
 */
export interface InjectPacingDto {
  /** Total number of injects fired. */
  totalFired: number
  /** Average time between inject fires. */
  averageTimeBetweenInjects: string | null
  /** Shortest gap between consecutive inject fires. */
  shortestGap: string | null
  /** Longest gap between consecutive inject fires. */
  longestGap: string | null
  /** Average inject firing rate (injects per hour). */
  injectsPerHour: number | null
  /** Busiest period analysis. */
  busiestPeriod: BusiestPeriodDto | null
}

/**
 * Information about the busiest period of inject activity.
 */
export interface BusiestPeriodDto {
  /** Start of the busiest 15-minute window. */
  startedAt: string
  /** End of the busiest period. */
  endedAt: string
  /** Number of injects fired in this period. */
  injectCount: number
}

// =============================================================================
// S07: Controller Activity Metrics Types (AAR metrics)
// =============================================================================

/**
 * Comprehensive controller activity metrics for after-action review.
 */
export interface ControllerActivitySummaryDto {
  /** Total number of controllers who fired injects. */
  totalControllers: number
  /** Total injects fired across all controllers. */
  totalInjectsFired: number
  /** Total injects skipped across all controllers. */
  totalInjectsSkipped: number
  /** Overall on-time rate across all controllers. */
  overallOnTimeRate: number | null
  /** Detailed activity for each controller. */
  controllers: ControllerActivityDto[]
}

/**
 * Detailed activity metrics for a single controller.
 */
export interface ControllerActivityDto {
  /** Controller's user ID. */
  controllerId: string | null
  /** Controller's display name. */
  controllerName: string
  /** Number of injects this controller fired. */
  injectsFired: number
  /** Number of injects this controller skipped. */
  injectsSkipped: number
  /** Percentage of total fired injects handled by this controller. */
  workloadPercentage: number
  /** On-time rate for this controller's fired injects. */
  onTimeRate: number | null
  /** Number of on-time inject fires. */
  onTimeCount: number
  /** Average timing variance for this controller. */
  averageVariance: string | null
  /** Phases where this controller was active. */
  phaseActivity: ControllerPhaseActivityDto[]
  /** First inject fired timestamp. */
  firstFireAt: string | null
  /** Last inject fired timestamp. */
  lastFireAt: string | null
}

/**
 * Controller activity within a specific phase.
 */
export interface ControllerPhaseActivityDto {
  phaseId: string | null
  phaseName: string
  sequence: number
  injectsFired: number
  injectsSkipped: number
}

// =============================================================================
// S08: Evaluator Coverage Metrics Types (AAR metrics)
// =============================================================================

/**
 * Comprehensive evaluator coverage metrics for after-action review.
 */
export interface EvaluatorCoverageSummaryDto {
  /** Total number of evaluators who recorded observations. */
  totalEvaluators: number
  /** Total observations recorded. */
  totalObservations: number
  /** Number of objectives covered by at least one observation. */
  objectivesCovered: number
  /** Total number of objectives defined. */
  totalObjectives: number
  /** Overall objective coverage rate. */
  objectiveCoverageRate: number | null
  /** Number of capabilities covered by at least one observation. */
  capabilitiesCovered: number
  /** Total number of capabilities evaluated in this exercise. */
  totalCapabilities: number
  /** Evaluator consistency indicator: Low variance = High consistency. */
  consistency: EvaluatorConsistencyDto | null
  /** Detailed coverage for each evaluator. */
  evaluators: EvaluatorCoverageDto[]
  /** Coverage matrix: objectives × evaluators. */
  coverageMatrix: ObjectiveCoverageRowDto[]
  /** Objectives with no observations (gaps). */
  uncoveredObjectives: UncoveredObjectiveDto[]
  /** Objectives with low coverage (1-2 observations). */
  lowCoverageObjectives: LowCoverageObjectiveDto[]
}

/**
 * Detailed coverage metrics for a single evaluator.
 */
export interface EvaluatorCoverageDto {
  /** Evaluator's user ID. */
  evaluatorId: string | null
  /** Evaluator's display name. */
  evaluatorName: string
  /** Total observations recorded by this evaluator. */
  observationCount: number
  /** Number of distinct objectives this evaluator covered. */
  objectivesCovered: number
  /** Number of distinct capabilities this evaluator covered. */
  capabilitiesCovered: number
  /** Average rating given by this evaluator (P=1, S=2, M=3, U=4). */
  averageRating: number | null
  /** P/S/M/U rating distribution for this evaluator. */
  ratingCounts: RatingCountsDto
  /** Phases where this evaluator was active. */
  phaseActivity: EvaluatorPhaseActivityDto[]
  /** First observation timestamp. */
  firstObservationAt: string | null
  /** Last observation timestamp. */
  lastObservationAt: string | null
}

/**
 * Evaluator activity within a specific phase.
 */
export interface EvaluatorPhaseActivityDto {
  phaseId: string | null
  phaseName: string
  sequence: number
  observationCount: number
}

/**
 * Coverage matrix row: one objective's coverage by all evaluators.
 */
export interface ObjectiveCoverageRowDto {
  objectiveId: string
  objectiveNumber: string
  objectiveName: string
  /** Total observations for this objective. */
  totalObservations: number
  /** Observations per evaluator: EvaluatorId → count. */
  byEvaluator: Record<string, number>
  /** Coverage status: Good, Low, None. */
  coverageStatus: string
}

/**
 * Objective with low coverage (1-2 observations).
 */
export interface LowCoverageObjectiveDto {
  id: string
  objectiveNumber: string
  name: string
  observationCount: number
}

/**
 * Evaluator consistency metrics based on rating variance.
 * High consistency means evaluators rate similarly when observing the same activities.
 */
export interface EvaluatorConsistencyDto {
  /** Overall consistency level: High, Moderate, Low, or Insufficient Data. */
  level: string
  /** Overall average rating across all evaluators. */
  overallAverageRating: number
  /** Standard deviation of average ratings between evaluators. */
  ratingStandardDeviation: number
  /** Evaluators with notably harsh ratings (avg > overall + 0.5). */
  harshRaters: EvaluatorRatingBiasDto[]
  /** Evaluators with notably lenient ratings (avg < overall - 0.5). */
  lenientRaters: EvaluatorRatingBiasDto[]
  /** Description of consistency finding. */
  description: string
}

/**
 * Indicates an evaluator's rating bias compared to overall average.
 */
export interface EvaluatorRatingBiasDto {
  evaluatorName: string
  averageRating: number
  deviation: number
}

// =============================================================================
// S06: Core Capability Performance Types (AAR metrics)
// =============================================================================

/**
 * Comprehensive capability performance metrics for after-action review.
 */
export interface CapabilityPerformanceSummaryDto {
  /** Total capabilities with observations. */
  capabilitiesEvaluated: number
  /** Target capabilities for this exercise (if any). */
  targetCapabilitiesCount: number
  /** Target capabilities that were actually evaluated. */
  targetCapabilitiesEvaluated: number
  /** Target capability coverage rate (null if no targets). */
  targetCoverageRate: number | null
  /** Total observations with capability tags. */
  totalTaggedObservations: number
  /** Total observations (for comparison - shows tagging rate). */
  totalObservations: number
  /** Percentage of observations tagged with capabilities. */
  taggingRate: number
  /** Performance for each capability (sorted by rating, worst first). */
  capabilities: CapabilityPerformanceDto[]
  /** Target capabilities with no observations (gaps). */
  unevaluatedTargets: UnevaluatedCapabilityDto[]
  /** Performance grouped by mission area. */
  byMissionArea: MissionAreaSummaryDto[]
}

/**
 * Performance metrics for a single core capability.
 */
export interface CapabilityPerformanceDto {
  /** Capability ID. */
  capabilityId: string
  /** Capability name (e.g., "Mass Care Services"). */
  name: string
  /** FEMA mission area (Prevention, Protection, Mitigation, Response, Recovery). */
  missionArea: string
  /** Number of observations tagged with this capability. */
  observationCount: number
  /** Average rating (P=1, S=2, M=3, U=4). Lower is better. */
  averageRating: number | null
  /** Descriptive rating category based on average. */
  ratingCategory: string
  /** P/S/M/U rating distribution. */
  ratingCounts: RatingCountsDto
  /** Whether this is a target capability for this exercise. */
  isTargetCapability: boolean
  /** Performance classification: Good, Satisfactory, Needs Improvement, Critical. */
  performanceLevel: string
}

/**
 * Target capability that was not evaluated.
 */
export interface UnevaluatedCapabilityDto {
  id: string
  name: string
  missionArea: string
}

/**
 * Performance summary grouped by FEMA mission area.
 */
export interface MissionAreaSummaryDto {
  /** Mission area name. */
  missionArea: string
  /** Number of capabilities evaluated in this area. */
  capabilitiesEvaluated: number
  /** Total observations in this area. */
  observationCount: number
  /** Average rating across all capabilities in this area. */
  averageRating: number | null
  /** Rating distribution for this mission area. */
  ratingCounts: RatingCountsDto
}

// =============================================================================
// Utility Functions
// =============================================================================

/**
 * Parse a .NET TimeSpan string (e.g., "00:15:30" or "1.02:30:45") to milliseconds.
 */
export function parseTimeSpan(timeSpan: string | null | undefined): number {
  if (!timeSpan) return 0

  // Handle negative timespans
  const isNegative = timeSpan.startsWith('-')
  const cleanTimeSpan = isNegative ? timeSpan.substring(1) : timeSpan

  // Format: [d.]hh:mm:ss[.fffffff]
  const dayMatch = cleanTimeSpan.match(/^(\d+)\./)
  const days = dayMatch ? parseInt(dayMatch[1], 10) : 0

  const timeMatch = cleanTimeSpan.match(/(\d+):(\d+):(\d+)/)
  if (!timeMatch) return 0

  const hours = parseInt(timeMatch[1], 10)
  const minutes = parseInt(timeMatch[2], 10)
  const seconds = parseInt(timeMatch[3], 10)

  const ms = ((days * 24 + hours) * 60 + minutes) * 60 * 1000 + seconds * 1000
  return isNegative ? -ms : ms
}

/**
 * Format milliseconds to a human-readable duration string.
 */
export function formatDuration(ms: number): string {
  const isNegative = ms < 0
  const absMs = Math.abs(ms)

  const totalSeconds = Math.floor(absMs / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  const prefix = isNegative ? '-' : ''

  if (hours > 0) {
    return `${prefix}${hours}h ${minutes}m`
  }
  if (minutes > 0) {
    return `${prefix}${minutes}m ${seconds}s`
  }
  return `${prefix}${seconds}s`
}

/**
 * Format timing variance for display (e.g., "+3 min" or "-8 min").
 */
export function formatVariance(timeSpan: string | null | undefined): string {
  if (!timeSpan) return 'N/A'

  const ms = parseTimeSpan(timeSpan)
  const minutes = Math.round(ms / 60000)

  if (minutes === 0) return 'On time'
  if (minutes > 0) return `+${minutes} min`
  return `${minutes} min`
}
