import { useState, useMemo } from 'react'
import { useNavigate, useParams, useLocation } from 'react-router-dom'
import {
  Box,
  Typography,
  Stack,
  Paper,
  IconButton,
  Skeleton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faArrowLeft, faHome } from '@fortawesome/free-solid-svg-icons'

import { InjectForm } from '../components/InjectForm'
import { useInjects } from '../hooks'
import { useExercise } from '../../exercises/hooks/useExercise'
import { usePhases } from '../../phases/hooks'
import { useBreadcrumbs } from '../../../core/contexts'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { TriggerType } from '../../../types'
import type { CreateInjectRequest, InjectDto, InjectFormValues } from '../types'

function buildDuplicateValues(source: InjectDto): Partial<InjectFormValues> {
  return {
    title: `${source.title} (Copy)`,
    description: source.description,
    target: source.target,
    source: source.source ?? '',
    deliveryMethodId: source.deliveryMethodId ?? '',
    deliveryMethodOther: source.deliveryMethodOther ?? '',
    injectType: source.injectType,
    expectedAction: source.expectedAction ?? '',
    controllerNotes: source.controllerNotes ?? '',
    phaseId: source.phaseId ?? '',
    objectiveIds: source.objectiveIds ?? [],
    sourceReference: source.sourceReference ?? '',
    priority: source.priority?.toString() ?? '',
    triggerType: source.triggerType ?? TriggerType.Manual,
    triggerCondition: source.triggerCondition ?? '',
    responsibleController: source.responsibleController ?? '',
    locationName: source.locationName ?? '',
    locationType: source.locationType ?? '',
    track: source.track ?? '',
  }
}

/**
 * Create Inject Page
 *
 * Form for creating a new inject in an exercise's MSEL.
 * Supports duplicate mode when navigated with `duplicateFrom` in router state.
 */
export const CreateInjectPage = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const { id: exerciseId } = useParams<{ id: string }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId || '')
  const { createInject, isCreating } = useInjects(exerciseId || '')
  const { phases } = usePhases(exerciseId || '')

  const duplicateFrom = (location.state as { duplicateFrom?: InjectDto } | null)?.duplicateFrom
  const isDuplicate = !!duplicateFrom

  const duplicateValues = useMemo(
    () => (duplicateFrom ? buildDuplicateValues(duplicateFrom) : undefined),
    [duplicateFrom],
  )

  // Set custom breadcrumbs with exercise name, MSEL, and New Inject
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'MSEL', path: `/exercises/${exerciseId}/msel` },
        { label: isDuplicate ? 'Duplicate Inject' : 'New Inject' },
      ]
      : undefined,
  )

  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)
  // TODO: Track form changes for unsaved warning - for now always false
  const hasChanges = false

  const handleBackClick = () => {
    if (hasChanges) {
      setShowUnsavedDialog(true)
    } else {
      navigate(`/exercises/${exerciseId}/msel`)
    }
  }

  const handleSubmit = async (request: CreateInjectRequest) => {
    await createInject(request)
    navigate(`/exercises/${exerciseId}/msel`)
  }

  const handleSubmitAndContinue = async (request: CreateInjectRequest) => {
    await createInject(request)
    // Hook handles success toast; form component handles reset internally
  }

  const handleCancel = () => {
    if (hasChanges) {
      setShowUnsavedDialog(true)
    } else {
      navigate(`/exercises/${exerciseId}/msel`)
    }
  }

  const handleConfirmLeave = () => {
    setShowUnsavedDialog(false)
    navigate(`/exercises/${exerciseId}/msel`)
  }

  const handleCancelLeave = () => {
    setShowUnsavedDialog(false)
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
        marginBottom={1}
      >
        <Stack direction="row" alignItems="center" spacing={1}>
          <IconButton onClick={handleBackClick} size="small">
            <FontAwesomeIcon icon={faArrowLeft} />
          </IconButton>
          <Typography variant="h5" component="h1">
            {isDuplicate ? 'New Inject (from duplicate)' : 'New Inject'}
          </Typography>
        </Stack>
      </Stack>

      {/* Exercise name subtitle */}
      <Typography variant="body2" color="text.secondary" marginBottom={3}>
        {exerciseLoading ? (
          <Skeleton width={200} />
        ) : (
          exercise?.name || 'Exercise'
        )}
      </Typography>

      {/* Form */}
      <Paper sx={{ p: 3 }}>
        <InjectForm
          exerciseId={exerciseId || ''}
          phases={phases}
          onSubmit={handleSubmit}
          onSubmitAndContinue={handleSubmitAndContinue}
          onCancel={handleCancel}
          isSubmitting={isCreating}
          initialValues={duplicateValues}
        />
      </Paper>

      {/* Unsaved Changes Dialog */}
      <Dialog open={showUnsavedDialog} onClose={handleCancelLeave}>
        <DialogTitle>Unsaved Changes</DialogTitle>
        <DialogContent>
          <Typography>
            You have unsaved changes. Are you sure you want to leave?
          </Typography>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleCancelLeave}>
            Keep Editing
          </CobraSecondaryButton>
          <CobraPrimaryButton onClick={handleConfirmLeave}>
            Discard Changes
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default CreateInjectPage
