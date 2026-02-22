import { useState, useMemo, useRef, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Skeleton,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Collapse,
  Portal,
  Checkbox,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faArrowLeft,
  faMagnifyingGlass,
  faListCheck,
  faPlay,
  faForwardStep,
  faHome,
  faFileImport,
  faFileExport,
  faPen,
  faTrash,
  faCopy,
} from '@fortawesome/free-solid-svg-icons'

/** Minimum time to show the saving indicator (ms) */
const MIN_INDICATOR_TIME = 1000

import { useInjects, useInjectOrganization, useInjectSelection } from '../hooks'
import { InjectOrganizationProvider } from '../contexts/InjectOrganizationContext'
import { useBreadcrumbs } from '../../../core/contexts'
import { useExercise } from '../../exercises/hooks/useExercise'
import { usePhases } from '../../phases/hooks'
import { useObjectiveSummaries } from '../../objectives/hooks'
import { PhaseFormDialog } from '../../phases/components'
import {
  InjectStatusChip,
  InjectFilterBar,
  ActiveFiltersBar,
  SortableTableHeader,
  GroupHeader,
  SortableInjectList,
  SortableInjectRow,
  BatchApprovalToolbar,
  SubmitForApprovalButton,
} from '../components'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { HighlightedText } from '../../../shared/components/HighlightedText'
import CobraStyles from '../../../theme/CobraStyles'
import { useExerciseRole } from '../../auth/hooks/useExerciseRole'
import { ApprovalStatusHeader } from '../../exercises/components'
import { useApprovalSettings } from '../../exercises/hooks/useApprovalSettings'
import { useAuth } from '../../../contexts'
import { InjectStatus } from '../../../types'
import type { InjectDto } from '../types'
import type { PhaseDto } from '../../phases/types'
import type { SortConfig, SortableColumn, InjectGroup } from '../types/organization'
import { formatScenarioTime, formatScheduledTime } from '../types'

import { isGroupExpanded } from '../utils/groupUtils'
import { ImportWizard } from '../../excel-import/components'
import { ExportDialog } from '../../excel-export/components'
import { PageHeader } from '@/shared/components'

/**
 * Check if the current sort order allows drag-and-drop reordering.
 * Reordering is allowed when:
 * - No sort is applied (column is null), OR
 * - Sorted by scheduledTime ascending (which matches sequence order)
 */
const isSequenceOrder = (sort: SortConfig): boolean => {
  // No explicit sort - use default sequence order
  if (sort.column === null || sort.direction === null) {
    return true
  }
  // Sorted by scheduled time ascending (matches sequence order)
  if (sort.column === 'scheduledTime' && sort.direction === 'asc') {
    return true
  }
  return false
}

/**
 * MSEL (Inject List) Page
 *
 * Displays all injects for an exercise with:
 * - Filtering by status, phase, and delivery method
 * - Sorting by multiple columns
 * - Grouping by phase or status
 * - Text search across multiple fields
 * - Fire/Skip actions for Controllers
 */
export const InjectListPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()

  if (!exerciseId) {
    return null
  }

  return (
    <InjectOrganizationProvider exerciseId={exerciseId}>
      <InjectListPageContent exerciseId={exerciseId} />
    </InjectOrganizationProvider>
  )
}

interface InjectListPageContentProps {
  exerciseId: string
}

