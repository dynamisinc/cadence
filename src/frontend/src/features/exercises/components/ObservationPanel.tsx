/**
 * ObservationPanel
 *
 * The right-column panel for the exercise conduct view. Displays:
 * - Collapsible header with Add Observation and EEG Entry buttons
 * - ObservationForm (when adding/editing)
 * - EEG Coverage Dashboard (compact)
 * - Scrollable ObservationList
 *
 * @module features/exercises
 */

import { FC, useState } from 'react'
import {
  Box,
  Paper,
  Stack,
  Typography,
  IconButton,
  Divider,
  Dialog,
  DialogContent,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlus,
  faChevronDown,
  faChevronUp,
  faClipboardCheck,
} from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'

import { CobraPrimaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { ObservationForm, ObservationList } from '../../observations'
import { EegEntryForm, EegCoverageDashboard } from '../../eeg/components'
import { eegEntryKeys } from '../../eeg/hooks/useEegEntries'
import { useCapabilities } from '../../capabilities/hooks/useCapabilities'
import { useExerciseTargetCapabilities } from '../hooks/useExerciseTargetCapabilities'
import type { ObservationDto, CreateObservationRequest } from '../../observations/types'
import type { InjectDto } from '../../injects/types'

interface ObservationPanelProps {
  /** The exercise ID */
  exerciseId: string
  /** Whether the current user can add observations */
  canAddObservations: boolean
  /** All injects (for linking observations to injects) */
  injects: InjectDto[]
  /** Current observations list */
  observations: ObservationDto[]
  /** Whether observations are loading */
  observationsLoading: boolean
  /** Observation load error, if any */
  observationsError: string | null
  /** Current exercise time string (for EEG entries) */
  displayTime: string
  /** Open an inject drawer by ID (called from observation list) */
  onInjectClick: (injectId: string) => void
  /** Pre-selected inject ID when opening form from an inject drawer */
  preSelectedInjectId?: string | null
  /** Clear pre-selected inject ID after form is submitted/cancelled */
  onClearPreSelectedInjectId: () => void
  /** Submit (create or update) an observation */
  onSubmitObservation: (data: CreateObservationRequest) => Promise<void>
  /** Delete an observation by ID */
  onDeleteObservation: (observationId: string) => Promise<void>
  /** ID of the observation currently being deleted (for loading state) */
  deletingObservationId: string | null
  /** Currently editing observation (if editing mode) */
  editingObservation: ObservationDto | null
  /** Set editing observation */
  onSetEditingObservation: (observation: ObservationDto | null) => void
  /**
   * Externally controlled observation form open state.
   * When provided, the panel is controlled by the parent for form open/close.
   */
  showObservationForm?: boolean
  /** Called when the panel's "Add" button is clicked or form is closed */
  onShowObservationFormChange?: (open: boolean) => void
}

/**
 * Right-column observations panel for the exercise conduct page.
 *
 * Accepts observations data and mutation callbacks from the parent page —
 * the parent (ExerciseConductPage) owns the useObservations hook so the same
 * data can be shared with the NarrativeView.
 */
export const ObservationPanel: FC<ObservationPanelProps> = ({
  exerciseId,
  canAddObservations,
  injects,
  observations,
  observationsLoading,
  observationsError,
  displayTime,
  onInjectClick,
  preSelectedInjectId,
  onClearPreSelectedInjectId,
  onSubmitObservation,
  onDeleteObservation,
  deletingObservationId,
  editingObservation,
  onSetEditingObservation,
  showObservationForm: showObservationFormProp,
  onShowObservationFormChange,
}) => {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { capabilities } = useCapabilities(false)
  const { targetCapabilities } = useExerciseTargetCapabilities(exerciseId)

  // Panel state
  const [observationsExpanded, setObservationsExpanded] = useState(true)
  // Form open state: controlled by parent if showObservationFormProp is provided
  const [showObservationFormInternal, setShowObservationFormInternal] = useState(false)
  const isControlled = showObservationFormProp !== undefined
  const showObservationForm = isControlled ? showObservationFormProp : showObservationFormInternal

  const setShowObservationForm = (open: boolean) => {
    if (isControlled) {
      onShowObservationFormChange?.(open)
    } else {
      setShowObservationFormInternal(open)
    }
  }

  const [isSubmittingObservation, setIsSubmittingObservation] = useState(false)

  // EEG Entry state
  const [showEegEntryForm, setShowEegEntryForm] = useState(false)
  const [eegPreSelectedTaskId, setEegPreSelectedTaskId] = useState<string | null>(null)
  const [eegPreSelectedCapabilityTargetId, setEegPreSelectedCapabilityTargetId] =
    useState<string | null>(null)

  // =========================================================================
  // Observation handlers
  // =========================================================================

  const handleSubmitObservation = async (data: CreateObservationRequest) => {
    setIsSubmittingObservation(true)
    try {
      await onSubmitObservation(data)
      setShowObservationForm(false)
      onSetEditingObservation(null)
      onClearPreSelectedInjectId()
    } finally {
      setIsSubmittingObservation(false)
    }
  }

  const handleEditObservation = (observation: ObservationDto) => {
    onSetEditingObservation(observation)
    setShowObservationForm(true)
  }

  const handleCancelObservationForm = () => {
    setShowObservationForm(false)
    onSetEditingObservation(null)
    onClearPreSelectedInjectId()
  }

  const handleOpenObservationForm = () => {
    setShowObservationForm(true)
  }


  // =========================================================================
  // EEG Entry handlers
  // =========================================================================

  const handleOpenEegEntry = () => {
    setShowEegEntryForm(true)
  }

  const handleCloseEegEntry = () => {
    setShowEegEntryForm(false)
    setEegPreSelectedTaskId(null)
    setEegPreSelectedCapabilityTargetId(null)
  }

  const handleEegEntrySaved = () => {
    queryClient.invalidateQueries({ queryKey: eegEntryKeys.coverage(exerciseId) })
  }

  const handleAssessTask = (taskId: string, capabilityTargetId: string) => {
    setEegPreSelectedTaskId(taskId)
    setEegPreSelectedCapabilityTargetId(capabilityTargetId)
    setShowEegEntryForm(true)
  }

  return (
    <>
      <Paper
        sx={{
          p: 3,
          height: '100%',
          overflow: 'hidden',
          display: 'flex',
          flexDirection: 'column',
          minHeight: 0,
        }}
      >
        {/* Panel Header */}
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          sx={{ mb: 2, flexShrink: 0 }}
        >
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="h6">Observations</Typography>
            <IconButton
              size="small"
              onClick={() => setObservationsExpanded(!observationsExpanded)}
              aria-label={observationsExpanded ? 'Collapse observations' : 'Expand observations'}
            >
              <FontAwesomeIcon
                icon={observationsExpanded ? faChevronUp : faChevronDown}
              />
            </IconButton>
          </Stack>

          {canAddObservations && !showObservationForm && (
            <Stack direction="row" spacing={1}>
              <CobraPrimaryButton
                size="small"
                startIcon={<FontAwesomeIcon icon={faPlus} />}
                onClick={handleOpenObservationForm}
              >
                Add
              </CobraPrimaryButton>
              <CobraPrimaryButton
                size="small"
                startIcon={<FontAwesomeIcon icon={faClipboardCheck} />}
                onClick={handleOpenEegEntry}
                sx={{ backgroundColor: 'secondary.main' }}
              >
                EEG Entry
              </CobraPrimaryButton>
            </Stack>
          )}
        </Stack>

        {/* Observation Form (fixed at top when expanded) */}
        {observationsExpanded && showObservationForm && (
          <Box sx={{ mb: 2, flexShrink: 0 }}>
            <ObservationForm
              exerciseId={exerciseId}
              inject={
                preSelectedInjectId
                  ? injects.find(i => i.id === preSelectedInjectId)
                  : undefined
              }
              injects={injects}
              capabilities={capabilities}
              targetCapabilityIds={targetCapabilities.map(c => c.id)}
              initialValues={
                editingObservation
                  ? {
                    rating: editingObservation.rating!,
                    content: editingObservation.content,
                    recommendation: editingObservation.recommendation ?? undefined,
                    injectId: editingObservation.injectId ?? undefined,
                    capabilityIds: editingObservation.capabilities?.map(c => c.id) ?? [],
                  }
                  : preSelectedInjectId
                    ? { rating: undefined as never, content: '', injectId: preSelectedInjectId }
                    : undefined
              }
              onSubmit={handleSubmitObservation}
              onCancel={handleCancelObservationForm}
              isSubmitting={isSubmittingObservation}
            />
            <Divider sx={{ my: 2 }} />
          </Box>
        )}

        {/* EEG Coverage Dashboard (compact, when expanded) */}
        {observationsExpanded && canAddObservations && (
          <Box sx={{ mb: 2, flexShrink: 0 }}>
            <EegCoverageDashboard
              exerciseId={exerciseId}
              compact
              onAssessTask={handleAssessTask}
              onDetailsClick={() => navigate(`/exercises/${exerciseId}/eeg-entries`)}
            />
          </Box>
        )}

        {/* Observation List - Scrollable */}
        {observationsExpanded && (
          <Box
            sx={{
              flex: 1,
              overflowY: 'auto',
              overflowX: 'hidden',
              pr: `${CobraStyles.Scrollbar.ContentSpacing}px`,
              ...CobraStyles.Scrollbar.Styling,
            }}
          >
            <ObservationList
              observations={observations}
              loading={observationsLoading}
              error={observationsError}
              canEdit={canAddObservations}
              onEdit={handleEditObservation}
              onDelete={onDeleteObservation}
              deletingId={deletingObservationId}
              onInjectClick={onInjectClick}
            />
          </Box>
        )}
      </Paper>

      {/* EEG Entry Form Dialog */}
      <Dialog
        open={showEegEntryForm}
        onClose={handleCloseEegEntry}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: { minHeight: '600px', maxHeight: '90vh' },
        }}
      >
        <DialogContent sx={{ p: 0 }}>
          <EegEntryForm
            exerciseId={exerciseId}
            exerciseTime={displayTime}
            availableInjects={injects.filter(i => i.status !== 'Draft')}
            preSelectedCapabilityTargetId={eegPreSelectedCapabilityTargetId ?? undefined}
            preSelectedTaskId={eegPreSelectedTaskId ?? undefined}
            onClose={handleCloseEegEntry}
            onSaved={handleEegEntrySaved}
          />
        </DialogContent>
      </Dialog>
    </>
  )
}
