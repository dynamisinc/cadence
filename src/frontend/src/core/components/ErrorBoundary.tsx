/**
 * ErrorBoundary Component
 *
 * Catches JavaScript errors in child component tree,
 * logs the error, and displays a reassuring fallback UI.
 *
 * Design Philosophy:
 * - Feels reassuring, not alarming
 * - Clear recovery options
 * - Maintains COBRA styling consistency
 * - Shows technical details only in development
 * - Responsive: wider error details on desktop
 *
 * Usage:
 * ```tsx
 * <ErrorBoundary>
 *   <YourComponent />
 * </ErrorBoundary>
 * ```
 */

import { Component, type ErrorInfo, type ReactNode } from 'react'
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
  faPaperPlane,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import {
  CobraPrimaryButton,
  CobraSecondaryButton,
} from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import { feedbackService } from '@/features/feedback'

interface ErrorBoundaryProps {
  /** Child components to render */
  children: ReactNode
  /** Optional fallback UI to display on error */
  fallback?: ReactNode
  /** Optional callback when error is caught */
  onError?: (error: Error, errorInfo: ErrorInfo) => void
}

interface ErrorBoundaryState {
  hasError: boolean
  error: Error | null
  errorInfo: ErrorInfo | null
  showDetails: boolean
  copied: boolean
  reportSending: boolean
  reportSent: boolean
  reportError: string | null
}

/**
 * Wrapper component to access hooks (useMediaQuery, useTheme) in class component
 */
function ErrorBoundaryUI({
  error,
  errorInfo,
  showDetails,
  copied,
  reportSending,
  reportSent,
  reportError,
  onReset,
  onReload,
  onToggleDetails,
  onCopy,
  onSendReport,
}: {
  error: Error | null
  errorInfo: ErrorInfo | null
  showDetails: boolean
  copied: boolean
  reportSending: boolean
  reportSent: boolean
  reportError: string | null
  onReset: () => void
  onReload: () => void
  onToggleDetails: () => void
  onCopy: () => void
  onSendReport: () => void
}) {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'))
  const isTablet = useMediaQuery(theme.breakpoints.up('md'))

  // Responsive widths
  const cardMaxWidth = isDesktop ? 600 : isTablet ? 520 : 480
  const detailsMaxHeight = isDesktop ? 300 : 200

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
          transition: 'max-width 0.3s ease',
        }}
      >
        {/* Friendly Icon */}
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

        {/* Friendly Heading */}
        <Typography
          variant={isTablet ? 'h5' : 'h6'}
          sx={{
            fontWeight: 600,
            mb: 1.5,
            color: 'text.primary',
          }}
        >
          We hit a snag
        </Typography>

        {/* Reassuring Message */}
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

        {/* Action Buttons */}
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          justifyContent="center"
          sx={{ mb: 3 }}
        >
          <CobraSecondaryButton
            onClick={onReset}
            startIcon={<FontAwesomeIcon icon={faArrowRotateLeft} />}
            fullWidth={!isTablet}
          >
            Try Again
          </CobraSecondaryButton>
          <CobraPrimaryButton
            onClick={onReload}
            startIcon={<FontAwesomeIcon icon={faRotateRight} />}
            fullWidth={!isTablet}
          >
            Refresh Page
          </CobraPrimaryButton>
        </Stack>

        {/* Send Error Report Button */}
        <Box sx={{ mb: 2 }}>
          {reportError && (
            <Typography
              variant="caption"
              color="error"
              sx={{ display: 'block', mb: 1 }}
            >
              {reportError}
            </Typography>
          )}
          <CobraSecondaryButton
            onClick={onSendReport}
            startIcon={
              reportSending ? (
                <FontAwesomeIcon icon={faSpinner} spin />
              ) : reportSent ? (
                <FontAwesomeIcon icon={faCheck} />
              ) : (
                <FontAwesomeIcon icon={faPaperPlane} />
              )
            }
            disabled={reportSending || reportSent}
          >
            {reportSending
              ? 'Sending...'
              : reportSent
                ? 'Report Sent'
                : 'Send Error Report'}
          </CobraSecondaryButton>
        </Box>

        {/* Development-only: Error Details */}
        {import.meta.env.DEV && error && (
          <Box sx={{ mt: 3 }}>
            <Box
              onClick={onToggleDetails}
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 0.75,
                cursor: 'pointer',
                color: 'text.secondary',
                fontSize: '0.8125rem',
                '&:hover': {
                  color: 'text.primary',
                },
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
                {/* Copy Button */}
                <Tooltip title={copied ? 'Copied!' : 'Copy error details'}>
                  <IconButton
                    onClick={onCopy}
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
                    pr: 4, // Space for copy button
                    fontSize: { xs: '0.7rem', md: '0.75rem' },
                    lineHeight: 1.5,
                    color: 'text.secondary',
                  }}
                >
                  {error.toString()}
                  {errorInfo?.componentStack && (
                    <>
                      {'\n\n'}
                      <span style={{ opacity: 0.7 }}>Component Stack:</span>
                      {errorInfo.componentStack}
                    </>
                  )}
                </Typography>
              </Paper>
            </Collapse>
          </Box>
        )}

        {/* Subtle Help Text */}
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

