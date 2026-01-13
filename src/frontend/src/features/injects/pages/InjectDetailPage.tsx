import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Typography,
  Stack,
  Paper,
  Skeleton,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Divider,
  Grid,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faArrowLeft,
  faPen,
  faTrash,
  faPlay,
  faForwardStep,
  faClock,
  faUser,
  faPaperPlane,
  faFileLines,
  faHome,
} from '@fortawesome/free-solid-svg-icons'
import { format, parseISO } from 'date-fns'

import { useInject } from '../hooks/useInject'
import { useInjects } from '../hooks/useInjects'
import { useExercise } from '../../exercises/hooks/useExercise'
import { useBreadcrumbs } from '../../../core/contexts'
import { InjectStatusChip, InjectTypeChip } from '../components'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraDeleteButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { usePermissions } from '../../../shared/hooks'
import { InjectStatus, DeliveryMethod } from '../../../types'
import {
  formatScenarioTime,
  formatScheduledTime,
  calculateVariance,
} from '../types'

/**
 * Inject Detail Page (S03)
 *
 * Displays full inject details including:
 * - Time information (scheduled, scenario, fired)
 * - Targeting (from, to, method)
 * - Content (description, expected action)
 * - Controller notes
 * - Status with fire/skip actions
 */
export const InjectDetailPage = () => {
  const navigate = useNavigate()
  const { exerciseId, injectId } = useParams<{
    exerciseId: string
    injectId: string
  }>()
  const { exercise } = useExercise(exerciseId || '')
  const { inject, loading, error } = useInject(exerciseId || '', injectId || '')
  const { fireInject, skipInject, deleteInject, isFiring, isSkipping, isDeleting } =
    useInjects(exerciseId || '')
  const { canFireInjects, canDelete } = usePermissions()

  // Set custom breadcrumbs with exercise name, MSEL, and inject number
  useBreadcrumbs(
    exercise && inject
      ? [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Exercises', path: '/exercises' },
        { label: exercise.name, path: `/exercises/${exerciseId}` },
        { label: 'MSEL', path: `/exercises/${exerciseId}/msel` },
        { label: `Inject #${inject.injectNumber}` },
      ]
      : undefined,
  )

  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipReason, setSkipReason] = useState('')
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)

  const handleBackClick = () => {
    navigate(`/exercises/${exerciseId}/msel`)
  }

  const handleEditClick = () => {
    navigate(`/exercises/${exerciseId}/injects/${injectId}/edit`)
  }

  const handleFireClick = async () => {
    if (inject) {
      await fireInject(inject.id)
    }
  }

  const handleSkipClick = () => {
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  const handleSkipConfirm = async () => {
    if (inject && skipReason.trim()) {
      await skipInject(inject.id, { reason: skipReason.trim() })
      setSkipDialogOpen(false)
      setSkipReason('')
    }
  }

  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipReason('')
  }

  const handleDeleteClick = () => {
    setDeleteDialogOpen(true)
  }

  const handleDeleteConfirm = async () => {
    if (inject) {
      await deleteInject(inject.id)
      navigate(`/exercises/${exerciseId}/msel`)
    }
  }

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false)
  }

  const formatDateTime = (isoString: string) => {
    try {
      return format(parseISO(isoString), "MMM d, yyyy 'at' h:mm a")
    } catch {
      return isoString
    }
  }

  const getDeliveryMethodLabel = (method: DeliveryMethod | null): string => {
    if (!method) return '—'
    const labels: Record<DeliveryMethod, string> = {
      Verbal: 'Verbal',
      Phone: 'Phone Call',
      Email: 'Email',
      Radio: 'Radio',
      Written: 'Written Document',
      Simulation: 'Simulation',
      Other: 'Other',
    }
    return labels[method] || method
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
  if (loading || !inject) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Stack direction="row" alignItems="center" spacing={1} marginBottom={3}>
          <IconButton onClick={handleBackClick} size="small">
            <FontAwesomeIcon icon={faArrowLeft} />
          </IconButton>
          <Skeleton variant="text" width={300} height={40} />
        </Stack>
        <Paper sx={{ p: 3 }}>
          <Skeleton variant="text" width="80%" height={30} />
          <Skeleton variant="text" width="60%" height={24} />
          <Skeleton variant="rectangular" height={200} sx={{ mt: 2 }} />
        </Paper>
      </Box>
    )
  }

  const isPending = inject.status === InjectStatus.Pending
  const isFired = inject.status === InjectStatus.Fired
  const isSkipped = inject.status === InjectStatus.Skipped
  const scenarioTimeDisplay = formatScenarioTime(
    inject.scenarioDay,
    inject.scenarioTime,
  )
  const scheduledTimeDisplay = formatScheduledTime(inject.scheduledTime)

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Header */}
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="flex-start"
        marginBottom={3}
      >
        <Stack direction="row" alignItems="center" spacing={1}>
          <IconButton onClick={handleBackClick} size="small">
            <FontAwesomeIcon icon={faArrowLeft} />
          </IconButton>
          <Box>
            <Typography variant="caption" color="text.secondary">
              INJ-{inject.injectNumber.toString().padStart(3, '0')}
            </Typography>
            <Typography variant="h5" component="h1">
              {inject.title}
            </Typography>
          </Box>
        </Stack>

        <Stack direction="row" spacing={1} alignItems="center">
          <InjectStatusChip status={inject.status} />
          <InjectTypeChip type={inject.injectType} />

          {/* Action buttons */}
          {isPending && canFireInjects && (
            <>
              <CobraPrimaryButton
                startIcon={<FontAwesomeIcon icon={faPlay} />}
                onClick={handleFireClick}
                disabled={isFiring}
              >
                Fire
              </CobraPrimaryButton>
              <CobraSecondaryButton
                startIcon={<FontAwesomeIcon icon={faForwardStep} />}
                onClick={handleSkipClick}
                disabled={isSkipping}
              >
                Skip
              </CobraSecondaryButton>
            </>
          )}

          {canFireInjects && (
            <IconButton onClick={handleEditClick} size="small">
              <FontAwesomeIcon icon={faPen} />
            </IconButton>
          )}

          {canDelete && (
            <IconButton onClick={handleDeleteClick} size="small" color="error">
              <FontAwesomeIcon icon={faTrash} />
            </IconButton>
          )}
        </Stack>
      </Stack>

      <Grid container spacing={3}>
        {/* Left Column - Main Content */}
        <Grid size={{ xs: 12, md: 8 }}>
          {/* Time Section */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              TIME
            </Typography>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faClock} style={{ color: 'rgba(0, 0, 0, 0.54)', fontSize: '1rem' }} />
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Scheduled
                    </Typography>
                    <Typography variant="body1">{scheduledTimeDisplay}</Typography>
                  </Box>
                </Stack>
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faFileLines} style={{ color: 'rgba(0, 0, 0, 0.54)', fontSize: '1rem' }} />
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Scenario Time
                    </Typography>
                    <Typography variant="body1">
                      {scenarioTimeDisplay ?? 'Not set'}
                    </Typography>
                  </Box>
                </Stack>
              </Grid>
            </Grid>

            {/* Fired/Skipped Info */}
            {isFired && inject.firedAt && (
              <>
                <Divider sx={{ my: 2 }} />
                <Stack direction="row" spacing={4}>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Fired At
                    </Typography>
                    <Typography variant="body1">
                      {formatDateTime(inject.firedAt)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      By
                    </Typography>
                    <Typography variant="body1">
                      {inject.firedByName ?? 'Unknown'}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Variance
                    </Typography>
                    <Typography variant="body1">
                      {calculateVariance(inject.scheduledTime, inject.firedAt)}
                    </Typography>
                  </Box>
                </Stack>
              </>
            )}

            {isSkipped && inject.skippedAt && (
              <>
                <Divider sx={{ my: 2 }} />
                <Stack spacing={1}>
                  <Stack direction="row" spacing={4}>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Skipped At
                      </Typography>
                      <Typography variant="body1">
                        {formatDateTime(inject.skippedAt)}
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        By
                      </Typography>
                      <Typography variant="body1">
                        {inject.skippedByName ?? 'Unknown'}
                      </Typography>
                    </Box>
                  </Stack>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Reason
                    </Typography>
                    <Typography variant="body1">{inject.skipReason}</Typography>
                  </Box>
                </Stack>
              </>
            )}
          </Paper>

          {/* Targeting Section */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              TARGETING
            </Typography>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faPaperPlane} style={{ color: 'rgba(0, 0, 0, 0.54)', fontSize: '1rem' }} />
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      From
                    </Typography>
                    <Typography variant="body1">
                      {inject.source ?? '—'}
                    </Typography>
                  </Box>
                </Stack>
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <FontAwesomeIcon icon={faUser} style={{ color: 'rgba(0, 0, 0, 0.54)', fontSize: '1rem' }} />
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      To
                    </Typography>
                    <Typography variant="body1">{inject.target}</Typography>
                  </Box>
                </Stack>
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Method
                  </Typography>
                  <Typography variant="body1">
                    {getDeliveryMethodLabel(inject.deliveryMethod)}
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          </Paper>

          {/* Content Section */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              CONTENT
            </Typography>

            <Box mb={3}>
              <Typography variant="caption" color="text.secondary">
                Description
              </Typography>
              <Typography
                variant="body1"
                sx={{ whiteSpace: 'pre-wrap', mt: 0.5 }}
              >
                {inject.description}
              </Typography>
            </Box>

            {inject.expectedAction && (
              <Box>
                <Typography variant="caption" color="text.secondary">
                  Expected Action
                </Typography>
                <Typography
                  variant="body1"
                  sx={{ whiteSpace: 'pre-wrap', mt: 0.5 }}
                >
                  {inject.expectedAction}
                </Typography>
              </Box>
            )}
          </Paper>

          {/* Controller Notes (only shown if exists) */}
          {inject.controllerNotes && (
            <Paper sx={{ p: 3, mb: 3, backgroundColor: 'grey.50' }}>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                CONTROLLER NOTES
              </Typography>
              <Typography
                variant="body2"
                sx={{ whiteSpace: 'pre-wrap', fontStyle: 'italic' }}
              >
                {inject.controllerNotes}
              </Typography>
            </Paper>
          )}
        </Grid>

        {/* Right Column - Metadata */}
        <Grid size={{ xs: 12, md: 4 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="subtitle2" color="text.secondary" gutterBottom>
              DETAILS
            </Typography>

            <Stack spacing={2}>
              <Box>
                <Typography variant="caption" color="text.secondary">
                  Inject Number
                </Typography>
                <Typography variant="body1">
                  INJ-{inject.injectNumber.toString().padStart(3, '0')}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  Phase
                </Typography>
                <Typography variant="body1">
                  {inject.phaseName ?? 'Unassigned'}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  Type
                </Typography>
                <Typography variant="body1">{inject.injectType}</Typography>
              </Box>

              {inject.triggerCondition && (
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Trigger Condition
                  </Typography>
                  <Typography variant="body2" sx={{ fontStyle: 'italic' }}>
                    {inject.triggerCondition}
                  </Typography>
                </Box>
              )}

              <Divider />

              <Box>
                <Typography variant="caption" color="text.secondary">
                  Created
                </Typography>
                <Typography variant="body2">
                  {formatDateTime(inject.createdAt)}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" color="text.secondary">
                  Last Updated
                </Typography>
                <Typography variant="body2">
                  {formatDateTime(inject.updatedAt)}
                </Typography>
              </Box>
            </Stack>
          </Paper>
        </Grid>
      </Grid>

      {/* Skip Reason Dialog */}
      <Dialog
        open={skipDialogOpen}
        onClose={handleSkipCancel}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" marginBottom={2}>
            Please provide a reason for skipping this inject. This will be
            recorded for the after-action report.
          </Typography>
          <CobraTextField
            label="Skip Reason"
            value={skipReason}
            onChange={e => setSkipReason(e.target.value)}
            multiline
            rows={3}
            fullWidth
            required
            placeholder="e.g., Time constraints, players ahead of schedule, etc."
          />
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleSkipCancel}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleSkipConfirm}
            disabled={!skipReason.trim() || isSkipping}
          >
            Skip Inject
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={handleDeleteCancel} maxWidth="sm">
        <DialogTitle>Delete Inject?</DialogTitle>
        <DialogContent>
          <Typography variant="body1">
            Are you sure you want to delete inject INJ-
            {inject.injectNumber.toString().padStart(3, '0')} "{inject.title}"?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleDeleteCancel}>
            Cancel
          </CobraSecondaryButton>
          <CobraDeleteButton onClick={handleDeleteConfirm} disabled={isDeleting}>
            Delete
          </CobraDeleteButton>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default InjectDetailPage
