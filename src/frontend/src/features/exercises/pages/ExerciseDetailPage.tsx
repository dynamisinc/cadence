import { useMemo, useCallback, useState } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Typography,
  Paper,
  CircularProgress,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHome, faPen, faCopy, faBoxArchive, faTrash, faEllipsisVertical, faGear } from '@fortawesome/free-solid-svg-icons'

import {
  useExercise,
  useSetupProgress,
  useMselSummary,
  useExerciseParticipants,
  exerciseCapabilityKeys,
} from '../hooks'
import { exerciseCapabilityService } from '../services/exerciseCapabilityService'
import {
  ExerciseForm,
  ExerciseHeader,
  ExerciseStatusActions,
  DuplicateExerciseDialog,
  ArchiveExerciseDialog,
  DeleteExerciseDialog,
  ExerciseSettingsDialog,
  ExerciseDetailTabs,
} from '../components'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraIconButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useUnsavedChangesWarning } from '../../../shared/hooks'
import { useExerciseRole } from '../../auth/hooks/useExerciseRole'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus } from '../../../types'
import { EffectiveRoleBadge } from '@/features/auth'
import { HelpTooltip } from '@/shared/components'
import { useExerciseActions } from '../hooks/useExerciseActions'
import type { CreateExerciseFormValues, UpdateExerciseRequest } from '../types'
import type { UserDto } from '../../users/types'

/**
 * Exercise Detail Page (S02, S14)
 *
 * Displays exercise details with tabbed interface:
 * - Details tab: Shows all exercise information (with edit capability)
 * - Objectives tab: Manage exercise objectives
 * - Participants tab: Manage exercise participants (S14)
 *
 * Editing rules per status:
 * - Draft: All fields editable
 * - Active: Only Name, Description, End Date editable
 * - Completed/Archived: No editing allowed
 */
