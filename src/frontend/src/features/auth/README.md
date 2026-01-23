# Authentication & Authorization Feature

Complete authentication and role-based authorization system for Cadence.

## Implementation Status

- ✅ All authentication pages implemented (S01-S12)
- ✅ JWT token management with refresh (S05-S08)
- ✅ API integration complete
- ✅ User management (S10-S12)
- ✅ Global role assignment (S13)
- ✅ Exercise role assignment (S14)
- ✅ **Role inheritance & resolution (S15) - NEW** ✨
- ✅ Password reset flow (S24)
- ✅ External auth providers ready (S17-S23)

## Components

### Pages

| Page | Route | Description |
|------|-------|-------------|
| `LoginPage` | `/auth/login` | Email/password sign-in with optional external providers |
| `RegisterPage` | `/auth/register` | New user account creation |
| `ForgotPasswordPage` | `/auth/forgot-password` | Password reset request |
| `ResetPasswordPage` | `/auth/reset-password` | Complete password reset with token |

### Shared Components

| Component | Purpose |
|-----------|---------|
| `AuthLayout` | Centered card layout for all auth pages |
| `PasswordRequirements` | Visual password strength indicator |

## Features

### Login Page (S04)

- Email/password authentication
- "Remember me" checkbox
- Password visibility toggle
- Forgot password link
- Register link
- Dynamic external provider buttons (Microsoft SSO when enabled)
- Offline indicator

### Registration Page (S01)

- Display Name, Email, Password, Confirm Password fields
- Real-time password requirements feedback:
  - Min 8 characters
  - At least 1 uppercase letter
  - At least 1 number
- Inline validation errors
- Password visibility toggles
- Loading state during submission

### Forgot Password Page (S24)

- Email input for password reset
- Success state with instructions
- Generic success message (prevents email enumeration)
- "Request another link" option
- Back to sign-in link

### Reset Password Page (S24)

- New password with strength validation
- Confirm password matching
- Token validation from URL parameter
- Expired/invalid token error handling
- Password requirements checklist

## Usage

### Routing Setup

```tsx
import { LoginPage, RegisterPage, ForgotPasswordPage, ResetPasswordPage } from '@/features/auth';

<Routes>
  <Route path="/auth/login" element={<LoginPage />} />
  <Route path="/auth/register" element={<RegisterPage />} />
  <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
  <Route path="/auth/reset-password" element={<ResetPasswordPage />} />
</Routes>
```

### Authentication Context

```tsx
import { AuthProvider, useAuth } from '@/contexts/AuthContext';

// Wrap app with AuthProvider
<AuthProvider>
  <App />
</AuthProvider>

// Use in components
const { user, isAuthenticated, login, logout } = useAuth();
```

## Type Safety

All forms use TypeScript types matching backend DTOs:

```typescript
import type { LoginRequest, RegistrationRequest, UserInfo } from '@/features/auth';
```

## Validation

Client-side validation includes:

- Email format (regex)
- Password requirements (min 8 chars, 1 uppercase, 1 number)
- Password confirmation matching
- Required field checks

## Phase 3 Integration Points

When implementing API integration in Phase 3:

1. **authService.ts** - Replace `throw new Error()` with actual API calls
2. **AuthContext.tsx** - Implement token storage and session management
3. **Pages** - Replace `alert()` with proper error handling and navigation

## COBRA Styling

All components use COBRA styled components:

- `CobraPrimaryButton` - Primary actions
- `CobraSecondaryButton` - External provider buttons
- `CobraLinkButton` - Cancel/back actions
- `CobraTextField` - All text inputs
- `CobraStyles.Spacing.FormFields` - Consistent spacing

## Accessibility

All forms include:

- Proper `aria-label` attributes
- Keyboard navigation support
- Focus management (autofocus on first field)
- Screen reader friendly markup
- Semantic HTML elements

## Testing

Phase 2 will include:

- Component rendering tests
- Form validation tests
- User interaction tests
- Accessibility tests

---

## 🆕 Role Inheritance & Resolution (S15)

Exercise roles override system roles, providing fine-grained access control per exercise.

