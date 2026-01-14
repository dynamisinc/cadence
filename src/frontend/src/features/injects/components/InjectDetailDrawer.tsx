/**
 * InjectDetailDrawer Component
 *
 * Slide-out drawer showing full inject details during conduct.
 * Provides read-only view of inject content without navigating away from conduct page.
 * Includes fire/skip/reset actions if user has control permissions.
 */

import {
  Drawer,
  Box,
  Typography,
  Stack,
  IconButton,
  Divider,
  Chip,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faXmark,
  faPlay,
  faForwardStep,
  faRotateLeft,
  faClock,
  faCalendarDay,
  faBullseye,
  faUser,
  faEnvelope,
  faClipboardList,
  faNoteSticky,
  faCodeBranch,
} from '@fortawesome/free-solid-svg-icons'

import { InjectStatusChip, InjectTypeChip } from './'
import { CobraPrimaryButton, CobraSecondaryButton, CobraDeleteButton } from '../../../theme/styledComponents'
import { InjectStatus, DeliveryMethod } from '../../../types'
import type { InjectDto } from '../types'
import { formatScheduledTime, formatScenarioTime, formatOffset } from '../types'

interface InjectDetailDrawerProps {
  /** The inject to display, or null to close drawer */
  inject: InjectDto | null
  /** Offset in milliseconds from exercise start */
  offsetMs?: number
  /** Whether the drawer is open */
  open: boolean
  /** Called when drawer should close */
  onClose: () => void
  /** Can the user control this inject (fire/skip/reset)? */
  canControl?: boolean
  /** Is an action currently being submitted? */
  isSubmitting?: boolean
  /** Called when fire button is clicked */
  onFire?: (injectId: string) => void
  /** Called when skip button is clicked */
  onSkip?: (injectId: string) => void
  /** Called when reset button is clicked */
  onReset?: (injectId: string) => void
}

const deliveryMethodLabels: Record<DeliveryMethod, string> = {
  [DeliveryMethod.Verbal]: 'Verbal',
  [DeliveryMethod.Phone]: 'Phone',
  [DeliveryMethod.Email]: 'Email',
  [DeliveryMethod.Radio]: 'Radio',
  [DeliveryMethod.Written]: 'Written',
  [DeliveryMethod.Simulation]: 'Simulation',
  [DeliveryMethod.Other]: 'Other',
}

