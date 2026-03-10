/**
 * FlatInjectList
 *
 * Renders the MSEL inject list as a flat (ungrouped) table with optional
 * drag-and-drop reordering support.
 *
 * Shows all injects in a single table including a Phase column (not visible
 * in the grouped view since grouping provides that context).
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
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Checkbox,
} from '@mui/material'
import { SortableTableHeader } from './SortableTableHeader'
import { SortableInjectList, SortableInjectRow } from './drag-drop'
import { MselInjectRowCells } from './MselInjectRowCells'
import { MselInjectRow } from './MselInjectRow'
import type { InjectDto } from '../types'
import type { SortConfig, SortableColumn } from '../types/organization'

export interface FlatInjectListProps {
  injects: InjectDto[]
  onRowClick: (id: string) => void
  canFireInjects: boolean
  canManage: boolean
  canReorder: boolean
  onFire: (e: React.MouseEvent, id: string) => void
  onSkip: (e: React.MouseEvent, id: string) => void
  onEdit: (e: React.MouseEvent, id: string) => void
  onDuplicate: (e: React.MouseEvent, inject: InjectDto) => void
  onDelete: (e: React.MouseEvent, inject: InjectDto) => void
  onReorder: (injectIds: string[]) => Promise<void>
  isFiring: boolean
  isSkipping: boolean
  isReordering: boolean
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
 * Flat inject list with drag-and-drop reordering support
 */
export const FlatInjectList = ({
  injects,
  onRowClick,
  canFireInjects,
  canManage,
  canReorder,
  onFire,
  onSkip,
  onEdit,
  onDuplicate,
  onDelete,
  onReorder,
  isFiring,
  isSkipping,
  isReordering,
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
}: FlatInjectListProps) => {
  const tableContent = (orderedInjects: InjectDto[]) => (
    <TableContainer component={Paper}>
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
            <SortableTableHeader
              column="phase"
              label="Phase"
              activeColumn={sort.column}
              direction={sort.direction}
              onSort={onSort}
              width={120}
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
                  showPhase
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
                showPhase
                approvalEnabled={approvalEnabled}
                exerciseId={exerciseId}
                isSelected={selectedIds.includes(inject.id)}
                onToggleSelection={onToggleSelection}
              />
            )
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )

  if (canReorder) {
    return (
      <SortableInjectList
        injects={injects}
        onReorder={onReorder}
        disabled={isReordering}
        showSavingIndicator={showSavingIndicator}
        onSavingChange={onSavingChange}
      >
        {tableContent}
      </SortableInjectList>
    )
  }

  return tableContent(injects)
}