const InjectListPageContent = ({ exerciseId }: InjectListPageContentProps) => {
  const navigate = useNavigate()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId)
  const {
    injects,
    loading,
    error,
    fireInject,
    skipInject,
    deleteInject,
    isFiring,
    isSkipping,
    isDeleting,
    fetchInjects,
    reorderInjects,
    isReordering: isReorderingInjects,
  } = useInjects(exerciseId)
  const {
    phases,
    fetchPhases,
    createPhase,
    updatePhase,
    deletePhase,
    movePhaseUp,
    movePhaseDown,
    isCreating: isCreatingPhase,
    isUpdating: isUpdatingPhase,
    isDeleting: isDeletingPhase,
    isReordering: isReorderingPhase,
  } = usePhases(exerciseId)
  const { summaries: objectives } = useObjectiveSummaries(exerciseId)
  const { can } = useExerciseRole(exerciseId)
  const canFireInjects = can('fire_inject')
  const canManage = can('edit_inject')

  // Approval workflow state (S11-S13)
  const { settings: approvalSettings } = useApprovalSettings(exerciseId)
  const { user } = useAuth()
  const approvalEnabled = approvalSettings?.requireInjectApproval ?? false

  // Convert phases to the format needed by useInjectOrganization
  const phaseInfo = useMemo(
    () => phases.map(p => ({ id: p.id, name: p.name, sequence: p.sequence })),
    [phases],
  )

  // Convert objectives to the format needed by useInjectOrganization
  const objectiveInfo = useMemo(
    () => objectives.map(o => ({ id: o.id, objectiveNumber: o.objectiveNumber, name: o.name })),
    [objectives],
  )

  // Organization hook
  const organization = useInjectOrganization(injects, phaseInfo, objectiveInfo)

  // Selection hook for batch operations (S12)
  const {
    selectedIds,
    toggleSelection,
    selectAll,
    clearSelection,
    selectionState,
  } = useInjectSelection({ injects: organization.organizedInjects })

  // Set custom breadcrumbs with exercise name and MSEL
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'MSEL' },
      ]
      : undefined,
  )

  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipInjectId, setSkipInjectId] = useState<string | null>(null)
  const [skipReason, setSkipReason] = useState('')

  // Delete confirmation dialog state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [deleteInjectData, setDeleteInjectData] = useState<{
    id: string
    title: string
  } | null>(null)

  // Phase form dialog state
  const [phaseFormOpen, setPhaseFormOpen] = useState(false)
  const [editingPhase, setEditingPhase] = useState<PhaseDto | null>(null)

  // Import wizard state
  const [importWizardOpen, setImportWizardOpen] = useState(false)

  // Export dialog state
  const [exportDialogOpen, setExportDialogOpen] = useState(false)

  // Centralized saving indicator state for drag-and-drop reordering
  const [showSavingIndicator, setShowSavingIndicator] = useState(false)
  const savingTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Cleanup timeout on unmount
  const handleSavingChange = useCallback((isSaving: boolean) => {
    if (isSaving) {
      // Clear any existing timeout
      if (savingTimeoutRef.current) {
        clearTimeout(savingTimeoutRef.current)
        savingTimeoutRef.current = null
      }
      setShowSavingIndicator(true)
    } else {
      // Calculate remaining time for minimum display
      savingTimeoutRef.current = setTimeout(() => {
        setShowSavingIndicator(false)
        savingTimeoutRef.current = null
      }, MIN_INDICATOR_TIME)
    }
  }, [])

  const isPhaseLoading = isCreatingPhase || isUpdatingPhase || isDeletingPhase || isReorderingPhase

  const handleRowClick = (injectId: string) => {
    navigate(`/exercises/${exerciseId}/injects/${injectId}`)
  }

  const handleCreateClick = () => {
    navigate(`/exercises/${exerciseId}/injects/new`)
  }

  const handleBackClick = () => {
    navigate(`/exercises/${exerciseId}`)
  }

  const handleFireClick = async (e: React.MouseEvent, injectId: string) => {
    e.stopPropagation()
    await fireInject(injectId)
  }

  const handleSkipClick = (e: React.MouseEvent, injectId: string) => {
    e.stopPropagation()
    setSkipInjectId(injectId)
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  const handleSkipConfirm = async () => {
    if (skipInjectId && skipReason.trim()) {
      await skipInject(skipInjectId, { reason: skipReason.trim() })
      setSkipDialogOpen(false)
      setSkipInjectId(null)
      setSkipReason('')
    }
  }

  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipInjectId(null)
    setSkipReason('')
  }

  // Edit and delete handlers
  const handleEditClick = (e: React.MouseEvent, injectId: string) => {
    e.stopPropagation()
    navigate(`/exercises/${exerciseId}/injects/${injectId}/edit`)
  }

  const handleDuplicateClick = (e: React.MouseEvent, inject: InjectDto) => {
    e.stopPropagation()
    navigate(`/exercises/${exerciseId}/injects/new`, {
      state: { duplicateFrom: inject },
    })
  }

  const handleDeleteClick = (e: React.MouseEvent, inject: InjectDto) => {
    e.stopPropagation()
    setDeleteInjectData({ id: inject.id, title: inject.title })
    setDeleteDialogOpen(true)
  }

  const handleDeleteConfirm = async () => {
    if (deleteInjectData) {
      await deleteInject(deleteInjectData.id)
      setDeleteDialogOpen(false)
      setDeleteInjectData(null)
    }
  }

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false)
    setDeleteInjectData(null)
  }

  // Phase management handlers
  const handleAddPhaseClick = () => {
    setEditingPhase(null)
    setPhaseFormOpen(true)
  }

  const handleEditPhase = (phase: PhaseDto) => {
    setEditingPhase(phase)
    setPhaseFormOpen(true)
  }

  const handleDeletePhase = async (phase: PhaseDto) => {
    await deletePhase(phase.id)
  }

  const handlePhaseFormClose = () => {
    setPhaseFormOpen(false)
    setEditingPhase(null)
  }

  const handlePhaseFormSave = async (values: { name: string; description: string }) => {
    if (editingPhase) {
      await updatePhase(editingPhase.id, {
        name: values.name,
        description: values.description || null,
      })
    } else {
      await createPhase({
        name: values.name,
        description: values.description || null,
      })
    }
    handlePhaseFormClose()
  }

  // Error state
  if (error && injects.length === 0) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error" gutterBottom>
          {error}
        </Typography>
        <CobraPrimaryButton onClick={() => window.location.reload()}>
          Retry
        </CobraPrimaryButton>
      </Box>
    )
  }

  const isLoading = loading || exerciseLoading

  // Phase options for filter dropdown
  const phaseOptions = phases.map(p => ({ id: p.id, name: p.name }))

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="MSEL"
        icon={faListCheck}
        showBackButton
        onBackClick={handleBackClick}
        subtitle={exerciseLoading ? <Skeleton width={200} /> : exercise?.name || 'Exercise'}
        actions={(canFireInjects || canManage) ? (
          <>
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faFileImport} />}
              onClick={() => setImportWizardOpen(true)}
            >
              Import
            </CobraSecondaryButton>
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faFileExport} />}
              onClick={() => setExportDialogOpen(true)}
            >
              Export
            </CobraSecondaryButton>
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleAddPhaseClick}
            >
              Add Phase
            </CobraSecondaryButton>
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faPlus} />}
              onClick={handleCreateClick}
            >
              New Inject
            </CobraPrimaryButton>
          </>
        ) : undefined}
      />

      {/* Approval Status Header (S06) - Shows approval progress if enabled */}
      <ApprovalStatusHeader exerciseId={exerciseId} showDetails />

      {/* Filter Bar */}
      <Box marginBottom={2}>
        <InjectFilterBar
          searchTerm={organization.searchTerm}
          onSearchChange={organization.setSearchTerm}
          onSearchClear={organization.clearSearch}
          filters={organization.filters}
          onStatusChange={organization.setStatusFilter}
          onPhaseChange={organization.setPhaseFilter}
          onMethodChange={organization.setMethodFilter}
          onObjectiveChange={organization.setObjectiveFilter}
          groupBy={organization.groupBy}
          onGroupByChange={organization.setGroupBy}
          phases={phaseOptions}
          objectives={objectives}
          showGroupControls={organization.groupBy !== 'none'}
          onExpandAll={organization.expandAllGroups}
          onCollapseAll={organization.collapseAllGroups}
        />
      </Box>

      {/* Active Filters Bar */}
      {organization.hasActiveFilters && (
        <Box marginBottom={2}>
          <ActiveFiltersBar
            filters={organization.activeFilterLabels}
            totalCount={organization.totalCount}
            filteredCount={organization.filteredCount}
            onRemoveFilter={organization.clearFilter}
            onClearAll={organization.clearAllFilters}
          />
        </Box>
      )}

      {/* Batch Approval Toolbar (S12) - only shown when approval enabled and items selected */}
      {approvalEnabled && selectedIds.length > 0 && (
        <BatchApprovalToolbar
          selectedIds={selectedIds}
          injects={organization.organizedInjects}
          exerciseId={exerciseId}
          currentUserId={user?.id || ''}
          selfApprovalPolicy={approvalSettings?.selfApprovalPolicy}
          onClearSelection={clearSelection}
        />
      )}

      {/* Loading skeleton state */}
      {isLoading && injects.length === 0 ? (
        <InjectTableSkeleton />
      ) : organization.filteredCount === 0 ? (
        <EmptyState
          hasInjects={injects.length > 0}
          canCreate={canFireInjects || canManage}
          onCreateClick={handleCreateClick}
          hasFilters={organization.hasActiveFilters}
          onClearFilters={organization.clearAllFilters}
        />
      ) : organization.groups ? (
        // Grouped view with drag-and-drop within groups
        <GroupedInjectView
          groups={organization.groups}
          getInjectsForGroup={organization.getInjectsForGroup}
          expandedGroups={organization.expandedGroups}
          toggleGroupExpanded={organization.toggleGroupExpanded}
          groupBy={organization.groupBy}
          phases={phases}
          canManage={canManage}
          canFireInjects={canFireInjects}
          canReorder={
            canManage &&
            !organization.hasActiveFilters &&
            !organization.debouncedSearchTerm &&
            isSequenceOrder(organization.sort)
          }
          onRowClick={handleRowClick}
          onFire={handleFireClick}
          onSkip={handleSkipClick}
          onEdit={handleEditClick}
          onDuplicate={handleDuplicateClick}
          onDelete={handleDeleteClick}
          onReorder={reorderInjects}
          onEditPhase={handleEditPhase}
          onDeletePhase={handleDeletePhase}
          onMovePhaseUp={movePhaseUp}
          onMovePhaseDown={movePhaseDown}
          isFiring={isFiring}
          isSkipping={isSkipping}
          isReordering={isReorderingInjects}
          isPhaseLoading={isPhaseLoading}
          searchTerm={organization.debouncedSearchTerm}
          sort={organization.sort}
          onSort={organization.toggleSort}
          showSavingIndicator={showSavingIndicator}
          onSavingChange={handleSavingChange}
          // Approval workflow props (S12-S13)
          approvalEnabled={approvalEnabled}
          exerciseId={exerciseId}
          selectedIds={selectedIds}
          onToggleSelection={toggleSelection}
          onSelectAll={selectionState === 'all' ? clearSelection : selectAll}
          selectionState={selectionState}
        />
      ) : (
        // Flat list view (no grouping) with drag-and-drop reordering
        <FlatInjectList
          injects={organization.organizedInjects}
          onRowClick={handleRowClick}
          canFireInjects={canFireInjects}
          canManage={canManage}
          canReorder={
            canManage &&
            !organization.hasActiveFilters &&
            !organization.debouncedSearchTerm &&
            isSequenceOrder(organization.sort)
          }
          onFire={handleFireClick}
          onSkip={handleSkipClick}
          onEdit={handleEditClick}
          onDuplicate={handleDuplicateClick}
          onDelete={handleDeleteClick}
          onReorder={reorderInjects}
          isFiring={isFiring}
          isSkipping={isSkipping}
          isReordering={isReorderingInjects}
          searchTerm={organization.debouncedSearchTerm}
          sort={organization.sort}
          onSort={organization.toggleSort}
          showSavingIndicator={showSavingIndicator}
          onSavingChange={handleSavingChange}
          // Approval workflow props (S12-S13)
          approvalEnabled={approvalEnabled}
          exerciseId={exerciseId}
          selectedIds={selectedIds}
          onToggleSelection={toggleSelection}
          onSelectAll={selectionState === 'all' ? clearSelection : selectAll}
          selectionState={selectionState}
        />
      )}

      {/* Centralized Saving Indicator for drag-and-drop reordering */}
      {showSavingIndicator && (
        <Portal>
          <Box
            sx={{
              position: 'fixed',
              top: 0,
              left: 0,
              right: 0,
              bottom: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(255, 255, 255, 0.6)',
              backdropFilter: 'blur(2px)',
              zIndex: 1300,
            }}
          >
            <Box
              sx={{
                bgcolor: 'background.paper',
                border: '1px solid',
                borderColor: 'divider',
                px: 3,
                py: 2,
                borderRadius: 2,
                boxShadow: '0 4px 20px rgba(0, 0, 0, 0.15)',
              }}
            >
              <Typography variant="body1">
                Saving changes...
              </Typography>
            </Box>
          </Box>
        </Portal>
      )}

      {/* Skip Reason Dialog */}
      <Dialog open={skipDialogOpen} onClose={handleSkipCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" marginBottom={2}>
            Please provide a reason for skipping this inject. This will be recorded
            for the after-action report.
          </Typography>
          <CobraTextField
            label="Skip Reason"
            value={skipReason}
            onChange={e => setSkipReason(e.target.value)}
            multiline
            rows={3}
            fullWidth
            required
            placeholder="e.g., Time constraints, players ahead of schedule, etc."
          />
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleSkipCancel}>Cancel</CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleSkipConfirm}
            disabled={!skipReason.trim() || isSkipping}
          >
            Skip Inject
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={handleDeleteCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Delete Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary">
            Are you sure you want to delete this inject?
          </Typography>
          {deleteInjectData && (
            <Typography variant="body1" fontWeight={500} marginTop={1}>
              "{deleteInjectData.title}"
            </Typography>
          )}
          <Typography variant="body2" color="text.secondary" marginTop={2}>
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleDeleteCancel}>Cancel</CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleDeleteConfirm}
            disabled={isDeleting}
            color="error"
          >
            Delete
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>

      {/* Phase Form Dialog */}
      <PhaseFormDialog
        open={phaseFormOpen}
        phase={editingPhase}
        onClose={handlePhaseFormClose}
        onSubmit={handlePhaseFormSave}
        isSubmitting={isCreatingPhase || isUpdatingPhase}
      />

      {/* Import Wizard */}
      <ImportWizard
        open={importWizardOpen}
        onClose={() => {
          // Refresh both phases and injects when wizard closes (import may create phases)
          fetchPhases()
          fetchInjects()
          setImportWizardOpen(false)
        }}
        exerciseId={exerciseId}
      />

      {/* Export Dialog */}
      <ExportDialog
        open={exportDialogOpen}
        onClose={() => setExportDialogOpen(false)}
        exerciseId={exerciseId}
        exerciseName={exercise?.name || 'Exercise'}
      />
    </Box>
  )
}

