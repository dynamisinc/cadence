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
  Tooltip,
  Alert,
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
  faCrosshairs,
  faLocationDot,
  faRoad,
  faUserTie,
  faFlag,
  faSitemap,
  faEye,
  faExclamationTriangle,
  faFileSignature,
} from '@fortawesome/free-solid-svg-icons'

import { InjectStatusChip, InjectTypeChip, SubmitForApprovalButton, ApprovalActionsButtons } from './'
import { CobraPrimaryButton, CobraSecondaryButton, CobraDeleteButton } from '../../../theme/styledComponents'
import { InjectStatus, DeliveryMethod } from '../../../types'
import type { InjectDto } from '../types'
import { formatScheduledTime, formatScenarioTime, formatOffset } from '../types'
import type { ObjectiveSummaryDto } from '../../objectives/types'
import { formatDateTime } from '../../../shared/utils/dateUtils'

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
  /** Can the user add observations? (Evaluators) */
  canAddObservation?: boolean
  /** Is an action currently being submitted? */
  isSubmitting?: boolean
  /** Called when fire button is clicked */
  onFire?: (injectId: string) => void
  /** Called when skip button is clicked */
  onSkip?: (injectId: string) => void
  /** Called when reset button is clicked */
  onReset?: (injectId: string) => void
  /** Called when user wants to add observation for this inject */
  onAddObservation?: (injectId: string) => void
  /** Available objectives for displaying linked objectives */
  objectives?: ObjectiveSummaryDto[]
  // Approval workflow props (S14)
  /** The exercise ID for approval actions */
  exerciseId?: string
  /** Whether approval workflow is enabled */
  approvalEnabled?: boolean
  /** Current user's ID for self-approval check */
  currentUserId?: string
  /** Can the user submit for approval (Controller+)? */
  canSubmitForApproval?: boolean
  /** Can the user approve/reject (Director/Admin)? */
  canApprove?: boolean
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

const priorityLabels: Record<number, string> = {
  1: 'Critical',
  2: 'High',
  3: 'Medium',
  4: 'Low',
  5: 'Info',
}

const priorityColors: Record<number, string> = {
  1: 'error.main',
  2: 'warning.main',
  3: 'info.main',
  4: 'text.secondary',
  5: 'text.disabled',
}

const getDeliveryMethodDisplay = (inject: InjectDto): string => {
  // Prefer the lookup-based delivery method name
  if (inject.deliveryMethodName) {
    if (inject.deliveryMethodOther) {
      return `${inject.deliveryMethodName}: ${inject.deliveryMethodOther}`
    }
    return inject.deliveryMethodName
  }
  // Fall back to legacy enum
  if (inject.deliveryMethod) {
    return deliveryMethodLabels[inject.deliveryMethod]
  }
  return ''
}

