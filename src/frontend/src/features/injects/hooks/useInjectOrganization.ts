/**
 * useInjectOrganization Hook
 *
 * Combines the organization context with utility functions to provide
 * computed filtered, sorted, and grouped injects.
 */

import { useMemo, useCallback, useEffect, useRef } from 'react'
import { useInjectOrganizationContext } from '../contexts/InjectOrganizationContext'
import type { InjectDto } from '../types'
import type { InjectGroup, SearchableField } from '../types/organization'
import { sortInjects, buildPhaseSequenceMap } from '../utils/sortUtils'
import { applyFilters, hasActiveFilters, getActiveFilterLabels, buildPhaseNameMap } from '../utils/filterUtils'
import { filterBySearch, getSearchMatches, createSearchMatchMap } from '../utils/searchUtils'
import { groupInjects, getInjectsForGroup, getGroupsContainingInjects } from '../utils/groupUtils'

export interface PhaseInfo {
  id: string
  name: string
  sequence: number
}

export interface UseInjectOrganizationResult {
  // Organization context values
  searchTerm: string
  debouncedSearchTerm: string
  filters: ReturnType<typeof useInjectOrganizationContext>['filters']
  sort: ReturnType<typeof useInjectOrganizationContext>['sort']
  groupBy: ReturnType<typeof useInjectOrganizationContext>['groupBy']
  expandedGroups: Set<string>

  // Actions
  setSearchTerm: (term: string) => void
  clearSearch: () => void
  setStatusFilter: ReturnType<typeof useInjectOrganizationContext>['setStatusFilter']
  setPhaseFilter: ReturnType<typeof useInjectOrganizationContext>['setPhaseFilter']
  setMethodFilter: ReturnType<typeof useInjectOrganizationContext>['setMethodFilter']
  clearFilter: ReturnType<typeof useInjectOrganizationContext>['clearFilter']
  clearAllFilters: () => void
  toggleSort: ReturnType<typeof useInjectOrganizationContext>['toggleSort']
  setGroupBy: ReturnType<typeof useInjectOrganizationContext>['setGroupBy']
  toggleGroupExpanded: ReturnType<typeof useInjectOrganizationContext>['toggleGroupExpanded']
  expandAllGroups: () => void
  collapseAllGroups: () => void

  // Computed values
  organizedInjects: InjectDto[]
  groups: InjectGroup[] | null
  getInjectsForGroup: (group: InjectGroup) => InjectDto[]
  totalCount: number
  filteredCount: number
  hasActiveFilters: boolean
  activeFilterLabels: Array<{ type: 'status' | 'phase' | 'method'; label: string; value: string }>
  searchMatchMap: Map<string, SearchableField[]>
}

/**
 * Hook that provides organized (filtered, sorted, grouped) injects
 */
