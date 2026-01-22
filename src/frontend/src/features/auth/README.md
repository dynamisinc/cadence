# Authentication Feature

UI components for user authentication in Cadence.

## Phase 1 Status: UI SHELLS ONLY

This is **Phase 1** implementation - UI components only with **NO API integration**.

- ✅ All authentication pages implemented
- ✅ Form validation (client-side)
- ✅ Password strength requirements
- ✅ Responsive layouts with COBRA styling
- ❌ API integration (Phase 3)
- ❌ Token management (Phase 3)
- ❌ Session persistence (Phase 3)

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

## Documentation References

- [S01: Registration Form](../../../../docs/features/authentication/S01-registration-form.md)
- [S04: Login Form](../../../../docs/features/authentication/S04-login-form.md)
- [S24: Password Reset](../../../../docs/features/authentication/S24-password-reset.md)
- [COBRA Styling](../../../docs/COBRA_STYLING.md)
- [Coding Standards](../../../docs/CODING_STANDARDS.md)
