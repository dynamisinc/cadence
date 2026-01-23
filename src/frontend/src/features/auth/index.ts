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
export { EffectiveRoleBadge } from './components/EffectiveRoleBadge';
export { PermissionGate } from './components/PermissionGate';
export { RoleExplanationTooltip } from './components/RoleExplanationTooltip';

// Hooks
export { useExerciseRole } from './hooks/useExerciseRole';

// Services
export { authService } from './services/authService';
export { roleResolutionService } from './services/roleResolutionService';

// Utilities
export { hasPermission, getRoleDisplayName, getRoleDescription, getRoleColor } from './utils/permissions';

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

export type { ExerciseRole, SystemRole, Permission } from './constants/rolePermissions';
export type { ExerciseParticipantDto, ExerciseAssignmentDto } from './services/roleResolutionService';
