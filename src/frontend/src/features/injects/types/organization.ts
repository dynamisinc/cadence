/**
 * Inject Organization Types
 *
 * Types for sorting, filtering, grouping, and searching injects.
 * Used by the InjectOrganizationContext and related hooks/components.
 */

import type { InjectStatus, DeliveryMethod } from '../../../types'

// =============================================================================
// Sorting Types
// =============================================================================

/**
 * Columns that can be sorted in the inject list
 */
export type SortableColumn =
  | 'injectNumber'
  | 'title'
  | 'scheduledTime'
  | 'scenarioTime'
  | 'status'
  | 'phase'

/**
 * Sort direction: ascending, descending, or null (default/no sort)
 */
export type SortDirection = 'asc' | 'desc' | null

/**
 * Current sort configuration
 */
export interface SortConfig {
  /** Column being sorted, or null for default order */
  column: SortableColumn | null
  /** Sort direction, or null for default order */
  direction: SortDirection
}

/**
 * Default sort configuration (Scheduled Time ascending)
 */
export const DEFAULT_SORT: SortConfig = {
  column: 'scheduledTime',
  direction: 'asc',
}

// =============================================================================
// Filtering Types
// =============================================================================

/**
 * Current filter state
 * Each filter supports multi-select (OR logic within filter)
 * Multiple filters combine with AND logic
 */
export interface FilterState {
  /** Selected status values (empty = all) */
  statuses: InjectStatus[]
  /** Selected phase IDs (null = "Unassigned", empty = all) */
  phaseIds: (string | null)[]
  /** Selected delivery methods (empty = all) */
  deliveryMethods: DeliveryMethod[]
  /** Selected objective IDs (null = "No objectives", empty = all) */
  objectiveIds: (string | null)[]
}

/**
 * Types of filters that can be cleared individually
 */
export type FilterType = 'status' | 'phase' | 'method' | 'objective'

/**
 * Default filter state (no filters applied)
 */
export const DEFAULT_FILTERS: FilterState = {
  statuses: [],
  phaseIds: [],
  deliveryMethods: [],
  objectiveIds: [],
}

// =============================================================================
// Grouping Types
// =============================================================================

/**
 * How to group injects
 */
export type GroupBy = 'none' | 'phase' | 'status'

/**
 * A grouped collection of injects
 */
export interface InjectGroup {
  /** Unique identifier for this group */
  id: string
  /** Display name for the group header */
  name: string
  /** Number of injects in this group */
  count: number
  /** Order index for sorting groups */
  sortOrder: number
  /** Injects in this group */
  injectIds: string[]
}

/**
 * Default grouping (by phase, matching current InjectListPage behavior)
 */
export const DEFAULT_GROUP_BY: GroupBy = 'phase'

// =============================================================================
// Search Types
// =============================================================================

/**
 * Fields that are searchable
 */
export type SearchableField =
  | 'title'
  | 'description'
  | 'source'
  | 'target'
  | 'expectedAction'
  | 'controllerNotes'
  | 'injectNumber'

/**
 * Result of a search match indicating where the match was found
 */
export interface SearchMatch {
  /** The inject ID that matched */
  injectId: string
  /** Fields that contained matches */
  matchedFields: SearchableField[]
}

/**
 * All searchable fields in order of importance
 */
export const SEARCHABLE_FIELDS: readonly SearchableField[] = [
  'title',
  'description',
  'target',
  'source',
  'expectedAction',
  'controllerNotes',
  'injectNumber',
] as const

/**
 * Debounce delay for search input (milliseconds)
 */
export const SEARCH_DEBOUNCE_MS = 300

// =============================================================================
// Combined Organization State
// =============================================================================

/**
 * Complete inject organization state
 */
export interface InjectOrganizationState {
  /** Current search term (raw input) */
  searchTerm: string
  /** Debounced search term (for filtering) */
  debouncedSearchTerm: string
  /** Current filter configuration */
  filters: FilterState
  /** Current sort configuration */
  sort: SortConfig
  /** Current grouping mode */
  groupBy: GroupBy
  /** Set of expanded group IDs */
  expandedGroups: Set<string>
  /** Whether expanded groups were restored from session storage */
  hasPersistedExpandedGroups: boolean
}

/**
 * Actions available for inject organization
 */
export interface InjectOrganizationActions {
  // Search
  setSearchTerm: (term: string) => void
  clearSearch: () => void

  // Filters
  setStatusFilter: (statuses: InjectStatus[]) => void
  setPhaseFilter: (phaseIds: (string | null)[]) => void
  setMethodFilter: (methods: DeliveryMethod[]) => void
  setObjectiveFilter: (objectiveIds: (string | null)[]) => void
  clearFilter: (filterType: FilterType) => void
  clearAllFilters: () => void

  // Sorting
  toggleSort: (column: SortableColumn) => void
  clearSort: () => void

  // Grouping
  setGroupBy: (groupBy: GroupBy) => void
  toggleGroupExpanded: (groupId: string) => void
  expandAllGroups: () => void
  collapseAllGroups: () => void
  /** Initialize expanded groups (only works if no persisted state) */
  initializeExpandedGroups: (groupIds: string[]) => void
}

/**
 * Complete context value combining state and actions
 */
export type InjectOrganizationContextValue =
  InjectOrganizationState & InjectOrganizationActions

// =============================================================================
// Session Storage Key
// =============================================================================

/**
 * Generate session storage key for an exercise's organization state
 */
export const getStorageKey = (exerciseId: string): string =>
  `cadence:inject-org:${exerciseId}`

/**
 * Persisted state shape for sessionStorage
 */
export interface PersistedOrganizationState {
  searchTerm: string
  filters: FilterState
  sort: SortConfig
  groupBy: GroupBy
  /** Expanded group IDs (stored as array for JSON serialization) */
  expandedGroupIds?: string[]
}
