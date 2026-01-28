/**
 * useFormattedTime Hook
 *
 * Hook that provides time formatting functions using user's preferences.
 *
 * @module features/settings
 */

import { useCallback } from 'react'
import { useUserPreferences } from '../contexts/UserPreferencesContext'
import {
  formatTime as formatTimeUtil,
  formatTimeWithSeconds,
  formatDateTime,
} from '../utils/timeFormat'

/**
 * Hook for formatting times according to user preferences
 *
 * @example
 * const { formatTime } = useFormattedTime()
 * return <span>{formatTime(inject.firedAt)}</span>
 */
export function useFormattedTime() {
  const { preferences } = useUserPreferences()
  const format = preferences?.timeFormat ?? 'TwentyFourHour'

  const formatTime = useCallback(
    (date: Date | string | number): string => {
      return formatTimeUtil(date, format)
    },
    [format],
  )

  const formatTimeSeconds = useCallback(
    (date: Date | string | number): string => {
      return formatTimeWithSeconds(date, format)
    },
    [format],
  )

  const formatDateAndTime = useCallback(
    (date: Date | string | number): string => {
      return formatDateTime(date, format)
    },
    [format],
  )

  return {
    formatTime,
    formatTimeSeconds,
    formatDateAndTime,
    timeFormat: format,
  }
}
