/**
 * Filter Utilities for Inject Organization
 *
 * Pure functions for filtering injects by status, phase, and delivery method.
 * All functions are side-effect free and return new arrays.
 */

import type { InjectDto } from '../types'
import type { FilterState, FilterType } from '../types/organization'
import { DEFAULT_FILTERS } from '../types/organization'
import type { InjectStatus, DeliveryMethod } from '../../../types'

/**
 * Check if a filter is active (has selections)
 * @param filter Array of selected filter values
 * @returns True if filter has any selections
 */
function isFilterActive<T>(filter: T[]): boolean {
  return filter.length > 0
}

/**
 * Filter injects by status
 * @param injects Array of injects to filter
 * @param statuses Selected status values (empty = all)
 * @returns Filtered injects
 */
export function filterByStatus(
  injects: InjectDto[],
  statuses: InjectStatus[],
): InjectDto[] {
  if (!isFilterActive(statuses)) {
    return injects
  }
  return injects.filter(inject => statuses.includes(inject.status))
}

/**
 * Filter injects by phase
 * @param injects Array of injects to filter
 * @param phaseIds Selected phase IDs (null = "Unassigned", empty = all)
 * @returns Filtered injects
 */
export function filterByPhase(
  injects: InjectDto[],
  phaseIds: (string | null)[],
): InjectDto[] {
  if (!isFilterActive(phaseIds)) {
    return injects
  }
  return injects.filter(inject => phaseIds.includes(inject.phaseId))
}

/**
 * Filter injects by delivery method
 * @param injects Array of injects to filter
 * @param methods Selected delivery methods (empty = all)
 * @returns Filtered injects
 */
export function filterByMethod(
  injects: InjectDto[],
  methods: DeliveryMethod[],
): InjectDto[] {
  if (!isFilterActive(methods)) {
    return injects
  }
  return injects.filter(
    inject => inject.deliveryMethod && methods.includes(inject.deliveryMethod),
  )
}

/**
 * Filter injects by objective
 * @param injects Array of injects to filter
 * @param objectiveIds Selected objective IDs (null = "No objectives", empty = all)
 * @returns Filtered injects
 */
export function filterByObjective(
  injects: InjectDto[],
  objectiveIds: (string | null)[],
): InjectDto[] {
  if (!isFilterActive(objectiveIds)) {
    return injects
  }
  return injects.filter(inject => {
    // If null is in the filter, include injects with no objectives
    if (objectiveIds.includes(null) && inject.objectiveIds.length === 0) {
      return true
    }
    // Check if inject has any of the selected objectives (OR logic)
    return inject.objectiveIds.some(objId =>
      objectiveIds.includes(objId),
    )
  })
}

/**
 * Apply all filters to injects (AND logic between filters)
 * @param injects Array of injects to filter
 * @param filters Filter state with all filter types
 * @returns Filtered injects matching all criteria
 */
export function applyFilters(
  injects: InjectDto[],
  filters: FilterState,
): InjectDto[] {
  let result = injects

  // Apply each filter in sequence (AND logic)
  result = filterByStatus(result, filters.statuses)
  result = filterByPhase(result, filters.phaseIds)
  result = filterByMethod(result, filters.deliveryMethods)
  result = filterByObjective(result, filters.objectiveIds)

  return result
}

/**
 * Count the number of active filters
 * @param filters Filter state
 * @returns Number of active filter categories
 */
export function countActiveFilters(filters: FilterState): number {
  let count = 0
  if (isFilterActive(filters.statuses)) count++
  if (isFilterActive(filters.phaseIds)) count++
  if (isFilterActive(filters.deliveryMethods)) count++
  if (isFilterActive(filters.objectiveIds)) count++
  return count
}

/**
 * Check if any filters are active
 * @param filters Filter state
 * @returns True if any filters are applied
 */
export function hasActiveFilters(filters: FilterState): boolean {
  return (
    isFilterActive(filters.statuses) ||
    isFilterActive(filters.phaseIds) ||
    isFilterActive(filters.deliveryMethods) ||
    isFilterActive(filters.objectiveIds)
  )
}

