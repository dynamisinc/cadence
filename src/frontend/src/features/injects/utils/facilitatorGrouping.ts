/**
 * Facilitator Grouping Utilities
 *
 * Utility functions for grouping injects in facilitator-paced conduct view.
 * Facilitator-paced mode uses sequence order instead of time-based progression.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { InjectStatus } from '../../../types'
import type { InjectDto } from '../types'

/**
 * Determines the current inject in facilitator-paced mode.
 * Current = first Draft inject in sequence order.
 *
 * @param injects All injects in the exercise
 * @returns The current inject, or null if no draft injects
 */
export const getCurrentInject = (injects: InjectDto[]): InjectDto | null => {
  const pending = injects
    .filter(i => i.status === InjectStatus.Draft)
    .sort((a, b) => a.sequence - b.sequence)

  return pending[0] || null
}

/**
 * Gets the next N injects after the current one in sequence order.
 * Used to display "Up Next" preview section.
 *
 * @param injects All injects in the exercise
 * @param currentSequence Sequence number of the current inject
 * @param count Number of injects to return (default 3)
 * @returns Array of upcoming draft injects
 */
export const getUpNextInjects = (
  injects: InjectDto[],
  currentSequence: number,
  count: number = 3,
): InjectDto[] => {
  return injects
    .filter(i => i.status === InjectStatus.Draft)
    .filter(i => i.sequence > currentSequence)
    .sort((a, b) => a.sequence - b.sequence)
    .slice(0, count)
}

/**
 * Gets all draft injects between current and target (for jump confirmation).
 * These injects will be deferred when jumping to a later inject.
 *
 * @param injects All injects in the exercise
 * @param currentSequence Sequence number of the current inject
 * @param targetSequence Sequence number of the target inject
 * @returns Array of injects that would be deferred
 */
export const getInjectsToSkip = (
  injects: InjectDto[],
  currentSequence: number,
  targetSequence: number,
): InjectDto[] => {
  return injects
    .filter(i => i.status === InjectStatus.Draft)
    .filter(i => i.sequence >= currentSequence && i.sequence < targetSequence)
    .sort((a, b) => a.sequence - b.sequence)
}
