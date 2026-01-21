/**
 * Story Time Utilities
 *
 * Functions for calculating and formatting story time in exercises.
 * Story time represents the fictional timeline within the scenario narrative.
 */

import { TimelineMode } from '../../../types'

/**
 * Represents a point in story time
 */
export interface StoryTime {
  day: number
  hours: number
  minutes: number
}

/**
 * Configuration for exercise starting story time
 */
export interface StoryTimeConfig {
  startDay: number
  startHours: number
  startMinutes: number
}

/**
 * Calculate current Story Time based on elapsed time and timeline mode.
 *
 * @param elapsedMs - Elapsed exercise time in milliseconds
 * @param timelineMode - Timeline mode (RealTime, Compressed, StoryOnly)
 * @param timeScale - Compression factor for Compressed mode (e.g., 4 = 4x faster)
 * @param startConfig - Starting story time configuration
 * @returns Story time object with day, hours, and minutes
 *
 * @example
 * // Real-time mode: 2:30 elapsed = Day 1 02:30 story
 * calculateStoryTime(9000000, TimelineMode.RealTime, null)
 * // => { day: 1, hours: 2, minutes: 30 }
 *
 * @example
 * // Compressed 4x: 15 min elapsed = 1 hour story
 * calculateStoryTime(900000, TimelineMode.Compressed, 4)
 * // => { day: 1, hours: 1, minutes: 0 }
 */
export const calculateStoryTime = (
  elapsedMs: number,
  timelineMode: TimelineMode,
  timeScale: number | null,
  startConfig: StoryTimeConfig = { startDay: 1, startHours: 0, startMinutes: 0 },
): StoryTime => {
  // Convert elapsed to story minutes based on mode
  let storyMinutes: number

  switch (timelineMode) {
    case TimelineMode.RealTime:
      storyMinutes = Math.floor(elapsedMs / 60000)
      break

    case TimelineMode.Compressed:
      {
        const scale = timeScale ?? 1
        storyMinutes = Math.floor((elapsedMs / 60000) * scale)
      }
      break

    case TimelineMode.StoryOnly:
      // In Story-only mode, return placeholder - caller should use inject's Story Time
      return { day: 0, hours: 0, minutes: 0 }
  }

  // Calculate from starting point
  const startTotalMinutes =
    (startConfig.startDay - 1) * 24 * 60 + startConfig.startHours * 60 + startConfig.startMinutes

  const currentTotalMinutes = startTotalMinutes + storyMinutes

  const day = Math.floor(currentTotalMinutes / (24 * 60)) + 1
  const remainingMinutes = currentTotalMinutes % (24 * 60)
  const hours = Math.floor(remainingMinutes / 60)
  const minutes = remainingMinutes % 60

  return { day, hours, minutes }
}

/**
 * Format Story Time for display.
 *
 * @param storyTime - Story time object
 * @returns Formatted string like "Day 1 • 08:32"
 *
 * @example
 * formatStoryTime({ day: 1, hours: 8, minutes: 32 })
 * // => "Day 1 • 08:32"
 */
export const formatStoryTime = (storyTime: StoryTime): string => {
  const { day, hours, minutes } = storyTime
  const timeStr = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
  return `Day ${day} • ${timeStr}`
}

/**
 * Parse inject scenario time to StoryTime object
 *
 * @param scenarioDay - Scenario day number
 * @param scenarioTime - Scenario time as HH:MM or HH:MM:SS
 * @returns StoryTime object or null if invalid
 */
export const parseInjectScenarioTime = (
  scenarioDay: number | null,
  scenarioTime: string | null,
): StoryTime | null => {
  if (scenarioDay === null || scenarioTime === null) {
    return null
  }

  // Validate format: must have at least one colon (HH:MM or HH:MM:SS)
  if (!scenarioTime.includes(':')) {
    return null
  }

  const stringParts = scenarioTime.split(':')

  // Must have at least 2 non-empty parts (hours and minutes)
  if (stringParts.length < 2 || stringParts[0] === '' || stringParts[1] === '') {
    return null
  }

  const parts = stringParts.map(Number)
  const [hours, minutes] = parts

  // Validate that parsing succeeded and values are valid
  if (Number.isNaN(hours) || Number.isNaN(minutes)) {
    return null
  }

  // Validate ranges
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
    return null
  }

  return {
    day: scenarioDay,
    hours,
    minutes,
  }
}
