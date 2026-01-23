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
import { Box, Typography } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCheck, faXmark } from '@fortawesome/free-solid-svg-icons'
import { useTheme } from '@mui/material/styles'
import type { PasswordRequirements as PasswordReqs } from '../types'

interface PasswordRequirementsProps {
  /** Password requirements validation state */
  requirements: PasswordReqs;
}

/**
 * Renders password requirements checklist with visual indicators
 */
export const PasswordRequirements: FC<PasswordRequirementsProps> = ({ requirements }) => {
  const theme = useTheme()

  const Requirement: FC<{ met: boolean; text: string }> = ({ met, text }) => (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <FontAwesomeIcon
        icon={met ? faCheck : faXmark}
        style={{
          color: met ? theme.palette.success.main : theme.palette.text.secondary,
          fontSize: '14px',
        }}
      />
      <Typography
        variant="body2"
        sx={{
          color: met ? theme.palette.success.main : theme.palette.text.secondary,
        }}
      >
        {text}
      </Typography>
    </Box>
  )

  return (
    <Box sx={{ mt: 1, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
      <Requirement met={requirements.minLength} text="At least 8 characters" />
      <Requirement met={requirements.hasUppercase} text="At least 1 uppercase letter" />
      <Requirement met={requirements.hasNumber} text="At least 1 number" />
    </Box>
  )
}
