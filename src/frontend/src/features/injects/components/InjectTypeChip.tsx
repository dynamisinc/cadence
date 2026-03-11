import { Chip } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { alpha } from '@mui/material'
import type { InjectType } from '../../../types'

interface InjectTypeChipProps {
  type: InjectType
}

/**
 * Type chip for injects with visual distinction
 *
 * Only shown for non-Standard types to reduce visual noise
 *
 * Types:
 * - Standard: Default/neutral (regular scheduled inject) — not rendered
 * - Contingency: Blue/info (backup if players deviate)
 * - Adaptive: Purple/light (branch based on player decision)
 * - Complexity: Orange (increase difficulty for advanced players)
 */
export const InjectTypeChip = ({ type }: InjectTypeChipProps) => {
  const theme = useTheme()

  // Don't show chip for standard injects (default) or if type is not set
  if (!type || type === 'Standard') {
    return null
  }

  const typeLower = type.toLowerCase()

  const getColors = (): { bg: string; text: string } => {
    switch (typeLower) {
      case 'contingency':
        return {
          bg: theme.palette.grid.main,
          text: theme.palette.info.dark,
        }
      case 'adaptive':
        return {
          bg: alpha(theme.palette.semantic.purple, 0.12),
          text: theme.palette.semantic.purple,
        }
      case 'complexity':
        return {
          bg: theme.palette.notifications.warning,
          text: theme.palette.notifications.warningText,
        }
      case 'standard':
      default:
        return {
          bg: theme.palette.primary.light,
          text: theme.palette.text.secondary,
        }
    }
  }

  const colors = getColors()

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
