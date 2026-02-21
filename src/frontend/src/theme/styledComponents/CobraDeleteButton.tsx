import { Button, type ButtonProps } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faTrash } from '@fortawesome/free-solid-svg-icons'
import { styled } from '@mui/material/styles'

/**
 * CobraDeleteButton - Red delete/remove button
 *
 * Use for:
 * - Delete actions
 * - Remove items
 * - Destructive operations
 *
 * Supports MUI size variants (small, medium, large).
 */
const StyledDeleteButton = styled(Button, {
  shouldForwardProp: (prop) => prop !== 'size',
})<{ size?: 'small' | 'medium' | 'large' }>(({ theme, size }) => ({
  background: theme.palette.buttonDelete.main,
  borderRadius: 50,
  color: theme.palette.buttonDelete.contrastText,
  textTransform: 'none' as const,
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
    background: theme.palette.buttonDelete.light,
  },
  '&:active': {
    background: theme.palette.buttonDelete.dark,
  },
}))

interface CobraDeleteButtonProps extends ButtonProps {
  hideIcon?: boolean;
}

export const CobraDeleteButton = ({
  hideIcon = false,
  startIcon,
  children,
  ...props
}: CobraDeleteButtonProps) => (
  <StyledDeleteButton
    startIcon={hideIcon ? undefined : (startIcon ?? <FontAwesomeIcon icon={faTrash} />)}
    {...props}
  >
    {children}
  </StyledDeleteButton>
)
