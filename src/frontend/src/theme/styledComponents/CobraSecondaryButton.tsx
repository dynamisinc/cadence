import { Button } from '@mui/material'
import { styled } from '@mui/material/styles'

/**
 * CobraSecondaryButton - Secondary/outline button
 *
 * Use for:
 * - Alternative actions
 * - Secondary form submissions
 * - Less prominent actions
 *
 * Supports MUI size variants (small, medium, large).
 */
export const CobraSecondaryButton = styled(Button, {
  shouldForwardProp: prop => prop !== 'size',
})<{ size?: 'small' | 'medium' | 'large' }>(({ theme, size }) => ({
  background: 'transparent',
  border: `1px solid ${theme.palette.buttonPrimary.main}`,
  borderRadius: 50,
  color: theme.palette.buttonPrimary.main,
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
    background: theme.palette.linkButton.light,
    borderColor: theme.palette.buttonPrimary.light,
  },
  '&:active': {
    background: theme.palette.buttonPrimary.dark,
    color: theme.palette.buttonPrimary.contrastText,
  },
}))