export const InjectDetailDrawer = ({
  inject,
  offsetMs,
  open,
  onClose,
  canControl = false,
  isSubmitting = false,
  onFire,
  onSkip,
  onReset,
}: InjectDetailDrawerProps) => {
  if (!inject) return null

  const isPending = inject.status === InjectStatus.Pending
  const isFired = inject.status === InjectStatus.Fired
  const isSkipped = inject.status === InjectStatus.Skipped

  const handleFire = () => {
    if (onFire) onFire(inject.id)
  }

  const handleSkip = () => {
    if (onSkip) onSkip(inject.id)
  }

  const handleReset = () => {
    if (onReset) onReset(inject.id)
  }

  // Format fired/skipped time for display
  const formatActionTime = (isoString: string | null): string => {
    if (!isoString) return ''
    const date = new Date(isoString)
    return date.toLocaleString([], {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      PaperProps={{
        sx: {
          width: { xs: '100%', sm: 420 },
          maxWidth: '100vw',
        },
      }}
    >
      <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        {/* Header */}
        <Box
          sx={{
            p: 2,
            borderBottom: 1,
            borderColor: 'divider',
            backgroundColor: 'background.default',
          }}
        >
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
            <Box sx={{ flex: 1, pr: 2 }}>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                <Typography variant="h6" component="span">
                  #{inject.injectNumber}
                </Typography>
                <InjectTypeChip type={inject.injectType} />
                <InjectStatusChip status={inject.status} />
              </Stack>
              <Typography variant="h6" fontWeight={500}>
                {inject.title}
              </Typography>
            </Box>
            <IconButton onClick={onClose} size="small">
              <FontAwesomeIcon icon={faXmark} />
            </IconButton>
          </Stack>
        </Box>

        {/* Content - Scrollable */}
        <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
          <Stack spacing={3}>
            {/* Time Information */}
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faClock} style={{ marginRight: 8 }} />
                Timing
              </Typography>
              <Stack spacing={1} sx={{ pl: 3 }}>
                <Stack direction="row" spacing={2}>
                  <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                    Scheduled:
                  </Typography>
                  <Typography variant="body2" fontFamily="monospace">
                    {formatScheduledTime(inject.scheduledTime)}
                  </Typography>
                </Stack>
                {offsetMs !== undefined && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Offset:
                    </Typography>
                    <Typography variant="body2" fontFamily="monospace">
                      {formatOffset(offsetMs)}
                    </Typography>
                  </Stack>
                )}
                {inject.scenarioDay && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Scenario:
                    </Typography>
                    <Typography variant="body2">
                      {formatScenarioTime(inject.scenarioDay, inject.scenarioTime)}
                    </Typography>
                  </Stack>
                )}
                {inject.phaseName && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Phase:
                    </Typography>
                    <Chip label={inject.phaseName} size="small" />
                  </Stack>
                )}
              </Stack>
            </Box>

            <Divider />

            {/* Target & Source */}
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faBullseye} style={{ marginRight: 8 }} />
                Delivery
              </Typography>
              <Stack spacing={1} sx={{ pl: 3 }}>
                <Stack direction="row" spacing={2}>
                  <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                    Target:
                  </Typography>
                  <Typography variant="body2" fontWeight={500}>
                    {inject.target}
                  </Typography>
                </Stack>
                {inject.source && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Source:
                    </Typography>
                    <Typography variant="body2">
                      {inject.source}
                    </Typography>
                  </Stack>
                )}
                {inject.deliveryMethod && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Method:
                    </Typography>
                    <Chip
                      icon={<FontAwesomeIcon icon={faEnvelope} />}
                      label={deliveryMethodLabels[inject.deliveryMethod]}
                      size="small"
                      variant="outlined"
                    />
                  </Stack>
                )}
              </Stack>
            </Box>

            <Divider />

            {/* Description */}
            <Box>
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                <FontAwesomeIcon icon={faClipboardList} style={{ marginRight: 8 }} />
                Description
              </Typography>
              <Typography
                variant="body2"
                sx={{
                  pl: 3,
                  whiteSpace: 'pre-wrap',
                  backgroundColor: 'action.hover',
                  p: 1.5,
                  borderRadius: 1,
                }}
              >
                {inject.description || 'No description provided.'}
              </Typography>
            </Box>

            {/* Expected Action */}
            {inject.expectedAction && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    <FontAwesomeIcon icon={faUser} style={{ marginRight: 8 }} />
                    Expected Action
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      pl: 3,
                      whiteSpace: 'pre-wrap',
                    }}
                  >
                    {inject.expectedAction}
                  </Typography>
                </Box>
              </>
            )}

            {/* Controller Notes */}
            {inject.controllerNotes && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    <FontAwesomeIcon icon={faNoteSticky} style={{ marginRight: 8 }} />
                    Controller Notes
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      pl: 3,
                      whiteSpace: 'pre-wrap',
                      fontStyle: 'italic',
                      color: 'text.secondary',
                    }}
                  >
                    {inject.controllerNotes}
                  </Typography>
                </Box>
              </>
            )}

            {/* Trigger Condition (for branching injects) */}
            {inject.triggerCondition && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    <FontAwesomeIcon icon={faCodeBranch} style={{ marginRight: 8 }} />
                    Trigger Condition
                  </Typography>
                  <Typography
                    variant="body2"
                    sx={{
                      pl: 3,
                      whiteSpace: 'pre-wrap',
                    }}
                  >
                    {inject.triggerCondition}
                  </Typography>
                </Box>
              </>
            )}

            {/* Fired/Skipped Info */}
            {(isFired || isSkipped) && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    <FontAwesomeIcon icon={faCalendarDay} style={{ marginRight: 8 }} />
                    {isFired ? 'Fired' : 'Skipped'} Info
                  </Typography>
                  <Stack spacing={1} sx={{ pl: 3 }}>
                    {isFired && inject.firedAt && (
                      <>
                        <Stack direction="row" spacing={2}>
                          <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                            Fired at:
                          </Typography>
                          <Typography variant="body2">
                            {formatActionTime(inject.firedAt)}
                          </Typography>
                        </Stack>
                        {inject.firedByName && (
                          <Stack direction="row" spacing={2}>
                            <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                              Fired by:
                            </Typography>
                            <Typography variant="body2">
                              {inject.firedByName}
                            </Typography>
                          </Stack>
                        )}
                      </>
                    )}
                    {isSkipped && inject.skippedAt && (
                      <>
                        <Stack direction="row" spacing={2}>
                          <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                            Skipped at:
                          </Typography>
                          <Typography variant="body2">
                            {formatActionTime(inject.skippedAt)}
                          </Typography>
                        </Stack>
                        {inject.skippedByName && (
                          <Stack direction="row" spacing={2}>
                            <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                              Skipped by:
                            </Typography>
                            <Typography variant="body2">
                              {inject.skippedByName}
                            </Typography>
                          </Stack>
                        )}
                        {inject.skipReason && (
                          <Stack direction="row" spacing={2}>
                            <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                              Reason:
                            </Typography>
                            <Typography variant="body2" fontStyle="italic">
                              {inject.skipReason}
                            </Typography>
                          </Stack>
                        )}
                      </>
                    )}
                  </Stack>
                </Box>
              </>
            )}
          </Stack>
        </Box>

        {/* Action Footer - Always visible with Close button */}
        <Box
          sx={{
            p: 2,
            borderTop: 1,
            borderColor: 'divider',
            backgroundColor: 'background.default',
          }}
        >
          <Stack direction="row" spacing={1} justifyContent="space-between">
            <CobraSecondaryButton
              size="small"
              startIcon={<FontAwesomeIcon icon={faXmark} />}
              onClick={onClose}
            >
              Close
            </CobraSecondaryButton>
            {canControl && (
              <Stack direction="row" spacing={1}>
                {isPending && (
                  <>
                    <CobraSecondaryButton
                      size="small"
                      startIcon={<FontAwesomeIcon icon={faForwardStep} />}
                      onClick={handleSkip}
                      disabled={isSubmitting}
                    >
                      Skip
                    </CobraSecondaryButton>
                    <CobraPrimaryButton
                      size="small"
                      startIcon={<FontAwesomeIcon icon={faPlay} />}
                      onClick={handleFire}
                      disabled={isSubmitting}
                      color="success"
                    >
                      Fire Inject
                    </CobraPrimaryButton>
                  </>
                )}
                {(isFired || isSkipped) && (
                  <CobraDeleteButton
                    size="small"
                    startIcon={<FontAwesomeIcon icon={faRotateLeft} />}
                    onClick={handleReset}
                    disabled={isSubmitting}
                  >
                    Reset to Pending
                  </CobraDeleteButton>
                )}
              </Stack>
            )}
          </Stack>
        </Box>
      </Box>
    </Drawer>
  )
}

export default InjectDetailDrawer
