/**
 * ReadyToFireBadge Component
 *
 * Displays a badge showing the count of injects ready to fire.
 * Pulses when count > 0 to draw attention.
 */

import { Badge, Box } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircleExclamation } from '@fortawesome/free-solid-svg-icons'
import { keyframes } from '@mui/system'

interface ReadyToFireBadgeProps {
  /** Number of injects ready to fire */
  count: number
}

const pulse = keyframes`
  0% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(1.1);
    opacity: 0.8;
  }
  100% {
    transform: scale(1);
    opacity: 1;
  }
`

export const ReadyToFireBadge = ({ count }: ReadyToFireBadgeProps) => {
  if (count === 0) {
    return null
  }

  return (
    <Badge
      badgeContent={count}
      color="error"
      sx={{
        '& .MuiBadge-badge': {
          animation: count > 0 ? `${pulse} 2s ease-in-out infinite` : 'none',
        },
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          color: 'error.main',
          px: 1,
        }}
      >
        <FontAwesomeIcon icon={faCircleExclamation} />
      </Box>
    </Badge>
  )
}

export default ReadyToFireBadge