### New Components

#### EffectiveRoleBadge

Displays user's effective role in an exercise with color coding and explanatory tooltip.

```tsx
import { EffectiveRoleBadge } from '@/features/auth';

// Show user's role in exercise header
<EffectiveRoleBadge exerciseId={exercise.id} showOverride />
```

**Color Coding:**
- 🔴 Red: Administrator, Exercise Director
- 🔵 Blue: Controller
- 🟢 Green: Evaluator
- ⚪ Gray: Observer

#### PermissionGate

Conditional rendering based on user permissions.

```tsx
import { PermissionGate } from '@/features/auth';

// Only show to users who can fire injects
<PermissionGate exerciseId={exerciseId} action="fire_inject">
  <FireInjectButton inject={inject} />
</PermissionGate>

// With fallback message
<PermissionGate
  exerciseId={exerciseId}
  action="manage_participants"
  fallback={<Alert>Requires Director role</Alert>}
>
  <ParticipantManager />
</PermissionGate>
```

#### RoleExplanationTooltip

Shows role hierarchy and permissions on hover.

```tsx
import { RoleExplanationTooltip } from '@/features/auth';

<RoleExplanationTooltip exerciseId={exerciseId} showPermissions>
  <InfoIcon />
</RoleExplanationTooltip>
```

### New Hooks

#### useExerciseRole

Determines user's effective role and permissions in an exercise.

```tsx
import { useExerciseRole } from '@/features/auth';

function InjectControls({ exerciseId, inject }) {
  const { effectiveRole, can, isLoading } = useExerciseRole(exerciseId);

  if (isLoading) return <Skeleton />;

  return (
    <>
      {can('view_exercise') && <ViewButton />}
      {can('fire_inject') && <FireButton inject={inject} />}
      {can('edit_inject') && <EditButton inject={inject} />}
    </>
  );
}
```

### Permission Types

```typescript
type Permission =
  | 'view_exercise'
  | 'add_observation'
  | 'fire_inject'
  | 'edit_inject'
  | 'manage_participants'
  | 'edit_exercise'
  | 'delete_exercise'
  | 'start_clock'
  | 'pause_clock';
```

### Role Hierarchy

```
Observer (level 1)
  ↓ inherits + adds
Evaluator (level 2)
  ↓ inherits + adds
Controller (level 3)
  ↓ inherits + adds
Exercise Director (level 4)
  ↓ inherits + adds
Administrator (level 5)
```

### System Role Mapping

When a user has no exercise-specific role, their system role maps to:
- **Admin** → Administrator
- **Manager** → Exercise Director
- **User** → Observer

### Full Integration Example

```tsx
import {
  EffectiveRoleBadge,
  PermissionGate,
  useExerciseRole
} from '@/features/auth';

function ExercisePage({ exerciseId }) {
  const { effectiveRole, can } = useExerciseRole(exerciseId);

  return (
    <Box>
      {/* Header with role badge */}
      <Stack direction="row" justifyContent="space-between">
        <Typography variant="h4">Exercise</Typography>
        <EffectiveRoleBadge exerciseId={exerciseId} showOverride />
      </Stack>

      {/* Role-based content */}
      <PermissionGate exerciseId={exerciseId} action="fire_inject">
        <InjectControls />
      </PermissionGate>

      <PermissionGate
        exerciseId={exerciseId}
        action="edit_exercise"
        fallback={<Alert>Settings require Director role</Alert>}
      >
        <ExerciseSettings />
      </PermissionGate>
    </Box>
  );
}
```

---

## Documentation References

- [S01: Registration Form](../../../../docs/features/authentication/S01-registration-form.md)
- [S04: Login Form](../../../../docs/features/authentication/S04-login-form.md)
- [S15: Role Inheritance](../../../../docs/features/authentication/S15-role-inheritance.md)
- [S24: Password Reset](../../../../docs/features/authentication/S24-password-reset.md)
- [COBRA Styling](../../../docs/COBRA_STYLING.md)
- [Coding Standards](../../../docs/CODING_STANDARDS.md)
