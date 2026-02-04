/**
 * ApprovalActionsButtons Component
 *
 * Approve and Reject buttons for Submitted injects (S04).
 * Only visible for Exercise Directors when inject is in Submitted status.
 *
 * @module features/injects/components
 */

import { useState } from 'react'
import { Box, Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faTimes, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraDeleteButton } from '@/theme/styledComponents'
import { InjectStatus } from '@/types'
import { useInjectApproval } from '../hooks'
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
  /** Whether the current user can approve (has ExerciseDirector or higher role) */
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
 * Only visible when:
 * - Inject is in Submitted status
 * - User has permission to approve (ExerciseDirector or Admin)
 * - User is not the one who submitted (self-approval blocked by default)
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
  canApprove = true,
  size = 'small',
  onActionComplete,
}: ApprovalActionsButtonsProps) => {
  const [approveDialogOpen, setApproveDialogOpen] = useState(false)
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false)

  const { approveInject, rejectInject, isApproving, isRejecting } =
    useInjectApproval(exerciseId)

  // Don't render if inject is not Submitted
  if (inject.status !== InjectStatus.Submitted) {
    return null
  }

  // Don't render if user can't approve
  if (!canApprove) {
    return null
  }

  // Check for self-approval (blocked by default)
  const isSelfApproval = inject.submittedByUserId === currentUserId

  const handleApprove = async (notes?: string) => {
    try {
      const approvedInject = await approveInject(inject.id, { notes })
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
        <Tooltip
          title={
            isSelfApproval
              ? 'You cannot approve your own submission'
              : 'Approve this inject'
          }
        >
          <span>
            <CobraPrimaryButton
              size={size}
              onClick={() => setApproveDialogOpen(true)}
              disabled={isApproving || isSelfApproval}
              startIcon={
                <FontAwesomeIcon
                  icon={isApproving ? faSpinner : faCheck}
                  spin={isApproving}
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
