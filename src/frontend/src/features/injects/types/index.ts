/**
 * Inject Feature Types
 *
 * TypeScript types for inject CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Injects.Models.DTOs
 */

import { InjectType, InjectStatus, DeliveryMethod, TriggerType } from '../../../types'

/**
 * Inject DTO - Response from API
 */
export interface InjectDto {
  id: string
  injectNumber: number
  title: string
  description: string
  scheduledTime: string // TimeOnly as HH:MM:SS
  deliveryTime: string | null // TimeSpan as HH:MM:SS
  scenarioDay: number | null
  scenarioTime: string | null // TimeOnly as HH:MM:SS
  target: string
  source: string | null
  // Legacy enum - kept for backward compatibility
  deliveryMethod: DeliveryMethod | null
  // New lookup-based delivery method
  deliveryMethodId: string | null
  deliveryMethodName: string | null
  deliveryMethodOther: string | null
  injectType: InjectType
  status: InjectStatus
  sequence: number
  parentInjectId: string | null
  triggerCondition: string | null
  expectedAction: string | null
  controllerNotes: string | null
  readyAt: string | null // DateTime as ISO string - when inject became Ready
  firedAt: string | null // DateTime as ISO string
  firedBy: string | null
  firedByName: string | null
  skippedAt: string | null // DateTime as ISO string
  skippedBy: string | null
  skippedByName: string | null
  skipReason: string | null
  mselId: string
  phaseId: string | null
  phaseName: string | null
  objectiveIds: string[]
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
  // New Phase G fields
  sourceReference: string | null
  priority: number | null
  triggerType: TriggerType
  responsibleController: string | null
  locationName: string | null
  locationType: string | null
  track: string | null
  // Approval workflow fields (S00-S09)
  submittedByUserId: string | null
  submittedByName: string | null
  submittedAt: string | null // DateTime as ISO string
  approvedByUserId: string | null
  approvedByName: string | null
  approvedAt: string | null // DateTime as ISO string
  approverNotes: string | null
  rejectedByUserId: string | null
  rejectedByName: string | null
  rejectedAt: string | null // DateTime as ISO string
  rejectionReason: string | null
  revertedByUserId: string | null
  revertedByName: string | null
  revertedAt: string | null // DateTime as ISO string
  revertReason: string | null
}

/**
 * Request body for creating a new inject
 */
export interface CreateInjectRequest {
  title: string
  description: string
  scheduledTime: string // HH:MM:SS format
  deliveryTime?: string | null // HH:MM:SS format
  scenarioDay?: number | null
  scenarioTime?: string | null // HH:MM:SS format
  target: string
  source?: string | null
  deliveryMethod?: DeliveryMethod | null
  deliveryMethodId?: string | null
  deliveryMethodOther?: string | null
  injectType?: InjectType
  expectedAction?: string | null
  controllerNotes?: string | null
  parentInjectId?: string | null
  triggerCondition?: string | null
  phaseId?: string | null
  objectiveIds?: string[] | null
  // Phase G fields
  sourceReference?: string | null
  priority?: number | null
  triggerType?: TriggerType
  responsibleController?: string | null
  locationName?: string | null
  locationType?: string | null
  track?: string | null
}

/**
 * Request body for updating an existing inject
 */
export interface UpdateInjectRequest {
  title: string
  description: string
  scheduledTime: string // HH:MM:SS format
  deliveryTime?: string | null // HH:MM:SS format
  scenarioDay?: number | null
  scenarioTime?: string | null // HH:MM:SS format
  target: string
  source?: string | null
  deliveryMethod?: DeliveryMethod | null
  deliveryMethodId?: string | null
  deliveryMethodOther?: string | null
  injectType?: InjectType
  expectedAction?: string | null
  controllerNotes?: string | null
  parentInjectId?: string | null
  triggerCondition?: string | null
  phaseId?: string | null
  objectiveIds?: string[] | null
  // Phase G fields
  sourceReference?: string | null
  priority?: number | null
  triggerType?: TriggerType
  responsibleController?: string | null
  locationName?: string | null
  locationType?: string | null
  track?: string | null
}

/**
 * Request body for firing an inject
 */
export interface FireInjectRequest {
  notes?: string | null
}

/**
 * Request body for skipping an inject
 */
export interface SkipInjectRequest {
  reason: string
}

