/**
 * ReadyToFireSection
 *
 * Displays injects with status = Ready that need immediate attention.
 * Highlighted with warning colors and prominent fire buttons.
 *
 * @module features/exercises
 * @see exercise-config/S06-clock-driven-conduct-view
 */

import { useState, useEffect } from 'react'
import {
  Box,
  Typography,
  Paper,
  Stack,
  IconButton,
  Collapse,
  Alert,
  Chip,
  Divider,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faBell,
  faChevronDown,
  faChevronUp,
  faEye,
  faFire,
  faForwardStep,
} from '@fortawesome/free-solid-svg-icons'
import { keyframes } from '@mui/system'

import type { InjectDto, SkipInjectRequest } from '../../../injects/types'
import { parseDeliveryTime, formatDeliveryTime, formatScenarioTime } from '../../../injects/types'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
  CobraTextField,
} from '../../../../theme/styledComponents'

// Pulse animation for bell icon
const pulse = keyframes`
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.7; transform: scale(1.1); }
`

interface ReadyToFireSectionProps {
  /** Injects with status = Ready */
  injects: InjectDto[]
  /** Current elapsed time in milliseconds */
  elapsedTimeMs: number
  /** Whether the current user can fire/skip */
  canControl?: boolean
  /** Whether an action is being submitted */
  isSubmitting?: boolean
  /** Called when fire button clicked */
  onFire: (injectId: string) => Promise<void> | void
  /** Called when skip button clicked */
  onSkip: (injectId: string, request: SkipInjectRequest) => Promise<void> | void
  /** Called when inject row is clicked to open details drawer */
  onInjectClick?: (inject: InjectDto) => void
  /**
   * Pre-confirmation callback for skip.
   * Returns true if confirmation dialog is being shown (reason dialog should wait).
   * Returns false if no confirmation needed (proceed to reason dialog immediately).
   */
  onSkipPreConfirmation?: (injectId: string) => boolean | null
  /** When set, opens the skip reason dialog for this inject (after pre-confirmation) */
  pendingSkipInjectId?: string | null
  /** Called when pending skip is cleared (dialog closed without completing) */
  onPendingSkipClear?: () => void
}

