/**
 * CreateUserModal - Modal for creating new users inline
 *
 * Used from the AddParticipantDialog to allow Exercise Directors and Admins
 * to create new system users without leaving the exercise context.
 *
 * New users are always created with "User" system role (Observer equivalent).
 *
 * @module features/users/components
 * @see authentication/S25-inline-user-creation.md
 */

import { useState, useEffect } from 'react'
import type { FC } from 'react'
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Stack,
  TextField,
  InputAdornment,
  IconButton,
  Alert,
  Typography,
  Box,
} from '@mui/material'
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faEye, faEyeSlash, faUserPlus, faCopy, faCheck } from '@fortawesome/free-solid-svg-icons'
import { CobraPrimaryButton, CobraSecondaryButton } from '../../../theme/styledComponents'
import CobraStyles from '../../../theme/CobraStyles'
import { PasswordRequirements } from '../../auth/components/PasswordRequirements'
import { validatePassword, isPasswordValid } from '../../auth/types'
import { userService } from '../services/userService'
import type { UserDto, CreateUserRequest } from '../types'
import { AxiosError } from 'axios'

interface CreateUserModalProps {
  /** Whether the modal is open */
  open: boolean
  /** Called when user closes the modal */
  onClose: () => void
  /** Called when a user is successfully created */
  onUserCreated: (user: UserDto) => void
}

interface FormState {
  displayName: string
  email: string
  password: string
}

interface FormErrors {
  displayName?: string
  email?: string
  password?: string
  general?: string
}

const initialFormState: FormState = {
  displayName: '',
  email: '',
  password: '',
}

/**
 * Email validation regex
 */
const isValidEmail = (email: string): boolean => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
}