/**
 * Form values for creating/editing an inject
 */
export interface InjectFormValues {
  title: string
  description: string
  scheduledTime: string // HH:MM format for time input
  deliveryTime: string // HH:MM:SS format for time input
  scenarioDay: string // String for form input, parsed to number
  scenarioTime: string // HH:MM format for time input
  target: string
  source: string
  // Legacy enum field (deprecated)
  deliveryMethod: DeliveryMethod | ''
  // New lookup-based delivery method
  deliveryMethodId: string
  deliveryMethodOther: string
  injectType: InjectType
  expectedAction: string
  controllerNotes: string
  triggerCondition: string
  phaseId: string
  objectiveIds: string[]
  // Phase G fields
  sourceReference: string
  priority: string // String for form input, parsed to number
  triggerType: TriggerType
  responsibleController: string
  locationName: string
  locationType: string
  track: string
}

/**
 * Inject field limits for validation
 */
export const INJECT_FIELD_LIMITS = {
  title: { min: 3, max: 200 },
  description: { max: 4000 },
  target: { max: 200 },
  source: { max: 200 },
  expectedAction: { max: 2000 },
  controllerNotes: { max: 2000 },
  triggerCondition: { max: 500 },
  skipReason: { max: 500 },
  scenarioDay: { min: 1, max: 99 },
  // Phase G fields
  sourceReference: { max: 50 },
  priority: { min: 1, max: 5 },
  responsibleController: { max: 200 },
  locationName: { max: 200 },
  locationType: { max: 100 },
  track: { max: 100 },
  deliveryMethodOther: { max: 100 },
}

/**
 * Inject grouped by phase for MSEL view
 */
export interface PhaseGroup {
  phaseId: string | null
  phaseName: string | null
  sequence: number
  injects: InjectDto[]
}

/**
 * Helper to format scenario time display
 * @param day Scenario day number
 * @param time Scenario time as HH:MM:SS
 * @returns Formatted string like "D1 08:00" or null if no scenario time
 */
export const formatScenarioTime = (
  day: number | null,
  time: string | null,
): string | null => {
  if (day === null) return null
  const timeStr = time ? time.substring(0, 5) : ''
  return timeStr ? `D${day} ${timeStr}` : `Day ${day}`
}

/**
 * Helper to format scheduled time display
 * @param time TimeOnly as HH:MM:SS
 * @returns Formatted string like "09:30 AM"
 */
export const formatScheduledTime = (time: string): string => {
  const [hours, minutes] = time.split(':').map(Number)
  const period = hours >= 12 ? 'PM' : 'AM'
  const hour12 = hours % 12 || 12
  return `${hour12}:${minutes.toString().padStart(2, '0')} ${period}`
}

/**
 * Helper to calculate variance between scheduled and fired time
 * @param scheduledTime Scheduled time as HH:MM:SS
 * @param firedAt Fired timestamp as ISO string
 * @returns Variance string like "+5 min" or "-2 min"
 */
export const calculateVariance = (
  scheduledTime: string,
  firedAt: string,
): string => {
  const [schedHours, schedMins] = scheduledTime.split(':').map(Number)
  const firedDate = new Date(firedAt)
  const firedHours = firedDate.getHours()
  const firedMins = firedDate.getMinutes()

  const schedTotalMins = schedHours * 60 + schedMins
  const firedTotalMins = firedHours * 60 + firedMins
  const variance = firedTotalMins - schedTotalMins

  if (variance === 0) return 'On time'
  const sign = variance > 0 ? '+' : ''
  return `${sign}${variance} min`
}

/**
 * Parse a time string (HH:MM:SS or HH:MM) to milliseconds from midnight
 * @param time Time string in HH:MM:SS or HH:MM format
 * @returns Milliseconds from midnight
 */
export const parseTimeToMs = (time: string): number => {
  const parts = time.split(':').map(Number)
  const hours = parts[0] || 0
  const minutes = parts[1] || 0
  const seconds = parts[2] || 0
  return (hours * 3600 + minutes * 60 + seconds) * 1000
}

/**
 * Calculate the scheduled offset for an inject relative to exercise start time
 * @param injectScheduledTime Inject's scheduled time as HH:MM:SS
 * @param exerciseStartTime Exercise's planned start time as HH:MM:SS (null defaults to 00:00:00)
 * @returns Offset in milliseconds from exercise start
 */
