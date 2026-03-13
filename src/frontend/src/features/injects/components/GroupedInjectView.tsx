/**
 * GroupedInjectView
 *
 * Renders the MSEL inject list grouped by phase or status, with collapsible group
 * headers and optional drag-and-drop reordering within each group.
 *
 * Each group uses a GroupHeader with optional phase management controls
 * (edit, delete, move up/down) when grouped by phase.
 *
 * Supports:
 * - Sortable table headers
 * - Batch selection checkboxes (approval workflow S12)
 * - Fire/Skip actions for Controllers
 * - Drag-and-drop reordering when canReorder is true
 *
 * @module features/injects
 */
import {
  Box,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Collapse,
  Checkbox,
} from '@mui/material'
import { GroupHeader } from './GroupHeader'
import { SortableTableHeader } from './SortableTableHeader'
import { SortableInjectList, SortableInjectRow } from './drag-drop'
import { MselInjectRowCells } from './MselInjectRowCells'
import { MselInjectRow } from './MselInjectRow'
import { isGroupExpanded } from '../utils/groupUtils'
import type { InjectDto } from '../types'
import type { PhaseDto } from '../../phases/types'
import type { SortConfig, SortableColumn, InjectGroup } from '../types/organization'

export interface GroupedInjectViewProps {
  groups: InjectGroup[]
  getInjectsForGroup: (group: InjectGroup) => InjectDto[]
  expandedGroups: Set<string>
  toggleGroupExpanded: (groupId: string) => void
  groupBy: 'phase' | 'status' | 'none'
  phases: PhaseDto[]
  canManage: boolean
  canFireInjects: boolean
  canReorder: boolean
  onRowClick: (id: string) => void
  onFire: (e: React.MouseEvent, id: string) => void
  onSkip: (e: React.MouseEvent, id: string) => void
  onEdit: (e: React.MouseEvent, id: string) => void
  onDuplicate: (e: React.MouseEvent, inject: InjectDto) => void
  onDelete: (e: React.MouseEvent, inject: InjectDto) => void
  onReorder: (injectIds: string[]) => Promise<void>
  onEditPhase: (phase: PhaseDto) => void
  onDeletePhase: (phase: PhaseDto) => Promise<void>
  onMovePhaseUp: (phaseId: string) => void
  onMovePhaseDown: (phaseId: string) => void
  isFiring: boolean
  isSkipping: boolean
  isReordering: boolean
  isPhaseLoading: boolean
  searchTerm: string
  sort: SortConfig
  onSort: (column: SortableColumn) => void
  showSavingIndicator: boolean
  onSavingChange: (isSaving: boolean) => void
  /** Approval workflow props (S12-S13) */
  approvalEnabled?: boolean
  exerciseId: string
  selectedIds?: string[]
  onToggleSelection?: (id: string) => void
  onSelectAll?: () => void
  selectionState?: 'none' | 'some' | 'all'
}

/**
 * Grouped inject view with drag-and-drop reordering within each group
 */
