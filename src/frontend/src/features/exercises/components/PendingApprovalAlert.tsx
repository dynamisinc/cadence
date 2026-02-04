/**
 * PendingApprovalAlert Component
 *
 * Alert shown to Exercise Directors when injects are pending approval (S06).
 *
 * @module features/exercises/components
 */

import { Alert, Button } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClock, faArrowRight } from '@fortawesome/free-solid-svg-icons'
import { useApprovalStatus } from '../hooks'

interface PendingApprovalAlertProps {
  /** The exercise ID */
  exerciseId: string
  /** Called when user clicks to review pending injects */
  onReviewClick: () => void
  /** Whether the current user can approve (has ExerciseDirector role) */
  canApprove?: boolean
}

/**
 * Pending Approval Alert
 *
 * Shows an info alert when there are injects pending approval.
 * Provides a button to navigate to the approval queue.
 *
 * @example
 * <PendingApprovalAlert
 *   exerciseId={exerciseId}
 *   onReviewClick={() => setActiveTab('pending')}
 *   canApprove={isDirector}
 * />
 */
export const PendingApprovalAlert = ({
  exerciseId,
  onReviewClick,
  canApprove = true,
}: PendingApprovalAlertProps) => {
  const { pendingCount, isLoading } = useApprovalStatus(exerciseId)

  // Don't show if loading, no pending, or user can't approve
  if (isLoading || pendingCount === 0 || !canApprove) {
    return null
  }

  return (
    <Alert
      severity="info"
      icon={<FontAwesomeIcon icon={faClock} />}
      action={
        <Button
          color="inherit"
          size="small"
          onClick={onReviewClick}
          endIcon={<FontAwesomeIcon icon={faArrowRight} />}
        >
          Review Now
        </Button>
      }
      sx={{ mb: 2 }}
    >
      {pendingCount} inject{pendingCount !== 1 ? 's' : ''} pending your approval
    </Alert>
  )
}

export default PendingApprovalAlert
