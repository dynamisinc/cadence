import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
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
import type { CreateInjectRequest } from '../types'

/**
 * Create Inject Page
 *
 * Form for creating a new inject in an exercise's MSEL.
 */
export const CreateInjectPage = () => {
  const navigate = useNavigate()
  const { id: exerciseId } = useParams<{ id: string }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId || '')
  const { createInject, isCreating } = useInjects(exerciseId || '')
  const { phases } = usePhases(exerciseId || '')

  // Set custom breadcrumbs with exercise name, MSEL, and New Inject
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'MSEL', path: `/exercises/${exerciseId}/msel` },
        { label: 'New Inject' },
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
            New Inject
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
          onCancel={handleCancel}
          isSubmitting={isCreating}
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
