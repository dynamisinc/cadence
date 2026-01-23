/**
 * PasswordRequirements - Visual feedback for password strength
 *
 * Shows password requirements with checkmarks/X marks:
 * - Min 8 characters
 * - At least 1 uppercase letter
 * - At least 1 number
 *
 * @module features/auth
 * @see authentication/S01-registration-form.md
 * @see authentication/S24-password-reset.md
 */
import { FC } from 'react'
import { Box, Typography, useTheme } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faXmark } from '@fortawesome/free-solid-svg-icons'
import type { PasswordRequirements as PasswordReqs } from '../types'

interface RequirementItemProps {
  met: boolean
  text: string
  successColor: string
  defaultColor: string
}

/**
 * Single requirement item with check/X icon
 */
const RequirementItem: FC<RequirementItemProps> = ({
  met,
  text,
  successColor,
  defaultColor,
}) => (
  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
    <FontAwesomeIcon
      icon={met ? faCheck : faXmark}
      style={{
        color: met ? successColor : defaultColor,
        fontSize: '14px',
      }}
    />
    <Typography
      variant="body2"
      sx={{
        color: met ? successColor : defaultColor,
      }}
    >
      {text}
    </Typography>
  </Box>
)

interface PasswordRequirementsProps {
  /** Password requirements validation state */
  requirements: PasswordReqs
}

/**
 * Renders password requirements checklist with visual indicators
 */
export const PasswordRequirements: FC<PasswordRequirementsProps> = ({ requirements }) => {
  const theme = useTheme()
  const successColor = theme.palette.success.main
  const defaultColor = theme.palette.text.secondary

  return (
    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
      <RequirementItem
        met={requirements.minLength}
        text="At least 8 characters"
        successColor={successColor}
        defaultColor={defaultColor}
      />
      <RequirementItem
        met={requirements.hasUppercase}
        text="At least 1 uppercase letter"
        successColor={successColor}
        defaultColor={defaultColor}
      />
      <RequirementItem
        met={requirements.hasNumber}
        text="At least 1 number"
        successColor={successColor}
        defaultColor={defaultColor}
      />
    </Box>
  )
}
