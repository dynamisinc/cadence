/**
 * Date Utilities
 *
 * Handles conversion of UTC dates from the API to local time for display.
 * The backend stores all dates in UTC (DateTime with GETUTCDATE() default).
 */

import { format, formatDistanceToNow, parseISO } from 'date-fns'

/**
 * Parses a UTC date string from the API and returns a Date object.
 * Ensures the date is interpreted as UTC even if the 'Z' suffix is missing.
 */
export function parseUtcDate(dateString: string): Date {
  // If the string doesn't end with 'Z' or timezone offset, append 'Z' to indicate UTC
  if (
    !dateString.endsWith('Z') &&
    !dateString.match(/[+-]\d{2}:\d{2}$/) &&
    !dateString.match(/[+-]\d{4}$/)
  ) {
    return parseISO(dateString + 'Z')
  }
  return parseISO(dateString)
}

/**
 * Formats a UTC date string from the API to a localized date/time string.
 * Uses the browser's locale for formatting.
 *
 * @param dateString - UTC date string from the API
 * @param formatString - date-fns format string (default: "PPp" = "Apr 29, 2024, 12:00 PM")
 * @returns Formatted date string in local time
 *
 * @example
 * formatDateTime("2024-01-15T10:30:00") // "Jan 15, 2024, 5:30 AM" (in EST)
 */
export function formatDateTime(
  dateString: string,
  formatString: string = 'PPp',
): string {
  const date = parseUtcDate(dateString)
  return format(date, formatString)
}

/**
 * Formats a UTC date string as a short date (no time).
 *
 * @param dateString - UTC date string from the API
 * @returns Formatted date string in local time (e.g., "Jan 15, 2024")
 */
export function formatDate(dateString: string): string {
  const date = parseUtcDate(dateString)
  return format(date, 'PP')
}

/**
 * Formats a UTC date string as time only.
 *
 * @param dateString - UTC date string from the API
 * @returns Formatted time string in local time (e.g., "5:30 PM")
 */
export function formatTime(dateString: string): string {
  const date = parseUtcDate(dateString)
  return format(date, 'p')
}

/**
 * Formats a UTC date string as a relative time (e.g., "5 minutes ago").
 *
 * @param dateString - UTC date string from the API
 * @param addSuffix - Whether to add "ago" suffix (default: true)
 * @returns Relative time string
 *
 * @example
 * formatRelativeTime("2024-01-15T10:30:00") // "2 hours ago"
 */
export function formatRelativeTime(
  dateString: string,
  addSuffix: boolean = true,
): string {
  const date = parseUtcDate(dateString)
  return formatDistanceToNow(date, { addSuffix })
}

/**
 * Formats a date for display, showing relative time if recent,
 * otherwise showing the full date/time.
 *
 * @param dateString - UTC date string from the API
 * @param recentThresholdHours - Hours before switching to full date (default: 24)
 * @returns Either relative time or formatted date/time
 *
 * @example
 * formatSmartDateTime("2024-01-15T10:30:00") // "5 minutes ago" or "Jan 15, 2024, 5:30 AM"
 */
export function formatSmartDateTime(
  dateString: string,
  recentThresholdHours: number = 24,
): string {
  const date = parseUtcDate(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffHours = diffMs / (1000 * 60 * 60)

  if (diffHours < recentThresholdHours) {
    return formatRelativeTime(dateString)
  }

  return formatDateTime(dateString)
}