export const ReadyToFireSection = ({
  injects,
  elapsedTimeMs,
  canControl = true,
  isSubmitting = false,
  onFire,
  onSkip,
  onInjectClick,
  onSkipPreConfirmation,
  pendingSkipInjectId,
  onPendingSkipClear,
}: ReadyToFireSectionProps) => {
  const [expanded, setExpanded] = useState(true)
  const [skipDialogOpen, setSkipDialogOpen] = useState(false)
  const [skipInjectId, setSkipInjectId] = useState<string | null>(null)
  const [skipReason, setSkipReason] = useState('')

  // Handle pending skip from parent (after pre-confirmation)
  useEffect(() => {
    if (pendingSkipInjectId) {
      setSkipInjectId(pendingSkipInjectId)
      setSkipReason('')
      setSkipDialogOpen(true)
    }
  }, [pendingSkipInjectId])

  const handleSkipClick = (injectId: string) => {
    // Check if pre-confirmation is needed
    if (onSkipPreConfirmation) {
      const needsConfirmation = onSkipPreConfirmation(injectId)
      if (needsConfirmation) {
        // Parent will show pre-confirmation dialog first
        // When confirmed, pendingSkipInjectId will be set to trigger this dialog
        return
      }
    }
    // No pre-confirmation needed, open reason dialog directly
    setSkipInjectId(injectId)
    setSkipReason('')
    setSkipDialogOpen(true)
  }

  const handleSkipConfirm = async () => {
    if (skipInjectId && skipReason.trim()) {
      await onSkip(skipInjectId, { reason: skipReason.trim() })
      setSkipDialogOpen(false)
      setSkipInjectId(null)
      setSkipReason('')
      // Clear pending skip from parent
      onPendingSkipClear?.()
    }
  }

  const handleSkipCancel = () => {
    setSkipDialogOpen(false)
    setSkipInjectId(null)
    setSkipReason('')
    // Clear pending skip from parent
    onPendingSkipClear?.()
  }

  // Don't render section if no ready injects
  if (injects.length === 0) {
    return null
  }

  return (
    <>
      <Paper
        sx={{
          borderLeft: 4,
          borderLeftColor: 'warning.main',
          backgroundColor: 'warning.50',
        }}
      >
        {/* Section Header */}
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.5,
            p: 2,
            cursor: 'pointer',
            '&:hover': {
              backgroundColor: 'warning.100',
            },
          }}
          onClick={() => setExpanded(!expanded)}
        >
          <Box
            sx={{
              color: 'warning.dark',
              animation: `${pulse} 1.5s ease-in-out infinite`,
            }}
          >
            <FontAwesomeIcon icon={faBell} size="lg" />
          </Box>

          <Typography variant="h6" sx={{ flexGrow: 1, color: 'warning.dark', fontWeight: 600 }}>
            READY TO FIRE
          </Typography>

          <Chip
            label={injects.length}
            color="warning"
            size="small"
            sx={{ fontWeight: 600 }}
          />

          <IconButton size="small">
            <FontAwesomeIcon icon={expanded ? faChevronUp : faChevronDown} />
          </IconButton>
        </Box>

        {/* Section Content */}
        <Collapse in={expanded}>
          <Box sx={{ p: 2, pt: 0 }}>
            {injects.length === 0 ? (
              <Alert severity="info" sx={{ mt: 2 }}>
                No injects ready to fire.
              </Alert>
            ) : (
              <Stack spacing={2}>
                {injects.map((inject, index) => {
                  const deliveryTimeMs = parseDeliveryTime(inject.deliveryTime)
                  const overdue = deliveryTimeMs !== null && elapsedTimeMs > deliveryTimeMs

                  return (
                    <Box key={inject.id}>
                      {index > 0 && <Divider sx={{ my: 2 }} />}
                      <Paper
                        variant="outlined"
                        sx={{
                          p: 1.5,
                          backgroundColor: 'background.paper',
                          border: '2px solid',
                          borderColor: overdue ? 'error.main' : 'warning.light',
                        }}
                      >
                        {/* Inject Header Row: metadata + actions all in one line */}
                        <Stack
                          direction="row"
                          spacing={1}
                          alignItems="center"
                          sx={{ mb: 1 }}
                        >
                          {/* Left: number, status chips, time */}
                          <Chip
                            label={`#${inject.injectNumber}`}
                            size="small"
                            color="primary"
                            sx={{ fontWeight: 600 }}
                          />
                          {overdue && (
                            <Chip
                              label="OVERDUE"
                              size="small"
                              color="error"
                              sx={{ fontWeight: 600 }}
                            />
                          )}
                          {inject.deliveryTime && (
                            <Typography
                              variant="caption"
                              color="text.secondary"
                              fontFamily="monospace"
                            >
                              {formatDeliveryTime(parseDeliveryTime(inject.deliveryTime) ?? 0)}
                            </Typography>
                          )}
                          {formatScenarioTime(inject.scenarioDay, inject.scenarioTime) && (
                            <Chip
                              label={formatScenarioTime(inject.scenarioDay, inject.scenarioTime)}
                              size="small"
                              variant="outlined"
                              color="info"
                              sx={{ fontFamily: 'monospace' }}
                            />
                          )}

                          {/* Spacer */}
                          <Box sx={{ flex: 1 }} />

                          {/* Right: View + action buttons */}
                          {onInjectClick && (
                            <CobraSecondaryButton
                              size="small"
                              startIcon={<FontAwesomeIcon icon={faEye} />}
                              onClick={() => onInjectClick(inject)}
                            >
                              View
                            </CobraSecondaryButton>
                          )}
                          {canControl && (
                            <>
                              <CobraSecondaryButton
                                size="small"
                                startIcon={<FontAwesomeIcon icon={faForwardStep} />}
                                onClick={() => handleSkipClick(inject.id)}
                                disabled={isSubmitting}
                              >
                                Skip
                              </CobraSecondaryButton>
                              <CobraPrimaryButton
                                size="small"
                                startIcon={<FontAwesomeIcon icon={faFire} />}
                                onClick={() => onFire(inject.id)}
                                disabled={isSubmitting}
                                sx={{ fontWeight: 600 }}
                              >
                                Fire
                              </CobraPrimaryButton>
                            </>
                          )}
                        </Stack>

                        {/* Inject Title */}
                        <Typography
                          variant="h6"
                          sx={{ mb: 0.5 }}
                        >
                          {inject.title}
                        </Typography>

                        {/* Inject Description */}
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          sx={{
                            mb: 1,
                            fontStyle: 'italic',
                            p: 1,
                            backgroundColor: 'grey.50',
                            borderRadius: 1,
                            borderLeft: '3px solid',
                            borderLeftColor: 'primary.light',
                          }}
                        >
                          "{inject.description}"
                        </Typography>

                        {/* Delivery Context */}
                        {(inject.target || inject.source || inject.deliveryMethodName) && (
                          <Stack direction="row" spacing={2} flexWrap="wrap">
                            {inject.target && (
                              <Typography variant="body2">
                                <strong>To:</strong> {inject.target}
                              </Typography>
                            )}
                            {inject.source && (
                              <Typography variant="body2">
                                <strong>From:</strong> {inject.source}
                              </Typography>
                            )}
                            {inject.deliveryMethodName && (
                              <Typography variant="body2">
                                <strong>Via:</strong> {inject.deliveryMethodName}
                              </Typography>
                            )}
                          </Stack>
                        )}
                      </Paper>
                    </Box>
                  )
                })}
              </Stack>
            )}
          </Box>
        </Collapse>
      </Paper>

      {/* Skip Reason Dialog */}
      <Dialog open={skipDialogOpen} onClose={handleSkipCancel} maxWidth="sm" fullWidth>
        <DialogTitle>Skip Inject</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Please provide a reason for skipping this inject. This will be recorded for the
            after-action report.
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

export default ReadyToFireSection
