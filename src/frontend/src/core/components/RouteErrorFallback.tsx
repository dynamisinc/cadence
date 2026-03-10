/**
 * RouteErrorFallback
 *
 * Error element for React Router's createBrowserRouter.
 * React Router catches route-level render errors internally,
 * so the app-level ErrorBoundary never sees them.
 * This component bridges that gap by using useRouteError()
 * to display a user-friendly error UI consistent with ErrorBoundary.
 */

import { useRouteError, useNavigate } from 'react-router-dom'
import {
  Box,
  Typography,
  Paper,
  Stack,
  Collapse,
  alpha,
  useMediaQuery,
  useTheme,
  Tooltip,
  IconButton,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faLifeRing,
  faRotateRight,
  faArrowRotateLeft,
  faChevronDown,
  faChevronUp,
  faCopy,
  faCheck,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import { useState } from 'react'

/** Duration to show the "Copied!" feedback before resetting (ms) */
const COPY_FEEDBACK_DURATION_MS = 2000

export const RouteErrorFallback = () => {
  const error = useRouteError()
  const navigate = useNavigate()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'))
  const isTablet = useMediaQuery(theme.breakpoints.up('md'))

  const [showDetails, setShowDetails] = useState(false)
  const [copied, setCopied] = useState(false)

  const cardMaxWidth = isDesktop ? 600 : isTablet ? 520 : 480
  const detailsMaxHeight = isDesktop ? 300 : 200

  const errorMessage =
    error instanceof Error ? error.toString() : String(error)
  const errorStack =
    error instanceof Error ? error.stack : undefined

  const handleGoBack = () => {
    navigate(-1)
  }

  const handleReload = () => {
    window.location.reload()
  }

  const handleCopy = async () => {
    let text = errorMessage
    if (errorStack) {
      text += '\n\nStack:\n' + errorStack
    }
    try {
      await navigator.clipboard.writeText(text)
      setCopied(true)
      setTimeout(() => setCopied(false), COPY_FEEDBACK_DURATION_MS)
    } catch (err) {
      console.error('Failed to copy error details:', err)
    }
  }

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100vh',
        padding: CobraStyles.Padding.MainWindow,
        background: theme =>
          theme.palette.mode === 'dark'
            ? `linear-gradient(135deg, ${alpha(theme.palette.info.dark, 0.1)} 0%, ${theme.palette.background.default} 50%)`
            : `linear-gradient(135deg, ${alpha(theme.palette.info.light, 0.05)} 0%, ${theme.palette.background.default} 50%)`,
      }}
    >
      <Paper
        elevation={0}
        sx={{
          p: { xs: 3, sm: 4, md: 5 },
          maxWidth: cardMaxWidth,
          width: '100%',
          textAlign: 'center',
          borderRadius: 3,
          border: '1px solid',
          borderColor: theme => alpha(theme.palette.divider, 0.3),
          background: theme => theme.palette.background.paper,
          boxShadow: theme =>
            `0 8px 32px ${alpha(theme.palette.common.black, 0.08)}`,
        }}
      >
        <Box
          sx={{
            width: { xs: 64, md: 80 },
            height: { xs: 64, md: 80 },
            borderRadius: '50%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto',
            mb: { xs: 2, md: 3 },
            background: theme => alpha(theme.palette.info.main, 0.1),
          }}
        >
          <FontAwesomeIcon
            icon={faLifeRing}
            style={{
              fontSize: isTablet ? '36px' : '28px',
              color: '#1e3a5f',
            }}
          />
        </Box>

        <Typography
          variant={isTablet ? 'h5' : 'h6'}
          sx={{ fontWeight: 600, mb: 1.5, color: 'text.primary' }}
        >
          We hit a snag
        </Typography>

        <Typography
          variant="body1"
          sx={{
            color: 'text.secondary',
            mb: 4,
            lineHeight: 1.6,
            maxWidth: 360,
            mx: 'auto',
            fontSize: { xs: '0.875rem', md: '1rem' },
          }}
        >
          Something unexpected happened, but don't worry — your data is safe.
          Let's get you back on track.
        </Typography>

        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          justifyContent="center"
          sx={{ mb: 3 }}
        >
          <CobraSecondaryButton
            onClick={handleGoBack}
            startIcon={<FontAwesomeIcon icon={faArrowRotateLeft} />}
            fullWidth={!isTablet}
          >
            Go Back
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={handleReload}
            startIcon={<FontAwesomeIcon icon={faRotateRight} />}
            fullWidth={!isTablet}
          >
            Refresh Page
          </CobraPrimaryButton>
        </Stack>

        {import.meta.env.DEV && (
          <Box sx={{ mt: 3 }}>
            <Box
              onClick={() => setShowDetails(v => !v)}
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 0.75,
                cursor: 'pointer',
                color: 'text.secondary',
                fontSize: '0.8125rem',
                '&:hover': { color: 'text.primary' },
              }}
            >
              <FontAwesomeIcon
                icon={showDetails ? faChevronUp : faChevronDown}
                style={{ fontSize: '10px' }}
              />
              <Typography variant="caption" sx={{ fontWeight: 500 }}>
                {showDetails ? 'Hide' : 'Show'} technical details
              </Typography>
            </Box>

            <Collapse in={showDetails}>
              <Paper
                variant="outlined"
                sx={{
                  mt: 2,
                  p: 2,
                  bgcolor: theme =>
                    theme.palette.mode === 'dark'
                      ? alpha(theme.palette.common.black, 0.2)
                      : alpha(theme.palette.common.black, 0.02),
                  textAlign: 'left',
                  maxHeight: detailsMaxHeight,
                  overflow: 'auto',
                  borderRadius: 2,
                  borderColor: theme => alpha(theme.palette.divider, 0.2),
                  position: 'relative',
                }}
              >
                <Tooltip title={copied ? 'Copied!' : 'Copy error details'}>
                  <IconButton
                    onClick={handleCopy}
                    size="small"
                    sx={{
                      position: 'absolute',
                      top: 8,
                      right: 8,
                      bgcolor: theme =>
                        alpha(theme.palette.background.paper, 0.8),
                      '&:hover': {
                        bgcolor: theme => theme.palette.background.paper,
                      },
                    }}
                  >
                    <FontAwesomeIcon
                      icon={copied ? faCheck : faCopy}
                      style={{
                        fontSize: '14px',
                        color: copied ? '#08682a' : undefined,
                      }}
                    />
                  </IconButton>
                </Tooltip>

                <Typography
                  variant="caption"
                  component="pre"
                  sx={{
                    fontFamily:
                      'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Consolas, monospace',
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word',
                    m: 0,
                    pr: 4,
                    fontSize: { xs: '0.7rem', md: '0.75rem' },
                    lineHeight: 1.5,
                    color: 'text.secondary',
                  }}
                >
                  {errorMessage}
                  {errorStack && (
                    <>
                      {'\n\n'}
                      <span style={{ opacity: 0.7 }}>Stack:</span>
                      {'\n'}
                      {errorStack}
                    </>
                  )}
                </Typography>
              </Paper>
            </Collapse>
          </Box>
        )}

        <Typography
          variant="caption"
          sx={{
            display: 'block',
            mt: 3,
            color: theme => alpha(theme.palette.text.secondary, 0.7),
            fontSize: { xs: '0.7rem', md: '0.75rem' },
          }}
        >
          If this keeps happening, try clearing your browser cache
        </Typography>
      </Paper>
    </Box>
  )
}

export default RouteErrorFallback