/**
 * Loading skeleton for the inject table
 */
const InjectTableSkeleton = () => {
  const skeletonRows = Array.from({ length: 5 }, (_, i) => i)

  return (
    <TableContainer component={Paper}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>Scheduled</TableCell>
            <TableCell>Scenario</TableCell>
            <TableCell>Title</TableCell>
            <TableCell>Target</TableCell>
            <TableCell>Method</TableCell>
            <TableCell>Status</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {skeletonRows.map(index => (
            <TableRow key={index}>
              <TableCell>
                <Skeleton variant="text" width={30} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={70} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={70} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={200} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={100} />
              </TableCell>
              <TableCell>
                <Skeleton variant="text" width={60} />
              </TableCell>
              <TableCell>
                <Skeleton variant="rounded" width={70} height={24} />
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  )
}

/**
 * Grouped inject view with drag-and-drop reordering within each group
 */
interface GroupedInjectViewProps {
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
  // Approval workflow props (S12-S13)
  approvalEnabled?: boolean
  exerciseId: string
  selectedIds?: string[]
  onToggleSelection?: (id: string) => void
  onSelectAll?: () => void
  selectionState?: 'none' | 'some' | 'all'
}

const GroupedInjectView = ({
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
  // Approval workflow props (S12-S13)
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
                    <InjectRowCells
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
                      // Approval props (S12-S13)
                      approvalEnabled={approvalEnabled}
                      exerciseId={exerciseId}
                      isSelected={selectedIds.includes(inject.id)}
                      onToggleSelection={onToggleSelection}
                    />
                  </SortableInjectRow>
                ) : (
                  <InjectRow
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
                    // Approval props (S12-S13)
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

/**
 * Flat inject list with drag-and-drop reordering support
 */
interface FlatInjectListProps {
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
  // Approval workflow props (S12-S13)
  approvalEnabled?: boolean
  exerciseId: string
  selectedIds?: string[]
  onToggleSelection?: (id: string) => void
  onSelectAll?: () => void
  selectionState?: 'none' | 'some' | 'all'
}

const FlatInjectList = ({
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
  // Approval workflow props (S12-S13)
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
                <InjectRowCells
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
                  // Approval props (S12-S13)
                  approvalEnabled={approvalEnabled}
                  exerciseId={exerciseId}
                  isSelected={selectedIds.includes(inject.id)}
                  onToggleSelection={onToggleSelection}
                />
              </SortableInjectRow>
            ) : (
              <InjectRow
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
                // Approval props (S12-S13)
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

interface InjectRowCellsProps {
  inject: InjectDto
  onClick: () => void
  canFireInjects: boolean
  canManage: boolean
  onFire: (e: React.MouseEvent) => void
  onSkip: (e: React.MouseEvent) => void
  onEdit: (e: React.MouseEvent) => void
  onDuplicate: (e: React.MouseEvent) => void
  onDelete: (e: React.MouseEvent) => void
  isFiring: boolean
  isSkipping: boolean
  searchTerm?: string
  showPhase?: boolean
  // Approval workflow props (S12-S13)
  approvalEnabled?: boolean
  exerciseId?: string
  isSelected?: boolean
  onToggleSelection?: (id: string) => void
}

/**
 * Table cells for an inject row (without the TableRow wrapper)
 * Used by SortableInjectRow which provides its own TableRow
 */
const InjectRowCells = ({
  inject,
  onClick,
  canFireInjects,
  canManage,
  onFire,
  onSkip,
  onEdit,
  onDuplicate,
  onDelete,
  isFiring,
  isSkipping,
  searchTerm = '',
  showPhase = false,
  // Approval workflow props (S12-S13)
  approvalEnabled = false,
  exerciseId = '',
  isSelected = false,
  onToggleSelection,
}: InjectRowCellsProps) => {
  const scenarioTimeDisplay = formatScenarioTime(inject.scenarioDay, inject.scenarioTime)
  const scheduledTimeDisplay = formatScheduledTime(inject.scheduledTime)
  const isPending = inject.status === InjectStatus.Draft

  const handleCheckboxClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    onToggleSelection?.(inject.id)
  }

  return (
    <>
      {/* Checkbox cell for batch selection (S12) */}
      {approvalEnabled && onToggleSelection && (
        <TableCell padding="checkbox" onClick={handleCheckboxClick}>
          <Checkbox
            checked={isSelected}
            inputProps={{ 'aria-label': `Select inject ${inject.injectNumber}` }}
          />
        </TableCell>
      )}
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography variant="body2" fontWeight={500}>
          {inject.injectNumber}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography variant="body2">{scheduledTimeDisplay}</Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          color={scenarioTimeDisplay ? 'text.primary' : 'text.secondary'}
        >
          {scenarioTimeDisplay ?? '—'}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          sx={{
            maxWidth: 300,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          <HighlightedText text={inject.title} searchTerm={searchTerm} />
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          color={inject.target ? 'text.primary' : 'text.secondary'}
          sx={{
            maxWidth: 140,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {inject.target ?? '—'}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <Typography
          variant="body2"
          color={inject.deliveryMethod ? 'text.primary' : 'text.secondary'}
        >
          {inject.deliveryMethod ?? '—'}
        </Typography>
      </TableCell>
      <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
        <InjectStatusChip status={inject.status} />
      </TableCell>
      {showPhase && (
        <TableCell onClick={onClick} sx={{ cursor: 'pointer' }}>
          <Typography
            variant="body2"
            color={inject.phaseName ? 'text.primary' : 'text.secondary'}
            sx={{
              maxWidth: 120,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {inject.phaseName ?? 'Unassigned'}
          </Typography>
        </TableCell>
      )}
      {(canFireInjects || canManage || approvalEnabled) && (
        <TableCell>
          <Stack direction="row" spacing={0.5}>
            {/* Quick Submit button (S13) - shown first for Draft injects when approval enabled */}
            {approvalEnabled && exerciseId && canManage && (
              <SubmitForApprovalButton
                inject={inject}
                exerciseId={exerciseId}
                approvalEnabled={approvalEnabled}
                canSubmit={canManage}
                size="small"
              />
            )}
            {/* Fire/Skip - only for Draft (when no approval) or Approved injects */}
            {canFireInjects && (
              // When approval enabled: only show for Approved injects
              // When approval disabled: show for Draft injects (isPending)
              (approvalEnabled ? inject.status === InjectStatus.Approved : isPending) && (
                <>
                  <Tooltip title="Fire inject">
                    <IconButton
                      size="small"
                      color="success"
                      onClick={onFire}
                      disabled={isFiring || isSkipping}
                    >
                      <FontAwesomeIcon icon={faPlay} size="sm" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Skip inject">
                    <IconButton
                      size="small"
                      color="warning"
                      onClick={onSkip}
                      disabled={isFiring || isSkipping}
                    >
                      <FontAwesomeIcon icon={faForwardStep} size="sm" />
                    </IconButton>
                  </Tooltip>
                </>
              )
            )}
            {canManage && (
              <>
                <Tooltip title="Edit inject">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={onEdit}
                  >
                    <FontAwesomeIcon icon={faPen} size="sm" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Duplicate inject">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={onDuplicate}
                  >
                    <FontAwesomeIcon icon={faCopy} size="sm" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Delete inject">
                  <IconButton
                    size="small"
                    color="error"
                    onClick={onDelete}
                  >
                    <FontAwesomeIcon icon={faTrash} size="sm" />
                  </IconButton>
                </Tooltip>
              </>
            )}
          </Stack>
        </TableCell>
      )}
    </>
  )
}

interface InjectRowProps {
  inject: InjectDto
  onClick: () => void
  canFireInjects: boolean
  canManage: boolean
  onFire: (e: React.MouseEvent) => void
  onSkip: (e: React.MouseEvent) => void
  onEdit: (e: React.MouseEvent) => void
  onDuplicate: (e: React.MouseEvent) => void
  onDelete: (e: React.MouseEvent) => void
  isFiring: boolean
  isSkipping: boolean
  searchTerm?: string
  showPhase?: boolean
  // Approval workflow props (S12-S13)
  approvalEnabled?: boolean
  exerciseId?: string
  isSelected?: boolean
  onToggleSelection?: (id: string) => void
}

const InjectRow = ({
  inject,
  onClick,
  canFireInjects,
  canManage,
  onFire,
  onSkip,
  onEdit,
  onDuplicate,
  onDelete,
  isFiring,
  isSkipping,
  searchTerm = '',
  showPhase = false,
  // Approval workflow props (S12-S13)
  approvalEnabled = false,
  exerciseId = '',
  isSelected = false,
  onToggleSelection,
}: InjectRowProps) => {
  const scenarioTimeDisplay = formatScenarioTime(inject.scenarioDay, inject.scenarioTime)
  const scheduledTimeDisplay = formatScheduledTime(inject.scheduledTime)
  const isPending = inject.status === InjectStatus.Draft

  const handleCheckboxClick = (e: React.MouseEvent) => {
    e.stopPropagation()
    onToggleSelection?.(inject.id)
  }

  return (
    <TableRow
      hover
      onClick={onClick}
      sx={{
        cursor: 'pointer',
        '& td': { minHeight: 44 },
        // Dim fired/skipped rows slightly
        opacity: isPending ? 1 : 0.85,
      }}
    >
      {/* Checkbox cell for batch selection (S12) */}
      {approvalEnabled && onToggleSelection && (
        <TableCell padding="checkbox" onClick={handleCheckboxClick}>
          <Checkbox
            checked={isSelected}
            inputProps={{ 'aria-label': `Select inject ${inject.injectNumber}` }}
          />
        </TableCell>
      )}
      <TableCell>
        <Typography variant="body2" fontWeight={500}>
          {inject.injectNumber}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography variant="body2">{scheduledTimeDisplay}</Typography>
      </TableCell>
      <TableCell>
        <Typography
          variant="body2"
          color={scenarioTimeDisplay ? 'text.primary' : 'text.secondary'}
        >
          {scenarioTimeDisplay ?? '—'}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography
          variant="body2"
          sx={{
            maxWidth: 300,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          <HighlightedText text={inject.title} searchTerm={searchTerm} />
        </Typography>
      </TableCell>
      <TableCell>
        <Typography
          variant="body2"
          color={inject.target ? 'text.primary' : 'text.secondary'}
          sx={{
            maxWidth: 140,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {inject.target ?? '—'}
        </Typography>
      </TableCell>
      <TableCell>
        <Typography
          variant="body2"
          color={inject.deliveryMethod ? 'text.primary' : 'text.secondary'}
        >
          {inject.deliveryMethod ?? '—'}
        </Typography>
      </TableCell>
      <TableCell>
        <InjectStatusChip status={inject.status} />
      </TableCell>
      {showPhase && (
        <TableCell>
          <Typography
            variant="body2"
            color={inject.phaseName ? 'text.primary' : 'text.secondary'}
            sx={{
              maxWidth: 120,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {inject.phaseName ?? 'Unassigned'}
          </Typography>
        </TableCell>
      )}
      {(canFireInjects || canManage || approvalEnabled) && (
        <TableCell>
          <Stack direction="row" spacing={0.5}>
            {/* Quick Submit button (S13) - shown first for Draft injects when approval enabled */}
            {approvalEnabled && exerciseId && canManage && (
              <SubmitForApprovalButton
                inject={inject}
                exerciseId={exerciseId}
                approvalEnabled={approvalEnabled}
                canSubmit={canManage}
                size="small"
              />
            )}
            {/* Fire/Skip - only for Draft (when no approval) or Approved injects */}
            {canFireInjects && (
              // When approval enabled: only show for Approved injects
              // When approval disabled: show for Draft injects (isPending)
              (approvalEnabled ? inject.status === InjectStatus.Approved : isPending) && (
                <>
                  <Tooltip title="Fire inject">
                    <IconButton
                      size="small"
                      color="success"
                      onClick={onFire}
                      disabled={isFiring || isSkipping}
                    >
                      <FontAwesomeIcon icon={faPlay} size="sm" />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="Skip inject">
                    <IconButton
                      size="small"
                      color="warning"
                      onClick={onSkip}
                      disabled={isFiring || isSkipping}
                    >
                      <FontAwesomeIcon icon={faForwardStep} size="sm" />
                    </IconButton>
                  </Tooltip>
                </>
              )
            )}
            {canManage && (
              <>
                <Tooltip title="Edit inject">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={onEdit}
                  >
                    <FontAwesomeIcon icon={faPen} size="sm" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Duplicate inject">
                  <IconButton
                    size="small"
                    color="primary"
                    onClick={onDuplicate}
                  >
                    <FontAwesomeIcon icon={faCopy} size="sm" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Delete inject">
                  <IconButton
                    size="small"
                    color="error"
                    onClick={onDelete}
                  >
                    <FontAwesomeIcon icon={faTrash} size="sm" />
                  </IconButton>
                </Tooltip>
              </>
            )}
          </Stack>
        </TableCell>
      )}
    </TableRow>
  )
}

interface EmptyStateProps {
  hasInjects: boolean
  canCreate: boolean
  onCreateClick: () => void
  hasFilters?: boolean
  onClearFilters?: () => void
}

const EmptyState = ({
  hasInjects,
  canCreate,
  onCreateClick,
  hasFilters = false,
  onClearFilters,
}: EmptyStateProps) => {
  if (hasInjects) {
    // Filtered to empty (search/filter result)
    return (
      <Paper
        sx={{
          py: 6,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'grey.50',
          border: '1px dashed',
          borderColor: 'grey.300',
        }}
      >
        <Box
          sx={{
            width: 80,
            height: 80,
            borderRadius: '50%',
            backgroundColor: 'grey.200',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 16px',
          }}
        >
          <FontAwesomeIcon
            icon={faMagnifyingGlass}
            style={{ fontSize: 40, color: '#9e9e9e' }}
          />
        </Box>
        <Typography variant="h6" gutterBottom>
          No matching injects
        </Typography>
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ maxWidth: 300, mx: 'auto', mb: hasFilters ? 2 : 0 }}
        >
          Try adjusting your search terms or filters to find the inject you're looking
          for.
        </Typography>
        {hasFilters && onClearFilters && (
          <CobraSecondaryButton onClick={onClearFilters}>
            Clear all filters
          </CobraSecondaryButton>
        )}
      </Paper>
    )
  }

  // No injects at all
  if (canCreate) {
    return (
      <Paper
        sx={{
          py: 8,
          px: 4,
          textAlign: 'center',
          backgroundColor: 'primary.50',
          border: '1px dashed',
          borderColor: 'primary.200',
        }}
      >
        <Box
          sx={{
            width: 100,
            height: 100,
            borderRadius: '50%',
            background: 'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 24px',
            boxShadow: '0 4px 20px rgba(33, 150, 243, 0.15)',
          }}
        >
          <FontAwesomeIcon icon={faListCheck} style={{ fontSize: 50, color: '#1976d2' }} />
        </Box>
        <Typography variant="h5" gutterBottom fontWeight={500}>
          Create Your First Inject
        </Typography>
        <Typography
          variant="body1"
          color="text.secondary"
          sx={{ maxWidth: 400, mx: 'auto', mb: 3 }}
        >
          Build out your MSEL by adding injects. Each inject represents an event,
          message, or action that will be delivered during exercise conduct.
        </Typography>
        <CobraPrimaryButton
          startIcon={<FontAwesomeIcon icon={faPlus} />}
          onClick={onCreateClick}
          size="large"
        >
          New Inject
        </CobraPrimaryButton>
      </Paper>
    )
  }

  // Viewer with no injects
  return (
    <Paper
      sx={{
        py: 6,
        px: 4,
        textAlign: 'center',
        backgroundColor: 'grey.50',
        border: '1px dashed',
        borderColor: 'grey.300',
      }}
    >
      <Box
        sx={{
          width: 80,
          height: 80,
          borderRadius: '50%',
          backgroundColor: 'grey.200',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          margin: '0 auto 16px',
        }}
      >
        <FontAwesomeIcon icon={faListCheck} style={{ fontSize: 40, color: '#9e9e9e' }} />
      </Box>
      <Typography variant="h6" gutterBottom>
        No Injects Yet
      </Typography>
      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ maxWidth: 350, mx: 'auto' }}
      >
        The MSEL for this exercise is empty. Controllers will add injects during
        exercise planning.
      </Typography>
    </Paper>
  )
}

export default InjectListPage
