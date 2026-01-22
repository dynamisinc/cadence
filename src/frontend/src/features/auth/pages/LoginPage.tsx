/**
 * LoginPage - User authentication entry point
 *
 * Supports:
 * - Email/password login (local Identity)
 * - "Remember me" option
 * - Password visibility toggle
 * - External providers (Microsoft SSO) when enabled
 * - Offline mode indicator
 * - Account lockout after failed attempts
 * - Session expired handling
 *
 * @module features/auth
 * @see authentication/S04-login-form.md
 * @see authentication/S06-failed-login-handling.md
 */
import { FC, useState, FormEvent, useEffect } from 'react';
import { Link, useNavigate, useLocation, useSearchParams } from 'react-router-dom';
import {
  Stack,
  Checkbox,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Divider,
  Typography,
  Box,
  Alert,
} from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faEye, faEyeSlash, faSpinner } from '@fortawesome/free-solid-svg-icons';
import { CobraPrimaryButton, CobraSecondaryButton, CobraTextField } from '../../../theme/styledComponents';
import CobraStyles from '../../../theme/CobraStyles';
import { AuthLayout } from '../components/AuthLayout';
import { useAuth } from '../../../contexts/AuthContext';
import { authService } from '../services/authService';
import type { AuthMethod } from '../types';

/**
 * Login page with email/password and optional external providers
 */
export const LoginPage: FC = () => {
  const { login, isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [emailError, setEmailError] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [authMethods, setAuthMethods] = useState<AuthMethod[]>([]);
  const [attemptsRemaining, setAttemptsRemaining] = useState<number | null>(null);
  const [lockoutEnd, setLockoutEnd] = useState<Date | null>(null);
  const [isOffline] = useState(!navigator.onLine);

  // Check for session expired message
  const sessionExpired = searchParams.get('expired') === 'true';

  // Redirect if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      const returnUrl = sessionStorage.getItem('returnUrl') || '/';
      sessionStorage.removeItem('returnUrl');
      navigate(returnUrl, { replace: true });
    }
  }, [isAuthenticated, navigate]);

  // Load available auth methods on mount
  useEffect(() => {
    const loadAuthMethods = async () => {
      try {
        const methods = await authService.getAvailableMethods();
        setAuthMethods(methods);
      } catch {
        // For Phase 1, fall back to Identity only
        setAuthMethods([
          { provider: 'Identity', displayName: 'Email/Password', isEnabled: true, isExternal: false },
        ]);
      }
    };
    loadAuthMethods();
  }, []);

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

    if (!password) {
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setAttemptsRemaining(null);
    setLockoutEnd(null);

    try {
      const result = await login({ email, password, rememberMe });

      if (result.isSuccess) {
        // Success - navigation handled by useEffect
        const returnUrl = (location.state as any)?.from?.pathname || '/';
        navigate(returnUrl, { replace: true });
      } else if (result.error) {
        // Handle error response
        setError(result.error.message);

        // Handle attempts remaining (S06)
        if (result.error.attemptsRemaining !== undefined) {
          setAttemptsRemaining(result.error.attemptsRemaining);
        }

        // Handle account lockout (S06)
        if (result.error.code === 'account_locked' && result.error.lockoutEnd) {
          setLockoutEnd(new Date(result.error.lockoutEnd));
        }

        // Clear password on failure (S06)
        setPassword('');
      }
    } catch (err: any) {
      // Handle network/unexpected errors
      if (err.response?.data) {
        const errorResponse = err.response.data;
        setError(errorResponse.message || 'Login failed');

        if (errorResponse.attemptsRemaining !== undefined) {
          setAttemptsRemaining(errorResponse.attemptsRemaining);
        }

        if (errorResponse.code === 'account_locked' && errorResponse.lockoutEnd) {
          setLockoutEnd(new Date(errorResponse.lockoutEnd));
        }
      } else if (!navigator.onLine) {
        setError('You\'re offline. Please check your connection.');
      } else {
        setError('Unable to connect to server. Please check your connection.');
      }

      // Clear password on failure (S06)
      setPassword('');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleExternalLogin = (provider: string) => {
    if (isOffline) {
      setError('External sign-in requires internet connection');
      return;
    }

    // Phase 3: Redirect to external provider OAuth flow
    window.location.href = `/api/auth/external/${provider}`;
  };

  const externalMethods = authMethods.filter(m => m.isExternal && m.isEnabled);

  return (
    <AuthLayout title="Sign In" showOfflineIndicator={isOffline}>
      <form onSubmit={handleSubmit}>
        <Stack spacing={CobraStyles.Spacing.FormFields}>
          {/* Session Expired Message */}
          {sessionExpired && (
            <Alert severity="info">
              Your session has expired. Please sign in again.
            </Alert>
          )}

          {/* General Error Message */}
          {error && !attemptsRemaining && !lockoutEnd && (
            <Alert severity="error">{error}</Alert>
          )}

          {/* Attempts Remaining Warning (S06) */}
          {attemptsRemaining !== null && attemptsRemaining > 0 && attemptsRemaining <= 3 && (
            <Alert severity="warning">
              {attemptsRemaining} {attemptsRemaining === 1 ? 'attempt' : 'attempts'} remaining before lockout
            </Alert>
          )}

          {/* Account Locked Message (S06) */}
          {lockoutEnd && (
            <Alert severity="error">
              Account locked. Try again at {lockoutEnd.toLocaleTimeString()}.
            </Alert>
          )}

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

          {/* Password Field */}
          <CobraTextField
            label="Password"
            type={showPassword ? 'text' : 'password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            fullWidth
            required
            autoComplete="current-password"
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

          {/* Remember Me Checkbox */}
          <FormControlLabel
            control={
              <Checkbox
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                size="small"
              />
            }
            label="Remember me"
          />

          {/* Sign In Button */}
          <CobraPrimaryButton
            type="submit"
            fullWidth
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <FontAwesomeIcon icon={faSpinner} spin />
            ) : (
              'Sign In'
            )}
          </CobraPrimaryButton>

          {/* Forgot Password Link */}
          <Box sx={{ textAlign: 'center' }}>
            <Link to="/forgot-password" style={{ textDecoration: 'none' }}>
              <Typography variant="body2" sx={{ color: 'buttonPrimary.main' }}>
                Forgot your password?
              </Typography>
            </Link>
          </Box>

          {/* External Providers */}
          {externalMethods.length > 0 && (
            <>
              <Divider sx={{ my: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  OR
                </Typography>
              </Divider>

              <Stack spacing={CobraStyles.Spacing.FormFields}>
                {externalMethods.map((method) => (
                  <CobraSecondaryButton
                    key={method.provider}
                    onClick={() => handleExternalLogin(method.provider)}
                    fullWidth
                    disabled={isOffline}
                  >
                    {method.displayName}
                  </CobraSecondaryButton>
                ))}
              </Stack>
            </>
          )}

          {/* Create Account Link */}
          <Box sx={{ textAlign: 'center', mt: 2 }}>
            <Typography variant="body2" color="text.secondary">
              Don't have an account?{' '}
              <Link to="/register" style={{ textDecoration: 'none' }}>
                <Typography
                  component="span"
                  variant="body2"
                  sx={{ color: 'buttonPrimary.main' }}
                >
                  Create one
                </Typography>
              </Link>
            </Typography>
          </Box>
        </Stack>
      </form>
    </AuthLayout>
  );
};
