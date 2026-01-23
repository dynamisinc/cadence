/**
 * ResetPasswordPage - Complete password reset with token
 *
 * Features:
 * - New password input with strength validation
 * - Confirm password matching
 * - Token validation from URL
 * - Expired/invalid token handling
 * - Password requirements feedback
 * - Success redirect to login
 *
 * @module features/auth
 * @see authentication/S24-password-reset.md
 */
import { useState, useEffect } from 'react'
import type { FC, FormEvent } from 'react'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import {
  Stack,
  IconButton,
  InputAdornment,
  Box,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEye, faEyeSlash, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraTextField } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { AuthLayout } from '../components/AuthLayout'
import { PasswordRequirements } from '../components/PasswordRequirements'
import { authService } from '../services/authService'
import { validatePassword, isPasswordValid } from '../types'

/**
 * Password reset completion page (accessed via email link)
 */
export const ResetPasswordPage: FC = () => {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [confirmPasswordError, setConfirmPasswordError] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [tokenError, setTokenError] = useState('')
  const [isSuccess, setIsSuccess] = useState(false)

  // Password requirements state
  const passwordReqs = validatePassword(newPassword)
  const passwordValid = isPasswordValid(newPassword)

  // Validate token on mount
  useEffect(() => {
    if (!token) {
      setTokenError('No reset token provided. Please request a new password reset.')
    }
  }, [token])

  const validateConfirmPassword = (value: string): boolean => {
    if (newPassword && value && newPassword !== value) {
      setConfirmPasswordError('Passwords do not match')
      return false
    }
    setConfirmPasswordError('')
    return true
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (!token) {
      return
    }

    if (!passwordValid) {
      return
    }

    const isConfirmPasswordValid = validateConfirmPassword(confirmPassword)
    if (!isConfirmPasswordValid) {
      return
    }

    setIsSubmitting(true)
    setError(null)

    try {
      await authService.completePasswordReset(token, newPassword)
      setIsSuccess(true)

      // Redirect to login after 3 seconds
      setTimeout(() => navigate('/login'), 3000)
    } catch (err: unknown) {
      interface ErrorData {
        code?: string
        message?: string
        error?: { code?: string; message?: string }
      }
      const axiosError = err as { response?: { data?: ErrorData } }
      if (axiosError.response?.data) {
        const errorResponse = axiosError.response.data
        if (errorResponse.code === 'invalid_token' || errorResponse.error?.code === 'invalid_token') {
          setTokenError('This reset link is invalid or has expired. Please request a new one.')
        } else {
          setError(errorResponse.message || errorResponse.error?.message || 'Failed to reset password')
        }
      } else {
        setError('Unable to connect to server. Please check your connection.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  // Show error if token is invalid/missing
  if (tokenError) {
    return (
      <AuthLayout title="Reset Link Invalid">
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          <Alert severity="error">{tokenError}</Alert>
          <Link to="/forgot-password" style={{ textDecoration: 'none' }}>
            <CobraPrimaryButton fullWidth>
              Request New Reset Link
            </CobraPrimaryButton>
          </Link>
          <Link to="/login" style={{ textDecoration: 'none' }}>
            <CobraPrimaryButton fullWidth>
              Back to Sign In
            </CobraPrimaryButton>
          </Link>
        </Stack>
      </AuthLayout>
    )
  }

  // Show success state
  if (isSuccess) {
    return (
      <AuthLayout title="Password Reset Successful">
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          <Alert severity="success">
            Your password has been reset successfully. Redirecting to login...
          </Alert>
          <Link to="/login" style={{ textDecoration: 'none' }}>
            <CobraPrimaryButton fullWidth>
              Go to Sign In
            </CobraPrimaryButton>
          </Link>
        </Stack>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Set New Password">
      <form onSubmit={handleSubmit}>
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          {/* Error Message */}
          {error && (
            <Alert severity="error">{error}</Alert>
          )}

          {/* New Password Field */}
          <Box>
            <CobraTextField
              label="New Password"
              type={showNewPassword ? 'text' : 'password'}
              value={newPassword}
              onChange={e => setNewPassword(e.target.value)}
              fullWidth
              required
              autoFocus
              autoComplete="new-password"
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowNewPassword(!showNewPassword)}
                      edge="end"
                      aria-label={showNewPassword ? 'Hide password' : 'Show password'}
                    >
                      <FontAwesomeIcon icon={showNewPassword ? faEyeSlash : faEye} />
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
            {/* Show password requirements when user starts typing */}
            {newPassword && <PasswordRequirements requirements={passwordReqs} />}
          </Box>

          {/* Confirm Password Field */}
          <CobraTextField
            label="Confirm Password"
            type={showConfirmPassword ? 'text' : 'password'}
            value={confirmPassword}
            onChange={e => {
              setConfirmPassword(e.target.value)
              if (confirmPasswordError) validateConfirmPassword(e.target.value)
            }}
            onBlur={() => validateConfirmPassword(confirmPassword)}
            error={!!confirmPasswordError}
            helperText={confirmPasswordError}
            fullWidth
            required
            autoComplete="new-password"
            InputProps={{
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    edge="end"
                    aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                  >
                    <FontAwesomeIcon icon={showConfirmPassword ? faEyeSlash : faEye} />
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          {/* Set New Password Button */}
          <CobraPrimaryButton
            type="submit"
            fullWidth
            disabled={isSubmitting || !passwordValid}
          >
            {isSubmitting ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : (
              'Set New Password'
            )}
          </CobraPrimaryButton>
        </Stack>
      </form>
    </AuthLayout>
  )
}
