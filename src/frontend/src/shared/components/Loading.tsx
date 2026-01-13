/**
 * Loading Component
 *
 * Centralized loading indicator for consistent UX across the application.
 * Supports different sizes and display modes.
 *
 * Usage:
 * ```tsx
 * // Full page loading
 * <Loading />
 *
 * // Inline loading with custom message
 * <Loading size="small" message="Saving..." />
 *
 * // Overlay loading (blocks interaction)
 * <Loading overlay />
 * ```
 */

import { Box, CircularProgress, Typography } from '@mui/material'
import { useTheme } from '@mui/material/styles'

export interface LoadingProps {
  /** Size of the spinner */
  size?: 'small' | 'medium' | 'large';
  /** Optional message to display below spinner */
  message?: string;
  /** If true, centers in full viewport height */
  fullPage?: boolean;
  /** If true, shows as an overlay that blocks interaction */
  overlay?: boolean;
}

const sizeMap = {
  small: 24,
  medium: 40,
  large: 56,
}

/**
 * Centralized loading indicator component
 */
export const Loading = ({
  size = 'medium',
  message,
  fullPage = false,
  overlay = false,
}: LoadingProps) => {
  const theme = useTheme()
  const spinnerSize = sizeMap[size]

  const content = (
    <Box
      data-testid="loading"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2,
        ...(fullPage && {
          minHeight: '100vh',
        }),
        ...(!fullPage && !overlay && {
          py: 4,
        }),
      }}
    >
      <CircularProgress
        size={spinnerSize}
        sx={{
          color: theme.palette.buttonPrimary.main,
        }}
      />
      {message && (
        <Typography
          variant={size === 'small' ? 'caption' : 'body2'}
          color="text.secondary"
        >
          {message}
        </Typography>
      )}
    </Box>
  )

  if (overlay) {
    return (
      <Box
        data-testid="loading-overlay"
        sx={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: 'rgba(255, 255, 255, 0.8)',
          zIndex: theme.zIndex.modal + 1,
        }}
      >
        {content}
      </Box>
    )
  }

  return content
}

export default Loading
