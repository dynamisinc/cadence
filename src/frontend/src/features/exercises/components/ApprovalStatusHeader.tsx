/**
 * ApprovalStatusHeader Component
 *
 * Displays approval progress bar and summary (S06).
 * Shows breakdown of Draft / Submitted / Approved inject counts.
 *
 * @module features/exercises/components
 */

import { Box, Typography, LinearProgress, Tooltip, Skeleton, Chip, Stack } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faPencil, faPaperPlane } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import { useApprovalStatus, useApprovalSettings } from '../hooks'

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
 * out of the total, plus summary chips showing Draft/Submitted/Approved counts.
 *
 * @example
 * <ApprovalStatusHeader exerciseId={exerciseId} showDetails />
 */
export const ApprovalStatusHeader = ({
  exerciseId,
  showDetails = true,
}: ApprovalStatusHeaderProps) => {
  const theme = useTheme()
  const { settings } = useApprovalSettings(exerciseId)
  const {
    status,
    isLoading,
    pendingCount,
    approvedCount,
    totalInjects,
    canPublish,
    approvalPercentage,
  } = useApprovalStatus(exerciseId)

  // Don't show if approval is not enabled
  if (!settings?.requireInjectApproval) {
    return null
  }

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

  // Calculate draft count (not yet submitted)
  const draftCount = totalInjects - approvedCount - pendingCount
  const progressColor = canPublish ? 'success' : 'warning'

  return (
    <Box sx={{ mb: 2 }}>
      {showDetails && (
        <Box mb={1}>
          {/* Status breakdown chips */}
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" sx={{ mb: 1 }}>
            <Typography variant="body2" color="text.secondary" sx={{ mr: 1 }}>
              Approval Status:
            </Typography>
            <Chip
              size="small"
              icon={<FontAwesomeIcon icon={faPencil} style={{ fontSize: 10 }} />}
              label={`${draftCount} Draft`}
              variant="outlined"
              sx={{
                borderColor: theme.palette.grey[400],
                color: theme.palette.text.secondary,
                '& .MuiChip-icon': { color: theme.palette.grey[500] },
              }}
            />
            <Chip
              size="small"
              icon={<FontAwesomeIcon icon={faPaperPlane} style={{ fontSize: 10 }} />}
              label={`${pendingCount} Awaiting Review`}
              variant="outlined"
              sx={{
                borderColor: theme.palette.warning.main,
                color: theme.palette.warning.dark,
                '& .MuiChip-icon': { color: theme.palette.warning.main },
              }}
            />
            <Chip
              size="small"
              icon={<FontAwesomeIcon icon={faCheck} style={{ fontSize: 10 }} />}
              label={`${approvedCount} Approved`}
              variant="outlined"
              sx={{
                borderColor: theme.palette.success.main,
                color: theme.palette.success.dark,
                '& .MuiChip-icon': { color: theme.palette.success.main },
              }}
            />
            {canPublish && (
              <Typography variant="body2" color="success.main" fontWeight={500} sx={{ ml: 1 }}>
                <FontAwesomeIcon icon={faCheck} style={{ marginRight: 4 }} />
                Ready to activate
              </Typography>
            )}
          </Stack>

          {/* Workflow hint when there are drafts */}
          {draftCount > 0 && pendingCount === 0 && approvedCount === 0 && (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
              Use the <strong>Submit for Approval</strong> button on each inject to start the approval workflow.
            </Typography>
          )}
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
