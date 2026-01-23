import { useMemo, useCallback, useState } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
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
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faHome, faList, faPen, faPlay, faCopy, faBoxArchive, faTrash, faUsers } from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import {
  useExercise,
  useSetupProgress,
  useDuplicateExercise,
  useExerciseStatus,
  useMselSummary,
} from '../hooks'
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
import { ExerciseParticipantsPage } from './ExerciseParticipantsPage'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraLinkButton,
  CobraDeleteButton,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { usePermissions, useUnsavedChangesWarning } from '../../../shared/hooks'
import { useBreadcrumbs } from '../../../core/contexts'
import { ExerciseStatus, DeliveryMode, TimelineMode } from '../../../types'
import { getExerciseTypeFullName } from '../../../theme/cobraTheme'
import { EffectiveRoleBadge } from '@/features/auth'
import type { CreateExerciseFormValues, UpdateExerciseRequest } from '../types'

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
  const { canManage } = usePermissions()

  // Derive edit state from URL path (ends with /edit)
  const isEditing = location.pathname.endsWith('/edit')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isDirty, setIsDirty] = useState(false)
  const [duplicateDialogOpen, setDuplicateDialogOpen] = useState(false)
  const [archiveDialogOpen, setArchiveDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [activeTab, setActiveTab] = useState(0)

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
        timeScale: values.timeScale ?? undefined,
        directorId: values.directorId?.trim() || undefined,
      }

      await updateExercise(request)
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
            {/* User's Exercise Role Badge */}
            <EffectiveRoleBadge exerciseId={id ?? null} showOverride />
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
            </Tabs>
          </Box>

          {/* Tab Panels */}
          <TabPanel value={activeTab} index={0}>
            <Stack spacing={2}>
              {/* Top row: Two equal-height cards */}
              <Grid
                container
                spacing={2}
                sx={{
                  // Equal height cards using CSS Grid
                  display: 'grid',
                  gridTemplateColumns: {
                    xs: '1fr',
                    md: exercise.status === ExerciseStatus.Draft
                      ? 'repeat(2, 1fr)'
                      : '1fr',
                  },
                  gap: 2,
                  // Fixed height for cards on desktop
                  '& > .MuiGrid-root': {
                    height: { md: 575 },
                  },
                }}
              >
                {/* Left column: Exercise Details */}
                <Grid>
                  <Paper
                    sx={{
                      p: 3,
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      overflow: 'auto',
                    }}
                  >
                    <Typography variant="h6" fontWeight={600} sx={{ mb: 3 }}>
                      Exercise Details
                    </Typography>

                    {/* Description */}
                    {exercise.description && (
                      <Box sx={{ mb: 3 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Description
                        </Typography>
                        <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>
                          {exercise.description}
                        </Typography>
                      </Box>
                    )}

                    {/* Two-column grid for metadata */}
                    <Grid container spacing={2.5}>
                      {/* Schedule */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Date
                        </Typography>
                        <Typography variant="body1">
                          {formatDate(exercise.scheduledDate)}
                        </Typography>
                      </Grid>

                      {/* Time */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Time
                        </Typography>
                        <Typography variant="body1">
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
                          sx={{ mb: 0.5 }}
                        >
                          Time Zone
                        </Typography>
                        <Typography variant="body1">{exercise.timeZoneId}</Typography>
                      </Grid>

                      {/* Location */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Location
                        </Typography>
                        <Typography variant="body1">
                          {exercise.location || 'Not specified'}
                        </Typography>
                      </Grid>

                      {/* Exercise Type */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Exercise Type
                        </Typography>
                        <Typography variant="body1">
                          {getExerciseTypeFullName(exercise.exerciseType)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          ({exercise.exerciseType})
                        </Typography>
                      </Grid>

                      {/* Practice Mode */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Mode
                        </Typography>
                        <Typography variant="body1">
                          {exercise.isPracticeMode ? 'Practice Mode' : 'Live Exercise'}
                        </Typography>
                      </Grid>

                      {/* Delivery Mode */}
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          fontWeight={500}
                          sx={{ mb: 0.5 }}
                        >
                          Inject Delivery
                        </Typography>
                        <Typography variant="body1">
                          {exercise.deliveryMode === DeliveryMode.ClockDriven
                            ? 'Clock-driven'
                            : 'Facilitator-paced'}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {exercise.deliveryMode === DeliveryMode.ClockDriven
                            ? 'Injects fire at scheduled times'
                            : 'Manual inject delivery'}
                        </Typography>
                      </Grid>

                      {/* Timeline Mode (only for Clock-driven) */}
                      {exercise.deliveryMode === DeliveryMode.ClockDriven && (
                        <Grid size={{ xs: 12, sm: 6 }}>
                          <Typography
                            variant="body2"
                            color="text.secondary"
                            fontWeight={500}
                            sx={{ mb: 0.5 }}
                          >
                            Timeline
                          </Typography>
                          <Typography variant="body1">
                            {exercise.timelineMode === TimelineMode.RealTime &&
                          'Real-time (1:1)'}
                            {exercise.timelineMode === TimelineMode.Compressed &&
                          `Compressed (${exercise.timeScale}x)`}
                            {exercise.timelineMode === TimelineMode.StoryOnly &&
                          'Story-only'}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {exercise.timelineMode === TimelineMode.RealTime &&
                          'Exercise clock matches wall clock'}
                            {exercise.timelineMode === TimelineMode.Compressed &&
                          `1 real minute = ${exercise.timeScale} story minutes`}
                            {exercise.timelineMode === TimelineMode.StoryOnly &&
                          'No real-time clock'}
                          </Typography>
                        </Grid>
                      )}

                      {/* Created / Updated info */}
                      <Grid size={12}>
                        <Box
                          sx={{
                            mt: 2,
                            pt: 2,
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
                        {mselSummary.firedCount + mselSummary.skippedCount} of{' '}
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
                          {mselSummary.pendingCount}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Pending
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" fontWeight={600} color="success.main">
                          {mselSummary.firedCount}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Fired
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="h6" fontWeight={600} color="warning.main">
                          {mselSummary.skippedCount}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Skipped
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
            <Paper
              sx={{
                p: 3,
                height: 575,
                display: 'flex',
                flexDirection: 'column',
                overflow: 'hidden',
              }}
            >
              <Box
                sx={{
                  flex: 1,
                  overflow: 'auto',
                  minHeight: 0,
                }}
              >
                <ObjectiveList exerciseId={exercise.id} canEdit={canEdit} />
              </Box>
            </Paper>
          </TabPanel>

          {/* Participants Tab */}
          <TabPanel value={activeTab} index={2}>
            <ExerciseParticipantsPage />
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

      {/* Unsaved changes dialog for navigation blocking */}
      <UnsavedChangesDialog />
    </Box>
  )
}

export default ExerciseDetailPage
