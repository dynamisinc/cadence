/**
 * Time Format Utilities
 *
 * Functions for formatting times according to user preferences.
 * Default is 24-hour (military) time per EM standard.
 *
 * @module features/settings
 */

import type { TimeFormat } from '../types'

/**
 * Format a date/time according to user's time format preference
 *
 * @param date - Date object, ISO string, or timestamp to format
 * @param format - Time format preference (TwentyFourHour or TwelveHour)
 * @returns Formatted time string (e.g., "14:30" or "2:30 PM")
 */
export function formatTime(
  date: Date | string | number,
  format: TimeFormat = 'TwentyFourHour',
): string {
  const d = new Date(date)

  if (isNaN(d.getTime())) {
    return '--:--'
  }

  if (format === 'TwentyFourHour') {
    return d.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })
  }

  return d.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
}

/**
 * Format a date/time with seconds
 *
 * @param date - Date object, ISO string, or timestamp to format
 * @param format - Time format preference
 * @returns Formatted time string with seconds (e.g., "14:30:45" or "2:30:45 PM")
 */
export function formatTimeWithSeconds(
  date: Date | string | number,
  format: TimeFormat = 'TwentyFourHour',
): string {
  const d = new Date(date)

  if (isNaN(d.getTime())) {
    return '--:--:--'
  }

  if (format === 'TwentyFourHour') {
    return d.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    })
  }

  return d.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    second: '2-digit',
    hour12: true,
  })
}

/**
 * Format a date and time together
 *
 * @param date - Date object, ISO string, or timestamp to format
 * @param format - Time format preference
 * @returns Formatted date and time string (e.g., "Jan 15, 2025 14:30")
 */
export function formatDateTime(
  date: Date | string | number,
  format: TimeFormat = 'TwentyFourHour',
): string {
  const d = new Date(date)

  if (isNaN(d.getTime())) {
    return '-- --, ---- --:--'
  }

  const dateStr = d.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })

  const timeStr = formatTime(d, format)

  return `${dateStr} ${timeStr}`
}

/**
 * Get the current time formatted according to preference
 * Useful for preview in settings panel
 *
 * @param format - Time format preference
 * @returns Current time formatted
 */
export function getCurrentTimeFormatted(format: TimeFormat = 'TwentyFourHour'): string {
  return formatTime(new Date(), format)
}

/**
 * Get example time strings for both formats
 * Useful for showing preview in settings
 */
export function getTimeFormatExamples(): { format: TimeFormat; example: string }[] {
  const sampleDate = new Date()
  sampleDate.setHours(14, 30, 0, 0) // 2:30 PM

  return [
    { format: 'TwentyFourHour', example: formatTime(sampleDate, 'TwentyFourHour') },
    { format: 'TwelveHour', example: formatTime(sampleDate, 'TwelveHour') },
  ]
}
