/**
 * useStoryTime Hook
 *
 * Calculates and formats story time based on exercise timeline mode and elapsed time.
 * Story time represents the fictional timeline within the scenario narrative.
 */

import { useMemo } from 'react'
import { TimelineMode } from '../../../types'
import type { ExerciseDto } from '../../exercises/types'
import type { InjectDto } from '../../injects/types'
import { calculateStoryTime, formatStoryTime, parseInjectScenarioTime, type StoryTime } from '../utils/storyTime'

/**
 * Hook options
 */
export interface UseStoryTimeOptions {
  /** The exercise configuration */
  exercise: ExerciseDto
  /** Elapsed exercise time in milliseconds */
  elapsedTimeMs: number
  /** Current inject (for StoryOnly mode) */
  currentInject?: InjectDto | null
}

/**
 * Hook result
 */
export interface UseStoryTimeResult {
  /** Calculated story time object */
  storyTime: StoryTime | null
  /** Formatted story time string */
  formattedStoryTime: string
  /** Whether exercise is in StoryOnly mode */
  isStoryOnly: boolean
}

/**
 * Calculate current story time based on exercise timeline mode
 *
 * @param options - Hook options
 * @returns Story time state
 *
 * @example
 * const { formattedStoryTime, isStoryOnly } = useStoryTime({
 *   exercise,
 *   elapsedTimeMs,
 *   currentInject
 * })
 */
export const useStoryTime = ({
  exercise,
  elapsedTimeMs,
  currentInject,
}: UseStoryTimeOptions): UseStoryTimeResult => {
  const isStoryOnly = exercise.timelineMode === TimelineMode.StoryOnly

  const storyTime = useMemo(() => {
    if (isStoryOnly) {
      // In Story-only mode, derive from current inject
      if (currentInject?.scenarioDay != null && currentInject?.scenarioTime != null) {
        return parseInjectScenarioTime(currentInject.scenarioDay, currentInject.scenarioTime)
      }
      return null
    }

    // For RealTime and Compressed modes, calculate from elapsed time
    return calculateStoryTime(elapsedTimeMs, exercise.timelineMode, exercise.timeScale)
  }, [elapsedTimeMs, exercise.timelineMode, exercise.timeScale, isStoryOnly, currentInject])

  const formattedStoryTime = storyTime ? formatStoryTime(storyTime) : '—'

  return { storyTime, formattedStoryTime, isStoryOnly }
}
