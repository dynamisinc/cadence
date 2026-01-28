import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faFileImport, faArrowRight } from '@fortawesome/free-solid-svg-icons'

import { ExerciseForm } from '../components'
import { exerciseService } from '../services/exerciseService'
import CobraStyles from '../../../theme/CobraStyles'
import { useUnsavedChangesWarning } from '../../../shared/hooks'
import type { CreateExerciseFormValues, CreateExerciseRequest, ExerciseDto } from '../types'
import { toast } from 'react-toastify'
import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import { ImportWizard } from '../../excel-import/components'

/**
 * Create Exercise Page (S01)
 *
 * Form for creating a new exercise with:
 * - Name (required)
 * - Exercise Type (required)
 * - Scheduled Date
 * - Description (optional)
 * - Location (optional)
 *
 * New exercises are created with:
 * - Status: Draft
 * - Practice Mode: Off
 */
export const CreateExercisePage = () => {
  const navigate = useNavigate()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDirty, setIsDirty] = useState(false)
  const [createdExercise, setCreatedExercise] = useState<ExerciseDto | null>(null)
  const [showPostCreateDialog, setShowPostCreateDialog] = useState(false)
  const [showImportWizard, setShowImportWizard] = useState(false)

  // Warn user before navigating away with unsaved changes
  // Don't warn if exercise was already created (createdExercise is set)
  const shouldWarn = isDirty && !isSubmitting && !createdExercise
  const { UnsavedChangesDialog } = useUnsavedChangesWarning(shouldWarn)

  const handleDirtyChange = useCallback((dirty: boolean) => {
    setIsDirty(dirty)
  }, [])

  const handleSubmit = async (values: CreateExerciseFormValues) => {
    setIsSubmitting(true)

    try {
      const request: CreateExerciseRequest = {
        name: values.name.trim(),
        exerciseType: values.exerciseType,
        scheduledDate: values.scheduledDate,
        description: values.description?.trim() || undefined,
        location: values.location?.trim() || undefined,
        timeZoneId: values.timeZoneId,
        isPracticeMode: values.isPracticeMode,
        deliveryMode: values.deliveryMode,
        timelineMode: values.timelineMode,
        clockMultiplier: values.clockMultiplier,
        directorId: values.directorId?.trim() || undefined,
      }

      const created = await exerciseService.createExercise(request)
      toast.success('Exercise created')
      // Clear dirty state since exercise was saved successfully
      setIsDirty(false)
      // Show post-create dialog offering MSEL import
      setCreatedExercise(created)
      setShowPostCreateDialog(true)
    } catch (err) {
      const message =
        err instanceof Error ? err.message : 'Failed to create exercise'
      toast.error(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleCancel = () => {
    navigate('/exercises')
  }

  const handleSkipImport = () => {
    setShowPostCreateDialog(false)
    if (createdExercise) {
      navigate(`/exercises/${createdExercise.id}`)
    }
  }

  const handleStartImport = () => {
    setShowPostCreateDialog(false)
    setShowImportWizard(true)
  }

  const handleImportWizardClose = () => {
    setShowImportWizard(false)
    if (createdExercise) {
      navigate(`/exercises/${createdExercise.id}/msel`)
    }
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      <Typography variant="h5" component="h1" gutterBottom>
        Create Exercise
      </Typography>

      <Paper sx={{ p: 3, maxWidth: 600 }}>
        <ExerciseForm
          onSubmit={handleSubmit}
          onCancel={handleCancel}
          isSubmitting={isSubmitting}
          onDirtyChange={handleDirtyChange}
        />
      </Paper>

      {/* Unsaved changes dialog for navigation blocking */}
      <UnsavedChangesDialog />

      {/* Post-create dialog offering MSEL import */}
      <Dialog
        open={showPostCreateDialog}
        onClose={handleSkipImport}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>
          Exercise Created
        </DialogTitle>
        <DialogContent>
          <Typography variant="body1" gutterBottom>
            <strong>{createdExercise?.name}</strong> has been created successfully.
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Would you like to import an MSEL from an Excel file now, or set it up later?
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Stack direction="row" spacing={2} width="100%" justifyContent="flex-end">
            <CobraSecondaryButton
              onClick={handleSkipImport}
              startIcon={<FontAwesomeIcon icon={faArrowRight} />}
            >
              Skip for Now
            </CobraSecondaryButton>
            <CobraPrimaryButton
              onClick={handleStartImport}
              startIcon={<FontAwesomeIcon icon={faFileImport} />}
            >
              Import MSEL
            </CobraPrimaryButton>
          </Stack>
        </DialogActions>
      </Dialog>

      {/* Import Wizard */}
      {createdExercise && (
        <ImportWizard
          open={showImportWizard}
          onClose={handleImportWizardClose}
          exerciseId={createdExercise.id}
        />
      )}
    </Box>
  )
}

export default CreateExercisePage
