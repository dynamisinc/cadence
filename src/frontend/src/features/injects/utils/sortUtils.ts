/**
 * Sort Utilities for Inject Organization
 *
 * Pure functions for sorting injects by various columns.
 * All functions are side-effect free and return new arrays.
 */

import type { InjectDto } from '../types'
import type { SortConfig, SortableColumn, SortDirection } from '../types/organization'
import { InjectStatus } from '../../../types'

/**
 * Status sort order: Draft → Synchronized → Released → Deferred
 */
const STATUS_ORDER: Record<string, number> = {
  [InjectStatus.Draft]: 0,
  [InjectStatus.Synchronized]: 1,
  [InjectStatus.Released]: 2,
  [InjectStatus.Deferred]: 3,
}

/**
 * Compare two values for sorting, handling nulls
 * @param a First value
 * @param b Second value
 * @param nullsLast If true, null values sort to the end
 * @returns -1, 0, or 1 for sorting
 */
function compareValues<T>(
  a: T | null | undefined, b: T | null | undefined, nullsLast = true,
): number {
  const aNull = a === null || a === undefined
  const bNull = b === null || b === undefined

  if (aNull && bNull) return 0
  if (aNull) return nullsLast ? 1 : -1
  if (bNull) return nullsLast ? -1 : 1

  if (a! < b!) return -1
  if (a! > b!) return 1
  return 0
}

/**
 * Compare two strings case-insensitively
 */
function compareStrings(
  a: string | null | undefined, b: string | null | undefined, nullsLast = true,
): number {
  const aNull = a === null || a === undefined || a === ''
  const bNull = b === null || b === undefined || b === ''

  if (aNull && bNull) return 0
  if (aNull) return nullsLast ? 1 : -1
  if (bNull) return nullsLast ? -1 : 1

  return a!.toLowerCase().localeCompare(b!.toLowerCase())
}

/**
 * Parse time string (HH:MM:SS) to minutes for comparison
 */
function parseTimeToMinutes(time: string | null | undefined): number | null {
  if (!time) return null
  const parts = time.split(':').map(Number)
  const hours = parts[0] || 0
  const minutes = parts[1] || 0
  return hours * 60 + minutes
}

/**
 * Get the sort comparator for a specific column
 * @param column The column to sort by
 * @param phases Map of phase IDs to their sequence numbers
 * @returns Comparator function
 */
function getComparator(
  column: SortableColumn,
  phases: Map<string, number>,
): (a: InjectDto, b: InjectDto) => number {
  switch (column) {
    case 'injectNumber':
      return (a, b) => compareValues(a.injectNumber, b.injectNumber)

    case 'title':
      return (a, b) => compareStrings(a.title, b.title)

    case 'scheduledTime':
      return (a, b) => {
        const aMinutes = parseTimeToMinutes(a.scheduledTime)
        const bMinutes = parseTimeToMinutes(b.scheduledTime)
        return compareValues(aMinutes, bMinutes)
      }

    case 'scenarioTime':
      return (a, b) => {
        // Sort by day first, then by time within day
        // Nulls sort last
        const aDayNull = a.scenarioDay === null
        const bDayNull = b.scenarioDay === null

        if (aDayNull && bDayNull) return 0
        if (aDayNull) return 1
        if (bDayNull) return -1

        const dayCompare = compareValues(a.scenarioDay, b.scenarioDay)
        if (dayCompare !== 0) return dayCompare

        // Same day, compare time
        const aMinutes = parseTimeToMinutes(a.scenarioTime)
        const bMinutes = parseTimeToMinutes(b.scenarioTime)
        return compareValues(aMinutes, bMinutes)
      }

    case 'status':
      return (a, b) => {
        const aOrder = STATUS_ORDER[a.status] ?? 99
        const bOrder = STATUS_ORDER[b.status] ?? 99
        return aOrder - bOrder
      }

    case 'phase':
      return (a, b) => {
        // Get phase sequence, unassigned (null) sorts last
        const aSeq = a.phaseId ? (phases.get(a.phaseId) ?? 999) : 1000
        const bSeq = b.phaseId ? (phases.get(b.phaseId) ?? 999) : 1000
        return aSeq - bSeq
      }

    default:
      return () => 0
  }
}

/**
 * Sort injects by the specified configuration
 * @param injects Array of injects to sort
 * @param config Sort configuration
 * @param phases Map of phase IDs to sequence numbers (for phase sorting)
 * @returns New sorted array
 */
export function sortInjects(
  injects: InjectDto[],
  config: SortConfig,
  phases: Map<string, number> = new Map(),
): InjectDto[] {
  // No sort configured, return original order
  if (!config.column || !config.direction) {
    return [...injects]
  }

  const comparator = getComparator(config.column, phases)
  const direction = config.direction === 'asc' ? 1 : -1

  // Use stable sort by including index
  return [...injects]
    .map((inject, index) => ({ inject, index }))
    .sort((a, b) => {
      const result = comparator(a.inject, b.inject) * direction
      // Stable sort: if equal, maintain original order
      return result !== 0 ? result : a.index - b.index
    })
    .map(({ inject }) => inject)
}

/**
 * Get the next sort direction in the toggle cycle
 * Current cycle: null → asc → desc → null
 * @param currentDirection Current sort direction
 * @returns Next direction in cycle
 */
export function getNextSortDirection(currentDirection: SortDirection): SortDirection {
  switch (currentDirection) {
    case null:
      return 'asc'
    case 'asc':
      return 'desc'
    case 'desc':
      return null
    default:
      return 'asc'
  }
}

/**
 * Toggle sort for a column, returning new sort config
 * If clicking a different column, start with ascending
 * If clicking the same column, cycle through directions
 * @param currentConfig Current sort configuration
 * @param column Column to toggle
 * @returns New sort configuration
 */
export function toggleSortConfig(
  currentConfig: SortConfig,
  column: SortableColumn,
): SortConfig {
  // Different column: start fresh with ascending
  if (currentConfig.column !== column) {
    return { column, direction: 'asc' }
  }

  // Same column: cycle direction
  const nextDirection = getNextSortDirection(currentConfig.direction)

  // If cycling back to null, clear the column
  if (nextDirection === null) {
    return { column: null, direction: null }
  }

  return { column, direction: nextDirection }
}

/**
 * Build a phase sequence map from phase data
 * @param phases Array of phase objects with id and sequence
 * @returns Map of phase ID to sequence number
 */
export function buildPhaseSequenceMap(
  phases: Array<{ id: string; sequence: number }>,
): Map<string, number> {
  return new Map(phases.map(p => [p.id, p.sequence]))
}
