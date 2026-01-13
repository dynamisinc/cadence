import { useState, useMemo, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Tooltip,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHome, faPen, faScrewdriverWrench } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import { useExercise } from '../hooks'
import { ExerciseForm, ExerciseStatusChip, ExerciseTypeChip } from '../components'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraLinkButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { usePermissions, useUnsavedChangesWarning } from '../../../shared/hooks'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus } from '../../../types'
import { getExerciseTypeFullName } from '../../../theme/cobraTheme'
import type { CreateExerciseFormValues, UpdateExerciseRequest } from '../types'

/**
 * Exercise Detail Page (S02)
 *
 * Displays exercise details with edit capability:
 * - View mode: Shows all exercise information
 * - Edit mode: Form for editing (when user clicks Edit)
 *
 * Editing rules per status:
 * - Draft: All fields editable
 * - Active: Only Name, Description, End Date editable
 * - Completed/Archived: No editing allowed
 */
export const ExerciseDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { exercise, loading, error, updateExercise } = useExercise(id)
  const { canManage } = usePermissions()

  const [isEditing, setIsEditing] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDirty, setIsDirty] = useState(false)

  // Set custom breadcrumbs with exercise name
  useBreadcrumbs(
    exercise
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name },
      ]
      : undefined,
  )

  // Warn user before navigating away with unsaved changes (only when editing)
  const { UnsavedChangesDialog } = useUnsavedChangesWarning(isEditing && isDirty && !isSubmitting)

  const handleDirtyChange = useCallback((dirty: boolean) => {
    setIsDirty(dirty)
  }, [])

  // Determine which fields should be disabled based on exercise status
  const disabledFields = useMemo<(keyof CreateExerciseFormValues)[]>(() => {
    if (!exercise) return []

    switch (exercise.status) {
      case ExerciseStatus.Active:
        // Active exercises: only name, description, end date editable
        return ['exerciseType', 'scheduledDate', 'startTime']
      case ExerciseStatus.Completed:
      case ExerciseStatus.Archived:
        // Read-only
        return [
          'name',
          'exerciseType',
          'scheduledDate',
          'description',
          'location',
          'startTime',
          'endTime',
        ]
      default:
        // Draft: all editable
        return []
    }
  }, [exercise])

  const canEdit = useMemo(() => {
    if (!exercise || !canManage) return false
    return (
      exercise.status !== ExerciseStatus.Completed &&
      exercise.status !== ExerciseStatus.Archived
    )
  }, [exercise, canManage])

  const handleEdit = () => {
    if (
      exercise?.status === ExerciseStatus.Completed ||
      exercise?.status === ExerciseStatus.Archived
    ) {
      return // Should not reach here, but safety check
    }
    setIsEditing(true)
  }

  const handleSubmit = async (values: CreateExerciseFormValues) => {
    setIsSubmitting(true)

    try {
      const request: UpdateExerciseRequest = {
        name: values.name.trim(),
        exerciseType: values.exerciseType,
        scheduledDate: values.scheduledDate,
        description: values.description?.trim() || undefined,
        location: values.location?.trim() || undefined,
        startTime: values.startTime || undefined,
        endTime: values.endTime || undefined,
      }

      await updateExercise(request)
      setIsEditing(false)
      setIsDirty(false)
    } catch {
      // Error handled by hook
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleCancel = () => {
    // Cancel exits edit mode immediately without confirmation.
    // Navigation away from the page (Back to List, browser back, etc.) will still
    // show the unsaved changes warning via useUnsavedChangesWarning.
    // NOTE: If confirmation on Cancel is desired later, use the confirm() function here.
    setIsEditing(false)
    setIsDirty(false)
  }

  const handleBackToList = () => {
    // useBlocker handles the unsaved changes warning automatically
    navigate('/exercises')
  }

  const handleViewMsel = () => {
    navigate(`/exercises/${id}/msel`)
  }

  const formatDate = (dateStr: string) => {
    try {
      return format(parseISO(dateStr), 'MMMM d, yyyy')
    } catch {
      return dateStr
    }
  }

  const formatTime = (timeStr: string | null) => {
    if (!timeStr) return null
    try {
      // Time comes as HH:MM:SS, format to 12-hour
      const [hours, minutes] = timeStr.split(':')
      const hour = parseInt(hours, 10)
      const ampm = hour >= 12 ? 'PM' : 'AM'
      const hour12 = hour % 12 || 12
      return `${hour12}:${minutes} ${ampm}`
    } catch {
      return timeStr
    }
  }

  // Loading state
  if (loading && !exercise) {
    return (
      <Box
        display="flex"
        justifyContent="center"
        alignItems="center"
        minHeight="200px"
      >
        <CircularProgress />
      </Box>
    )
  }

  // Error state
  if (error && !exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography color="error" gutterBottom>
          {error}
        </Typography>
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
      </Box>
    )
  }

  // Not found
  if (!exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography variant="h6" gutterBottom>
          Exercise not found
        </Typography>
        <CobraPrimaryButton onClick={() => navigate('/exercises')}>
          Back to Exercises
        </CobraPrimaryButton>
      </Box>
    )
  }

  // Read-only message for completed/archived
  const readOnlyMessage =
    exercise.status === ExerciseStatus.Completed
      ? 'Completed exercises cannot be modified'
      : exercise.status === ExerciseStatus.Archived
        ? 'Archived exercises cannot be modified'
        : null

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="flex-start"
        marginBottom={3}
      >
        <Box>
          <Stack direction="row" spacing={2} alignItems="center">
            <Typography variant="h5" component="h1">
              {exercise.name}
            </Typography>
            {exercise.isPracticeMode && (
              <Tooltip title="Practice Mode - excluded from production reports">
                <Box component="span" sx={{ color: 'action.active' }}>
                  <FontAwesomeIcon icon={faScrewdriverWrench} />
                </Box>
              </Tooltip>
            )}
          </Stack>
          <Stack direction="row" spacing={1} mt={1}>
            <ExerciseTypeChip type={exercise.exerciseType} />
            <ExerciseStatusChip status={exercise.status} />
          </Stack>
        </Box>

        <Stack direction="row" spacing={1}>
          <CobraLinkButton onClick={handleBackToList}>
            Back to List
          </CobraLinkButton>
          <CobraPrimaryButton
            startIcon={<ListAltIcon />}
            onClick={handleViewMsel}
          >
            View MSEL
          </CobraPrimaryButton>
          {canEdit && !isEditing && (
            <CobraSecondaryButton
              startIcon={<FontAwesomeIcon icon={faPen} />}
              onClick={handleEdit}
            >
              Edit
            </CobraSecondaryButton>
          )}
        </Stack>
      </Stack>

      {/* Read-only warning */}
      {readOnlyMessage && (
        <Paper
          sx={{
            p: 2,
            mb: 2,
            backgroundColor: 'grey.100',
          }}
        >
          <Typography color="text.secondary">{readOnlyMessage}</Typography>
        </Paper>
      )}

      {/* Content */}
      <Paper sx={{ p: 3 }}>
        {isEditing ? (
          <ExerciseForm
            exercise={exercise}
            onSubmit={handleSubmit}
            onCancel={handleCancel}
            isSubmitting={isSubmitting}
            disabledFields={disabledFields}
            onDirtyChange={handleDirtyChange}
          />
        ) : (
          <Stack spacing={3}>
            {/* Description */}
            {exercise.description && (
              <Box>
                <Typography variant="subtitle2" color="text.secondary">
                  Description
                </Typography>
                <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
                  {exercise.description}
                </Typography>
              </Box>
            )}

            <Divider />

            {/* Schedule */}
            <Box>
              <Typography
                variant="subtitle2"
                color="text.secondary"
                gutterBottom
              >
                Schedule
              </Typography>
              <Stack spacing={1}>
                <Typography variant="body1">
                  <strong>Date:</strong> {formatDate(exercise.scheduledDate)}
                </Typography>
                {(exercise.startTime || exercise.endTime) && (
                  <Typography variant="body1">
                    <strong>Time:</strong>{' '}
                    {exercise.startTime
                      ? formatTime(exercise.startTime)
                      : 'TBD'}
                    {exercise.endTime &&
                      ` - ${formatTime(exercise.endTime)}`}
                  </Typography>
                )}
                <Typography variant="body1">
                  <strong>Time Zone:</strong> {exercise.timeZoneId}
                </Typography>
              </Stack>
            </Box>

            {/* Location */}
            {exercise.location && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">
                    Location
                  </Typography>
                  <Typography variant="body1">{exercise.location}</Typography>
                </Box>
              </>
            )}

            <Divider />

            {/* Type Info */}
            <Box>
              <Typography
                variant="subtitle2"
                color="text.secondary"
                gutterBottom
              >
                Exercise Type
              </Typography>
              <Typography variant="body1">
                {exercise.exerciseType} -{' '}
                {getExerciseTypeFullName(exercise.exerciseType)}
              </Typography>
            </Box>
          </Stack>
        )}
      </Paper>

      {/* Unsaved changes dialog for navigation blocking */}
      <UnsavedChangesDialog />
    </Box>
  )
}

export default ExerciseDetailPage