export const GroupedInjectView = ({
  groups,
  getInjectsForGroup,
  expandedGroups,
  toggleGroupExpanded,
  groupBy,
  phases,
  canManage,
  canFireInjects,
  canReorder,
  onRowClick,
  onFire,
  onSkip,
  onEdit,
  onDuplicate,
  onDelete,
  onReorder,
  onEditPhase,
  onDeletePhase,
  onMovePhaseUp,
  onMovePhaseDown,
  isFiring,
  isSkipping,
  isReordering,
  isPhaseLoading,
  searchTerm,
  sort,
  onSort,
  showSavingIndicator,
  onSavingChange,
  approvalEnabled = false,
  exerciseId,
  selectedIds = [],
  onToggleSelection,
  onSelectAll,
  selectionState = 'none',
}: GroupedInjectViewProps) => {
  return (
    <Stack spacing={2}>
      {groups.map(group => {
        const groupInjects = getInjectsForGroup(group)
        const expanded = isGroupExpanded(expandedGroups, group.id)

        // Extract phase ID from group ID (format: "phase-{uuid}" or "phase-unassigned")
        const phaseId = group.id.startsWith('phase-') && group.id !== 'phase-unassigned'
          ? group.id.replace('phase-', '')
          : null

        // Get phase object for edit dialog
        const phase = phaseId ? phases.find(p => p.id === phaseId) : null

        // Calculate first/last for real phases only (excluding unassigned)
        const realPhaseGroups = groups.filter(
          g => g.id.startsWith('phase-') && g.id !== 'phase-unassigned',
        )
        const phaseGroupIndex = realPhaseGroups.findIndex(g => g.id === group.id)
        const isFirstPhase = phaseGroupIndex === 0
        const isLastPhase = phaseGroupIndex === realPhaseGroups.length - 1

        const tableContent = (orderedInjects: InjectDto[]) => (
          <Table size="small">
            <TableHead>
              <TableRow>
                {/* Checkbox column for batch selection (S12) */}
                {approvalEnabled && onToggleSelection && (
                  <TableCell padding="checkbox" sx={{ width: 48 }}>
                    <Checkbox
                      indeterminate={selectionState === 'some'}
                      checked={selectionState === 'all'}
                      onChange={onSelectAll}
                      inputProps={{ 'aria-label': 'Select all injects' }}
                    />
                  </TableCell>
                )}
                {canReorder && <TableCell width={40} />}
                <SortableTableHeader
                  column="injectNumber"
                  label="#"
                  activeColumn={sort.column}
                  direction={sort.direction}
                  onSort={onSort}
                  width={60}
                />
                <SortableTableHeader
                  column="scheduledTime"
                  label="Scheduled"
                  activeColumn={sort.column}
                  direction={sort.direction}
                  onSort={onSort}
                  width={100}
                />
                <SortableTableHeader
                  column="scenarioTime"
                  label="Scenario"
                  activeColumn={sort.column}
                  direction={sort.direction}
                  onSort={onSort}
                  width={100}
                />
                <SortableTableHeader
                  column="title"
                  label="Title"
                  activeColumn={sort.column}
                  direction={sort.direction}
                  onSort={onSort}
                />
                <TableCell width={140}>Target</TableCell>
                <TableCell width={90}>Method</TableCell>
                <SortableTableHeader
                  column="status"
                  label="Status"
                  activeColumn={sort.column}
                  direction={sort.direction}
                  onSort={onSort}
                  width={90}
                />
                {(canFireInjects || canManage) && <TableCell width={140}>Actions</TableCell>}
              </TableRow>
            </TableHead>
            <TableBody>
              {orderedInjects.map(inject => (
                canReorder ? (
                  <SortableInjectRow
                    key={inject.id}
                    inject={inject}
                    disabled={isReordering}
                  >
                    <MselInjectRowCells
                      inject={inject}
                      onClick={() => onRowClick(inject.id)}
                      canFireInjects={canFireInjects}
                      canManage={canManage}
                      onFire={e => onFire(e, inject.id)}
                      onSkip={e => onSkip(e, inject.id)}
                      onEdit={e => onEdit(e, inject.id)}
                      onDuplicate={e => onDuplicate(e, inject)}
                      onDelete={e => onDelete(e, inject)}
                      isFiring={isFiring}
                      isSkipping={isSkipping}
                      searchTerm={searchTerm}
                      approvalEnabled={approvalEnabled}
                      exerciseId={exerciseId}
                      isSelected={selectedIds.includes(inject.id)}
                      onToggleSelection={onToggleSelection}
                    />
                  </SortableInjectRow>
                ) : (
                  <MselInjectRow
                    key={inject.id}
                    inject={inject}
                    onClick={() => onRowClick(inject.id)}
                    canFireInjects={canFireInjects}
                    canManage={canManage}
                    onFire={e => onFire(e, inject.id)}
                    onSkip={e => onSkip(e, inject.id)}
                    onEdit={e => onEdit(e, inject.id)}
                    onDuplicate={e => onDuplicate(e, inject)}
                    onDelete={e => onDelete(e, inject)}
                    isFiring={isFiring}
                    isSkipping={isSkipping}
                    searchTerm={searchTerm}
                    approvalEnabled={approvalEnabled}
                    exerciseId={exerciseId}
                    isSelected={selectedIds.includes(inject.id)}
                    onToggleSelection={onToggleSelection}
                  />
                )
              ))}
            </TableBody>
          </Table>
        )

        return (
          <Box key={group.id}>
            <GroupHeader
              name={group.name}
              count={group.count}
              expanded={expanded}
              onToggle={() => toggleGroupExpanded(group.id)}
              groupBy={groupBy}
              statusValue={group.name}
              canManagePhases={canManage}
              phaseManagement={
                groupBy === 'phase' && phaseId
                  ? {
                    phaseId,
                    isFirst: isFirstPhase,
                    isLast: isLastPhase,
                    onEdit: () => phase && onEditPhase(phase),
                    onDelete: () => phase && onDeletePhase(phase),
                    onMoveUp: () => phaseId && onMovePhaseUp(phaseId),
                    onMoveDown: () => phaseId && onMovePhaseDown(phaseId),
                    isLoading: isPhaseLoading,
                  }
                  : undefined
              }
            />
            <Collapse in={expanded}>
              <TableContainer
                component={Paper}
                variant="outlined"
                sx={{
                  borderTopLeftRadius: 0,
                  borderTopRightRadius: 0,
                  borderTop: 0,
                }}
              >
                {canReorder ? (
                  <SortableInjectList
                    injects={groupInjects}
                    onReorder={onReorder}
                    disabled={isReordering}
                    showSavingIndicator={showSavingIndicator}
                    onSavingChange={onSavingChange}
                  >
                    {tableContent}
                  </SortableInjectList>
                ) : (
                  tableContent(groupInjects)
                )}
              </TableContainer>
            </Collapse>
          </Box>
        )
      })}
    </Stack>
  )
}
