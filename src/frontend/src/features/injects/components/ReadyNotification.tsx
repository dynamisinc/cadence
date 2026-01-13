/**
 * ReadyNotification Component
 *
 * Visual notification that appears when a new inject becomes ready to fire.
 * Shows a brief animated banner at the top of the inject list.
 */

import { useState, useEffect, useRef } from 'react'
import { Box, Typography, Fade } from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircleExclamation } from '@fortawesome/free-solid-svg-icons'
import { keyframes } from '@mui/system'

interface ReadyNotificationProps {
  /** Current count of ready-to-fire injects */
  readyCount: number
  /** Duration to show notification in ms */
  duration?: number
}

const slideIn = keyframes`
  0% {
    transform: translateY(-100%);
    opacity: 0;
  }
  100% {
    transform: translateY(0);
    opacity: 1;
  }
`

const pulse = keyframes`
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.7;
  }
`

export const ReadyNotification = ({
  readyCount,
  duration = 3000,
}: ReadyNotificationProps) => {
  const [visible, setVisible] = useState(false)
  const [newCount, setNewCount] = useState(0)
  const prevCountRef = useRef(readyCount)

  useEffect(() => {
    // Check if ready count increased (new inject became ready)
    if (readyCount > prevCountRef.current && prevCountRef.current >= 0) {
      const diff = readyCount - prevCountRef.current
      setNewCount(diff)
      setVisible(true)

      // Hide after duration
      const timer = setTimeout(() => {
        setVisible(false)
      }, duration)

      return () => clearTimeout(timer)
    }

    prevCountRef.current = readyCount
  }, [readyCount, duration])

  // Don't render if not visible
  if (!visible) {
    return null
  }

  return (
    <Fade in={visible}>
      <Box
        sx={{
          position: 'relative',
          mb: 2,
          p: 1.5,
          backgroundColor: 'error.main',
          color: 'error.contrastText',
          borderRadius: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 1,
          animation: `${slideIn} 0.3s ease-out, ${pulse} 1s ease-in-out infinite`,
          boxShadow: 3,
        }}
      >
        <FontAwesomeIcon icon={faCircleExclamation} />
        <Typography variant="body2" fontWeight={600}>
          {newCount === 1
            ? 'New inject ready to fire!'
            : `${newCount} new injects ready to fire!`}
        </Typography>
      </Box>
    </Fade>
  )
}

export default ReadyNotification
