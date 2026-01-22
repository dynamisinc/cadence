/**
 * ForgotPasswordPage - Password reset request
 *
 * Features:
 * - Email input for password reset
 * - Success state with instructions
 * - Back to sign in link
 * - Generic success message (prevents email enumeration)
 *
 * @module features/auth
 * @see authentication/S24-password-reset.md
 */
import { FC, useState, FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Stack,
  Typography,
  Box,
  Alert,
} from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSpinner, faEnvelope, faArrowLeft } from '@fortawesome/free-solid-svg-icons';
import { CobraPrimaryButton, CobraLinkButton, CobraTextField } from '../../../theme/styledComponents';
import CobraStyles from '../../../theme/CobraStyles';
import { AuthLayout } from '../components/AuthLayout';
import { authService } from '../services/authService';

/**
 * Password reset request page
 */
export const ForgotPasswordPage: FC = () => {
  const [email, setEmail] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [emailError, setEmailError] = useState('');
  const [successState, setSuccessState] = useState(false);

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

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!validateEmail(email)) {
      return;
    }

    setIsSubmitting(true);

    try {
      // Always show success message to prevent email enumeration (S24)
      await authService.requestPasswordReset(email);
      setSuccessState(true);
    } catch (error) {
      // Even on error, show success message to prevent enumeration (S24)
      console.error('Password reset request failed:', error);
      setSuccessState(true);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRequestAnother = () => {
    setSuccessState(false);
    setEmail('');
  };

  if (successState) {
    return (
      <AuthLayout title="Check Your Email">
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          <Box sx={{ textAlign: 'center', py: 2 }}>
            <FontAwesomeIcon
              icon={faEnvelope}
              style={{ fontSize: 48, color: '#0020c2', marginBottom: 16 }}
            />
            <Typography variant="body1" paragraph>
              If an account exists for <strong>{email}</strong>, we've sent a password reset link.
            </Typography>
            <Typography variant="body2" color="text.secondary" paragraph>
              The link will expire in 1 hour.
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Didn't receive the email? Check your spam folder or{' '}
              <Typography
                component="span"
                variant="body2"
                sx={{ color: 'buttonPrimary.main', cursor: 'pointer' }}
                onClick={handleRequestAnother}
              >
                request another link
              </Typography>
              .
            </Typography>
          </Box>

          <Link to="/login" style={{ textDecoration: 'none' }}>
            <CobraLinkButton fullWidth startIcon={<FontAwesomeIcon icon={faArrowLeft} />}>
              Back to Sign In
            </CobraLinkButton>
          </Link>
        </Stack>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Reset Password">
      <form onSubmit={handleSubmit}>
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Enter your email address and we'll send you a link to reset your password.
          </Typography>

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
            autoFocus
            autoComplete="email"
          />

          {/* Send Reset Link Button */}
          <CobraPrimaryButton
            type="submit"
            fullWidth
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : (
              'Send Reset Link'
            )}
          </CobraPrimaryButton>

          {/* Back to Sign In */}
          <Link to="/login" style={{ textDecoration: 'none' }}>
            <CobraLinkButton fullWidth startIcon={<FontAwesomeIcon icon={faArrowLeft} />}>
              Back to Sign In
            </CobraLinkButton>
          </Link>
        </Stack>
      </form>
    </AuthLayout>
  );
};
