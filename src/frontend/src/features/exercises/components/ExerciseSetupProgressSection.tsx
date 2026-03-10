/**
 * ExerciseSetupProgressSection
 *
 * The "Details" tab content for the ExerciseDetailPage. Renders:
 * - Left column: Exercise Details card (schedule, time, timezone, location,
 *   director, type, mode, delivery, timeline, audit trail)
 * - Right column (Draft only): SetupProgress checklist card
 * - Full-width: TargetCapabilitiesDisplay (S04)
 * - Full-width: MSEL Progress bar (when injects exist)
 *
 * @module features/exercises
 */

import { FC } from 'react'
import {
  Box,
  Typography,
  Paper,
  Grid,
  LinearProgress,
  Stack,
} from '@mui/material'
import { formatDate } from '../../../shared/utils/dateUtils'
import { ExerciseStatus, DeliveryMode, TimelineMode } from '../../../types'
import { getExerciseTypeFullName } from '../../../theme/cobraTheme'
import { SetupProgress } from './SetupProgress'
import { TargetCapabilitiesDisplay } from './TargetCapabilitiesDisplay'
import type { ExerciseDto, SetupProgressDto, MselSummaryDto } from '../types'
import type { ExerciseParticipantDto } from '../types'

interface ExerciseSetupProgressSectionProps {
  /** The exercise to display */
  exercise: ExerciseDto
  /** Exercise ID (used for capability queries) */
  exerciseId: string
  /** Setup progress data */
  setupProgress: SetupProgressDto | undefined
  /** Whether setup progress is loading */
  setupProgressLoading: boolean
  /** Setup progress error */
  setupProgressError: Error | null
  /** MSEL summary for the progress bar */
  mselSummary: MselSummaryDto | undefined
  /** The assigned Exercise Director participant */
  director: ExerciseParticipantDto | undefined
}

/**
 * Formats a date string to a long readable format.
 */
const formatDateLong = (dateStr: string): string => {
  try {
    return formatDate(dateStr)
  } catch {
    return dateStr
  }
}

/**
 * Formats a time string (HH:MM:SS) to 12-hour display format.
 */
const formatTime = (timeStr: string | null): string | null => {
  if (!timeStr) return null
  try {
    const [hours, minutes] = timeStr.split(':')
    const hour = parseInt(hours, 10)
    const ampm = hour >= 12 ? 'PM' : 'AM'
    const hour12 = hour % 12 || 12
    return `${hour12}:${minutes} ${ampm}`
  } catch {
    return timeStr
  }
}

/**
 * Details tab content for the ExerciseDetailPage.
 *
 * Shows exercise metadata in a responsive grid layout. For Draft exercises,
 * also shows the setup progress checklist in the right column.
 */
export const ExerciseSetupProgressSection: FC<ExerciseSetupProgressSectionProps> = ({
  exercise,
  exerciseId,
  setupProgress,
  setupProgressLoading,
  setupProgressError,
  mselSummary,
  director,
}) => {
  return (
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
                  {formatDateLong(exercise.scheduledDate)}
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
                    Created {formatDate(exercise.createdAt)}
                    {exercise.updatedAt !== exercise.createdAt && (
                      <>
                        {' · '}
                        Last updated{' '}
                        {formatDate(exercise.updatedAt)}
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
      <TargetCapabilitiesDisplay exerciseId={exerciseId} />

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
  )
}

export default ExerciseSetupProgressSection
