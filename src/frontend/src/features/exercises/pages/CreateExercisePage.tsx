import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box, Typography, Paper } from '@mui/material'

import { ExerciseForm } from '../components'
import { exerciseService } from '../services/exerciseService'
import CobraStyles from '../../../theme/CobraStyles'
import { useUnsavedChangesWarning } from '../../../shared/hooks'
import type { CreateExerciseFormValues, CreateExerciseRequest } from '../types'
import { toast } from 'react-toastify'

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

  // Warn user before navigating away with unsaved changes
  useUnsavedChangesWarning(isDirty && !isSubmitting)

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
      }

      const created = await exerciseService.createExercise(request)
      toast.success('Exercise created')
      // Navigate to exercise detail/setup view
      navigate(`/exercises/${created.id}`)
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
    </Box>
  )
}

export default CreateExercisePage