export const CreateUserModal: FC<CreateUserModalProps> = ({
  open,
  onClose,
  onUserCreated,
}) => {
  const [form, setForm] = useState<FormState>(initialFormState)
  const [errors, setErrors] = useState<FormErrors>({})
  const [showPassword, setShowPassword] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [createdUser, setCreatedUser] = useState<UserDto | null>(null)
  const [copied, setCopied] = useState(false)
  const [passwordTouched, setPasswordTouched] = useState(false)

  // Reset form when modal opens/closes
  useEffect(() => {
    if (open) {
      setForm(initialFormState)
      setErrors({})
      setShowPassword(false)
      setIsSubmitting(false)
      setCreatedUser(null)
      setCopied(false)
      setPasswordTouched(false)
    }
  }, [open])

  const passwordRequirements = validatePassword(form.password)

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {}

    if (!form.displayName.trim()) {
      newErrors.displayName = 'Display name is required'
    }

    if (!form.email.trim()) {
      newErrors.email = 'Email is required'
    } else if (!isValidEmail(form.email)) {
      newErrors.email = 'Please enter a valid email address'
    }

    if (!form.password) {
      newErrors.password = 'Password is required'
    } else if (!isPasswordValid(form.password)) {
      newErrors.password = 'Password does not meet requirements'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleFieldChange = (field: keyof FormState) => (
    event: React.ChangeEvent<HTMLInputElement>,
  ) => {
    setForm(prev => ({ ...prev, [field]: event.target.value }))
    // Clear field error on change
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: undefined }))
    }
    // Clear general error on any change
    if (errors.general) {
      setErrors(prev => ({ ...prev, general: undefined }))
    }
  }

  const handlePasswordBlur = () => {
    setPasswordTouched(true)
    if (form.password && !isPasswordValid(form.password)) {
      setErrors(prev => ({ ...prev, password: 'Password does not meet requirements' }))
    }
  }

  const handleSubmit = async () => {
    if (!validateForm()) {
      return
    }

    setIsSubmitting(true)
    setErrors({})

    try {
      const request: CreateUserRequest = {
        displayName: form.displayName.trim(),
        email: form.email.trim(),
        password: form.password,
      }

      const user = await userService.createUser(request)
      setCreatedUser(user)
    } catch (error) {
      const axiosError = error as AxiosError<{ error?: string; message?: string }>

      if (axiosError.response?.status === 409) {
        setErrors({ email: 'A user with this email already exists' })
      } else if (axiosError.response?.data?.message) {
        setErrors({ general: axiosError.response.data.message })
      } else {
        setErrors({ general: 'Failed to create user. Please try again.' })
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleCopyPassword = async () => {
    try {
      await navigator.clipboard.writeText(form.password)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard API may not be available
      console.error('Failed to copy password')
    }
  }

  const handleDone = () => {
    if (createdUser) {
      onUserCreated(createdUser)
    }
    onClose()
  }

  const isFormValid =
    form.displayName.trim() !== '' &&
    form.email.trim() !== '' &&
    isValidEmail(form.email) &&
    isPasswordValid(form.password)

  // Success state - show credentials
  if (createdUser) {
    return (
      <Dialog open={open} onClose={handleDone} maxWidth="sm" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <FontAwesomeIcon icon={faCheck} color="success" />
            User Created Successfully
          </Box>
        </DialogTitle>

        <DialogContent>
          <Stack spacing={CobraStyles.Spacing.FormFields}>
            <Alert severity="success" sx={{ mb: 1 }}>
              <strong>{createdUser.displayName}</strong> has been created with{' '}
              <strong>User</strong> role.
            </Alert>

            <Alert severity="warning">
              <Typography variant="body2" sx={{ mb: 1 }}>
                <strong>Important:</strong> Share these credentials securely with the user.
              </Typography>
              <Box sx={{ mt: 1 }}>
                <Typography variant="body2">
                  <strong>Email:</strong> {createdUser.email}
                </Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 0.5 }}>
                  <Typography variant="body2">
                    <strong>Password:</strong>{' '}
                    <code style={{ fontFamily: 'monospace', backgroundColor: '#f5f5f5', padding: '2px 6px', borderRadius: '4px' }}>
                      {showPassword ? form.password : '••••••••'}
                    </code>
                  </Typography>
                  <IconButton
                    size="small"
                    onClick={() => setShowPassword(prev => !prev)}
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                    tabIndex={-1}
                  >
                    <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} size="sm" />
                  </IconButton>
                  <IconButton
                    size="small"
                    onClick={handleCopyPassword}
                    aria-label="Copy password"
                    color={copied ? 'success' : 'default'}
                  >
                    <FontAwesomeIcon icon={copied ? faCheck : faCopy} size="sm" />
                  </IconButton>
                </Box>
              </Box>
            </Alert>

            <Typography variant="body2" color="text.secondary">
              You can now assign this user an exercise role.
            </Typography>
          </Stack>
        </DialogContent>

        <DialogActions>
          <CobraPrimaryButton onClick={handleDone}>
            Done
          </CobraPrimaryButton>
        </DialogActions>
      </Dialog>
    )
  }

  // Form state
  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faUserPlus} />
          Create New User
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={CobraStyles.Spacing.FormFields} sx={{ mt: 1 }}>
          {errors.general && (
            <Alert severity="error">{errors.general}</Alert>
          )}

          <TextField
            label="Display Name"
            value={form.displayName}
            onChange={handleFieldChange('displayName')}
            error={Boolean(errors.displayName)}
            helperText={errors.displayName || ' '}
            required
            fullWidth
            autoFocus
            disabled={isSubmitting}
          />

          <TextField
            label="Email Address"
            type="email"
            value={form.email}
            onChange={handleFieldChange('email')}
            error={Boolean(errors.email)}
            helperText={errors.email || ' '}
            required
            fullWidth
            disabled={isSubmitting}
          />

          <Box>
            <TextField
              label="Password"
              type={showPassword ? 'text' : 'password'}
              value={form.password}
              onChange={handleFieldChange('password')}
              onBlur={handlePasswordBlur}
              error={passwordTouched && Boolean(errors.password)}
              helperText={passwordTouched && errors.password ? errors.password : ' '}
              required
              fullWidth
              disabled={isSubmitting}
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      onClick={() => setShowPassword(prev => !prev)}
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
            {passwordTouched && form.password && (
              <PasswordRequirements requirements={passwordRequirements} />
            )}
          </Box>

          <Alert severity="info" icon={false}>
            <Typography variant="body2">
              This user will be created with <strong>User</strong> system role
              (Observer equivalent). You can assign their exercise role after
              creation.
            </Typography>
          </Alert>
        </Stack>
      </DialogContent>

      <DialogActions>
        <CobraSecondaryButton onClick={onClose} disabled={isSubmitting}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleSubmit}
          disabled={!isFormValid || isSubmitting}
        >
          {isSubmitting ? 'Creating...' : 'Create User'}
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  )
}
