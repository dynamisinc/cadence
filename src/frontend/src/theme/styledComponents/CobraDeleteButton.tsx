import { Button, type ButtonProps } from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import { styled } from '@mui/material/styles'

/**
 * CobraDeleteButton - Red delete/remove button
 *
 * Use for:
 * - Delete actions
 * - Remove items
 * - Destructive operations
 */
const StyledDeleteButton = styled(Button)(({ theme }) => ({
  background: theme.palette.buttonDelete.main,
  borderRadius: 50,
  color: theme.palette.buttonDelete.contrastText,
  paddingBottom: 5,
  paddingLeft: 20,
  paddingRight: 20,
  paddingTop: 5,
  textTransform: 'none',
  '&:hover': {
    background: theme.palette.buttonDelete.light,
  },
  '&:active': {
    background: theme.palette.buttonDelete.dark,
  },
}))

interface CobraDeleteButtonProps extends Omit<ButtonProps, 'startIcon'> {
  hideIcon?: boolean;
}

export const CobraDeleteButton = ({
  hideIcon = false,
  children,
  ...props
}: CobraDeleteButtonProps) => (
  <StyledDeleteButton startIcon={!hideIcon ? <DeleteIcon /> : undefined} {...props}>
    {children}
  </StyledDeleteButton>
)
