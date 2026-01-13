/**
 * Inject Feature Types
 *
 * TypeScript types for inject CRUD operations.
 * Matches backend DTOs in Cadence.Core.Features.Injects.Models.DTOs
 */

import { InjectType, InjectStatus, DeliveryMethod } from '../../../types'

/**
 * Inject DTO - Response from API
 */
export interface InjectDto {
  id: string
  injectNumber: number
  title: string
  description: string
  scheduledTime: string // TimeOnly as HH:MM:SS
  scenarioDay: number | null
  scenarioTime: string | null // TimeOnly as HH:MM:SS
  target: string
  source: string | null
  deliveryMethod: DeliveryMethod | null
  injectType: InjectType
  status: InjectStatus
  sequence: number
  parentInjectId: string | null
  triggerCondition: string | null
  expectedAction: string | null
  controllerNotes: string | null
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
  createdAt: string // DateTime as ISO string
  updatedAt: string // DateTime as ISO string
}

/**
 * Request body for creating a new inject
 */
export interface CreateInjectRequest {
  title: string
  description: string
  scheduledTime: string // HH:MM:SS format
  scenarioDay?: number | null
  scenarioTime?: string | null // HH:MM:SS format
  target: string
  source?: string | null
  deliveryMethod?: DeliveryMethod | null
  injectType?: InjectType
  expectedAction?: string | null
  controllerNotes?: string | null
  parentInjectId?: string | null
  triggerCondition?: string | null
  phaseId?: string | null
}

/**
 * Request body for updating an existing inject
 */
export interface UpdateInjectRequest {
  title: string
  description: string
  scheduledTime: string // HH:MM:SS format
  scenarioDay?: number | null
  scenarioTime?: string | null // HH:MM:SS format
  target: string
  source?: string | null
  deliveryMethod?: DeliveryMethod | null
  injectType?: InjectType
  expectedAction?: string | null
  controllerNotes?: string | null
  parentInjectId?: string | null
  triggerCondition?: string | null
  phaseId?: string | null
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
  scenarioDay: string // String for form input, parsed to number
  scenarioTime: string // HH:MM format for time input
  target: string
  source: string
  deliveryMethod: DeliveryMethod | ''
  injectType: InjectType
  expectedAction: string
  controllerNotes: string
  triggerCondition: string
  phaseId: string
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
