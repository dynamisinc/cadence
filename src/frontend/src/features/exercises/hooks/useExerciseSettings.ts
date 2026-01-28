/**
 * useExerciseSettings Hook
 *
 * React Query hook for fetching and caching exercise settings.
 * Used by conduct views to check confirmation dialog preferences.
 */

import { useQuery } from '@tanstack/react-query'
import { exerciseService } from '../services/exerciseService'
import type { ExerciseSettingsDto } from '../types'

/** Query key factory for exercise settings */
export const exerciseSettingsQueryKey = (exerciseId: string) =>
  ['exercise-settings', exerciseId] as const

/**
 * Hook for fetching exercise settings
 *
 * @param exerciseId - The exercise ID to fetch settings for
 * @returns Settings data and loading/error states
 */
export const useExerciseSettings = (exerciseId: string | undefined) => {
  const {
    data: settings,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: exerciseSettingsQueryKey(exerciseId ?? ''),
    queryFn: () => exerciseService.getSettings(exerciseId!),
    enabled: !!exerciseId,
    // Settings don't change often, cache for longer
    staleTime: 5 * 60 * 1000, // 5 minutes
  })

  // Default settings (all confirmations enabled)
  const defaultSettings: ExerciseSettingsDto = {
    clockMultiplier: 1,
    autoFireEnabled: false,
    confirmFireInject: true,
    confirmSkipInject: true,
    confirmClockControl: true,
  }

  return {
    settings: settings ?? defaultSettings,
    isLoading,
    error: error
      ? error instanceof Error
        ? error.message
        : 'Failed to load settings'
      : null,
    refetch,
    // Convenience accessors with defaults
    confirmFireInject: settings?.confirmFireInject ?? true,
    confirmSkipInject: settings?.confirmSkipInject ?? true,
    confirmClockControl: settings?.confirmClockControl ?? true,
    autoFireEnabled: settings?.autoFireEnabled ?? false,
  }
}

export default useExerciseSettings
