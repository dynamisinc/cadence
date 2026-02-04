/**
 * SubmitForApprovalButton Component
 *
 * Button to submit a Draft inject for approval (S03).
 * Only visible when approval workflow is enabled and inject is in Draft status.
 *
 * @module features/injects/components
 */

import { Tooltip } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPaperPlane, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton } from '@/theme/styledComponents'
import { InjectStatus } from '@/types'
import { useInjectApproval } from '../hooks'
import type { InjectDto } from '../types'

interface SubmitForApprovalButtonProps {
  /** The inject to submit */
  inject: InjectDto
  /** The exercise ID */
  exerciseId: string
  /** Whether approval workflow is enabled for this exercise */
  approvalEnabled: boolean
  /** Whether the current user can submit (has Controller or higher role) */
  canSubmit?: boolean
  /** Size variant */
  size?: 'small' | 'medium'
  /** Optional callback after successful submission */
  onSubmitted?: (inject: InjectDto) => void
}

/**
 * Submit for Approval Button
 *
 * Renders a button to submit a Draft inject for approval.
 * Only visible when:
 * - Approval workflow is enabled for the exercise
 * - Inject is in Draft status
 * - User has permission to submit
 *
 * @example
 * <SubmitForApprovalButton
 *   inject={inject}
 *   exerciseId={exerciseId}
 *   approvalEnabled={settings?.requireInjectApproval}
 *   onSubmitted={handleSubmitted}
 * />
 */
export const SubmitForApprovalButton = ({
  inject,
  exerciseId,
  approvalEnabled,
  canSubmit = true,
  size = 'small',
  onSubmitted,
}: SubmitForApprovalButtonProps) => {
  const { submitForApproval, isSubmitting } = useInjectApproval(exerciseId)

  // Don't render if approval is disabled or inject is not in Draft
  if (!approvalEnabled || inject.status !== InjectStatus.Draft) {
    return null
  }

  // Don't render if user can't submit
  if (!canSubmit) {
    return null
  }

  const handleSubmit = async () => {
    try {
      const submittedInject = await submitForApproval(inject.id)
      onSubmitted?.(submittedInject)
    } catch {
      // Error handling is done in the hook
    }
  }

  return (
    <Tooltip title="Submit this inject for director approval">
      <span>
        <CobraPrimaryButton
          size={size}
          onClick={handleSubmit}
          disabled={isSubmitting}
          startIcon={
            <FontAwesomeIcon
              icon={isSubmitting ? faSpinner : faPaperPlane}
              spin={isSubmitting}
            />
          }
          sx={{ minWidth: 'auto', whiteSpace: 'nowrap' }}
        >
          Submit for Approval
        </CobraPrimaryButton>
      </span>
    </Tooltip>
  )
}

export default SubmitForApprovalButton
