/**
 * MobileBlocker Component
 *
 * Displays a blocking message on mobile devices (< 768px viewport width).
 * Cadence is optimized for tablets and desktops only.
 *
 * Per S04-responsive-design.md:
 * - Screen widths < 768px should show a blocker message
 * - "Cadence is optimized for tablets and desktops. Please use a larger screen."
 */

import { useState, useEffect, type ReactNode } from 'react'
import { Box, Typography, Paper, useMediaQuery } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faMobileScreen, faArrowRight, faDesktop } from '@fortawesome/free-solid-svg-icons'

interface MobileBlockerProps {
  /** Child components to render when viewport is supported */
  children: ReactNode;
}

/**
 * Blocks mobile users from accessing the application
 * Shows a friendly message directing them to use a larger device
 */
export const MobileBlocker = ({ children }: MobileBlockerProps) => {
  const theme = useTheme()
  // md breakpoint is 768px per our custom breakpoints
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const [width, setWidth] = useState(
    typeof window !== 'undefined' ? window.innerWidth : 1024,
  )

  useEffect(() => {
    const handleResize = () => setWidth(window.innerWidth)
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  if (isMobile) {
    return (
      <Box
        data-testid="mobile-blocker"
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          minHeight: '100vh',
          backgroundColor: theme.palette.background.default,
          p: 3,
        }}
      >
        <Paper
          elevation={0}
          sx={{
            p: 4,
            maxWidth: 400,
            textAlign: 'center',
            border: '1px solid',
            borderColor: 'divider',
            borderRadius: 2,
          }}
        >
          {/* Device Icons */}
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: 2,
              mb: 3,
              color: theme.palette.buttonPrimary.main,
            }}
          >
            <FontAwesomeIcon icon={faMobileScreen} style={{ fontSize: 48 }} />
            <FontAwesomeIcon icon={faArrowRight} style={{ fontSize: 24 }} />
            <FontAwesomeIcon icon={faDesktop} style={{ fontSize: 48 }} />
          </Box>

          <Typography variant="h5" gutterBottom fontWeight="bold">
            Larger Screen Required
          </Typography>

          <Typography
            variant="body1"
            color="text.secondary"
            sx={{ mb: 3, lineHeight: 1.6 }}
          >
            Cadence works best on tablets and desktops.
            Please use a device with a screen width of at least 768 pixels.
          </Typography>

          <Paper
            variant="outlined"
            sx={{
              p: 2,
              backgroundColor: theme.palette.grid.light,
              borderColor: theme.palette.divider,
            }}
          >
            <Typography variant="body2" color="text.secondary">
              Current width: <strong>{width}px</strong>
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Minimum required: <strong>768px</strong>
            </Typography>
          </Paper>
        </Paper>
      </Box>
    )
  }

  return <>{children}</>
}

export default MobileBlocker
