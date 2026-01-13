/**
 * Exercise Clock Feature Types
 *
 * TypeScript types for exercise clock operations.
 * Matches backend DTOs in Cadence.Core.Features.ExerciseClock.Models.DTOs
 */

import { ExerciseClockState } from '../../../types'

/**
 * Clock State DTO - Response from API
 */
export interface ClockStateDto {
  exerciseId: string
  state: ExerciseClockState
  startedAt: string | null // DateTime as ISO string
  elapsedTime: string // TimeSpan as HH:MM:SS or d.HH:MM:SS
  startedBy: string | null
  startedByName: string | null
  capturedAt: string // DateTime as ISO string
}

/**
 * Parse elapsed time string to total milliseconds
 */
export const parseElapsedTime = (elapsedTime: string): number => {
  // Format can be HH:MM:SS or d.HH:MM:SS
  const parts = elapsedTime.split(':')
  if (parts.length < 3) return 0

  let days = 0
  let hours = 0
  let minutes = 0
  let seconds = 0

  if (parts[0].includes('.')) {
    // Format: d.HH:MM:SS
    const dayHour = parts[0].split('.')
    days = parseInt(dayHour[0], 10)
    hours = parseInt(dayHour[1], 10)
  } else {
    hours = parseInt(parts[0], 10)
  }

  minutes = parseInt(parts[1], 10)
  // Seconds might have fractional part
  seconds = parseFloat(parts[2])

  return (
    days * 24 * 60 * 60 * 1000 +
    hours * 60 * 60 * 1000 +
    minutes * 60 * 1000 +
    seconds * 1000
  )
}

/**
 * Format milliseconds to display string (HH:MM:SS)
 */
export const formatElapsedTime = (ms: number): string => {
  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60

  return [
    hours.toString().padStart(2, '0'),
    minutes.toString().padStart(2, '0'),
    seconds.toString().padStart(2, '0'),
  ].join(':')
}
