/**
 * BatchApprovalToolbar Component
 *
 * Toolbar for batch approve/reject operations (S05).
 * Shows when injects are selected and provides batch actions.
 *
 * @module features/injects/components
 */

import { useState, useMemo } from 'react'
import { Paper, Typography, Box, Divider } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faCheck,
  faTimes,
  faXmark,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraDeleteButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import { InjectStatus, SelfApprovalPolicy } from '@/types'
import { useInjectApproval } from '../hooks'
import { ApproveDialog } from './ApproveDialog'
import { RejectDialog } from './RejectDialog'
import type { InjectDto } from '../types'

interface BatchApprovalToolbarProps {
  /** IDs of selected injects */
  selectedIds: string[]
  /** All injects in the list (to filter approvable ones) */
  injects: InjectDto[]
  /** The exercise ID */
  exerciseId: string
  /** Current user's ID */
  currentUserId: string
  /** Organization's self-approval policy */
  selfApprovalPolicy?: SelfApprovalPolicy
  /** Called to clear selection after action */
  onClearSelection: () => void
  /** Optional callback after batch action */
  onActionComplete?: () => void
}

/**
 * Batch Approval Toolbar
 *
 * Shows when one or more injects are selected, allowing batch
 * approve or reject operations. Self-approval handling depends on
 * organization policy:
 * - NeverAllowed: Self-submitted injects cannot be processed
 * - AllowedWithWarning: Self-submitted injects included with warning
 * - AlwaysAllowed: No restrictions on self-approval
 *
 * @example
 * <BatchApprovalToolbar
 *   selectedIds={selectedIds}
 *   injects={injects}
 *   exerciseId={exerciseId}
 *   currentUserId={user.id}
 *   selfApprovalPolicy={SelfApprovalPolicy.NeverAllowed}
 *   onClearSelection={() => setSelectedIds([])}
 * />
 */
