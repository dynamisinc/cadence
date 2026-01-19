import { useState, useMemo, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  CircularProgress,
  Divider,
  Grid,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHome, faList, faPen, faPlay, faCopy, faBoxArchive, faTrash } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import { useExercise, useSetupProgress, useDuplicateExercise, useExerciseStatus } from '../hooks'
import {
  ExerciseForm,
  ExerciseHeader,
  ExerciseStatusActions,
  SetupProgress,
  DuplicateExerciseDialog,
  ArchiveExerciseDialog,
  DeleteExerciseDialog,
} from '../components'
import { ObjectiveList } from '../../objectives'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraLinkButton,
  CobraDeleteButton,
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
  const [duplicateDialogOpen, setDuplicateDialogOpen] = useState(false)
  const [archiveDialogOpen, setArchiveDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)

  // Setup progress for Draft exercises
  const {
    data: setupProgress,
    isLoading: setupProgressLoading,
    error: setupProgressError,
  } = useSetupProgress(id ?? '')

  // Duplicate exercise mutation
  const { duplicate, isDuplicating } = useDuplicateExercise()

  // Archive/delete actions
  const { archive, isTransitioning: isArchiving } = useExerciseStatus(id ?? '')

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
        timeZoneId: values.timeZoneId,
        isPracticeMode: values.isPracticeMode,
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

  const handleArchived = () => {
    // Navigate back to exercises list after archive
    navigate('/exercises')
  }

  const handleDeleted = () => {
    // Navigate back to exercises list after delete
    navigate('/exercises')
  }

  // Determine if exercise can be deleted (never published OR already archived)
  const canDelete = useMemo(() => {
    if (!exercise || !canManage) return false
    return !exercise.hasBeenPublished || exercise.status === ExerciseStatus.Archived
  }, [exercise, canManage])

  // Can archive if not already archived and user can manage
  const canArchive = useMemo(() => {
    if (!exercise || !canManage) return false
    return exercise.status !== ExerciseStatus.Archived
  }, [exercise, canManage])

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
      <ExerciseHeader
        exercise={exercise}
        marginBottom={3}
        actions={
          <>
            <CobraLinkButton onClick={handleBackToList}>
              Back to List
            </CobraLinkButton>
            <CobraPrimaryButton
              startIcon={<FontAwesomeIcon icon={faList} />}
              onClick={handleViewMsel}
            >
              View MSEL
            </CobraPrimaryButton>
            {exercise.status === ExerciseStatus.Active && !isEditing && (
              <CobraPrimaryButton
                startIcon={<FontAwesomeIcon icon={faPlay} />}
                onClick={() => navigate(`/exercises/${id}/conduct`)}
              >
                Conduct
              </CobraPrimaryButton>
            )}
            {canEdit && !isEditing && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faPen} />}
                onClick={handleEdit}
              >
                Edit
              </CobraSecondaryButton>
            )}
            {!isEditing && canManage && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faCopy} />}
                onClick={() => setDuplicateDialogOpen(true)}
              >
                Duplicate
              </CobraSecondaryButton>
            )}
            {!isEditing && canArchive && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faBoxArchive} />}
                onClick={() => setArchiveDialogOpen(true)}
              >
                Archive
              </CobraSecondaryButton>
            )}
            {!isEditing && canDelete && (
              <CobraDeleteButton
                startIcon={<FontAwesomeIcon icon={faTrash} />}
                onClick={() => setDeleteDialogOpen(true)}
              >
                Delete
              </CobraDeleteButton>
            )}
            {!isEditing && (
              <ExerciseStatusActions
                exercise={exercise}
                isReadyToActivate={setupProgress?.isReadyToActivate}
              />
            )}
          </>
        }
      />

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

      {/* Content - Grid layout for Draft (with sidebar) vs other statuses */}
      <Grid container spacing={3}>
        {/* Main content column */}
        <Grid size={{ xs: 12, md: exercise.status === ExerciseStatus.Draft && !isEditing ? 8 : 12 }}>
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

          {/* Objectives Section (only show in view mode) */}
          {!isEditing && (
            <Paper sx={{ p: 3, mt: 3 }}>
              <ObjectiveList
                exerciseId={exercise.id}
                canEdit={canEdit}
              />
            </Paper>
          )}
        </Grid>

        {/* Setup Progress sidebar for Draft exercises */}
        {exercise.status === ExerciseStatus.Draft && !isEditing && (
          <Grid size={{ xs: 12, md: 4 }}>
            <SetupProgress
              progress={setupProgress}
              isLoading={setupProgressLoading}
              error={setupProgressError}
            />
          </Grid>
        )}
      </Grid>

      {/* Duplicate Exercise Dialog */}
      <DuplicateExerciseDialog
        open={duplicateDialogOpen}
        exercise={exercise}
        onClose={() => setDuplicateDialogOpen(false)}
        onSubmit={async request => {
          await duplicate({ exerciseId: exercise.id, request })
          setDuplicateDialogOpen(false)
        }}
        isSubmitting={isDuplicating}
      />

      {/* Archive Exercise Dialog */}
      <ArchiveExerciseDialog
        open={archiveDialogOpen}
        exercise={exercise}
        onClose={() => setArchiveDialogOpen(false)}
        onConfirm={async () => {
          await archive()
          handleArchived()
        }}
        isArchiving={isArchiving}
      />

      {/* Delete Exercise Dialog */}
      <DeleteExerciseDialog
        open={deleteDialogOpen}
        exercise={exercise}
        onClose={() => setDeleteDialogOpen(false)}
        onDeleted={handleDeleted}
      />

      {/* Unsaved changes dialog for navigation blocking */}
      <UnsavedChangesDialog />
    </Box>
  )
}

export default ExerciseDetailPage
