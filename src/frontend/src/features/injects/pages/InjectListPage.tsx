import { useState, useMemo, useRef, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Skeleton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Portal,
  Tooltip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faListCheck,
  faHome,
  faFileImport,
  faFileExport,
  faLayerGroup,
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
  InjectFilterBar,
  ActiveFiltersBar,
  BatchApprovalToolbar,
} from '../components'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useExerciseRole } from '../../auth/hooks/useExerciseRole'
import { ApprovalStatusHeader } from '../../exercises/components'
import { useApprovalSettings } from '../../exercises/hooks/useApprovalSettings'
import { useAuth } from '../../../contexts'
import type { InjectDto } from '../types'
import type { PhaseDto } from '../../phases/types'
import type { SortConfig } from '../types/organization'

import { ImportWizard } from '../../excel-import/components'
import { ExportDialog } from '../../excel-export/components'
import { HelpTooltip, PageHeader } from '@/shared/components'
import { useContainerWidth } from '@/shared/hooks'
import { InjectTableSkeleton } from '../components/InjectTableSkeleton'
import { GroupedInjectView } from '../components/GroupedInjectView'
import { FlatInjectList } from '../components/FlatInjectList'
import { InjectEmptyState } from '../components/InjectEmptyState'

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

/** Content area width (px) below which buttons/filters switch to compact (icon-only) mode */
const COMPACT_BREAKPOINT = 800

const InjectListPageContent = ({ exerciseId }: InjectListPageContentProps) => {
  const navigate = useNavigate()
  const containerRef = useRef<HTMLDivElement>(null)
  const containerWidth = useContainerWidth(containerRef)
  const compactButtons = containerWidth > 0 && containerWidth < COMPACT_BREAKPOINT
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
  const { effectiveRole, can } = useExerciseRole(exerciseId)
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
    <Box ref={containerRef} padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="MSEL"
        icon={faListCheck}
        showBackButton
        onBackClick={handleBackClick}
        subtitle={exerciseLoading ? <Skeleton width={200} /> : exercise?.name || 'Exercise'}
        chips={<HelpTooltip helpKey="msel.overview" exerciseRole={effectiveRole ?? undefined} compact />}
        actions={(canFireInjects || canManage) ? (
          <>
            <Tooltip title="Import">
              <CobraSecondaryButton
                startIcon={compactButtons ? undefined : <FontAwesomeIcon icon={faFileImport} />}
                onClick={() => setImportWizardOpen(true)}
                aria-label="Import"
                size={compactButtons ? 'small' : 'medium'}
              >
                {compactButtons ? <FontAwesomeIcon icon={faFileImport} /> : 'Import'}
              </CobraSecondaryButton>
            </Tooltip>
            <Tooltip title="Export">
              <CobraSecondaryButton
                startIcon={compactButtons ? undefined : <FontAwesomeIcon icon={faFileExport} />}
                onClick={() => setExportDialogOpen(true)}
                aria-label="Export"
                size={compactButtons ? 'small' : 'medium'}
              >
                {compactButtons ? <FontAwesomeIcon icon={faFileExport} /> : 'Export'}
              </CobraSecondaryButton>
            </Tooltip>
            <Tooltip title="Add Phase">
              <CobraSecondaryButton
                startIcon={compactButtons ? undefined : <FontAwesomeIcon icon={faLayerGroup} />}
                onClick={handleAddPhaseClick}
                aria-label="Add Phase"
                size={compactButtons ? 'small' : 'medium'}
              >
                {compactButtons ? <FontAwesomeIcon icon={faLayerGroup} /> : 'Add Phase'}
              </CobraSecondaryButton>
            </Tooltip>
            <Tooltip title="New Inject">
              <CobraPrimaryButton
                startIcon={compactButtons ? undefined : <FontAwesomeIcon icon={faPlus} />}
                onClick={handleCreateClick}
                aria-label="New Inject"
                size={compactButtons ? 'small' : 'medium'}
              >
                {compactButtons ? <FontAwesomeIcon icon={faPlus} /> : 'New Inject'}
              </CobraPrimaryButton>
            </Tooltip>
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
          compact={compactButtons}
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
        <InjectEmptyState
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

export default InjectListPage
