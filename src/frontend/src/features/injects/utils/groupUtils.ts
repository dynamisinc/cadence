/**
 * Group Utilities for Inject Organization
 *
 * Pure functions for grouping injects by phase or status.
 * All functions are side-effect free and return new data structures.
 */

import type { InjectDto } from '../types'
import type { GroupBy, InjectGroup } from '../types/organization'
import { InjectStatus } from '../../../types'

/**
 * Status group ordering and labels
 */
const STATUS_GROUP_CONFIG: Record<string, { order: number; label: string }> = {
  [InjectStatus.Pending]: { order: 0, label: 'Pending' },
  [InjectStatus.Fired]: { order: 1, label: 'Fired' },
  [InjectStatus.Skipped]: { order: 2, label: 'Skipped' },
}

/**
 * Group injects by their status
 * @param injects Array of injects to group
 * @returns Array of groups ordered by status (Pending → Fired → Skipped)
 */
export function groupByStatus(injects: InjectDto[]): InjectGroup[] {
  // Build groups map
  const groupMap = new Map<string, string[]>()

  injects.forEach(inject => {
    const status = inject.status
    const existing = groupMap.get(status) || []
    groupMap.set(status, [...existing, inject.id])
  })

  // Convert to InjectGroup array
  const groups: InjectGroup[] = []

  Object.entries(STATUS_GROUP_CONFIG).forEach(([status, config]) => {
    const injectIds = groupMap.get(status) || []
    if (injectIds.length > 0) {
      groups.push({
        id: `status-${status}`,
        name: config.label,
        count: injectIds.length,
        sortOrder: config.order,
        injectIds,
      })
    }
  })

  // Sort by order and return
  return groups.sort((a, b) => a.sortOrder - b.sortOrder)
}

/**
 * Group injects by their phase
 * @param injects Array of injects to group
 * @param phases Array of phase objects with id, name, and sequence
 * @returns Array of groups ordered by phase sequence (Unassigned last)
 */
export function groupByPhase(
  injects: InjectDto[],
  phases: Array<{ id: string; name: string; sequence: number }>,
): InjectGroup[] {
  // Build groups map
  const groupMap = new Map<string | null, string[]>()

  injects.forEach(inject => {
    const phaseId = inject.phaseId
    const existing = groupMap.get(phaseId) || []
    groupMap.set(phaseId, [...existing, inject.id])
  })

  // Convert to InjectGroup array
  const groups: InjectGroup[] = []

  // Add groups for existing phases
  phases.forEach(phase => {
    const injectIds = groupMap.get(phase.id) || []
    if (injectIds.length > 0) {
      groups.push({
        id: `phase-${phase.id}`,
        name: phase.name,
        count: injectIds.length,
        sortOrder: phase.sequence,
        injectIds,
      })
    }
  })

  // Add unassigned group if any
  const unassignedIds = groupMap.get(null) || []
  if (unassignedIds.length > 0) {
    groups.push({
      id: 'phase-unassigned',
      name: 'Unassigned',
      count: unassignedIds.length,
      sortOrder: 9999, // Sort last
      injectIds: unassignedIds,
    })
  }

  // Sort by order and return
  return groups.sort((a, b) => a.sortOrder - b.sortOrder)
}

/**
 * Group injects based on the groupBy setting
 * @param injects Array of injects to group
 * @param groupBy Grouping mode
 * @param phases Array of phase objects (required for phase grouping)
 * @returns Array of groups, or null if groupBy is 'none'
 */
export function groupInjects(
  injects: InjectDto[],
  groupBy: GroupBy,
  phases: Array<{ id: string; name: string; sequence: number }> = [],
): InjectGroup[] | null {
  switch (groupBy) {
    case 'none':
      return null
    case 'status':
      return groupByStatus(injects)
    case 'phase':
      return groupByPhase(injects, phases)
    default:
      return null
  }
}

/**
 * Get injects for a specific group
 * @param injects Array of all injects
 * @param group Group to get injects for
 * @returns Array of injects in the group
 */
export function getInjectsForGroup(
  injects: InjectDto[],
  group: InjectGroup,
): InjectDto[] {
  // Maintain the order from injectIds
  return group.injectIds
    .map(id => injects.find(i => i.id === id))
    .filter((inject): inject is InjectDto => inject !== undefined)
}

/**
 * Get group ID for an inject based on grouping mode
 * @param inject Inject to get group for
 * @param groupBy Grouping mode
 * @returns Group ID string
 */
export function getGroupIdForInject(
  inject: InjectDto,
  groupBy: GroupBy,
): string {
  switch (groupBy) {
    case 'status':
      return `status-${inject.status}`
    case 'phase':
      return inject.phaseId ? `phase-${inject.phaseId}` : 'phase-unassigned'
    default:
      return ''
  }
}

/**
 * Initialize expanded groups (all expanded by default)
 * @param groups Array of groups
 * @returns Set of all group IDs
 */
export function initExpandedGroups(groups: InjectGroup[]): Set<string> {
  return new Set(groups.map(g => g.id))
}

/**
 * Toggle expansion state of a group
 * @param expandedGroups Current set of expanded group IDs
 * @param groupId Group ID to toggle
 * @returns New set with group toggled
 */
export function toggleGroupExpansion(
  expandedGroups: Set<string>,
  groupId: string,
): Set<string> {
  const newSet = new Set(expandedGroups)
  if (newSet.has(groupId)) {
    newSet.delete(groupId)
  } else {
    newSet.add(groupId)
  }
  return newSet
}

/**
 * Expand all groups
 * @param groups Array of groups
 * @returns Set containing all group IDs
 */
export function expandAllGroups(groups: InjectGroup[]): Set<string> {
  return new Set(groups.map(g => g.id))
}

/**
 * Collapse all groups
 * @returns Empty set
 */
export function collapseAllGroups(): Set<string> {
  return new Set()
}

/**
 * Check if a group is expanded
 * @param expandedGroups Set of expanded group IDs
 * @param groupId Group ID to check
 * @returns True if group is expanded
 */
export function isGroupExpanded(
  expandedGroups: Set<string>,
  groupId: string,
): boolean {
  return expandedGroups.has(groupId)
}

/**
 * Get display options for group by dropdown
 */
export function getGroupByOptions(): Array<{ value: GroupBy; label: string }> {
  return [
    { value: 'none', label: 'None' },
    { value: 'phase', label: 'Phase' },
    { value: 'status', label: 'Status' },
  ]
}

/**
 * Get group IDs that contain any of the given inject IDs
 * Used to auto-expand groups when search/filter matches
 * @param groups Array of groups
 * @param injectIds Set of inject IDs to find
 * @returns Set of group IDs that contain matching injects
 */
export function getGroupsContainingInjects(
  groups: InjectGroup[],
  injectIds: Set<string>,
): Set<string> {
  const matchingGroupIds = new Set<string>()

  groups.forEach(group => {
    const hasMatch = group.injectIds.some(id => injectIds.has(id))
    if (hasMatch) {
      matchingGroupIds.add(group.id)
    }
  })

  return matchingGroupIds
}