export class ErrorBoundary extends Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = {
      hasError: false,
      error: null,
      errorInfo: null,
      showDetails: false,
      copied: false,
      reportSending: false,
      reportSent: false,
      reportError: null,
    }
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Log the error
    console.error('ErrorBoundary caught an error:', error, errorInfo)

    this.setState({ errorInfo })

    // Call optional error callback
    if (this.props.onError) {
      this.props.onError(error, errorInfo)
    }
  }

  handleReset = (): void => {
    this.setState({
      hasError: false,
      error: null,
      errorInfo: null,
      showDetails: false,
      copied: false,
    })
  }

  handleReload = (): void => {
    window.location.reload()
  }

  toggleDetails = (): void => {
    this.setState(prev => ({ showDetails: !prev.showDetails }))
  }

  handleCopy = async (): Promise<void> => {
    const { error, errorInfo } = this.state
    if (!error) return

    let text = error.toString()
    if (errorInfo?.componentStack) {
      text += '\n\nComponent Stack:' + errorInfo.componentStack
    }

    try {
      await navigator.clipboard.writeText(text)
      this.setState({ copied: true })
      // Reset copied state after 2 seconds
      setTimeout(() => {
        this.setState({ copied: false })
      }, 2000)
    } catch (err) {
      console.error('Failed to copy error details:', err)
    }
  }

  handleSendReport = async (): Promise<void> => {
    const { error, errorInfo } = this.state
    if (!error) return

    this.setState({ reportSending: true, reportError: null })

    try {
      await feedbackService.submitErrorReport({
        errorMessage: error.toString(),
        stackTrace: error.stack ?? undefined,
        componentStack: errorInfo?.componentStack ?? undefined,
        url: window.location.href,
        browser: navigator.userAgent,
      })
      this.setState({ reportSent: true, reportSending: false })
    } catch (err) {
      console.error('Failed to send error report:', err)
      this.setState({
        reportSending: false,
        reportError: 'Failed to send report. Please try again.',
      })
    }
  }

  render(): ReactNode {
    if (this.state.hasError) {
      // Custom fallback provided
      if (this.props.fallback) {
        return this.props.fallback
      }

      // Default error UI - Reassuring and user-friendly
      return (
        <ErrorBoundaryUI
          error={this.state.error}
          errorInfo={this.state.errorInfo}
          showDetails={this.state.showDetails}
          copied={this.state.copied}
          reportSending={this.state.reportSending}
          reportSent={this.state.reportSent}
          reportError={this.state.reportError}
          onReset={this.handleReset}
          onReload={this.handleReload}
          onToggleDetails={this.toggleDetails}
          onCopy={this.handleCopy}
          onSendReport={this.handleSendReport}
        />
      )
    }

    return this.props.children
  }
}

export default ErrorBoundary
