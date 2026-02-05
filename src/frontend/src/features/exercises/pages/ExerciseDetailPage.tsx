import { useMemo, useCallback, useState } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import {
  Box,
  Typography,
  Paper,
  CircularProgress,
  Grid,
  LinearProgress,
  Stack,
  Tabs,
  Tab,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHome, faPen, faCopy, faBoxArchive, faTrash, faUsers, faEllipsisVertical, faGear, faClipboardCheck } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import {
  useExercise,
  useSetupProgress,
  useDuplicateExercise,
  useExerciseStatus,
  useMselSummary,
  useExerciseParticipants,
  exerciseCapabilityKeys,
} from '../hooks'
import { exerciseCapabilityService } from '../services/exerciseCapabilityService'
import {
  ExerciseForm,
  ExerciseHeader,
  ExerciseStatusActions,
  SetupProgress,
  DuplicateExerciseDialog,
  ArchiveExerciseDialog,
  DeleteExerciseDialog,
  ExerciseSettingsDialog,
  TargetCapabilitiesDisplay,
} from '../components'
import { ObjectiveList } from '../../objectives'
import { CapabilityTargetList } from '../../eeg'
import { ExerciseParticipantsPage } from './ExerciseParticipantsPage'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraIconButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { useUnsavedChangesWarning } from '../../../shared/hooks'
import { useExerciseRole } from '../../auth/hooks/useExerciseRole'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus, DeliveryMode, TimelineMode } from '../../../types'
import { getExerciseTypeFullName } from '../../../theme/cobraTheme'
import { EffectiveRoleBadge } from '@/features/auth'
import type { CreateExerciseFormValues, UpdateExerciseRequest } from '../types'
import type { UserDto } from '../../users/types'

/**
 * Tab Panel Component
 */
interface TabPanelProps {
  children?: React.ReactNode
  index: number
  value: number
}

