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
import ArrowBackIcon from '@mui/icons-material/ArrowBack'

import { InjectForm } from '../components/InjectForm'
import { useInject } from '../hooks/useInject'
import { useInjects } from '../hooks'
import { useExercise } from '../../exercises/hooks/useExercise'
import { usePhases } from '../../phases/hooks'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import type { UpdateInjectRequest } from '../types'

/**
 * Edit Inject Page
 *
 * Form for editing an existing inject.
 */
export const EditInjectPage = () => {
  const navigate = useNavigate()
  const { exerciseId, injectId } = useParams<{
    exerciseId: string
    injectId: string
  }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId || '')
  const { inject, loading: injectLoading, error } = useInject(
    exerciseId || '',
    injectId || '',
  )
  const { updateInject, isUpdating } = useInjects(exerciseId || '')
  const { phases } = usePhases(exerciseId || '')

  const [showUnsavedDialog, setShowUnsavedDialog] = useState(false)
  // TODO: Track form changes for unsaved warning - for now always false
  const hasChanges = false

  const handleBackClick = () => {
    if (hasChanges) {
      setShowUnsavedDialog(true)
    } else {
      navigate(`/exercises/${exerciseId}/injects/${injectId}`)
    }
  }

  const handleSubmit = async (request: UpdateInjectRequest) => {
    if (injectId) {
      await updateInject(injectId, request)
      navigate(`/exercises/${exerciseId}/injects/${injectId}`)
    }
  }

  const handleCancel = () => {
    if (hasChanges) {
      setShowUnsavedDialog(true)
    } else {
      navigate(`/exercises/${exerciseId}/injects/${injectId}`)
    }
  }

  const handleConfirmLeave = () => {
    setShowUnsavedDialog(false)
    navigate(`/exercises/${exerciseId}/injects/${injectId}`)
  }

  const handleCancelLeave = () => {
    setShowUnsavedDialog(false)
  }

  // Error state
  if (error) {
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

  // Loading state
  if (injectLoading || !inject) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Stack direction="row" alignItems="center" spacing={1} marginBottom={3}>
          <IconButton onClick={handleBackClick} size="small">
            <ArrowBackIcon />
          </IconButton>
          <Skeleton variant="text" width={200} height={40} />
        </Stack>
        <Paper sx={{ p: 3 }}>
          <Skeleton variant="text" width="80%" height={30} />
          <Skeleton variant="text" width="60%" height={24} />
          <Skeleton variant="rectangular" height={300} sx={{ mt: 2 }} />
        </Paper>
      </Box>
    )
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
            <ArrowBackIcon />
          </IconButton>
          <Box>
            <Typography variant="caption" color="text.secondary">
              INJ-{inject.injectNumber.toString().padStart(3, '0')}
            </Typography>
            <Typography variant="h5" component="h1">
              Edit Inject
            </Typography>
          </Box>
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
          inject={inject}
          phases={phases}
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={isUpdating}
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

export default EditInjectPage
