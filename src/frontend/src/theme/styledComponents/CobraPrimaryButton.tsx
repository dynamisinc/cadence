import { Button } from '@mui/material'
import { styled } from '@mui/material/styles'

/**
 * CobraPrimaryButton - Primary action button with navy blue background
 *
 * Use for:
 * - Main form submissions (Save, Submit, Create)
 * - Primary call-to-action buttons
 * - Confirm actions
 *
 * Supports MUI size variants:
 * - size="small" — compact padding for toolbars and dense UIs
 * - size="medium" (default) — standard COBRA padding
 * - size="large" — larger padding for prominent actions
 *
 * Example:
 * ```tsx
 * <CobraPrimaryButton onClick={handleSave}>Save Note</CobraPrimaryButton>
 * <CobraPrimaryButton size="small" onClick={handleAdd}>Add</CobraPrimaryButton>
 * ```
 */
export const CobraPrimaryButton = styled(Button, {
  shouldForwardProp: prop => prop !== 'size',
})<{ size?: 'small' | 'medium' | 'large' }>(({ theme, size }) => ({
  background: theme.palette.buttonPrimary.main,
  borderRadius: 50,
  color: theme.palette.buttonPrimary.contrastText,
  textTransform: 'none' as const,
  whiteSpace: 'nowrap' as const,
  ...(size === 'small'
    ? {
      paddingBottom: 3,
      paddingLeft: 12,
      paddingRight: 12,
      paddingTop: 3,
      fontSize: '0.8125rem',
    }
    : size === 'large'
      ? {
        paddingBottom: 8,
        paddingLeft: 28,
        paddingRight: 28,
        paddingTop: 8,
      }
      : {
        // Default (medium)
        paddingBottom: 5,
        paddingLeft: 20,
        paddingRight: 20,
        paddingTop: 5,
      }),
  '&:hover': {
    background: theme.palette.buttonPrimary.light,
  },
  '&:active': {
    background: theme.palette.buttonPrimary.dark,
  },
  '&.Mui-disabled': {
    background: theme.palette.primary.light,
    color: theme.palette.primary.dark,
  },
}))
