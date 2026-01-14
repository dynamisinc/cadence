import { Chip } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import type { InjectType } from '../../../types'

interface InjectTypeChipProps {
  type: InjectType
}

/**
 * Get inject type chip colors
 *
 * Types:
 * - Standard: Default/neutral (regular scheduled inject)
 * - Contingency: Blue/info (backup if players deviate)
 * - Adaptive: Purple/light (branch based on player decision)
 * - Complexity: Orange (increase difficulty for advanced players)
 */
const getInjectTypeChipColor = (
  type: string,
): { bg: string; text: string } => {
  const typeLower = type.toLowerCase()

  switch (typeLower) {
    case 'contingency':
      return {
        bg: cobraTheme.palette.grid.main,
        text: cobraTheme.palette.info.dark,
      }
    case 'adaptive':
      return {
        bg: '#E8DEF8', // Light purple
        text: '#4A148C', // Deep purple
      }
    case 'complexity':
      return {
        bg: cobraTheme.palette.notifications.warning,
        text: cobraTheme.palette.notifications.warningText,
      }
    case 'standard':
    default:
      return {
        bg: cobraTheme.palette.primary.light,
        text: cobraTheme.palette.text.secondary,
      }
  }
}

/**
 * Type chip for injects with visual distinction
 *
 * Only shown for non-Standard types to reduce visual noise
 */
export const InjectTypeChip = ({ type }: InjectTypeChipProps) => {
  // Don't show chip for standard injects (default) or if type is not set
  if (!type || type === 'Standard') {
    return null
  }

  const colors = getInjectTypeChipColor(type)

  return (
    <Chip
      label={type}
      size="small"
      sx={{
        backgroundColor: colors.bg,
        color: colors.text,
        fontWeight: 500,
        fontSize: '0.75rem',
      }}
    />
  )
}

export default InjectTypeChip
