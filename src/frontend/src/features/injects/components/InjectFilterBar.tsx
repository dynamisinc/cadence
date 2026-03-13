/**
 * InjectFilterBar Component
 *
 * Toolbar containing all inject organization controls:
 * - Search input
 * - Status filter dropdown
 * - Phase filter dropdown
 * - Method filter dropdown
 * - Group by dropdown
 */

import { useRef, useEffect } from 'react'
import { Box, Stack, InputAdornment, Tooltip } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faMagnifyingGlass,
  faXmark,
  faCircle,
  faFolder,
  faEnvelope,
  faExpand,
  faCompress,
  faCrosshairs,
} from '@fortawesome/free-solid-svg-icons'
import { CobraTextField, CobraLinkButton } from '../../../theme/styledComponents'
import { FilterDropdown, type FilterOption } from './FilterDropdown'
import { GroupByDropdown } from './GroupByDropdown'
import type { GroupBy, FilterState } from '../types/organization'
import type { InjectStatus, DeliveryMethod } from '../../../types'
import { InjectStatus as InjectStatusEnum, DeliveryMethod as DeliveryMethodEnum } from '../../../types'

export interface PhaseOption {
  id: string | null
  name: string
}

export interface ObjectiveOption {
  id: string
  objectiveNumber: string
  name: string
}

export interface InjectFilterBarProps {
  // Search
  searchTerm: string
  onSearchChange: (term: string) => void
  onSearchClear: () => void

  // Filters
  filters: FilterState
  onStatusChange: (statuses: InjectStatus[]) => void
  onPhaseChange: (phaseIds: (string | null)[]) => void
  onMethodChange: (methods: DeliveryMethod[]) => void
  onObjectiveChange: (objectiveIds: (string | null)[]) => void

  // Grouping
  groupBy: GroupBy
  onGroupByChange: (groupBy: GroupBy) => void

  // Phase options (from exercise)
  phases: PhaseOption[]

  // Objective options (from exercise)
  objectives?: ObjectiveOption[]

  // Group expand/collapse
  showGroupControls?: boolean
  onExpandAll?: () => void
  onCollapseAll?: () => void

  // Responsive
  /** When true, filter/group controls show icon-only with tooltips */
  compact?: boolean
}

// Status filter options
const statusOptions: FilterOption<InjectStatus>[] = [
  { value: InjectStatusEnum.Draft, label: 'Draft' },
  { value: InjectStatusEnum.Synchronized, label: 'Synchronized' },
  { value: InjectStatusEnum.Released, label: 'Released' },
  { value: InjectStatusEnum.Deferred, label: 'Deferred' },
]

// Delivery method filter options
const methodOptions: FilterOption<DeliveryMethod>[] = [
  { value: DeliveryMethodEnum.Email, label: 'Email' },
  { value: DeliveryMethodEnum.Phone, label: 'Phone' },
  { value: DeliveryMethodEnum.Radio, label: 'Radio' },
  { value: DeliveryMethodEnum.Verbal, label: 'Verbal' },
  { value: DeliveryMethodEnum.Written, label: 'Written' },
  { value: DeliveryMethodEnum.Simulation, label: 'Simulation' },
  { value: DeliveryMethodEnum.Other, label: 'Other' },
]

export const InjectFilterBar = ({
  searchTerm,
  onSearchChange,
  onSearchClear,
  filters,
  onStatusChange,
  onPhaseChange,
  onMethodChange,
  onObjectiveChange,
  groupBy,
  onGroupByChange,
  phases,
  objectives = [],
  showGroupControls = false,
  onExpandAll,
  onCollapseAll,
  compact: compactFilters = false,
}: InjectFilterBarProps) => {
  const theme = useTheme()
  const searchInputRef = useRef<HTMLInputElement>(null)

  // Build phase options including "Unassigned"
  const phaseOptions: FilterOption<string | null>[] = [
    ...phases.map(p => ({ value: p.id, label: p.name })),
    { value: null, label: 'Unassigned' },
  ]

  // Build objective options including "No objectives"
  const objectiveOptions: FilterOption<string | null>[] = [
    ...objectives.map(o => ({ value: o.id, label: `${o.objectiveNumber}: ${o.name}` })),
    { value: null, label: 'No objectives' },
  ]

  // Keyboard shortcut for search (Ctrl+F / Cmd+F)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
        e.preventDefault()
        searchInputRef.current?.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [])

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: 2,
      }}
    >
      {/* Left side: Search and filters */}
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ flex: 1, minWidth: 0, flexWrap: 'wrap' }}>
        {/* Search input */}
        <CobraTextField
          inputRef={searchInputRef}
          placeholder="Search injects..."
          value={searchTerm}
          onChange={e => onSearchChange(e.target.value)}
          size="small"
          sx={{ minWidth: 130, flex: '1 1 200px', maxWidth: { xs: '100%', md: 280, lg: 360 } }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <FontAwesomeIcon
                  icon={faMagnifyingGlass}
                  style={{ fontSize: '0.875rem', color: theme.palette.neutral[500] }}
                />
              </InputAdornment>
            ),
            endAdornment: searchTerm ? (
              <InputAdornment position="end">
                <Tooltip title="Clear search (Esc)">
                  <Box
                    component="button"
                    onClick={onSearchClear}
                    sx={{
                      border: 'none',
                      background: 'none',
                      cursor: 'pointer',
                      padding: 0.5,
                      display: 'flex',
                      alignItems: 'center',
                      color: 'text.secondary',
                      '&:hover': { color: 'text.primary' },
                    }}
                    aria-label="Clear search"
                  >
                    <FontAwesomeIcon icon={faXmark} style={{ fontSize: '0.875rem' }} />
                  </Box>
                </Tooltip>
              </InputAdornment>
            ) : null,
          }}
        />

        {/* Filter dropdowns */}
        <FilterDropdown
          label="Status"
          icon={faCircle}
          options={statusOptions}
          selected={filters.statuses}
          onChange={onStatusChange}
          compact={compactFilters}
        />

        <FilterDropdown
          label="Phase"
          icon={faFolder}
          options={phaseOptions}
          selected={filters.phaseIds}
          onChange={onPhaseChange}
          compact={compactFilters}
        />

        <FilterDropdown
          label="Method"
          icon={faEnvelope}
          options={methodOptions}
          selected={filters.deliveryMethods}
          onChange={onMethodChange}
          compact={compactFilters}
        />

        {objectives.length > 0 && (
          <FilterDropdown
            label="Objective"
            icon={faCrosshairs}
            options={objectiveOptions}
            selected={filters.objectiveIds}
            onChange={onObjectiveChange}
            compact={compactFilters}
          />
        )}
      </Stack>

      {/* Right side: Grouping and expand/collapse */}
      <Stack direction="row" spacing={2} alignItems="center">
        <GroupByDropdown value={groupBy} onChange={onGroupByChange} compact={compactFilters} />

        {showGroupControls && groupBy !== 'none' && (
          <Stack direction="row" spacing={0.5}>
            <Tooltip title="Expand all groups">
              <CobraLinkButton size="small" onClick={onExpandAll}>
                <FontAwesomeIcon icon={faExpand} style={{ fontSize: '0.875rem' }} />
              </CobraLinkButton>
            </Tooltip>
            <Tooltip title="Collapse all groups">
              <CobraLinkButton size="small" onClick={onCollapseAll}>
                <FontAwesomeIcon icon={faCompress} style={{ fontSize: '0.875rem' }} />
              </CobraLinkButton>
            </Tooltip>
          </Stack>
        )}
      </Stack>
    </Box>
  )
}

export default InjectFilterBar