export const InjectDetailDrawer = ({
  inject,
  offsetMs,
  open,
  onClose,
  canControl = false,
  canAddObservation = false,
  isSubmitting = false,
  onFire,
  onSkip,
  onReset,
  onAddObservation,
  objectives = [],
  // Approval workflow props (S14)
  exerciseId = '',
  approvalEnabled = false,
  currentUserId = '',
  canSubmitForApproval = false,
  canApprove = false,
}: InjectDetailDrawerProps) => {
  if (!inject) return null

  const isPending = inject.status === InjectStatus.Draft
  const isSubmitted = inject.status === InjectStatus.Submitted
  const isApproved = inject.status === InjectStatus.Approved
  const isFired = inject.status === InjectStatus.Released
  const isSkipped = inject.status === InjectStatus.Deferred

  const handleFire = () => {
    if (onFire) onFire(inject.id)
  }

  const handleSkip = () => {
    if (onSkip) onSkip(inject.id)
  }

  const handleReset = () => {
    if (onReset) onReset(inject.id)
  }

  const handleAddObservation = () => {
    if (onAddObservation) onAddObservation(inject.id)
  }

  // Format fired/skipped time for display
  const formatActionTime = (isoString: string | null): string => {
    if (!isoString) return ''
    return formatDateTime(isoString)
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
              {/* Approval Status Info (S14) */}
              {approvalEnabled && isSubmitted && inject.submittedByName && (
                <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                  Submitted by {inject.submittedByName}
                  {inject.submittedAt && ` on ${formatActionTime(inject.submittedAt)}`}
                </Typography>
              )}
              {approvalEnabled && isApproved && inject.approvedByName && (
                <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                  Approved by {inject.approvedByName}
                  {inject.approvedAt && ` on ${formatActionTime(inject.approvedAt)}`}
                </Typography>
              )}
            </Box>
            <IconButton onClick={onClose} size="small">
              <FontAwesomeIcon icon={faXmark} />
            </IconButton>
          </Stack>
        </Box>

        {/* Content - Scrollable */}
        <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
          <Stack spacing={3}>
            {/* Rejection Alert (S14) - shown for Draft injects that were previously rejected */}
            {approvalEnabled && isPending && inject.rejectionReason && (
              <Alert
                severity="warning"
                icon={<FontAwesomeIcon icon={faExclamationTriangle} />}
                sx={{ mb: 1 }}
              >
                <Typography variant="subtitle2" fontWeight={600}>
                  Previously Rejected
                </Typography>
                {inject.rejectedByName && inject.rejectedAt && (
                  <Typography variant="caption" display="block" color="text.secondary">
                    Rejected by {inject.rejectedByName} on {formatActionTime(inject.rejectedAt)}
                  </Typography>
                )}
                <Typography variant="body2" sx={{ mt: 0.5 }}>
                  {inject.rejectionReason}
                </Typography>
              </Alert>
            )}

            {/* Approver Notes (S14) - shown for Approved injects */}
            {approvalEnabled && isApproved && inject.approverNotes && (
              <Box sx={{ mb: 1 }}>
                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                  <FontAwesomeIcon icon={faFileSignature} style={{ marginRight: 8 }} />
                  Approver Notes
                </Typography>
                <Typography
                  variant="body2"
                  sx={{
                    pl: 3,
                    whiteSpace: 'pre-wrap',
                    fontStyle: 'italic',
                    color: 'text.secondary',
                    backgroundColor: 'action.hover',
                    p: 1.5,
                    borderRadius: 1,
                  }}
                >
                  {inject.approverNotes}
                </Typography>
              </Box>
            )}

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
                {(inject.deliveryMethodName || inject.deliveryMethod) && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Method:
                    </Typography>
                    <Chip
                      icon={<FontAwesomeIcon icon={faEnvelope} />}
                      label={getDeliveryMethodDisplay(inject)}
                      size="small"
                      variant="outlined"
                    />
                  </Stack>
                )}
                {inject.track && (
                  <Stack direction="row" spacing={2}>
                    <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                      Track:
                    </Typography>
                    <Chip
                      icon={<FontAwesomeIcon icon={faRoad} />}
                      label={inject.track}
                      size="small"
                      variant="outlined"
                    />
                  </Stack>
                )}
              </Stack>
            </Box>

            {/* Organization - only show if any org fields are set */}
            {(inject.responsibleController || inject.locationName || inject.priority !== null) && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    <FontAwesomeIcon icon={faSitemap} style={{ marginRight: 8 }} />
                    Organization
                  </Typography>
                  <Stack spacing={1} sx={{ pl: 3 }}>
                    {inject.responsibleController && (
                      <Stack direction="row" spacing={2}>
                        <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                          Controller:
                        </Typography>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <FontAwesomeIcon icon={faUserTie} style={{ fontSize: '0.75rem', color: 'rgba(0,0,0,0.54)' }} />
                          <Typography variant="body2">
                            {inject.responsibleController}
                          </Typography>
                        </Stack>
                      </Stack>
                    )}
                    {inject.locationName && (
                      <Stack direction="row" spacing={2}>
                        <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                          Location:
                        </Typography>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <FontAwesomeIcon icon={faLocationDot} style={{ fontSize: '0.75rem', color: 'rgba(0,0,0,0.54)' }} />
                          <Typography variant="body2">
                            {inject.locationName}
                            {inject.locationType && ` (${inject.locationType})`}
                          </Typography>
                        </Stack>
                      </Stack>
                    )}
                    {inject.priority !== null && (
                      <Stack direction="row" spacing={2}>
                        <Typography variant="body2" color="text.secondary" sx={{ width: 100 }}>
                          Priority:
                        </Typography>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <FontAwesomeIcon icon={faFlag} style={{ fontSize: '0.75rem', color: 'rgba(0,0,0,0.54)' }} />
                          <Typography variant="body2" sx={{ color: priorityColors[inject.priority] || 'text.secondary' }}>
                            {inject.priority} - {priorityLabels[inject.priority] || 'Unknown'}
                          </Typography>
                        </Stack>
                      </Stack>
                    )}
                  </Stack>
                </Box>
              </>
            )}

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

            {/* Linked Objectives */}
            {inject.objectiveIds.length > 0 && (
              <>
                <Divider />
                <Box>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    <FontAwesomeIcon icon={faCrosshairs} style={{ marginRight: 8 }} />
                    Linked Objectives
                  </Typography>
                  <Stack spacing={0.5} sx={{ pl: 3 }}>
                    {inject.objectiveIds.map(objId => {
                      const objective = objectives.find(o => o.id === objId)
                      if (!objective) {
                        return (
                          <Typography key={objId} variant="body2" color="text.secondary">
                            Unknown objective
                          </Typography>
                        )
                      }
                      return (
                        <Tooltip
                          key={objId}
                          title={objective.description || 'No description'}
                          placement="left"
                          arrow
                        >
                          <Chip
                            label={`${objective.objectiveNumber}: ${objective.name}`}
                            size="small"
                            variant="outlined"
                            sx={{
                              justifyContent: 'flex-start',
                              width: 'fit-content',
                              cursor: 'help',
                            }}
                          />
                        </Tooltip>
                      )
                    })}
                  </Stack>
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
            <Stack direction="row" spacing={1}>
              <CobraSecondaryButton
                size="small"
                startIcon={<FontAwesomeIcon icon={faXmark} />}
                onClick={onClose}
              >
                Close
              </CobraSecondaryButton>
              {/* Add Observation button - visible for evaluators when inject is fired */}
              {canAddObservation && isFired && onAddObservation && (
                <Tooltip title="Add an observation about this inject">
                  <CobraSecondaryButton
                    size="small"
                    startIcon={<FontAwesomeIcon icon={faEye} />}
                    onClick={handleAddObservation}
                  >
                    Add Observation
                  </CobraSecondaryButton>
                </Tooltip>
              )}
            </Stack>
            <Stack direction="row" spacing={1}>
              {/* Approval Workflow Actions (S14) */}
              {approvalEnabled && exerciseId && (
                <>
                  {/* Submit for Approval - for Draft injects */}
                  {isPending && canSubmitForApproval && (
                    <SubmitForApprovalButton
                      inject={inject}
                      exerciseId={exerciseId}
                      approvalEnabled={approvalEnabled}
                      canSubmit={canSubmitForApproval}
                      size="small"
                    />
                  )}
                  {/* Approve/Reject - for Submitted injects */}
                  {isSubmitted && canApprove && (
                    <ApprovalActionsButtons
                      inject={inject}
                      exerciseId={exerciseId}
                      currentUserId={currentUserId}
                      canApprove={canApprove}
                      size="small"
                    />
                  )}
                </>
              )}
              {/* Conduct Actions */}
              {canControl && (
                <>
                  {(isPending || isApproved) && (
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
                      Reset to Draft
                    </CobraDeleteButton>
                  )}
                </>
              )}
            </Stack>
          </Stack>
        </Box>
      </Box>
    </Drawer>
  )
}

export default InjectDetailDrawer
