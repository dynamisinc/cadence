/**
 * Inject Utils Index
 *
 * Central export for all inject utility functions.
 */

// Sort utilities
export {
  sortInjects,
  getNextSortDirection,
  toggleSortConfig,
  buildPhaseSequenceMap,
} from './sortUtils'

// Filter utilities
export {
  filterByStatus,
  filterByPhase,
  filterByMethod,
  applyFilters,
  countActiveFilters,
  hasActiveFilters,
  clearFilter,
  clearAllFilters,
  getActiveFilterLabels,
  buildPhaseNameMap,
} from './filterUtils'

// Search utilities
export {
  matchesSearch,
  filterBySearch,
  getMatchingFields,
  getSearchMatches,
  createSearchMatchMap,
  findMatchIndices,
  hasNonVisibleMatch,
  getMatchDescription,
  escapeRegex,
} from './searchUtils'

// Group utilities
export {
  groupByStatus,
  groupByPhase,
  groupInjects,
  getInjectsForGroup,
  getGroupIdForInject,
  initExpandedGroups,
  toggleGroupExpansion,
  expandAllGroups,
  collapseAllGroups,
  isGroupExpanded,
  getGroupByOptions,
} from './groupUtils'

// Clock-driven grouping utilities
export {
  groupInjectsForClockDriven,
  formatCountdown,
  UPCOMING_WINDOW_MS,
  type GroupedInjects,
} from './clockDrivenGrouping'
