/**
 * Authentication Feature - Public Exports
 *
 * Barrel file for authentication feature exports
 */

// Pages
export { LoginPage } from './pages/LoginPage';
export { RegisterPage } from './pages/RegisterPage';
export { ForgotPasswordPage } from './pages/ForgotPasswordPage';
export { ResetPasswordPage } from './pages/ResetPasswordPage';

// Components
export { AuthLayout } from './components/AuthLayout';
export { PasswordRequirements } from './components/PasswordRequirements';

// Services
export { authService } from './services/authService';

// Types
export type {
  LoginRequest,
  RegistrationRequest,
  UserInfo,
  AuthResponse,
  AuthError,
  AuthMethod,
  PasswordResetRequest,
  CompletePasswordResetRequest,
  PasswordRequirements,
} from './types';

export { validatePassword, isPasswordValid } from './types';