export const ExerciseDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { exercise, loading, error, updateExercise } = useExercise(id)
  const { can, effectiveRole } = useExerciseRole(id ?? null)
  const queryClient = useQueryClient()

  // Derive edit state from URL path (ends with /edit)
  const isEditing = location.pathname.endsWith('/edit')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDirty, setIsDirty] = useState(false)
  const [activeTab, setActiveTab] = useState(0)

  // Setup progress for Draft exercises
  const {
    data: setupProgress,
    isLoading: setupProgressLoading,
    error: setupProgressError,
  } = useSetupProgress(id ?? '')

  // MSEL summary for progress display
  const { data: mselSummary } = useMselSummary(id ?? '')

  // Exercise participants (for director display)
  const { participants } = useExerciseParticipants(id ?? '')
  const director = useMemo(
    () => participants.find(p => p.exerciseRole === 'ExerciseDirector'),
    [participants],
  )

  // Convert director participant to UserDto for the form
  const directorAsUser: UserDto | null = useMemo(() => {
    if (!director) return null
    return {
      id: director.userId,
      email: director.email,
      displayName: director.displayName,
      systemRole: director.systemRole,
      status: 'Active', // Participants are always active users
      lastLoginAt: null,
      createdAt: director.addedAt,
    }
  }, [director])

  // Lifecycle action dialogs + mutations (extracted hook)
  const {
    duplicateDialogOpen,
    archiveDialogOpen,
    deleteDialogOpen,
    settingsDialogOpen,
    openDuplicateDialog,
    closeDuplicateDialog,
    openArchiveDialog,
    closeArchiveDialog,
    openDeleteDialog,
    closeDeleteDialog,
    openSettingsDialog,
    closeSettingsDialog,
    moreMenuAnchor,
    handleMoreMenuOpen,
    handleMoreMenuClose,
    canDelete,
    canArchive,
    duplicate,
    isDuplicating,
    archive,
    isArchiving,
    handleArchived,
    handleDeleted,
  } = useExerciseActions({
    exerciseId: id ?? '',
    exercise: exercise ?? null,
    canEditExercise: can('edit_exercise'),
    canDeleteExercise: can('delete_exercise'),
  })

  // Set custom breadcrumbs with exercise name (show loading placeholder while fetching)
  useBreadcrumbs([
    { label: 'Home', path: '/', icon: faHome },
    { label: 'Exercises', path: '/exercises' },
    { label: exercise?.name ?? 'Loading...' },
  ])

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
    if (!exercise || !can('edit_exercise')) return false
    return (
      exercise.status !== ExerciseStatus.Completed &&
      exercise.status !== ExerciseStatus.Archived
    )
  }, [exercise, can])

  const handleEdit = () => {
    if (
      exercise?.status === ExerciseStatus.Completed ||
      exercise?.status === ExerciseStatus.Archived
    ) {
      return // Should not reach here, but safety check
    }
    navigate(`/exercises/${id}/edit`)
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
        deliveryMode: values.deliveryMode,
        timelineMode: values.timelineMode,
        clockMultiplier: values.clockMultiplier,
        directorId: values.directorId?.trim() || undefined,
      }

      await updateExercise(request)

      // Update target capabilities if changed (S04)
      if (values.targetCapabilityIds !== undefined) {
        await exerciseCapabilityService.setTargetCapabilities(
          id!,
          values.targetCapabilityIds,
        )
        // Invalidate capabilities cache so detail view reflects updates
        queryClient.invalidateQueries({
          queryKey: exerciseCapabilityKeys.targetCapabilities(id!),
        })
      }

      setIsDirty(false)
      // Navigate back to detail view after successful save
      navigate(`/exercises/${id}`, { replace: true })
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
    setIsDirty(false)
    // Navigate back to detail view
    navigate(`/exercises/${id}`, { replace: true })
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
            {/* Contextual Help */}
            <HelpTooltip helpKey="hub.overview" exerciseRole={effectiveRole ?? undefined} compact />
            {/* User's Exercise Role Badge */}
            <EffectiveRoleBadge exerciseId={id ?? null} showOverride />
            {canEdit && !isEditing && (
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faPen} />}
                onClick={handleEdit}
              >
                Edit
              </CobraSecondaryButton>
            )}
            {!isEditing && (
              <ExerciseStatusActions
                exercise={exercise}
                isReadyToActivate={setupProgress?.isReadyToActivate}
              />
            )}
            {/* More menu for lifecycle actions */}
            {!isEditing && can('edit_exercise') && (
              <>
                <CobraIconButton
                  onClick={handleMoreMenuOpen}
                  aria-label="More actions"
                  aria-controls={moreMenuAnchor ? 'exercise-more-menu' : undefined}
                  aria-haspopup="true"
                  aria-expanded={moreMenuAnchor ? 'true' : undefined}
                >
                  <FontAwesomeIcon icon={faEllipsisVertical} />
                </CobraIconButton>
                <Menu
                  id="exercise-more-menu"
                  anchorEl={moreMenuAnchor}
                  open={Boolean(moreMenuAnchor)}
                  onClose={handleMoreMenuClose}
                  anchorOrigin={{
                    vertical: 'bottom',
                    horizontal: 'right',
                  }}
                  transformOrigin={{
                    vertical: 'top',
                    horizontal: 'right',
                  }}
                >
                  <MenuItem
                    onClick={() => {
                      handleMoreMenuClose()
                      openSettingsDialog()
                    }}
                  >
                    <ListItemIcon>
                      <FontAwesomeIcon icon={faGear} />
                    </ListItemIcon>
                    <ListItemText>Settings</ListItemText>
                  </MenuItem>
                  <Divider />
                  <MenuItem
                    onClick={() => {
                      handleMoreMenuClose()
                      openDuplicateDialog()
                    }}
                  >
                    <ListItemIcon>
                      <FontAwesomeIcon icon={faCopy} />
                    </ListItemIcon>
                    <ListItemText>Duplicate</ListItemText>
                  </MenuItem>
                  {canArchive && (
                    <MenuItem
                      onClick={() => {
                        handleMoreMenuClose()
                        openArchiveDialog()
                      }}
                    >
                      <ListItemIcon>
                        <FontAwesomeIcon icon={faBoxArchive} />
                      </ListItemIcon>
                      <ListItemText>Archive</ListItemText>
                    </MenuItem>
                  )}
                  {canDelete && [
                    <Divider key="delete-divider" />,
                    <MenuItem
                      key="delete-item"
                      onClick={() => {
                        handleMoreMenuClose()
                        openDeleteDialog()
                      }}
                      sx={{ color: 'error.main' }}
                    >
                      <ListItemIcon sx={{ color: 'error.main' }}>
                        <FontAwesomeIcon icon={faTrash} />
                      </ListItemIcon>
                      <ListItemText>Delete</ListItemText>
                    </MenuItem>,
                  ]}
                </Menu>
              </>
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

      {/* Content */}
      {isEditing ? (
        <Paper sx={{ p: 2 }}>
          <ExerciseForm
            exercise={exercise}
            onSubmit={handleSubmit}
            onCancel={handleCancel}
            isSubmitting={isSubmitting}
            disabledFields={disabledFields}
            onDirtyChange={handleDirtyChange}
            director={directorAsUser}
          />
        </Paper>
      ) : (
        <ExerciseDetailTabs
          exercise={exercise}
          exerciseId={id!}
          activeTab={activeTab}
          onTabChange={setActiveTab}
          canEdit={canEdit}
          setupProgress={setupProgress}
          setupProgressLoading={setupProgressLoading}
          setupProgressError={setupProgressError}
          mselSummary={mselSummary}
          director={director}
        />
      )}

      {/* Duplicate Exercise Dialog */}
      <DuplicateExerciseDialog
        open={duplicateDialogOpen}
        exercise={exercise}
        onClose={closeDuplicateDialog}
        onSubmit={async request => {
          await duplicate({ exerciseId: exercise.id, request })
          closeDuplicateDialog()
        }}
        isSubmitting={isDuplicating}
      />

      {/* Archive Exercise Dialog */}
      <ArchiveExerciseDialog
        open={archiveDialogOpen}
        exercise={exercise}
        onClose={closeArchiveDialog}
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
        onClose={closeDeleteDialog}
        onDeleted={handleDeleted}
      />

      {/* Exercise Settings Dialog */}
      <ExerciseSettingsDialog
        open={settingsDialogOpen}
        exerciseId={exercise.id}
        exerciseName={exercise.name}
        onClose={closeSettingsDialog}
      />

      {/* Unsaved changes dialog for navigation blocking */}
      <UnsavedChangesDialog />
    </Box>
  )
}

export default ExerciseDetailPage