export const BatchApprovalToolbar = ({
  selectedIds,
  injects,
  exerciseId,
  currentUserId,
  selfApprovalPolicy = SelfApprovalPolicy.NeverAllowed,
  onClearSelection,
  onActionComplete,
}: BatchApprovalToolbarProps) => {
  const [approveDialogOpen, setApproveDialogOpen] = useState(false)
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false)

  const { batchApprove, batchReject, isBatchApproving, isBatchRejecting } =
    useInjectApproval(exerciseId)

  // Calculate which selected injects can actually be approved
  // Self-approval handling depends on organization policy
  const { approvableIds, skippedCount, skippedReasons, selfApprovalCount } = useMemo(() => {
    const approvable: string[] = []
    const reasons: string[] = []
    let selfCount = 0

    selectedIds.forEach(id => {
      const inject = injects.find(i => i.id === id)
      if (!inject) {
        reasons.push('Inject not found')
        return
      }
      if (inject.status !== InjectStatus.Submitted) {
        reasons.push('Not in Submitted status')
        return
      }
      // Self-approval handling based on organization policy
      if (inject.submittedByUserId === currentUserId) {
        if (selfApprovalPolicy === SelfApprovalPolicy.NeverAllowed) {
          reasons.push('Cannot approve own submission (policy: Never Allowed)')
          return
        }
        // AllowedWithWarning or AlwaysAllowed - include but track count
        selfCount++
      }
      approvable.push(id)
    })

    return {
      approvableIds: approvable,
      skippedCount: selectedIds.length - approvable.length,
      skippedReasons: [...new Set(reasons)], // Unique reasons
      selfApprovalCount: selfCount,
    }
  }, [selectedIds, injects, currentUserId, selfApprovalPolicy])

  // Don't render if nothing selected
  if (selectedIds.length === 0) {
    return null
  }

  const handleBatchApprove = async (notes?: string) => {
    try {
      await batchApprove({
        injectIds: approvableIds,
        notes,
      })
      setApproveDialogOpen(false)
      onClearSelection()
      onActionComplete?.()
    } catch {
      // Error handling is done in the hook
    }
  }

  const handleBatchReject = async (reason: string) => {
    try {
      await batchReject({
        injectIds: approvableIds,
        reason,
      })
      setRejectDialogOpen(false)
      onClearSelection()
      onActionComplete?.()
    } catch {
      // Error handling is done in the hook
    }
  }

  const isLoading = isBatchApproving || isBatchRejecting

  return (
    <>
      <Paper
        elevation={2}
        sx={{
          p: 2,
          mb: 2,
          display: 'flex',
          alignItems: 'center',
          gap: 2,
          flexWrap: 'wrap',
          bgcolor: 'background.paper',
          borderLeft: 4,
          borderColor: 'primary.main',
        }}
      >
        <Typography variant="body1">
          <strong>{selectedIds.length}</strong> inject
          {selectedIds.length !== 1 ? 's' : ''} selected
        </Typography>

        {skippedCount > 0 && (
          <Typography variant="body2" color="text.secondary">
            ({skippedCount} cannot be processed
            {skippedReasons.length > 0 && `: ${skippedReasons.join(', ')}`})
          </Typography>
        )}

        {selfApprovalCount > 0 && selfApprovalPolicy === SelfApprovalPolicy.AllowedWithWarning && (
          <Typography variant="body2" color="warning.main">
            ({selfApprovalCount} self-submitted - will be flagged in audit log)
          </Typography>
        )}

        <Box sx={{ flexGrow: 1 }} />

        <Divider orientation="vertical" flexItem sx={{ mx: 1 }} />

        <CobraPrimaryButton
          size="small"
          disabled={approvableIds.length === 0 || isLoading}
          onClick={() => setApproveDialogOpen(true)}
          startIcon={
            <FontAwesomeIcon
              icon={isBatchApproving ? faSpinner : faCheck}
              spin={isBatchApproving}
            />
          }
        >
          Approve ({approvableIds.length})
        </CobraPrimaryButton>

        <CobraDeleteButton
          size="small"
          variant="outlined"
          disabled={approvableIds.length === 0 || isLoading}
          onClick={() => setRejectDialogOpen(true)}
          startIcon={
            <FontAwesomeIcon
              icon={isBatchRejecting ? faSpinner : faTimes}
              spin={isBatchRejecting}
            />
          }
        >
          Reject ({approvableIds.length})
        </CobraDeleteButton>

        <CobraSecondaryButton
          size="small"
          onClick={onClearSelection}
          disabled={isLoading}
          startIcon={<FontAwesomeIcon icon={faXmark} />}
        >
          Clear
        </CobraSecondaryButton>
      </Paper>

      {/* Batch Approve Dialog */}
      <ApproveDialog
        open={approveDialogOpen}
        inject={
          approvableIds.length > 0
            ? {
              // Create a fake inject for display purposes
              id: '',
              injectNumber: 0,
              title: `${approvableIds.length} selected inject${approvableIds.length !== 1 ? 's' : ''}`,
              description: `This will approve all ${approvableIds.length} selected inject${approvableIds.length !== 1 ? 's' : ''}.`,
            } as InjectDto
            : null
        }
        onConfirm={handleBatchApprove}
        onCancel={() => setApproveDialogOpen(false)}
        isLoading={isBatchApproving}
      />

      {/* Batch Reject Dialog */}
      <RejectDialog
        open={rejectDialogOpen}
        inject={
          approvableIds.length > 0
            ? {
              // Create a fake inject for display purposes
              id: '',
              injectNumber: 0,
              title: `${approvableIds.length} selected inject${approvableIds.length !== 1 ? 's' : ''}`,
              description: `This will reject all ${approvableIds.length} selected inject${approvableIds.length !== 1 ? 's' : ''} with the same reason.`,
            } as InjectDto
            : null
        }
        onConfirm={handleBatchReject}
        onCancel={() => setRejectDialogOpen(false)}
        isLoading={isBatchRejecting}
      />
    </>
  )
}

export default BatchApprovalToolbar
