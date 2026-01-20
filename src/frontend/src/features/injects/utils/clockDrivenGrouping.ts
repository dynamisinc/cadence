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
  /** Injects with status = Ready */
  ready: InjectDto[]
  /** Pending injects with DeliveryTime within next 30 minutes */
  upcoming: InjectDto[]
  /** Fired and Skipped injects */
  completed: InjectDto[]
}

/**
 * Group injects for clock-driven delivery mode
 *
 * Groups injects into three sections based on their status and delivery time:
 * - Ready: status === Ready
 * - Upcoming: status === Pending AND deliveryTime within next 30 minutes
 * - Completed: status === Fired OR status === Skipped
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
  const completed: InjectDto[] = []

  const upcomingWindowEndMs = elapsedTimeMs + UPCOMING_WINDOW_MS

  for (const inject of injects) {
    // Completed section - Fired or Skipped
    if (inject.status === InjectStatus.Fired || inject.status === InjectStatus.Skipped) {
      completed.push(inject)
      continue
    }

    // Ready section - Ready status
    if (inject.status === InjectStatus.Ready) {
      ready.push(inject)
      continue
    }

    // Upcoming section - Pending with DeliveryTime in the next 30 minutes
    if (inject.status === InjectStatus.Pending) {
      const deliveryTimeMs = parseDeliveryTime(inject.deliveryTime)

      if (deliveryTimeMs !== null) {
        const timeUntilDelivery = deliveryTimeMs - elapsedTimeMs

        // Only include if delivery time is in the future and within the window
        if (timeUntilDelivery > 0 && deliveryTimeMs <= upcomingWindowEndMs) {
          upcoming.push(inject)
        }
      }
      // Injects without deliveryTime or outside the window are not shown
    }
  }

  // Sort upcoming by DeliveryTime ascending (soonest first)
  upcoming.sort((a, b) => {
    const aTime = parseDeliveryTime(a.deliveryTime) ?? 0
    const bTime = parseDeliveryTime(b.deliveryTime) ?? 0
    return aTime - bTime
  })

  return { ready, upcoming, completed }
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
