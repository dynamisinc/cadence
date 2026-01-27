import { IconButton } from '@mui/material'
import { styled } from '@mui/material/styles'

/**
 * CobraIconButton - Icon-only button for compact actions
 *
 * Use for:
 * - Overflow menus (more actions)
 * - Toolbar actions
 * - Compact icon-only controls
 */
export const CobraIconButton = styled(IconButton)(({ theme }) => ({
  color: theme.palette.text.secondary,
  borderRadius: 8,
  padding: 8,
  '&:hover': {
    backgroundColor: theme.palette.action.hover,
    color: theme.palette.buttonPrimary.main,
  },
  '&:active': {
    backgroundColor: theme.palette.buttonPrimary.light,
  },
}))
