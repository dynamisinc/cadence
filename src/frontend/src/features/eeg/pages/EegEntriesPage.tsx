/**
 * EegEntriesPage
 *
 * Standalone page for viewing and managing all EEG entries in an exercise.
 * Features:
 * - Tab navigation between Entries and Coverage views
 * - Full CRUD capabilities for evaluator observations
 * Accessible via in-exercise navigation at /exercises/:id/eeg-entries.
 *
 * Tab content is delegated to:
 * - EegEntriesTab  — entries list with filtering and view-mode switching
 * - EegCoverageTab — capability target coverage dashboard
 */

import { useState, useCallback, useEffect } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import {
  Box,
  Stack,
  CircularProgress,
  Alert,
  Dialog,
  DialogContent,
  Tabs,
  Tab,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faHome,
  faPlus,
  faClipboardCheck,
  faFileExport,
  faFileWord,
  faChartBar,
} from '@fortawesome/free-solid-svg-icons'
import { useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'

import { useExercise } from '../../exercises/hooks'
import { useExerciseRole } from '../../auth'
import { useAuth } from '../../../contexts/AuthContext'
import { useEegEntries, eegEntryKeys, useEegCoverage } from '../hooks/useEegEntries'
import { useInjects } from '../../injects/hooks'
import { EegEntryForm } from '../components/EegEntryForm'
import { EegExportDialog } from '../components/EegExportDialog'
import { EegDocumentDialog } from '../components/EegDocumentDialog'
import { EegEntriesTab } from '../components/EegEntriesTab'
import { EegCoverageTab } from '../components/EegCoverageTab'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'
import { useBreadcrumbs } from '../../../core/contexts'
import { type EegEntryDto } from '../types'
import { PageHeader, HelpTooltip } from '@/shared/components'
import CobraStyles from '@/theme/CobraStyles'

type TabValue = 'entries' | 'coverage'

export const EegEntriesPage = () => {
  const { id: exerciseId } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchParams, setSearchParams] = useSearchParams()

  // Core data hooks
  const { user } = useAuth()
  const { exercise, loading: exerciseLoading, error: exerciseError } = useExercise(exerciseId)
  const { can, effectiveRole } = useExerciseRole(exerciseId ?? null)
  const {
    eegEntries,
    loading: entriesLoading,
    error: entriesError,
  } = useEegEntries(exerciseId!)
  const { injects } = useInjects(exerciseId!)
  const { coverage } = useEegCoverage(exerciseId!)

  // Tab state - sync with URL
  const [activeTab, setActiveTab] = useState<TabValue>(() => {
    const tabParam = searchParams.get('tab')
    return tabParam === 'coverage' ? 'coverage' : 'entries'
  })

  // Dialog state
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [editingEntry, setEditingEntry] = useState<EegEntryDto | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [showExportDialog, setShowExportDialog] = useState(false)
  const [showDocumentDialog, setShowDocumentDialog] = useState(false)
  const [preSelectedCapabilityTargetId, setPreSelectedCapabilityTargetId] =
    useState<string | null>(null)
  const [preSelectedTaskId, setPreSelectedTaskId] = useState<string | null>(null)

  // Set breadcrumbs
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'EEG Entries', path: `/exercises/${exerciseId}/eeg-entries` },
      ]
      : [],
  )

  // Sync tab changes with URL
  useEffect(() => {
    const tabParam = searchParams.get('tab')
    const expectedTab = tabParam === 'coverage' ? 'coverage' : 'entries'
    if (activeTab !== expectedTab) {
      setActiveTab(expectedTab)
    }
  }, [searchParams, activeTab])

  // Permissions
  const canCreate = can('add_observation') // Evaluators can add EEG entries
  const canEdit = can('add_observation')
  const canDelete = can('delete_observation') // Directors only can delete
  const canExport = can('delete_observation') // Directors can export

  // Handlers
  const handleCreateClick = () => {
    setPreSelectedCapabilityTargetId(null)
    setPreSelectedTaskId(null)
    setShowCreateDialog(true)
  }

  const handleAssessTask = useCallback((taskId: string, capabilityTargetId: string) => {
    setPreSelectedCapabilityTargetId(capabilityTargetId)
    setPreSelectedTaskId(taskId)
    setShowCreateDialog(true)
  }, [])

  const handleCloseCreateDialog = () => {
    setShowCreateDialog(false)
    setPreSelectedCapabilityTargetId(null)
    setPreSelectedTaskId(null)
  }

  const handleEntrySaved = () => {
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.byExercise(exerciseId!) })
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.coverage(exerciseId!) })
    handleCloseCreateDialog()
    setEditingEntry(null)
  }

  const handleEdit = (entry: EegEntryDto) => {
    setEditingEntry(entry)
  }

  const handleCloseEditDialog = () => {
    setEditingEntry(null)
  }

  const handleDelete = async (entryId: string) => {
    const entry = eegEntries.find(e => e.id === entryId)
    if (!entry) return

    setDeletingId(entryId)
    try {
      const { eegEntryService } = await import('../services/eegService')
      await eegEntryService.delete(exerciseId!, entryId)
      queryClient.invalidateQueries({ queryKey: eegEntryKeys.byExercise(exerciseId!) })
      queryClient.invalidateQueries({ queryKey: eegEntryKeys.coverage(exerciseId!) })
      notify.success('EEG entry deleted')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete EEG entry'
      notify.error(message)
    } finally {
      setDeletingId(null)
    }
  }

  const handleInjectClick = (injectId: string) => {
    navigate(`/exercises/${exerciseId}/msel?inject=${injectId}`)
  }

  const handleTabChange = (_event: React.SyntheticEvent, newValue: TabValue) => {
    setActiveTab(newValue)
    setSearchParams({ tab: newValue })
  }

  const handleCoverageDetailsClick = useCallback(() => {
    handleTabChange({} as React.SyntheticEvent, 'coverage')
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  // Loading state
  if (exerciseLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    )
  }

  // Error state
  if (exerciseError || !exercise) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        {exerciseError || 'Exercise not found'}
      </Alert>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <PageHeader
        title="EEG Entries"
        icon={faClipboardCheck}
        subtitle={`Exercise Evaluation Guide entries for ${exercise.name}`}
        chips={<HelpTooltip helpKey="eeg.overview" exerciseRole={effectiveRole ?? undefined} compact />}
        actions={
          <Stack direction="row" spacing={2}>
            {/* Show Generate button only on Coverage tab */}
            {activeTab === 'coverage' && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faFileWord} />}
                onClick={() => setShowDocumentDialog(true)}
              >
                Generate EEG
              </CobraSecondaryButton>
            )}
            {canExport && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faFileExport} />}
                onClick={() => setShowExportDialog(true)}
              >
                Export
              </CobraSecondaryButton>
            )}
            {canCreate && activeTab === 'entries' && (
              <CobraPrimaryButton
                startIcon={<FontAwesomeIcon icon={faPlus} />}
                onClick={handleCreateClick}
              >
                Add Entry
              </CobraPrimaryButton>
            )}
          </Stack>
        }
        mb={2}
      />

      {/* Tab Navigation */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs
          value={activeTab}
          onChange={handleTabChange}
          aria-label="EEG page tabs"
        >
          <Tab
            label="Entries"
            value="entries"
            icon={<FontAwesomeIcon icon={faClipboardCheck} />}
            iconPosition="start"
            aria-label="Entries tab"
          />
          <Tab
            label="Coverage"
            value="coverage"
            icon={<FontAwesomeIcon icon={faChartBar} />}
            iconPosition="start"
            aria-label="Coverage tab"
          />
        </Tabs>
      </Box>

      {/* Entries Tab */}
      {activeTab === 'entries' && (
        <EegEntriesTab
          exerciseId={exerciseId!}
          eegEntries={eegEntries}
          entriesLoading={entriesLoading}
          entriesError={entriesError}
          coverage={coverage ?? null}
          canCreate={canCreate}
          canEdit={canEdit}
          canDelete={canDelete}
          currentUserId={user?.id}
          deletingId={deletingId}
          onCreateClick={handleCreateClick}
          onEdit={handleEdit}
          onDelete={handleDelete}
          onInjectClick={handleInjectClick}
          onCoverageDetailsClick={handleCoverageDetailsClick}
        />
      )}

      {/* Coverage Tab */}
      {activeTab === 'coverage' && (
        <EegCoverageTab
          exerciseId={exerciseId!}
          onAssessTask={canCreate ? handleAssessTask : undefined}
        />
      )}

      {/* Create Entry Dialog */}
      <Dialog
        open={showCreateDialog}
        onClose={handleCloseCreateDialog}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: { minHeight: '600px', maxHeight: '90vh' },
        }}
      >
        <DialogContent sx={{ p: 0 }}>
          <EegEntryForm
            exerciseId={exerciseId!}
            availableInjects={injects.filter(i => i.status !== 'Draft')}
            preSelectedCapabilityTargetId={preSelectedCapabilityTargetId ?? undefined}
            preSelectedTaskId={preSelectedTaskId ?? undefined}
            onClose={handleCloseCreateDialog}
            onSaved={handleEntrySaved}
          />
        </DialogContent>
      </Dialog>

      {/* Edit Entry Dialog */}
      <Dialog
        open={!!editingEntry}
        onClose={handleCloseEditDialog}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: { minHeight: '600px', maxHeight: '90vh' },
        }}
      >
        <DialogContent sx={{ p: 0 }}>
          {editingEntry && (
            <EegEntryForm
              exerciseId={exerciseId!}
              editEntry={editingEntry}
              availableInjects={injects.filter(i => i.status !== 'Draft')}
              onClose={handleCloseEditDialog}
              onSaved={handleEntrySaved}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Export Dialog */}
      {showExportDialog && (
        <EegExportDialog
          open={showExportDialog}
          exerciseId={exerciseId!}
          exerciseName={exercise.name}
          coverage={coverage}
          onClose={() => setShowExportDialog(false)}
        />
      )}

      {/* Document Generation Dialog */}
      {showDocumentDialog && (
        <EegDocumentDialog
          open={showDocumentDialog}
          exerciseId={exerciseId!}
          exerciseName={exercise.name}
          coverage={coverage}
          onClose={() => setShowDocumentDialog(false)}
        />
      )}
    </Box>
  )
}

export default EegEntriesPage
