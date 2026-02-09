/**
 * RegisterPage - New user account creation
 *
 * Features:
 * - Display Name, Email, Password, Confirm Password fields
 * - Real-time password requirements feedback
 * - Password visibility toggles
 * - Inline validation errors
 * - Loading state during submission
 * - First user admin welcome message
 *
 * @module features/auth
 * @see authentication/S01-registration-form.md
 * @see authentication/S02-password-requirements.md
 * @see authentication/S03-first-user-admin.md
 */
import { useState, useEffect } from 'react'
import type { FC, FormEvent } from 'react'
import { Link, useNavigate, useLocation } from 'react-router-dom'
import {
  Stack,
  IconButton,
  InputAdornment,
  Typography,
  Box,
  Alert,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEye, faEyeSlash, faSpinner } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraTextField } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { AuthLayout } from '../components/AuthLayout'
import { PasswordRequirements } from '../components/PasswordRequirements'
import { useAuth } from '../../../contexts/AuthContext'
import { validatePassword, isPasswordValid } from '../types'
import { organizationService } from '../../organizations/services/organizationService'
import { toast } from 'react-toastify'

/**
 * Registration page for creating new user accounts
 */
export const RegisterPage: FC = () => {
  const { register, isAuthenticated, refreshAccessToken } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const locationState = location.state as {
    from?: { pathname?: string }
    inviteEmail?: string
    inviteCode?: string
    inviteOrgName?: string
  } | null
  const returnUrl = locationState?.from?.pathname || '/'
  const inviteEmail = locationState?.inviteEmail || ''
  const inviteCode = locationState?.inviteCode || ''
  const inviteOrgName = locationState?.inviteOrgName || ''

  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState(inviteEmail)
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Focus tracking
  const [passwordTouched, setPasswordTouched] = useState(false)
  const [confirmPasswordTouched, setConfirmPasswordTouched] = useState(false)

  // Validation errors
  const [displayNameError, setDisplayNameError] = useState('')
  const [emailError, setEmailError] = useState('')
  const [passwordError, setPasswordError] = useState('')
  const [confirmPasswordError, setConfirmPasswordError] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [isFirstUser, setIsFirstUser] = useState(false)

  // Password requirements state
  const passwordReqs = validatePassword(password)
  const passwordIsValid = isPasswordValid(password)

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate(returnUrl, { replace: true })
    }
  }, [isAuthenticated, navigate, returnUrl])

  const validateDisplayName = (value: string): boolean => {
    if (!value.trim()) {
      setDisplayNameError('Display name is required')
      return false
    }
    setDisplayNameError('')
    return true
  }

  const validateEmail = (value: string): boolean => {
    if (!value) {
      setEmailError('Email is required')
      return false
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(value)) {
      setEmailError('Please enter a valid email address')
      return false
    }
    setEmailError('')
    return true
  }

  const validatePasswordField = (value: string): boolean => {
    if (!passwordIsValid && value) {
      setPasswordError('Password does not meet requirements')
      return false
    }
    setPasswordError('')
    return true
  }

  const validateConfirmPassword = (value: string): boolean => {
    if (password && value && password !== value) {
      setConfirmPasswordError('Passwords do not match')
      return false
    }
    setConfirmPasswordError('')
    return true
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    // Validate passwords match
    if (password !== confirmPassword) {
      setConfirmPasswordError('Passwords do not match')
      return
    }

    // Validate all fields
    const isDisplayNameValid = validateDisplayName(displayName)
    const isEmailValid = validateEmail(email)
    const isConfirmPasswordValid = validateConfirmPassword(confirmPassword)

    if (!isDisplayNameValid || !isEmailValid || !passwordIsValid || !isConfirmPasswordValid) {
      return
    }

    setIsSubmitting(true)
    setError(null)
    setFieldErrors({})

    try {
      const result = await register({ email, password, displayName })

      if (result.isSuccess) {
        // Auto-accept invitation if registering from an invite link
        if (inviteCode) {
          try {
            await organizationService.acceptInvitation(inviteCode)
            // Refresh token so JWT picks up the new org context
            await refreshAccessToken()
            toast.success(`Welcome! You've joined ${inviteOrgName || 'the organization'}`)
          } catch {
            // If accept fails (e.g., already used), continue — user can retry from invite page
          }
        }

        // Check if first user (S03)
        if (result.isFirstUser) {
          setIsFirstUser(true)
          // Show admin welcome message before redirecting
          setTimeout(() => navigate('/', { replace: true }), 3000)
        } else {
          navigate('/', { replace: true })
        }
      } else if (result.error) {
        setError(result.error.message)

        // Handle field-specific errors
        if (result.error.validationErrors) {
          const errors: Record<string, string> = {}
          Object.entries(result.error.validationErrors).forEach(([field, messages]) => {
            errors[field] = messages[0] // Take first error message
          })
          setFieldErrors(errors)
        }
      }
    } catch (err: unknown) {
      type AxiosErrorType = {
        response?: { data?: { message?: string; validationErrors?: Record<string, string[]> } }
      }
      const axiosError = err as AxiosErrorType
      if (axiosError.response?.data) {
        const errorResponse = axiosError.response.data
        setError(errorResponse.message || 'Registration failed')

        if (errorResponse.validationErrors) {
          const errors: Record<string, string> = {}
          Object.entries(errorResponse.validationErrors).forEach(([field, messages]) => {
            errors[field] = Array.isArray(messages) ? messages[0] : messages
          })
          setFieldErrors(errors)
        }
      } else {
        setError('Unable to connect to server. Please check your connection.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthLayout title="Create Account">
      <form onSubmit={handleSubmit}>
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          {/* First User Admin Welcome Message (S03) */}
          {isFirstUser && (
            <Alert severity="success">
              Welcome! You are the first user and have been assigned the Administrator role.
              You can manage users and configure the system.
            </Alert>
          )}

          {/* General Error Message */}
          {error && !Object.keys(fieldErrors).length && (
            <Alert severity="error">{error}</Alert>
          )}

          {/* Display Name Field */}
          <CobraTextField
            label="Display Name"
            value={displayName}
            onChange={e => {
              setDisplayName(e.target.value)
              if (displayNameError) validateDisplayName(e.target.value)
            }}
            onBlur={() => validateDisplayName(displayName)}
            error={!!displayNameError}
            helperText={displayNameError || ' '}
            fullWidth
            required
            autoFocus
            autoComplete="name"
          />

          {/* Email Field */}
          <CobraTextField
            label="Email Address"
            type="email"
            value={email}
            onChange={e => {
              setEmail(e.target.value)
              if (emailError) validateEmail(e.target.value)
            }}
            onBlur={() => validateEmail(email)}
            error={!!emailError}
            helperText={emailError || ' '}
            fullWidth
            required
            autoComplete="email"
          />

          {/* Password Field */}
          <Box>
            <CobraTextField
              label="Password"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={e => {
                setPassword(e.target.value)
                // Clear error when user starts typing after blur
                if (passwordError) {
                  setPasswordError('')
                }
              }}
              onBlur={() => {
                setPasswordTouched(true)
                validatePasswordField(password)
              }}
              error={passwordTouched && !!passwordError}
              helperText={passwordTouched && passwordError ? passwordError : ' '}
              fullWidth
              required
              autoComplete="new-password"
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowPassword(!showPassword)}
                      edge="end"
                      aria-label={showPassword ? 'Hide password' : 'Show password'}
                      size="small"
                      tabIndex={-1}
                    >
                      <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} size="sm" />
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
            {/* Show password requirements only after field has been touched */}
            {passwordTouched && password && <PasswordRequirements requirements={passwordReqs} />}
          </Box>

          {/* Confirm Password Field */}
          <CobraTextField
            label="Confirm Password"
            type={showConfirmPassword ? 'text' : 'password'}
            value={confirmPassword}
            onChange={e => {
              setConfirmPassword(e.target.value)
              // Clear error when user starts typing after blur
              if (confirmPasswordError) {
                setConfirmPasswordError('')
              }
            }}
            onBlur={() => {
              setConfirmPasswordTouched(true)
              validateConfirmPassword(confirmPassword)
            }}
            error={confirmPasswordTouched && !!confirmPasswordError}
            helperText={confirmPasswordTouched && confirmPasswordError ? confirmPasswordError : ' '}
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
                    size="small"
                    tabIndex={-1}
                  >
                    <FontAwesomeIcon icon={showConfirmPassword ? faEyeSlash : faEye} size="sm" />
                  </IconButton>
                </InputAdornment>
              ),
            }}
          />

          {/* Create Account Button */}
          <CobraPrimaryButton
            type="submit"
            fullWidth
            disabled={isSubmitting || !passwordIsValid}
          >
            {isSubmitting ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : (
              'Create Account'
            )}
          </CobraPrimaryButton>

          {/* Sign In Link */}
          <Box sx={{ textAlign: 'center', mt: 2 }}>
            <Typography variant="body2" color="text.secondary">
              Already have an account?{' '}
              <Link to="/login" state={location.state} style={{ textDecoration: 'none' }}>
                <Typography
                  component="span"
                  variant="body2"
                  sx={{ color: 'buttonPrimary.main' }}
                >
                  Sign in
                </Typography>
              </Link>
            </Typography>
          </Box>
        </Stack>
      </form>
    </AuthLayout>
  )
}