/**
 * Clear a specific filter type
 * @param filters Current filter state
 * @param filterType Type of filter to clear
 * @returns New filter state with specified filter cleared
 */
export function clearFilter(
  filters: FilterState,
  filterType: FilterType,
): FilterState {
  switch (filterType) {
    case 'status':
      return { ...filters, statuses: [] }
    case 'phase':
      return { ...filters, phaseIds: [] }
    case 'method':
      return { ...filters, deliveryMethods: [] }
    case 'objective':
      return { ...filters, objectiveIds: [] }
    default:
      return filters
  }
}

/**
 * Clear all filters
 * @returns Default (empty) filter state
 */
export function clearAllFilters(): FilterState {
  return { ...DEFAULT_FILTERS }
}

/**
 * Get display labels for active filters
 * @param filters Filter state
 * @param phaseMap Map of phase IDs to names
 * @param objectiveMap Map of objective IDs to names
 * @returns Array of filter labels with their type and value
 */
export function getActiveFilterLabels(
  filters: FilterState,
  phaseMap: Map<string | null, string>,
  objectiveMap?: Map<string | null, string>,
): Array<{ type: FilterType; label: string; value: string }> {
  const labels: Array<{ type: FilterType; label: string; value: string }> = []

  // Status filters
  if (filters.statuses.length > 0) {
    if (filters.statuses.length === 1) {
      labels.push({
        type: 'status',
        label: 'Status',
        value: filters.statuses[0],
      })
    } else {
      labels.push({
        type: 'status',
        label: 'Status',
        value: `${filters.statuses.length} selected`,
      })
    }
  }

  // Phase filters
  if (filters.phaseIds.length > 0) {
    if (filters.phaseIds.length === 1) {
      const phaseName = phaseMap.get(filters.phaseIds[0]) || 'Unassigned'
      labels.push({
        type: 'phase',
        label: 'Phase',
        value: phaseName,
      })
    } else {
      labels.push({
        type: 'phase',
        label: 'Phase',
        value: `${filters.phaseIds.length} selected`,
      })
    }
  }

  // Method filters
  if (filters.deliveryMethods.length > 0) {
    if (filters.deliveryMethods.length === 1) {
      labels.push({
        type: 'method',
        label: 'Method',
        value: filters.deliveryMethods[0],
      })
    } else {
      labels.push({
        type: 'method',
        label: 'Method',
        value: `${filters.deliveryMethods.length} selected`,
      })
    }
  }

  // Objective filters
  if (filters.objectiveIds.length > 0) {
    if (filters.objectiveIds.length === 1) {
      const objectiveName = objectiveMap?.get(filters.objectiveIds[0]) || 'No objectives'
      labels.push({
        type: 'objective',
        label: 'Objective',
        value: objectiveName,
      })
    } else {
      labels.push({
        type: 'objective',
        label: 'Objective',
        value: `${filters.objectiveIds.length} selected`,
      })
    }
  }

  return labels
}

/**
 * Build a phase name map from phase data
 * @param phases Array of phase objects with id and name
 * @returns Map of phase ID (or null for unassigned) to display name
 */
export function buildPhaseNameMap(
  phases: Array<{ id: string; name: string }>,
): Map<string | null, string> {
  const map = new Map<string | null, string>()
  map.set(null, 'Unassigned')
  phases.forEach(p => map.set(p.id, p.name))
  return map
}

/**
 * Build an objective name map from objective data
 * @param objectives Array of objective objects with id and name/objectiveNumber
 * @returns Map of objective ID (or null for no objectives) to display name
 */
export function buildObjectiveNameMap(
  objectives: Array<{ id: string; name: string; objectiveNumber: string }>,
): Map<string | null, string> {
  const map = new Map<string | null, string>()
  map.set(null, 'No objectives')
  objectives.forEach(o => map.set(o.id, `${o.objectiveNumber}: ${o.name}`))
  return map
}
