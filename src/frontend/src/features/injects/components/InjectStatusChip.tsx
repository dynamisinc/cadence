import { Chip } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import type { InjectStatus } from '../../../types'

interface InjectStatusChipProps {
  status: InjectStatus
}

/**
 * Get inject status chip colors
 *
 * Per MSEL requirements:
 * - Pending: Gray/neutral (waiting to be delivered)
 * - Fired: Green/success (delivered to players)
 * - Skipped: Orange/warning (intentionally not delivered)
 */
const getInjectStatusChipColor = (
  status: string,
): { bg: string; text: string } => {
  const statusLower = status.toLowerCase()

  switch (statusLower) {
    case 'fired':
      return {
        bg: cobraTheme.palette.notifications.success,
        text: cobraTheme.palette.notifications.successText,
      }
    case 'skipped':
      return {
        bg: cobraTheme.palette.notifications.warning,
        text: cobraTheme.palette.notifications.warningText,
      }
    case 'pending':
    default:
      return {
        bg: cobraTheme.palette.statusChart.grey,
        text: cobraTheme.palette.text.primary,
      }
  }
}

/**
 * Status chip for injects with COBRA-compliant colors
 *
 * Per MSEL requirements:
 * - Pending: Gray styling (not yet delivered)
 * - Fired: Green styling (delivered to players)
 * - Skipped: Orange/warning styling (intentionally not delivered)
 */
export const InjectStatusChip = ({ status }: InjectStatusChipProps) => {
  const colors = getInjectStatusChipColor(status)

  return (
    <Chip
      label={status}
      size="small"
      sx={{
        backgroundColor: colors.bg,
        color: colors.text,
        fontWeight: 500,
        minWidth: 70,
      }}
    />
  )
}

export default InjectStatusChip