function TabPanel({ children, value, index }: TabPanelProps) {
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`exercise-tabpanel-${index}`}
      aria-labelledby={`exercise-tab-${index}`}
    >
      {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
    </div>
  )
}

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
  const { can } = useExerciseRole(id ?? null)
  const queryClient = useQueryClient()

  // Derive edit state from URL path (ends with /edit)
  const isEditing = location.pathname.endsWith('/edit')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDirty, setIsDirty] = useState(false)
  const [duplicateDialogOpen, setDuplicateDialogOpen] = useState(false)
  const [archiveDialogOpen, setArchiveDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [settingsDialogOpen, setSettingsDialogOpen] = useState(false)
  const [activeTab, setActiveTab] = useState(0)
  const [moreMenuAnchor, setMoreMenuAnchor] = useState<null | HTMLElement>(null)

  // Setup progress for Draft exercises
  const {
    data: setupProgress,
    isLoading: setupProgressLoading,
    error: setupProgressError,
  } = useSetupProgress(id ?? '')

  // MSEL summary for progress display
  const { data: mselSummary } = useMselSummary(id ?? '')

  // Duplicate exercise mutation
  const { duplicate, isDuplicating } = useDuplicateExercise()

  // Archive/delete actions
  const { archive, isTransitioning: isArchiving } = useExerciseStatus(id ?? '')

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

  const handleArchived = () => {
    // Navigate back to exercises list after archive
    navigate('/exercises')
  }

  const handleDeleted = () => {
    // Navigate back to exercises list after delete
    navigate('/exercises')
  }

  // More menu handlers
  const handleMoreMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setMoreMenuAnchor(event.currentTarget)
  }

  const handleMoreMenuClose = () => {
    setMoreMenuAnchor(null)
  }

  // Determine if exercise can be deleted (never published OR already archived)
  const canDelete = useMemo(() => {
    if (!exercise || !can('delete_exercise')) return false
    return !exercise.hasBeenPublished || exercise.status === ExerciseStatus.Archived
  }, [exercise, can])

  // Can archive if not already archived and user can edit
  const canArchive = useMemo(() => {
    if (!exercise || !can('edit_exercise')) return false
    return exercise.status !== ExerciseStatus.Archived
  }, [exercise, can])

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
                      setSettingsDialogOpen(true)
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
                      setDuplicateDialogOpen(true)
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
                        setArchiveDialogOpen(true)
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
                        setDeleteDialogOpen(true)
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
        <Paper sx={{ p: 3 }}>
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
        <>
          {/* Tabs */}
          <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
            <Tabs
              value={activeTab}
              onChange={(_, newValue) => setActiveTab(newValue)}
              aria-label="exercise detail tabs"
            >
              <Tab label="Details" id="exercise-tab-0" aria-controls="exercise-tabpanel-0" />
              <Tab label="Objectives" id="exercise-tab-1" aria-controls="exercise-tabpanel-1" />
              <Tab
                label="Participants"
                icon={<FontAwesomeIcon icon={faUsers} />}
                iconPosition="start"
                id="exercise-tab-2"
                aria-controls="exercise-tabpanel-2"
              />
              <Tab
                label="EEG Setup"
                icon={<FontAwesomeIcon icon={faClipboardCheck} />}
                iconPosition="start"
                id="exercise-tab-3"
                aria-controls="exercise-tabpanel-3"
              />
            </Tabs>
          </Box>

          {/* Tab Panels */}
          <TabPanel value={activeTab} index={0}>
            <Stack spacing={2}>
              {/* Top row: Two cards side-by-side for Draft, single column otherwise */}
              <Grid
                container
                spacing={2}
                sx={{
                  display: 'grid',
                  gridTemplateColumns: {
                    xs: '1fr',
                    md: exercise.status === ExerciseStatus.Draft
                      ? 'repeat(2, 1fr)'
                      : '1fr',
                  },
                  gap: 2,
                  alignItems: 'start',
                }}
              >
                {/* Left column: Exercise Details */}
                <Grid>
                  <Paper sx={{ p: 2 }}>

                    <Typography variant="h6" fontWeight={600} sx={{ mb: 1.5 }}>
                      Exercise Details
                    </Typography>

                    {/* Description */}
                    {exercise.description && (
                      <Box sx={{ mb: 1.5 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Description
                        </Typography>
                        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                          {exercise.description}
                        </Typography>
                      </Box>
                    )}

                    {/* Two-column grid for metadata */}
                    <Grid container spacing={1.5}>
                      {/* Schedule */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Date
                        </Typography>
                        <Typography variant="body2">
                          {formatDate(exercise.scheduledDate)}
                        </Typography>
                      </Grid>

                      {/* Time */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Time
                        </Typography>
                        <Typography variant="body2">
                          {exercise.startTime ? formatTime(exercise.startTime) : 'TBD'}
                          {exercise.endTime && ` - ${formatTime(exercise.endTime)}`}
                        </Typography>
                      </Grid>

                      {/* Time Zone */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Time Zone
                        </Typography>
                        <Typography variant="body2">{exercise.timeZoneId}</Typography>
                      </Grid>

                      {/* Location */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Location
                        </Typography>
                        <Typography variant="body2">
                          {exercise.location || 'Not specified'}
                        </Typography>
                      </Grid>

                      {/* Exercise Director */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Exercise Director
                        </Typography>
                        <Typography variant="body2">
                          {director?.displayName || 'Not assigned'}
                        </Typography>
                        {director?.email && (
                          <Typography variant="caption" color="text.secondary">
                            {director.email}
                          </Typography>
                        )}
                      </Grid>

                      {/* Exercise Type */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Exercise Type
                        </Typography>
                        <Typography variant="body2">
                          {getExerciseTypeFullName(exercise.exerciseType)}
                        </Typography>
                      </Grid>

                      {/* Practice Mode */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Mode
                        </Typography>
                        <Typography variant="body2">
                          {exercise.isPracticeMode ? 'Practice Mode' : 'Live Exercise'}
                        </Typography>
                      </Grid>

                      {/* Delivery Mode */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.25 }}
                        >
                          Inject Delivery
                        </Typography>
                        <Typography variant="body2">
                          {exercise.deliveryMode === DeliveryMode.ClockDriven
                            ? 'Clock-driven'
                            : 'Facilitator-paced'}
                        </Typography>
                      </Grid>

                      {/* Timeline Mode (only for Clock-driven) */}
                      {exercise.deliveryMode === DeliveryMode.ClockDriven && (
                        <Grid size={{ xs: 12, sm: 6 }}>
                          <Typography
                            variant="body2"
                            color="text.secondary"
                            fontWeight={500}
                            sx={{ mb: 0.25 }}
                          >
                            Timeline
                          </Typography>
                          <Typography variant="body2">
                            {exercise.timelineMode === TimelineMode.RealTime &&
                          'Real-time (1:1)'}
                            {exercise.timelineMode === TimelineMode.Compressed &&
                          `Compressed (${exercise.clockMultiplier}x)`}
                            {exercise.timelineMode === TimelineMode.StoryOnly &&
                          'Story-only'}
                          </Typography>
                        </Grid>
                      )}

                      {/* Created / Updated info */}
                      <Grid size={12}>
                        <Box
                          sx={{
                            mt: 1.5,
                            pt: 1.5,
                            borderTop: 1,
                            borderColor: 'divider',
                          }}
                        >
                          <Typography variant="caption" color="text.secondary">
                            Created {format(parseISO(exercise.createdAt), 'MMM d, yyyy')}
                            {exercise.updatedAt !== exercise.createdAt && (
                              <>
                                {' · '}
                                Last updated{' '}
                                {format(parseISO(exercise.updatedAt), 'MMM d, yyyy')}
                              </>
                            )}
                          </Typography>
                        </Box>
                      </Grid>
                    </Grid>
                  </Paper>
                </Grid>

                {/* Right column: Setup Progress (Draft only) */}
                {exercise.status === ExerciseStatus.Draft && (
                  <Grid>
                    <SetupProgress
                      progress={setupProgress}
                      isLoading={setupProgressLoading}
                      error={setupProgressError}
                    />
                  </Grid>
                )}
              </Grid>

              {/* Target Capabilities (S04) */}
              <TargetCapabilitiesDisplay exerciseId={id!} />

              {/* Bottom row: MSEL Progress */}
              {mselSummary && mselSummary.totalInjects > 0 && (
                <Paper sx={{ p: 2.5 }}>
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={2}
                    alignItems={{ sm: 'center' }}
                    justifyContent="space-between"
                  >
                    <Box sx={{ flex: 1 }}>
                      <Typography variant="subtitle1" fontWeight={600}>
                        MSEL Progress
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {mselSummary.releasedCount + mselSummary.deferredCount} of{' '}
                        {mselSummary.totalInjects} injects completed
                      </Typography>
                    </Box>

                    <Stack
                      direction="row"
                      spacing={3}
                      sx={{ minWidth: { sm: 300 } }}
                      alignItems="center"
                    >
                      <Box sx={{ flex: 1 }}>
                        <LinearProgress
                          variant="determinate"
                          value={mselSummary.completionPercentage}
                          sx={{
                            height: 8,
                            borderRadius: 4,
                            backgroundColor: 'grey.200',
                            '& .MuiLinearProgress-bar': {
                              borderRadius: 4,
                              backgroundColor:
                            mselSummary.completionPercentage === 100
                              ? 'success.main'
                              : 'primary.main',
                            },
                          }}
                        />
                      </Box>
                      <Typography
                        variant="body2"
                        fontWeight={600}
                        sx={{ minWidth: 45, textAlign: 'right' }}
                      >
                        {mselSummary.completionPercentage}%
                      </Typography>
                    </Stack>

                    <Stack direction="row" spacing={2}>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" fontWeight={600}>
                          {mselSummary.draftCount}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Draft
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" fontWeight={600} color="success.main">
                          {mselSummary.releasedCount}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Released
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" fontWeight={600} color="warning.main">
                          {mselSummary.deferredCount}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Deferred
                        </Typography>
                      </Box>
                    </Stack>
                  </Stack>
                </Paper>
              )}
            </Stack>
          </TabPanel>

          {/* Objectives Tab */}
          <TabPanel value={activeTab} index={1}>
            <Paper sx={{ p: 3 }}>
              <ObjectiveList exerciseId={exercise.id} canEdit={canEdit} />
            </Paper>
          </TabPanel>

          {/* Participants Tab */}
          <TabPanel value={activeTab} index={2}>
            <ExerciseParticipantsPage exerciseId={id} />
          </TabPanel>

          {/* EEG Setup Tab */}
          <TabPanel value={activeTab} index={3}>
            <Paper sx={{ p: 3 }}>
              <CapabilityTargetList exerciseId={exercise.id} canEdit={canEdit} />
            </Paper>
          </TabPanel>
        </>
      )}

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

      {/* Exercise Settings Dialog */}
      <ExerciseSettingsDialog
        open={settingsDialogOpen}
        exerciseId={exercise.id}
        exerciseName={exercise.name}
        onClose={() => setSettingsDialogOpen(false)}
      />

      {/* Unsaved changes dialog for navigation blocking */}
      <UnsavedChangesDialog />
    </Box>
  )
}

export default ExerciseDetailPage
