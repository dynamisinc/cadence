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
import { FC, useState, FormEvent, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Stack,
  IconButton,
  InputAdornment,
  Typography,
  Box,
  Alert,
} from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faEye, faEyeSlash, faSpinner } from '@fortawesome/free-solid-svg-icons';
import { CobraPrimaryButton, CobraTextField } from '../../../theme/styledComponents';
import CobraStyles from '../../../theme/CobraStyles';
import { AuthLayout } from '../components/AuthLayout';
import { PasswordRequirements } from '../components/PasswordRequirements';
import { useAuth } from '../../../contexts/AuthContext';
import { validatePassword, isPasswordValid } from '../types';

/**
 * Registration page for creating new user accounts
 */
export const RegisterPage: FC = () => {
  const { register, isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Validation errors
  const [displayNameError, setDisplayNameError] = useState('');
  const [emailError, setEmailError] = useState('');
  const [confirmPasswordError, setConfirmPasswordError] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isFirstUser, setIsFirstUser] = useState(false);

  // Password requirements state
  const passwordReqs = validatePassword(password);
  const passwordIsValid = isPasswordValid(password);

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate('/', { replace: true });
    }
  }, [isAuthenticated, navigate]);

  const validateDisplayName = (value: string): boolean => {
    if (!value.trim()) {
      setDisplayNameError('Display name is required');
      return false;
    }
    setDisplayNameError('');
    return true;
  };

  const validateEmail = (value: string): boolean => {
    if (!value) {
      setEmailError('Email is required');
      return false;
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(value)) {
      setEmailError('Please enter a valid email address');
      return false;
    }
    setEmailError('');
    return true;
  };

  const validateConfirmPassword = (value: string): boolean => {
    if (password && value && password !== value) {
      setConfirmPasswordError('Passwords do not match');
      return false;
    }
    setConfirmPasswordError('');
    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    // Validate passwords match
    if (password !== confirmPassword) {
      setConfirmPasswordError('Passwords do not match');
      return;
    }

    // Validate all fields
    const isDisplayNameValid = validateDisplayName(displayName);
    const isEmailValid = validateEmail(email);
    const isConfirmPasswordValid = validateConfirmPassword(confirmPassword);

    if (!isDisplayNameValid || !isEmailValid || !passwordIsValid || !isConfirmPasswordValid) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setFieldErrors({});

    try {
      const result = await register({ email, password, displayName });

      if (result.isSuccess) {
        // Check if first user (S03)
        if (result.isFirstUser) {
          setIsFirstUser(true);
          // Show admin welcome message before redirecting
          setTimeout(() => navigate('/', { replace: true }), 3000);
        } else {
          navigate('/', { replace: true });
        }
      } else if (result.error) {
        setError(result.error.message);

        // Handle field-specific errors
        if (result.error.validationErrors) {
          const errors: Record<string, string> = {};
          Object.entries(result.error.validationErrors).forEach(([field, messages]) => {
            errors[field] = messages[0]; // Take first error message
          });
          setFieldErrors(errors);
        }
      }
    } catch (err: any) {
      if (err.response?.data) {
        const errorResponse = err.response.data;
        setError(errorResponse.message || 'Registration failed');

        if (errorResponse.validationErrors) {
          const errors: Record<string, string> = {};
          Object.entries(errorResponse.validationErrors).forEach(([field, messages]: [string, any]) => {
            errors[field] = Array.isArray(messages) ? messages[0] : messages;
          });
          setFieldErrors(errors);
        }
      } else {
        setError('Unable to connect to server. Please check your connection.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

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
            onChange={(e) => {
              setDisplayName(e.target.value);
              if (displayNameError) validateDisplayName(e.target.value);
            }}
            onBlur={() => validateDisplayName(displayName)}
            error={!!displayNameError}
            helperText={displayNameError}
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
            onChange={(e) => {
              setEmail(e.target.value);
              if (emailError) validateEmail(e.target.value);
            }}
            onBlur={() => validateEmail(email)}
            error={!!emailError}
            helperText={emailError}
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
              onChange={(e) => setPassword(e.target.value)}
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
                    >
                      <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} />
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
            {/* Show password requirements when user starts typing */}
            {password && <PasswordRequirements requirements={passwordReqs} />}
          </Box>

          {/* Confirm Password Field */}
          <CobraTextField
            label="Confirm Password"
            type={showConfirmPassword ? 'text' : 'password'}
            value={confirmPassword}
            onChange={(e) => {
              setConfirmPassword(e.target.value);
              if (confirmPasswordError) validateConfirmPassword(e.target.value);
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
              <Link to="/login" style={{ textDecoration: 'none' }}>
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
  );
};
