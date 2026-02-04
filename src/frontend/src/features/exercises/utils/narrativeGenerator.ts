/**
 * Narrative Generator Utilities
 *
 * Generates narrative text from inject data for the Observer-friendly
 * narrative view during exercise conduct.
 */

import { InjectStatus } from '../../../types'
import type { InjectDto } from '../../injects/types'

/**
 * Generate "The Story So Far" narrative from fired injects.
 * Returns an array of description paragraphs sorted chronologically.
 *
 * @param injects All injects in the exercise
 * @returns Array of narrative paragraphs from fired injects
 */
export const generateStorySoFar = (injects: InjectDto[]): string[] => {
  // Filter to only fired injects
  const firedInjects = injects.filter(
    inject => inject.status === InjectStatus.Released && inject.firedAt,
  )

  // Sort by firedAt ascending (oldest first to tell story chronologically)
  firedInjects.sort((a, b) => {
    const aTime = a.firedAt ? new Date(a.firedAt).getTime() : 0
    const bTime = b.firedAt ? new Date(b.firedAt).getTime() : 0
    return aTime - bTime
  })

  // Return descriptions as narrative paragraphs
  return firedInjects.map(inject => inject.description)
}

/**
 * Generate "What's Happening Now" narrative for the next inject.
 *
 * @param nextInject The next inject to be fired (or null if none)
 * @returns Narrative text describing the current event, or null if no inject
 */
export const generateCurrentEvent = (nextInject: InjectDto | null): string | null => {
  if (!nextInject) return null

  const { target, source, description } = nextInject

  // Build narrative text
  let narrative = ''

  if (source) {
    narrative = `${source} is delivering information to ${target}. `
  } else {
    narrative = `${target} is receiving new information. `
  }

  narrative += description

  return narrative
}

/**
 * Generate "Coming Up" preview items for upcoming injects.
 * Returns brief summaries of what's next, limited to 5 items.
 *
 * @param upcomingInjects Injects scheduled to occur soon (sorted by time)
 * @returns Array of preview strings (max 5)
 */
export const generateUpcomingPreview = (upcomingInjects: InjectDto[]): string[] => {
  // Limit to 5 items
  const limitedInjects = upcomingInjects.slice(0, 5)

  return limitedInjects.map(inject => {
    const { title, target } = inject
    return `${target} will need to respond to: ${title}`
  })
}

/**
 * Format scenario time for narrative display
 * @param scenarioDay Day number in the scenario
 * @param scenarioTime Time string (HH:MM:SS)
 * @returns Formatted string like "Day 2, 08:00"
 */
export const formatNarrativeTime = (
  scenarioDay: number | null,
  scenarioTime: string | null,
): string | null => {
  if (!scenarioDay) return null

  const timeStr = scenarioTime ? scenarioTime.substring(0, 5) : ''
  return timeStr ? `Day ${scenarioDay}, ${timeStr}` : `Day ${scenarioDay}`
}
