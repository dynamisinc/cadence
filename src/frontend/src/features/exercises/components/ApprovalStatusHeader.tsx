/**
 * ApprovalStatusHeader Component
 *
 * Displays approval progress bar and summary (S06).
 * Shows how many injects are approved vs pending.
 *
 * @module features/exercises/components
 */

import { Box, Typography, LinearProgress, Tooltip, Skeleton } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faClock } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { useApprovalStatus } from '../hooks'

interface ApprovalStatusHeaderProps {
  /** The exercise ID */
  exerciseId: string
  /** Whether to show detailed status text */
  showDetails?: boolean
}

/**
 * Approval Status Header
 *
 * Shows a progress bar indicating how many injects have been approved
 * out of the total, plus summary text showing pending count.
 *
 * @example
 * <ApprovalStatusHeader exerciseId={exerciseId} showDetails />
 */
export const ApprovalStatusHeader = ({
  exerciseId,
  showDetails = true,
}: ApprovalStatusHeaderProps) => {
  const theme = useTheme()
  const {
    status,
    isLoading,
    pendingCount,
    approvedCount,
    totalInjects,
    canPublish,
    approvalPercentage,
  } = useApprovalStatus(exerciseId)

  if (isLoading) {
    return (
      <Box sx={{ mb: 2 }}>
        <Skeleton variant="rectangular" height={8} sx={{ mb: 1 }} />
        <Skeleton width="60%" height={20} />
      </Box>
    )
  }

  if (!status || totalInjects === 0) {
    return null
  }

  const progressColor = canPublish ? 'success' : 'warning'

  return (
    <Box sx={{ mb: 2 }}>
      {showDetails && (
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
          <Typography variant="body2" color="text.secondary">
            <FontAwesomeIcon
              icon={faCheck}
              style={{ marginRight: 6, color: theme.palette.success.main }}
            />
            {approvedCount} of {totalInjects} injects approved
          </Typography>
          <Typography
            variant="body2"
            color={canPublish ? 'success.main' : 'warning.main'}
            fontWeight={500}
          >
            {canPublish ? (
              <>
                <FontAwesomeIcon icon={faCheck} style={{ marginRight: 4 }} />
                Ready to publish
              </>
            ) : (
              <>
                <FontAwesomeIcon icon={faClock} style={{ marginRight: 4 }} />
                {pendingCount} pending approval
              </>
            )}
          </Typography>
        </Box>
      )}

      <Tooltip
        title={`${approvalPercentage.toFixed(0)}% approved (${approvedCount}/${totalInjects})`}
      >
        <LinearProgress
          variant="determinate"
          value={approvalPercentage}
          color={progressColor}
          sx={{
            height: 8,
            borderRadius: 1,
            bgcolor: theme.palette.grey[200],
            '& .MuiLinearProgress-bar': {
              borderRadius: 1,
            },
          }}
        />
      </Tooltip>
    </Box>
  )
}

export default ApprovalStatusHeader
