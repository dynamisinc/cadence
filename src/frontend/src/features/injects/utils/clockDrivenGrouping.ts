/**
 * Clock-Driven Inject Grouping Utilities
 *
 * Utilities for grouping and formatting injects in clock-driven delivery mode.
 * Used by ClockDrivenConductView (CLK-06).
 */

import { InjectStatus } from '../../../types'
import type { InjectDto } from '../types'
import { parseDeliveryTime } from '../types'

/**
 * Upcoming window in milliseconds (30 minutes)
 * Injects within this window from current elapsed time are shown in Upcoming section
 */
export const UPCOMING_WINDOW_MS = 30 * 60 * 1000

/**
 * Grouped injects for clock-driven conduct view
 */
export interface GroupedInjects {
  /** Injects with status = Synchronized */
  ready: InjectDto[]
  /** Draft injects with DeliveryTime within next 30 minutes */
  upcoming: InjectDto[]
  /** Draft injects outside the 30-minute window or without DeliveryTime */
  later: InjectDto[]
  /** Released and Deferred injects */
  completed: InjectDto[]
}

/**
 * Group injects for clock-driven delivery mode
 *
 * Groups injects into four sections based on their status and delivery time:
 * - Ready: status === Synchronized (delivery time reached, awaiting fire)
 * - Upcoming: status === Draft AND deliveryTime within next 30 minutes
 * - Later: status === Draft AND (deliveryTime > 30 min away OR no deliveryTime)
 * - Completed: status === Released OR status === Deferred
 *
 * @param injects Array of injects to group
 * @param elapsedTimeMs Current elapsed time in milliseconds
 * @returns Grouped injects
 */
export const groupInjectsForClockDriven = (
  injects: InjectDto[],
  elapsedTimeMs: number,
): GroupedInjects => {
  const ready: InjectDto[] = []
  const upcoming: InjectDto[] = []
  const later: InjectDto[] = []
  const completed: InjectDto[] = []

  const upcomingWindowEndMs = elapsedTimeMs + UPCOMING_WINDOW_MS

  for (const inject of injects) {
    // Completed section - Released or Deferred
    if (inject.status === InjectStatus.Released || inject.status === InjectStatus.Deferred) {
      completed.push(inject)
      continue
    }

    // Ready section - Synchronized status
    if (inject.status === InjectStatus.Synchronized) {
      ready.push(inject)
      continue
    }

    // Draft injects - sort into Upcoming or Later
    if (inject.status === InjectStatus.Draft) {
      const deliveryTimeMs = parseDeliveryTime(inject.deliveryTime)

      if (deliveryTimeMs !== null) {
        const timeUntilDelivery = deliveryTimeMs - elapsedTimeMs

        // Upcoming: delivery time is in the future and within the 30-min window
        if (timeUntilDelivery > 0 && deliveryTimeMs <= upcomingWindowEndMs) {
          upcoming.push(inject)
        } else {
          // Later: delivery time is past (should transition to Synchronized) or > 30 min away
          later.push(inject)
        }
      } else {
        // No deliveryTime set - put in Later section
        later.push(inject)
      }
    }
  }

  // Sort upcoming by DeliveryTime ascending (soonest first)
  upcoming.sort((a, b) => {
    const aTime = parseDeliveryTime(a.deliveryTime) ?? 0
    const bTime = parseDeliveryTime(b.deliveryTime) ?? 0
    return aTime - bTime
  })

  // Sort later by sequence (MSEL order)
  later.sort((a, b) => a.sequence - b.sequence)

  return { ready, upcoming, later, completed }
}

/**
 * Format countdown to a target time
 *
 * @param targetMs Target time in milliseconds from exercise start
 * @param currentMs Current elapsed time in milliseconds
 * @returns Formatted countdown string like "in 12:45" or "in 1h 30m"
 */
export const formatCountdown = (targetMs: number, currentMs: number): string => {
  const diff = targetMs - currentMs

  if (diff <= 0) {
    return 'now'
  }

  const totalSeconds = Math.floor(diff / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60

  // For times over 60 minutes, show hours
  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60)
    const mins = minutes % 60
    return `in ${hours}h ${mins}m`
  }

  // For times under 60 minutes, show MM:SS
  return `in ${minutes}:${String(seconds).padStart(2, '0')}`
}
