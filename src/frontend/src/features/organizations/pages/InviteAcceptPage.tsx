/**
 * InviteAcceptPage - Accept organization invitation
 *
 * Public page accessible via `/invite/:code` from invitation emails.
 * Validates the invitation code and allows authenticated users to accept.
 * Unauthenticated users are prompted to sign in.
 *
 * States handled:
 * - Loading: Validating invitation code
 * - Valid + Authenticated: Show invitation details with accept button
 * - Valid + Unauthenticated: Prompt to sign in
 * - Invalid/Expired: Show error message
 * - Already Member: Show friendly message
 * - Accepted: Show success message
 *
 * @module features/organizations/pages
 */
import { useState, useEffect, useRef } from 'react'
import type { FC } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Box,
  Paper,
  Typography,
  Alert,
  CircularProgress,
  Stack,
  Divider,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import {
  faEnvelope,
  faUserShield,
  faCheckCircle,
  faExclamationTriangle,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '@/theme/styledComponents'
import CobraStyles from '@/theme/CobraStyles'
import { AuthLayout } from '@/features/auth/components/AuthLayout'
import { useAuth } from '@/contexts/AuthContext'
import { organizationService } from '../services/organizationService'
import type { Invitation } from '../types'
import { getOrgRoleLabel } from '../types'
import { toast } from 'react-toastify'

type PageState =
  | 'loading'
  | 'valid'
  | 'invalid'
  | 'accepting'
  | 'accepted'
  | 'already-member'
  | 'error'

export const InviteAcceptPage: FC = () => {
  const { code } = useParams<{ code: string }>()
  const navigate = useNavigate()
  const { isAuthenticated, user } = useAuth()

  const [state, setState] = useState<PageState>('loading')
  const [invitation, setInvitation] = useState<Invitation | null>(null)
  const [errorMessage, setErrorMessage] = useState<string>('')
  const redirectTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Clean up redirect timer on unmount
  useEffect(() => {
    return () => {
      if (redirectTimerRef.current) {
        clearTimeout(redirectTimerRef.current)
      }
    }
  }, [])

  // Validate invitation code on mount
  useEffect(() => {
    const validateCode = async () => {
      if (!code) {
        setState('invalid')
        setErrorMessage('Invalid invitation link')
        return
      }

      try {
        console.log('[InviteAcceptPage] Validating code:', code)
        const invite = await organizationService.validateInvitation(code)
        console.log('[InviteAcceptPage] Validation success:', invite)
        setInvitation(invite)
        setState('valid')
      } catch (error) {
        console.error('[InviteAcceptPage] Validation failed:', error)
        setState('invalid')
        setErrorMessage('This invitation is no longer valid')
      }
    }

    validateCode()
  }, [code])

  const handleAccept = async () => {
    if (!code) return

    setState('accepting')

    try {
      console.log('[InviteAcceptPage] Accepting invitation:', code)
      await organizationService.acceptInvitation(code)
      console.log('[InviteAcceptPage] Invitation accepted successfully')
      setState('accepted')
      toast.success('Welcome! You\'ve joined the organization')

      // Navigate to home after 2 seconds
      redirectTimerRef.current = setTimeout(() => {
        navigate('/', { replace: true })
      }, 2000)
    } catch (error: unknown) {
      console.error('[InviteAcceptPage] Accept failed:', error)

      // Check for 409 Conflict (already a member)
      const axiosError = error as { response?: { status?: number; data?: { message?: string } } }
      if (axiosError.response?.status === 409) {
        setState('already-member')
      } else {
        setState('error')
        const errorMsg = axiosError.response?.data?.message || 'Failed to accept invitation'
        setErrorMessage(errorMsg)
        toast.error(errorMsg)
      }
    }
  }

  const handleSignIn = () => {
    // Save return URL so we can come back after login
    navigate('/login', {
      state: { from: { pathname: `/invite/${code}` } },
    })
  }

  const handleGoToDashboard = () => {
    navigate('/', { replace: true })
  }

  // Loading state
  if (state === 'loading') {
    return (
      <AuthLayout title="Validating Invitation">
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: 200,
            gap: 2,
          }}
        >
          <CircularProgress size={48} />
          <Typography variant="body2" color="text.secondary">
            Validating your invitation...
          </Typography>
        </Box>
      </AuthLayout>
    )
  }

  // Invalid/Not Found
  if (state === 'invalid') {
    return (
      <AuthLayout title="Invalid Invitation">
        <Stack spacing={3}>
          <Box sx={{ textAlign: 'center' }}>
            <FontAwesomeIcon
              icon={faExclamationTriangle}
              size="3x"
              style={{ color: '#f44336' }}
            />
          </Box>

          <Alert severity="error">
            {errorMessage || 'This invitation is no longer valid'}
          </Alert>

          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
            It may have been cancelled, expired, or already used.
          </Typography>

          {isAuthenticated ? (
            <CobraPrimaryButton onClick={handleGoToDashboard} fullWidth>
              Go to Dashboard
            </CobraPrimaryButton>
          ) : (
            <Stack spacing={2}>
              <CobraPrimaryButton onClick={handleSignIn} fullWidth>
                Sign In
              </CobraPrimaryButton>
              <CobraSecondaryButton onClick={() => navigate('/register')} fullWidth>
                Create an Account
              </CobraSecondaryButton>
            </Stack>
          )}
        </Stack>
      </AuthLayout>
    )
  }

  // Already a member
  if (state === 'already-member') {
    return (
      <AuthLayout title="Already a Member">
        <Stack spacing={3}>
          <Box sx={{ textAlign: 'center' }}>
            <FontAwesomeIcon
              icon={faCheckCircle}
              size="3x"
              style={{ color: '#4caf50' }}
            />
          </Box>

          <Alert severity="info">
            You're already a member of this organization
          </Alert>

          <CobraPrimaryButton onClick={handleGoToDashboard} fullWidth>
            Go to Dashboard
          </CobraPrimaryButton>
        </Stack>
      </AuthLayout>
    )
  }

  // Accepted successfully
  if (state === 'accepted') {
    return (
      <AuthLayout title="Welcome!">
        <Stack spacing={3}>
          <Box sx={{ textAlign: 'center' }}>
            <FontAwesomeIcon
              icon={faCheckCircle}
              size="3x"
              style={{ color: '#4caf50' }}
            />
          </Box>

          <Alert severity="success">
            You've successfully joined the organization
          </Alert>

          <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
            Redirecting to dashboard...
          </Typography>

          <CircularProgress size={24} sx={{ mx: 'auto' }} />
        </Stack>
      </AuthLayout>
    )
  }

  // Error during accept
  if (state === 'error') {
    return (
      <AuthLayout title="Error">
        <Stack spacing={3}>
          <Box sx={{ textAlign: 'center' }}>
            <FontAwesomeIcon
              icon={faExclamationTriangle}
              size="3x"
              style={{ color: '#f44336' }}
            />
          </Box>

          <Alert severity="error">
            {errorMessage || 'An error occurred while accepting the invitation'}
          </Alert>

          <Stack spacing={2}>
            <CobraSecondaryButton onClick={() => window.location.reload()} fullWidth>
              Try Again
            </CobraSecondaryButton>
            {isAuthenticated ? (
              <CobraPrimaryButton onClick={handleGoToDashboard} fullWidth>
                Go to Dashboard
              </CobraPrimaryButton>
            ) : (
              <CobraPrimaryButton onClick={handleSignIn} fullWidth>
                Sign In
              </CobraPrimaryButton>
            )}
          </Stack>
        </Stack>
      </AuthLayout>
    )
  }

  // Valid invitation (state === 'valid' or 'accepting')
  if (!invitation) {
    return null // Should not happen
  }

  // Not authenticated - prompt to sign in
  if (!isAuthenticated) {
    return (
      <AuthLayout title="Sign In Required">
        <Stack spacing={3}>
          <Alert severity="info">
            You've been invited to join an organization. Please sign in to accept this
            invitation.
          </Alert>

          <Paper sx={{ p: 2, bgcolor: 'background.default' }}>
            <Stack spacing={1.5}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                <FontAwesomeIcon icon={faEnvelope} style={{ color: '#666' }} />
                <Box sx={{ flex: 1 }}>
                  <Typography variant="caption" color="text.secondary">
                    Invited Email
                  </Typography>
                  <Typography variant="body2" fontWeight={500}>
                    {invitation.email}
                  </Typography>
                </Box>
              </Box>

              <Divider />

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                <FontAwesomeIcon icon={faUserShield} style={{ color: '#666' }} />
                <Box sx={{ flex: 1 }}>
                  <Typography variant="caption" color="text.secondary">
                    Assigned Role
                  </Typography>
                  <Typography variant="body2" fontWeight={500}>
                    {getOrgRoleLabel(invitation.role)}
                  </Typography>
                </Box>
              </Box>

              <Divider />

              <Box>
                <Typography variant="caption" color="text.secondary">
                  Invited By
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {invitation.invitedByName}
                </Typography>
              </Box>
            </Stack>
          </Paper>

          <CobraPrimaryButton onClick={handleSignIn} fullWidth>
            Sign In to Accept
          </CobraPrimaryButton>

          <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'center' }}>
            Don't have an account? You can create one after signing in.
          </Typography>
        </Stack>
      </AuthLayout>
    )
  }

  // Authenticated - show invitation details and accept button
  return (
    <AuthLayout title="You're Invited!">
      <Stack spacing={3}>
        <Alert severity="success">
          You've been invited to join an organization
        </Alert>

        <Paper sx={{ p: 2, bgcolor: 'background.default' }}>
          <Stack spacing={1.5}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <FontAwesomeIcon icon={faEnvelope} style={{ color: '#666' }} />
              <Box sx={{ flex: 1 }}>
                <Typography variant="caption" color="text.secondary">
                  Invited Email
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {invitation.email}
                </Typography>
              </Box>
            </Box>

            <Divider />

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
              <FontAwesomeIcon icon={faUserShield} style={{ color: '#666' }} />
              <Box sx={{ flex: 1 }}>
                <Typography variant="caption" color="text.secondary">
                  Your Role
                </Typography>
                <Typography variant="body2" fontWeight={500}>
                  {getOrgRoleLabel(invitation.role)}
                </Typography>
              </Box>
            </Box>

            <Divider />

            <Box>
              <Typography variant="caption" color="text.secondary">
                Invited By
              </Typography>
              <Typography variant="body2" fontWeight={500}>
                {invitation.invitedByName}
              </Typography>
            </Box>
          </Stack>
        </Paper>

        {/* Check email match warning */}
        {user && invitation.email.toLowerCase() !== user.email.toLowerCase() && (
          <Alert severity="warning">
            This invitation was sent to <strong>{invitation.email}</strong>, but you're
            signed in as <strong>{user.email}</strong>. The invitation will still work.
          </Alert>
        )}

        <CobraPrimaryButton
          onClick={handleAccept}
          fullWidth
          disabled={state === 'accepting'}
          startIcon={
            state === 'accepting' ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : undefined
          }
        >
          {state === 'accepting' ? 'Accepting...' : 'Accept Invitation'}
        </CobraPrimaryButton>

        <CobraSecondaryButton onClick={handleGoToDashboard} fullWidth>
          Decline
        </CobraSecondaryButton>
      </Stack>
    </AuthLayout>
  )
}

export default InviteAcceptPage