export function useInjectOrganization(
  injects: InjectDto[],
  phases: PhaseInfo[] = [],
): UseInjectOrganizationResult {
  const context = useInjectOrganizationContext()

  // Build lookup maps
  const phaseSequenceMap = useMemo(
    () => buildPhaseSequenceMap(phases),
    [phases],
  )

  const phaseNameMap = useMemo(
    () => buildPhaseNameMap(phases),
    [phases],
  )

  // Apply search filtering
  const searchFiltered = useMemo(() => {
    if (!context.debouncedSearchTerm) return injects
    return filterBySearch(injects, context.debouncedSearchTerm)
  }, [injects, context.debouncedSearchTerm])

  // Apply attribute filters
  const filtered = useMemo(() => {
    return applyFilters(searchFiltered, context.filters)
  }, [searchFiltered, context.filters])

  // Apply sorting
  const sorted = useMemo(() => {
    return sortInjects(filtered, context.sort, phaseSequenceMap)
  }, [filtered, context.sort, phaseSequenceMap])

  // Apply grouping
  const groups = useMemo(() => {
    return groupInjects(sorted, context.groupBy, phases)
  }, [sorted, context.groupBy, phases])

  // Get search match map for highlighting
  const searchMatchMap = useMemo(() => {
    if (!context.debouncedSearchTerm) return new Map()
    const matches = getSearchMatches(injects, context.debouncedSearchTerm)
    return createSearchMatchMap(matches)
  }, [injects, context.debouncedSearchTerm])

  // Get active filter labels
  const activeFilterLabels = useMemo(() => {
    return getActiveFilterLabels(context.filters, phaseNameMap)
  }, [context.filters, phaseNameMap])

  // Helper to get injects for a group
  const getGroupInjects = useCallback(
    (group: InjectGroup) => getInjectsForGroup(sorted, group),
    [sorted],
  )

  // Expand all groups helper
  const expandAllGroupsCallback = useCallback(() => {
    if (groups) {
      groups.forEach(group => {
        if (!context.expandedGroups.has(group.id)) {
          context.toggleGroupExpanded(group.id)
        }
      })
    }
  }, [groups, context])

  // Initialize expanded groups when groups first become available
  // Only if there's no persisted state - the context handles this check internally
  // IMPORTANT: We need to wait for phases to load before initializing when groupBy='phase',
  // otherwise we might expand wrong group IDs (e.g., 'phase-unassigned' instead of actual phase IDs)
  const hasInitializedRef = useRef(false)

  useEffect(() => {
    // Don't initialize until we have groups
    if (!groups || groups.length === 0 || hasInitializedRef.current) return

    // If grouping by phase and phases exist in the exercise, wait until they're loaded into groups
    // This prevents initializing with wrong group IDs before phases load
    if (context.groupBy === 'phase' && phases.length > 0) {
      const hasActualPhaseGroups = groups.some(g => g.id.startsWith('phase-') && g.id !== 'phase-unassigned')
      if (!hasActualPhaseGroups) {
        // Phases exist but haven't been incorporated into groups yet - wait
        return
      }
    }

    hasInitializedRef.current = true

    // Get all group IDs for initial expansion
    const allGroupIds = groups.map(g => g.id)
    context.initializeExpandedGroups(allGroupIds)
  }, [groups, phases.length, context.groupBy, context.initializeExpandedGroups])

  // Track previous search/filter state to detect changes
  const prevSearchRef = useRef<string>('')
  const prevFilterCountRef = useRef<number>(0)

  // Auto-expand groups when search or filter is ADDED (not when cleared)
  // Note: Initial expansion is handled by the context based on hasPersistedExpandedGroups
  useEffect(() => {
    if (!groups) return

    const currentFilterCount =
      context.filters.statuses.length +
      context.filters.phaseIds.length +
      context.filters.deliveryMethods.length

    const prevSearch = prevSearchRef.current
    const prevFilterCount = prevFilterCountRef.current

    // Detect if search/filter became MORE restrictive (added, not cleared)
    const searchAdded = context.debouncedSearchTerm && !prevSearch
    const searchModified = context.debouncedSearchTerm && prevSearch && context.debouncedSearchTerm !== prevSearch
    const filtersAdded = currentFilterCount > prevFilterCount

    // Update refs
    prevSearchRef.current = context.debouncedSearchTerm
    prevFilterCountRef.current = currentFilterCount

    // Only auto-expand on search/filter changes, not on initial render
    if (searchAdded || searchModified || filtersAdded) {
      // Search or filter added/modified: expand groups containing matching injects
      const matchingInjectIds = new Set(sorted.map(i => i.id))
      const groupsToExpand = getGroupsContainingInjects(groups, matchingInjectIds)

      groupsToExpand.forEach(groupId => {
        if (!context.expandedGroups.has(groupId)) {
          context.toggleGroupExpanded(groupId)
        }
      })
    }
    // When search is cleared or filters removed, do nothing - keep current expansion state
  }, [groups, sorted, context.debouncedSearchTerm, context.filters])

  return {
    // Context state
    searchTerm: context.searchTerm,
    debouncedSearchTerm: context.debouncedSearchTerm,
    filters: context.filters,
    sort: context.sort,
    groupBy: context.groupBy,
    expandedGroups: context.expandedGroups,

    // Context actions
    setSearchTerm: context.setSearchTerm,
    clearSearch: context.clearSearch,
    setStatusFilter: context.setStatusFilter,
    setPhaseFilter: context.setPhaseFilter,
    setMethodFilter: context.setMethodFilter,
    clearFilter: context.clearFilter,
    clearAllFilters: context.clearAllFilters,
    toggleSort: context.toggleSort,
    setGroupBy: context.setGroupBy,
    toggleGroupExpanded: context.toggleGroupExpanded,
    expandAllGroups: expandAllGroupsCallback,
    collapseAllGroups: context.collapseAllGroups,

    // Computed values
    organizedInjects: sorted,
    groups,
    getInjectsForGroup: getGroupInjects,
    totalCount: injects.length,
    filteredCount: sorted.length,
    hasActiveFilters: hasActiveFilters(context.filters) || !!context.debouncedSearchTerm,
    activeFilterLabels,
    searchMatchMap,
  }
}

export default useInjectOrganization
