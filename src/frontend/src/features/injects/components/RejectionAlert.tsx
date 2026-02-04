/**
 * RejectionAlert Component
 *
 * Alert shown when an inject has a previous rejection (S04).
 * Displays the rejection reason to help the author make corrections.
 *
 * @module features/injects/components
 */

import { Alert, AlertTitle, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faExclamationTriangle } from '@fortawesome/free-solid-svg-icons'
import type { InjectDto } from '../types'

interface RejectionAlertProps {
  /** The inject with potential rejection info */
  inject: InjectDto
}

/**
 * Format a date string for display
 */
const formatDate = (dateString: string | null): string => {
  if (!dateString) return ''
  try {
    const date = new Date(dateString)
    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  } catch {
    return ''
  }
}

/**
 * Rejection Alert
 *
 * Shows a warning alert with the rejection reason when an inject
 * was previously rejected. Helps the author understand what needs
 * to be corrected before resubmitting.
 *
 * @example
 * <RejectionAlert inject={inject} />
 */
export const RejectionAlert = ({ inject }: RejectionAlertProps) => {
  // Don't render if no rejection reason
  if (!inject.rejectionReason) {
    return null
  }

  return (
    <Alert
      severity="warning"
      icon={<FontAwesomeIcon icon={faExclamationTriangle} />}
      sx={{ mb: 2 }}
    >
      <AlertTitle>Previous Submission Rejected</AlertTitle>
      <Typography variant="body2">
        <strong>Reason:</strong> {inject.rejectionReason}
      </Typography>
      {inject.rejectedAt && (
        <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
          Rejected on {formatDate(inject.rejectedAt)}
        </Typography>
      )}
    </Alert>
  )
}

export default RejectionAlert
