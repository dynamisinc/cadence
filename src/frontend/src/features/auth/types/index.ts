/**
 * Authentication Types
 *
 * TypeScript types matching backend DTOs for authentication feature
 */

/**
 * Login request DTO
 */
export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

/**
 * Registration request DTO
 */
export interface RegistrationRequest {
  email: string;
  displayName: string;
  password: string;
}

/**
 * User information returned after successful authentication
 * Matches backend UserInfo DTO
 */
export interface UserInfo {
  id: string;
  email: string;
  displayName: string;
  role: string;
  status: string;
  lastLoginAt?: string;
  exerciseRoles?: Record<string, string>;
  linkedProviders?: string[];
}

/**
 * Authentication response with JWT tokens
 * Matches backend AuthResponse DTO
 */
export interface AuthResponse {
  isSuccess: boolean;
  userId?: string;
  displayName?: string;
  email?: string;
  role?: string;
  accessToken?: string;
  refreshToken?: string;
  expiresIn: number;
  tokenType: string;
  status?: string;
  isNewAccount?: boolean;
  isFirstUser?: boolean;
  error?: AuthError;
}

/**
 * Authentication error response
 * Matches backend AuthError DTO
 */
export interface AuthError {
  code: string;
  message: string;
  attemptsRemaining?: number;
  lockoutEnd?: string;
  validationErrors?: Record<string, string[]>;
}

/**
 * Available authentication method
 * Matches backend AuthMethod DTO
 */
export interface AuthMethod {
  provider: string;
  displayName: string;
  icon?: string;
  isEnabled: boolean;
  isExternal: boolean;
}

/**
 * Password reset request DTO
 */
export interface PasswordResetRequest {
  email: string;
}

/**
 * Complete password reset DTO
 */
export interface CompletePasswordResetRequest {
  token: string;
  newPassword: string;
}

/**
 * Password requirements validation
 */
export interface PasswordRequirements {
  minLength: boolean;
  hasUppercase: boolean;
  hasNumber: boolean;
}

/**
 * Validate password against requirements
 */
export const validatePassword = (password: string): PasswordRequirements => {
  return {
    minLength: password.length >= 8,
    hasUppercase: /[A-Z]/.test(password),
    hasNumber: /[0-9]/.test(password),
  };
};

/**
 * Check if password meets all requirements
 */
export const isPasswordValid = (password: string): boolean => {
  const requirements = validatePassword(password);
  return requirements.minLength && requirements.hasUppercase && requirements.hasNumber;
};
