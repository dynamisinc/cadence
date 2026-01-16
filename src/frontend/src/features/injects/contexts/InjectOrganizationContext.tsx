/**
 * InjectOrganizationContext
 *
 * Provides centralized state management for inject filtering, sorting,
 * grouping, and searching. State is persisted to sessionStorage per exercise.
 */

import {
  createContext,
  useContext,
  useState,
  useCallback,
  useMemo,
  useEffect,
  useRef,
  type ReactNode,
} from 'react'
import { useDebounce } from '../../../shared/hooks'
import type {
  InjectOrganizationContextValue,
  SortConfig,
  FilterState,
  GroupBy,
  SortableColumn,
  FilterType,
  PersistedOrganizationState,
} from '../types/organization'
import {
  DEFAULT_SORT,
  DEFAULT_FILTERS,
  DEFAULT_GROUP_BY,
  getStorageKey,
  SEARCH_DEBOUNCE_MS,
} from '../types/organization'
import { toggleSortConfig } from '../utils/sortUtils'
import { clearFilter as clearFilterUtil, clearAllFilters as clearAllFiltersUtil } from '../utils/filterUtils'
import type { InjectStatus, DeliveryMethod } from '../../../types'

// Create context with undefined default
const InjectOrganizationContext = createContext<
  InjectOrganizationContextValue | undefined
>(undefined)

export interface InjectOrganizationProviderProps {
  /** Exercise ID for storage key */
  exerciseId: string
  /** Children to render */
  children: ReactNode
}

/**
 * Load persisted state from sessionStorage
 */
function loadPersistedState(exerciseId: string): Partial<PersistedOrganizationState> | null {
  try {
    const stored = sessionStorage.getItem(getStorageKey(exerciseId))
    if (stored) {
      return JSON.parse(stored)
    }
  } catch {
    // Ignore parse errors
  }
  return null
}

/**
 * Save state to sessionStorage
 */
function savePersistedState(
  exerciseId: string,
  state: PersistedOrganizationState,
) {
  try {
    sessionStorage.setItem(getStorageKey(exerciseId), JSON.stringify(state))
  } catch {
    // Ignore storage errors
  }
}