export const calculateScheduledOffset = (
  injectScheduledTime: string,
  exerciseStartTime: string | null,
): number => {
  const injectMs = parseTimeToMs(injectScheduledTime)
  const startMs = exerciseStartTime ? parseTimeToMs(exerciseStartTime) : 0
  return injectMs - startMs
}

/**
 * Format an offset in milliseconds to a display string
 * @param ms Offset in milliseconds
 * @returns Formatted string like "+00:45" or "+01:30"
 */
export const formatOffset = (ms: number): string => {
  const totalSeconds = Math.floor(Math.abs(ms) / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const sign = ms < 0 ? '-' : '+'
  return `${sign}${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
}

/**
 * Upcoming window in milliseconds (30 minutes)
 */
export const UPCOMING_WINDOW_MS = 30 * 60 * 1000

/**
 * Due soon threshold in milliseconds (5 minutes)
 * Injects within this window get special highlighting
 */
export const DUE_SOON_THRESHOLD_MS = 5 * 60 * 1000

/**
 * Format time remaining until an inject is due
 * @param timeRemainingMs Time remaining in milliseconds (positive = time until due)
 * @returns Formatted string like "04:23" (countdown)
 */
export const formatTimeRemaining = (timeRemainingMs: number): string => {
  const absMs = Math.abs(timeRemainingMs)
  const totalSeconds = Math.floor(absMs / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
}

/**
 * Parse a DeliveryTime string (TimeSpan) to milliseconds
 * Handles both "HH:MM:SS" and "d.HH:MM:SS" formats
 * @param deliveryTime TimeSpan string from API
 * @returns Milliseconds from exercise start, or null if input is null
 */
export const parseDeliveryTime = (deliveryTime: string | null): number | null => {
  if (!deliveryTime) return null

  // Handle day component if present (format: "d.HH:MM:SS")
  const dayMatch = deliveryTime.match(/^(\d+)\.(.+)$/)
  let days = 0
  let timeStr = deliveryTime

  if (dayMatch) {
    days = parseInt(dayMatch[1], 10)
    timeStr = dayMatch[2]
  }

  const parts = timeStr.split(':').map(Number)
  const hours = parts[0] || 0
  const minutes = parts[1] || 0
  const seconds = parts[2] || 0

  return ((days * 24 + hours) * 60 + minutes) * 60 * 1000 + seconds * 1000
}

/**
 * Format milliseconds as DeliveryTime string (TimeSpan)
 * @param ms Milliseconds from exercise start
 * @returns Formatted string like "00:30:00" or "1.02:15:30" for longer durations
 */
export const formatDeliveryTime = (ms: number): string => {
  const totalSeconds = Math.floor(ms / 1000)
  const days = Math.floor(totalSeconds / (24 * 3600))
  const hours = Math.floor((totalSeconds % (24 * 3600)) / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  const timeStr = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`

  if (days > 0) {
    return `${days}.${timeStr}`
  }

  return timeStr
}

// =============================================================================
// Approval Workflow Types (S00-S09)
// =============================================================================

/**
 * Request to approve an inject
 */
export interface ApproveInjectRequest {
  notes?: string | null
  /** Set to true to confirm self-approval when policy requires it (S11). */
  confirmSelfApproval?: boolean
}

/**
 * Request to reject an inject
 */
export interface RejectInjectRequest {
  reason: string
}

/**
 * Request to revert an approved inject back to submitted
 */
export interface RevertApprovalRequest {
  reason: string
}

/**
 * Request to batch approve multiple injects
 */
export interface BatchApproveRequest {
  injectIds: string[]
  notes?: string | null
}

/**
 * Request to batch reject multiple injects
 */
export interface BatchRejectRequest {
  injectIds: string[]
  reason: string
}

/**
 * Result of a batch approval/rejection operation
 */
export interface BatchApprovalResult {
  approvedCount: number
  rejectedCount: number
  skippedCount: number
  skippedReasons: string[]
  processedInjects: InjectDto[]
}

/**
 * Approval field limits for validation
 */
export const APPROVAL_FIELD_LIMITS = {
  approverNotes: { max: 1000 },
  rejectionReason: { min: 10, max: 1000 },
  revertReason: { min: 10, max: 1000 },
}
