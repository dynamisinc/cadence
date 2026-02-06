/**
 * ApprovalActionsButtons Component
 *
 * Approve and Reject buttons for Submitted injects (S04).
 * Only visible for authorized roles when inject is in Submitted status.
 * Properly handles self-approval based on organization policy (S11).
 *
 * @module features/injects/components
 */

import { useState } from 'react'
import { Box, Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faTimes, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraDeleteButton } from '@/theme/styledComponents'
import { InjectStatus } from '@/types'
import { useInjectApproval, useCanApproveInject } from '../hooks'
import { ApproveDialog } from './ApproveDialog'
import { RejectDialog } from './RejectDialog'
import type { InjectDto } from '../types'

interface ApprovalActionsButtonsProps {
  /** The inject to approve/reject */
  inject: InjectDto
  /** The exercise ID */
  exerciseId: string
  /** Current user's ID */
  currentUserId: string
  /** Whether the user can approve (ExerciseDirector+) - hint */
  canApprove?: boolean
  /** Size variant */
  size?: 'small' | 'medium'
  /** Optional callback after successful action */
  onActionComplete?: (inject: InjectDto) => void
}

/**
 * Approval Actions Buttons
 *
 * Renders Approve and Reject buttons for a Submitted inject.
 * Uses organization policy to determine self-approval behavior:
 * - NeverAllowed: Button disabled for self-submitted injects
 * - AllowedWithWarning: Shows confirmation warning before self-approval
 * - AlwaysAllowed: No restrictions
 *
 * @example
 * <ApprovalActionsButtons
 *   inject={inject}
 *   exerciseId={exerciseId}
 *   currentUserId={user.id}
 *   onActionComplete={handleActionComplete}
 * />
 */
export const ApprovalActionsButtons = ({
  inject,
  exerciseId,
  currentUserId,
  canApprove: canApproveProp = true,
  size = 'small',
  onActionComplete,
}: ApprovalActionsButtonsProps) => {
  const [approveDialogOpen, setApproveDialogOpen] = useState(false)
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false)

  const { approveInject, rejectInject, isApproving, isRejecting } =
    useInjectApproval(exerciseId)

  // Check actual permission from backend (S11: includes org policy check)
  const { data: permissionCheck, isLoading: isCheckingPermission } =
    useCanApproveInject(
      exerciseId,
      inject.status === InjectStatus.Submitted ? inject.id : undefined,
      canApproveProp, // Only check if prop indicates user might be able to approve
    )

  // Don't render if inject is not Submitted
  if (inject.status !== InjectStatus.Submitted) {
    return null
  }

  // Don't render if user can't approve (use backend check when available)
  const canApprove = permissionCheck?.canApprove ?? canApproveProp
  if (!canApprove && !isCheckingPermission) {
    return null
  }

  // Self-approval handling based on organization policy
  const isSelfApproval = inject.submittedByUserId === currentUserId
  const requiresConfirmation = permissionCheck?.requiresConfirmation ?? false
  const permissionMessage = permissionCheck?.message

  // Determine tooltip message
  const getApproveTooltip = () => {
    if (!canApprove) {
      return permissionMessage || 'You are not authorized to approve injects'
    }
    if (isSelfApproval && requiresConfirmation) {
      return 'You can approve your own submission (confirmation required)'
    }
    return 'Approve this inject'
  }

  const handleApprove = async (notes?: string) => {
    try {
      // Include confirmSelfApproval flag when this is a self-approval that requires confirmation
      const approvedInject = await approveInject(inject.id, {
        notes,
        confirmSelfApproval: isSelfApproval && requiresConfirmation,
      })
      setApproveDialogOpen(false)
      onActionComplete?.(approvedInject)
    } catch {
      // Error handling is done in the hook
    }
  }

  const handleReject = async (reason: string) => {
    try {
      const rejectedInject = await rejectInject(inject.id, { reason })
      setRejectDialogOpen(false)
      onActionComplete?.(rejectedInject)
    } catch {
      // Error handling is done in the hook
    }
  }

  return (
    <>
      <Box display="flex" gap={1}>
        <Tooltip title={getApproveTooltip()}>
          <span>
            <CobraPrimaryButton
              size={size}
              onClick={() => setApproveDialogOpen(true)}
              disabled={isApproving || isCheckingPermission || !canApprove}
              startIcon={
                <FontAwesomeIcon
                  icon={isApproving || isCheckingPermission ? faSpinner : faCheck}
                  spin={isApproving || isCheckingPermission}
                />
              }
              sx={{ minWidth: 'auto' }}
            >
              Approve
            </CobraPrimaryButton>
          </span>
        </Tooltip>

        <Tooltip title="Reject this inject and return to author">
          <span>
            <CobraDeleteButton
              size={size}
              variant="outlined"
              onClick={() => setRejectDialogOpen(true)}
              disabled={isRejecting}
              startIcon={
                <FontAwesomeIcon
                  icon={isRejecting ? faSpinner : faTimes}
                  spin={isRejecting}
                />
              }
              sx={{ minWidth: 'auto' }}
            >
              Reject
            </CobraDeleteButton>
          </span>
        </Tooltip>
      </Box>

      <ApproveDialog
        open={approveDialogOpen}
        inject={inject}
        onConfirm={handleApprove}
        onCancel={() => setApproveDialogOpen(false)}
        isLoading={isApproving}
        isSelfApproval={isSelfApproval}
        requiresConfirmation={requiresConfirmation}
      />

      <RejectDialog
        open={rejectDialogOpen}
        inject={inject}
        onConfirm={handleReject}
        onCancel={() => setRejectDialogOpen(false)}
        isLoading={isRejecting}
      />
    </>
  )
}

export default ApprovalActionsButtons