export const InjectOrganizationProvider = ({
  exerciseId,
  children,
}: InjectOrganizationProviderProps) => {
  // Load initial state from storage
  const persisted = useMemo(() => loadPersistedState(exerciseId), [exerciseId])

  // Track whether we restored expanded groups from persisted state
  // Only true if the array was explicitly present AND non-empty
  const hasPersistedExpandedGroups = useMemo(
    () => persisted !== null && Array.isArray(persisted.expandedGroupIds) && persisted.expandedGroupIds.length > 0,
    [persisted],
  )

  // Search state
  const [searchTerm, setSearchTermState] = useState(persisted?.searchTerm ?? '')
  const debouncedSearchTerm = useDebounce(searchTerm, SEARCH_DEBOUNCE_MS)

  // Filter state - merge with defaults to handle new fields added after persisted state was saved
  const [filters, setFilters] = useState<FilterState>(() => ({
    ...DEFAULT_FILTERS,
    ...persisted?.filters,
  }))

  // Sort state
  const [sort, setSort] = useState<SortConfig>(
    persisted?.sort ?? DEFAULT_SORT,
  )

  // Group state
  const [groupBy, setGroupByState] = useState<GroupBy>(
    persisted?.groupBy ?? DEFAULT_GROUP_BY,
  )

  // Expanded groups (persisted as array, stored as Set)
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(
    () => new Set(persisted?.expandedGroupIds ?? []),
  )

  // Track the previous groupBy to detect actual changes (not just initial mount)
  const prevGroupByRef = useRef<GroupBy>(groupBy)

  // Create a stable string representation for dependency comparison
  // This avoids triggering the persistence effect on every Set reference change
  const expandedGroupsKey = useMemo(
    () => Array.from(expandedGroups).sort().join(','),
    [expandedGroups],
  )

  // Persist state changes - only persist expanded groups if they're non-empty
  // This prevents overwriting good state with empty state on initial mount
  useEffect(() => {
    savePersistedState(exerciseId, {
      searchTerm,
      filters,
      sort,
      groupBy,
      expandedGroupIds: Array.from(expandedGroups),
    })
  }, [exerciseId, searchTerm, filters, sort, groupBy, expandedGroupsKey])

  // Reset expanded groups when groupBy ACTUALLY changes (not on initial mount)
  useEffect(() => {
    // Only reset if groupBy actually changed from its previous value
    if (prevGroupByRef.current !== groupBy) {
      prevGroupByRef.current = groupBy
      setExpandedGroups(new Set())
    }
  }, [groupBy])

  // Search actions
  const setSearchTerm = useCallback((term: string) => {
    setSearchTermState(term)
  }, [])

  const clearSearch = useCallback(() => {
    setSearchTermState('')
  }, [])

  // Filter actions
  const setStatusFilter = useCallback((statuses: InjectStatus[]) => {
    setFilters(prev => ({ ...prev, statuses }))
  }, [])

  const setPhaseFilter = useCallback((phaseIds: (string | null)[]) => {
    setFilters(prev => ({ ...prev, phaseIds }))
  }, [])

  const setMethodFilter = useCallback((deliveryMethods: DeliveryMethod[]) => {
    setFilters(prev => ({ ...prev, deliveryMethods }))
  }, [])

  const setObjectiveFilter = useCallback((objectiveIds: (string | null)[]) => {
    setFilters(prev => ({ ...prev, objectiveIds }))
  }, [])

  const clearFilter = useCallback((filterType: FilterType) => {
    setFilters(prev => clearFilterUtil(prev, filterType))
  }, [])

  const clearAllFilters = useCallback(() => {
    setFilters(clearAllFiltersUtil())
  }, [])

  // Sort actions
  const toggleSort = useCallback((column: SortableColumn) => {
    setSort(prev => toggleSortConfig(prev, column))
  }, [])

  const clearSort = useCallback(() => {
    setSort({ column: null, direction: null })
  }, [])

  // Group actions
  const setGroupBy = useCallback((newGroupBy: GroupBy) => {
    setGroupByState(newGroupBy)
  }, [])

  const toggleGroupExpanded = useCallback((groupId: string) => {
    setExpandedGroups(prev => {
      const next = new Set(prev)
      if (next.has(groupId)) {
        next.delete(groupId)
      } else {
        next.add(groupId)
      }
      return next
    })
  }, [])

  const expandAllGroups = useCallback(() => {
    // This needs to be called with the actual group IDs
    // The consumer should call this with the groups
  }, [])

  const collapseAllGroups = useCallback(() => {
    setExpandedGroups(new Set())
  }, [])

  // Initialize expanded groups with given IDs (only if no persisted state)
  // This is called by the hook when groups are first available
  const initializeExpandedGroups = useCallback((groupIds: string[]) => {
    if (!hasPersistedExpandedGroups) {
      setExpandedGroups(new Set(groupIds))
    }
  }, [hasPersistedExpandedGroups])

  // Combine state and actions
  const value: InjectOrganizationContextValue = useMemo(
    () => ({
      // State
      searchTerm,
      debouncedSearchTerm,
      filters,
      sort,
      groupBy,
      expandedGroups,
      hasPersistedExpandedGroups,

      // Search actions
      setSearchTerm,
      clearSearch,

      // Filter actions
      setStatusFilter,
      setPhaseFilter,
      setMethodFilter,
      setObjectiveFilter,
      clearFilter,
      clearAllFilters,

      // Sort actions
      toggleSort,
      clearSort,

      // Group actions
      setGroupBy,
      toggleGroupExpanded,
      expandAllGroups,
      collapseAllGroups,
      initializeExpandedGroups,
    }),
    [
      searchTerm,
      debouncedSearchTerm,
      filters,
      sort,
      groupBy,
      expandedGroups,
      hasPersistedExpandedGroups,
      setSearchTerm,
      clearSearch,
      setStatusFilter,
      setPhaseFilter,
      setMethodFilter,
      setObjectiveFilter,
      clearFilter,
      clearAllFilters,
      toggleSort,
      clearSort,
      setGroupBy,
      toggleGroupExpanded,
      expandAllGroups,
      collapseAllGroups,
      initializeExpandedGroups,
    ],
  )

  return (
    <InjectOrganizationContext.Provider value={value}>
      {children}
    </InjectOrganizationContext.Provider>
  )
}

/**
 * Hook to access inject organization context
 * @throws Error if used outside of InjectOrganizationProvider
 */
export function useInjectOrganizationContext(): InjectOrganizationContextValue {
  const context = useContext(InjectOrganizationContext)
  if (!context) {
    throw new Error(
      'useInjectOrganizationContext must be used within InjectOrganizationProvider',
    )
  }
  return context
}

export default InjectOrganizationContext
