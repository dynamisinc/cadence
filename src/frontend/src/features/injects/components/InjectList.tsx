/**
 * InjectList Component
 *
 * Displays a list of injects for exercise conduct with fire/skip/reset controls.
 * Used in the ExerciseConductPage.
 */

import { useState } from 'react'
import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Typography,
  Stack,
  IconButton,
  Tooltip,
  Skeleton,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faPlay,
  faForwardStep,
  faRotateLeft,
} from '@fortawesome/free-solid-svg-icons'

import { InjectStatusChip, InjectTypeChip } from './'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../theme/styledComponents'
import { InjectStatus } from '../../../types'
import type { InjectDto, SkipInjectRequest } from '../types'
import { formatScheduledTime, formatScenarioTime } from '../types'

interface InjectListProps {
  /** List of injects to display */
  injects: InjectDto[]
  /** Is data loading? */
  loading?: boolean
  /** Error message if failed to load */
  error?: string | null
  /** Can the user control injects (fire/skip/reset)? */
  canControl?: boolean
  /** Called when fire button is clicked */
  onFire?: (injectId: string) => Promise<void>
  /** Called when skip button is clicked */
  onSkip?: (injectId: string, request: SkipInjectRequest) => Promise<void>
  /** Called when reset button is clicked */
  onReset?: (injectId: string) => Promise<void>
}

export const InjectList = ({
  injects,
  loading = false,
  error = null,
  canControl = false,
  onFire,
  onSkip,
  onReset,
}: InjectListProps) => {
  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipInjectId, setSkipInjectId] = useState<string | null>(null)
  const [skipReason, setSkipReason] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleFireClick = async (injectId: string) => {
    if (onFire) {
      setIsSubmitting(true)
      try {
        await onFire(injectId)
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  const handleSkipClick = (injectId: string) => {
    setSkipInjectId(injectId)
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  const handleSkipConfirm = async () => {
    if (skipInjectId && skipReason.trim() && onSkip) {
      setIsSubmitting(true)
      try {
        await onSkip(skipInjectId, { reason: skipReason.trim() })
        setSkipDialogOpen(false)
        setSkipInjectId(null)
        setSkipReason('')
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipInjectId(null)
    setSkipReason('')
  }

  const handleResetClick = async (injectId: string) => {
    if (onReset) {
      setIsSubmitting(true)
      try {
        await onReset(injectId)
      } finally {
        setIsSubmitting(false)
      }
    }
  }

  // Error state
  if (error) {
    return <Alert severity="error">{error}</Alert>
  }

  // Loading state
  if (loading && injects.length === 0) {
    return (
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>#</TableCell>
              <TableCell>Time</TableCell>
              <TableCell>Title</TableCell>
              <TableCell>Status</TableCell>
              {canControl && <TableCell>Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {[1, 2, 3].map((i) => (
              <TableRow key={i}>
                <TableCell><Skeleton width={30} /></TableCell>
                <TableCell><Skeleton width={60} /></TableCell>
                <TableCell><Skeleton width={200} /></TableCell>
                <TableCell><Skeleton width={70} /></TableCell>
                {canControl && <TableCell><Skeleton width={80} /></TableCell>}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    )
  }

  // Empty state
  if (injects.length === 0) {
    return (
      <Box sx={{ py: 4, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          No injects in this exercise's MSEL.
        </Typography>
      </Box>
    )
  }

  return (
    <>
      <TableContainer component={Paper} variant="outlined">
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell width={50}>#</TableCell>
              <TableCell width={80}>Time</TableCell>
              <TableCell>Title</TableCell>
              <TableCell width={80}>Type</TableCell>
              <TableCell width={90}>Status</TableCell>
              {canControl && <TableCell width={120}>Actions</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {injects.map((inject) => {
              const isPending = inject.status === InjectStatus.Pending
              const isFired = inject.status === InjectStatus.Fired
              const isSkipped = inject.status === InjectStatus.Skipped

              return (
                <TableRow
                  key={inject.id}
                  sx={{
                    opacity: isPending ? 1 : 0.7,
                    backgroundColor: isFired
                      ? 'success.50'
                      : isSkipped
                        ? 'warning.50'
                        : 'inherit',
                  }}
                >
                  <TableCell>
                    <Typography variant="body2" fontWeight={500}>
                      {inject.injectNumber}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {formatScheduledTime(inject.scheduledTime)}
                    </Typography>
                    {inject.scenarioDay && (
                      <Typography variant="caption" color="text.secondary" display="block">
                        {formatScenarioTime(inject.scenarioDay, inject.scenarioTime)}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography
                      variant="body2"
                      sx={{
                        maxWidth: 300,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                      }}
                    >
                      {inject.title}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <InjectTypeChip type={inject.injectType} />
                  </TableCell>
                  <TableCell>
                    <InjectStatusChip status={inject.status} />
                  </TableCell>
                  {canControl && (
                    <TableCell>
                      <Stack direction="row" spacing={0.5}>
                        {isPending && (
                          <>
                            <Tooltip title="Fire inject">
                              <IconButton
                                size="small"
                                color="success"
                                onClick={() => handleFireClick(inject.id)}
                                disabled={isSubmitting}
                              >
                                <FontAwesomeIcon icon={faPlay} size="sm" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Skip inject">
                              <IconButton
                                size="small"
                                color="warning"
                                onClick={() => handleSkipClick(inject.id)}
                                disabled={isSubmitting}
                              >
                                <FontAwesomeIcon icon={faForwardStep} size="sm" />
                              </IconButton>
                            </Tooltip>
                          </>
                        )}
                        {(isFired || isSkipped) && (
                          <Tooltip title="Reset to pending">
                            <IconButton
                              size="small"
                              onClick={() => handleResetClick(inject.id)}
                              disabled={isSubmitting}
                            >
                              <FontAwesomeIcon icon={faRotateLeft} size="sm" />
                            </IconButton>
                          </Tooltip>
                        )}
                      </Stack>
                    </TableCell>
                  )}
                </TableRow>
              )
            })}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Skip Reason Dialog */}
      <Dialog open={skipDialogOpen} onClose={handleSkipCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" marginBottom={2}>
            Please provide a reason for skipping this inject. This will be recorded
            for the after-action report.
          </Typography>
          <CobraTextField
            label="Skip Reason"
            value={skipReason}
            onChange={(e) => setSkipReason(e.target.value)}
            multiline
            rows={3}
            fullWidth
            required
            placeholder="e.g., Time constraints, players ahead of schedule, etc."
          />
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={handleSkipCancel} disabled={isSubmitting}>
            Cancel
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleSkipConfirm}
            disabled={!skipReason.trim() || isSubmitting}
          >
            Skip Inject
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    </>
  )
}

export default InjectList
