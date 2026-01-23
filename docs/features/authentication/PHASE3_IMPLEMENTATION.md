# Authentication Phase 3: API Integration & Token Management

**Status:** ✅ Complete
**Date:** 2026-01-22
**Implemented by:** frontend-agent

## Overview

Phase 3 completes the authentication feature by implementing full API integration and JWT token management. The implementation follows security best practices with access tokens in memory, refresh tokens in HttpOnly cookies, proactive token refresh, and cross-tab logout synchronization.

## Components Implemented

### 1. Authentication Service (`src/frontend/src/features/auth/services/authService.ts`)

**Purpose:** API client for all authentication operations

**Key Features:**
- Axios instance configured with `withCredentials: true` for HttpOnly cookie support
- All authentication endpoints implemented:
  - `login()` - Email/password authentication
  - `register()` - New account creation
  - `logout()` - Session termination
  - `refreshToken()` - Access token refresh
  - `requestPasswordReset()` - Password reset initiation
  - `completePasswordReset()` - Password reset completion
  - `getAvailableMethods()` - Available auth providers

**API Base URL:** `VITE_API_URL` from environment (defaults to `http://localhost:5071`)

### 2. API Client with Interceptors (`src/frontend/src/core/services/api.ts`)

**Purpose:** Main axios instance with authentication support

**Key Features:**

#### Request Interceptor
- Adds `Authorization: Bearer {token}` header automatically
- Adds correlation ID (`X-Correlation-Id`) for request tracing
- Uses token getter function from AuthProvider

#### Response Interceptor
- Detects 401 Unauthorized responses
- Automatically attempts token refresh
- Retries failed request with new token
- Redirects to login with return URL if refresh fails
- Prevents infinite retry loops with `_retry` flag

#### Configuration Function
- `setAuthInterceptors(tokenGetter, tokenRefresher)` - Called by AuthProvider
- Provides access to current token and refresh capability

### 3. AuthContext (`src/frontend/src/contexts/AuthContext.tsx`)

**Purpose:** Central authentication state management

**Key Features:**

#### Token Management (S05)
- **Access Token**: Stored in React state (memory only - never localStorage)
- **Refresh Token**: Stored in HttpOnly cookie (managed by backend)
- **Token Parsing**: Extracts user info (id, email, displayName, role) from JWT claims

#### Proactive Token Refresh (S07)
- Schedules refresh 2 minutes before expiry
- Uses `setTimeout` with cleanup on unmount
- Prevents API calls from failing due to expired tokens
- Handles refresh failures gracefully

#### Session Recovery (S08)
- Attempts token refresh on mount
- Restores session if valid refresh token exists
- Shows loading state during initial check
- Redirects to login with return URL on expiry

#### Cross-Tab Logout (S09)
- Uses localStorage events for synchronization
- Logs out all tabs when user logs out in one tab
- Prevents stale sessions across browser tabs

#### API Integration
- Configures axios interceptors with token getter and refresher
- Enables automatic 401 retry with token refresh
- Manages correlation between AuthProvider and API client

### 4. Updated Types (`src/frontend/src/features/auth/types/index.ts`)

**Changes:**
- `UserInfo.role` changed from `roles: string[]` to `role: string` (matches backend)
- `UserInfo` expanded with `status`, `lastLoginAt`, `exerciseRoles`, `linkedProviders`
- `AuthResponse` completely rewritten to match backend DTO structure
- `AuthError` updated with `attemptsRemaining`, `lockoutEnd`, `validationErrors`
- `AuthMethod` expanded with `icon`, `isEnabled`, `isExternal` fields

### 5. ProtectedRoute (`src/frontend/src/core/components/ProtectedRoute.tsx`)

**Purpose:** Route wrapper for authenticated pages

**Key Features:**
- Uses `useAuth()` hook for authentication state
- Shows `<Loading />` during initial auth check
- Redirects to `/login` with return URL if unauthenticated
- Optional role checking (Administrator bypasses all checks)
- Preserves location for post-login redirect

## Security Implementation

### Access Token Storage (S05)
✅ **In memory only** (React state)
- Not stored in localStorage (XSS protection)
- Cleared on page refresh (requires session recovery)
- Short-lived (15 minutes default)

### Refresh Token Storage (S05)
✅ **HttpOnly cookie** (managed by backend)
- Not accessible to JavaScript (XSS protection)
- Secure flag in production (HTTPS only)
- SameSite=Strict (CSRF protection)
- Longer-lived (4 hours default, 30 days with Remember Me)

### Token Refresh Strategy (S07)
✅ **Proactive refresh** 2 minutes before expiry
- Prevents API call failures
- Seamless user experience
- Handles refresh failures gracefully

✅ **Reactive refresh** on 401 errors
- Automatic retry after refresh
- Return URL preservation
- Redirect to login if refresh fails

### Session Expiration (S08)
✅ **Graceful handling**
- Return URL stored in sessionStorage
- Query parameter `?expired=true` for messaging
- Clear error messages
- No data loss

### Cross-Tab Synchronization (S09)
✅ **localStorage events**
- Logout propagates to all tabs
- Prevents stale sessions
- Immediate redirect

## API Contract

### Login
```typescript
POST /api/auth/login
Body: { email, password, rememberMe? }
Response: { isSuccess, userId?, accessToken?, expiresIn, ... }
Cookie: refreshToken (HttpOnly, Secure, SameSite=Strict)
```

### Register
```typescript
POST /api/auth/register
Body: { email, displayName, password }
Response: { isSuccess, userId?, accessToken?, isFirstUser?, ... }
Cookie: refreshToken (HttpOnly, Secure, SameSite=Strict)
```

### Refresh Token
```typescript
POST /api/auth/refresh
Cookie: refreshToken (sent automatically)
Response: { isSuccess, accessToken?, expiresIn, ... }
```

### Logout
```typescript
POST /api/auth/logout
Cookie: refreshToken (cleared by server)
Response: 200 OK
```

## Usage Example

### In a Component

```typescript
import { useAuth } from '@/contexts/AuthContext';

function MyComponent() {
  const { user, isAuthenticated, login, logout } = useAuth();

  const handleLogin = async () => {
    const response = await login({
      email: 'user@example.com',
      password: 'SecurePass123',
      rememberMe: true,
    });

    if (response.isSuccess) {
      // User is now logged in, tokens are managed automatically
      navigate('/dashboard');
    } else {
      // Show error message
      toast.error(response.error?.message || 'Login failed');
    }
  };

  return (
    <div>
      {isAuthenticated ? (
        <>
          <p>Welcome, {user?.displayName}!</p>
          <button onClick={logout}>Logout</button>
        </>
      ) : (
        <button onClick={handleLogin}>Login</button>
      )}
    </div>
  );
}
```

### Protected Route

```typescript
import { ProtectedRoute } from '@/core/components/ProtectedRoute';

// In your router
<Route
  path="/admin"
  element={
    <ProtectedRoute requiredRole="Administrator">
      <AdminPage />
    </ProtectedRoute>
  }
/>
```

### Making Authenticated API Calls

```typescript
import { apiClient } from '@/core/services/api';

// Axios automatically adds Authorization header
const response = await apiClient.get('/api/exercises');

// If token expired, axios will:
// 1. Intercept 401 error
// 2. Refresh token automatically
// 3. Retry original request
// 4. Return successful response
```

## Environment Configuration

### `.env` file
```bash
VITE_API_URL=http://localhost:5071
```

### Development
- Backend runs on port 5071 (Azure Functions local)
- Frontend runs on port 5173 (Vite dev server)
- CORS must be configured on backend for `http://localhost:5173`
- Cookies work across different ports with `withCredentials: true`

### Production
- Backend and frontend on same domain (Azure Static Web Apps)
- `VITE_API_URL` can be empty for same-origin requests
- Cookies automatically included (same-site)

## Testing Checklist

### Manual Testing
- [ ] Login with valid credentials
- [ ] Login with invalid credentials shows error
- [ ] Register new account
- [ ] Register with existing email shows error
- [ ] Logout clears session
- [ ] Logout in one tab logs out other tabs
- [ ] Refresh page maintains session
- [ ] Token expires after 15 minutes (or configured time)
- [ ] Expired session redirects to login with return URL
- [ ] API call with expired token auto-refreshes and succeeds
- [ ] Remember Me extends session duration

### Security Testing
- [ ] Access token not in localStorage
- [ ] Refresh token in HttpOnly cookie
- [ ] Cookie has Secure flag in production
- [ ] Cookie has SameSite=Strict
- [ ] Unauthorized API calls redirect to login
- [ ] Cross-tab logout works

## Related Stories

- ✅ S03: Registration Form
- ✅ S04: Login Form
- ✅ S05: JWT Token Issuance
- ✅ S07: Token Refresh
- ✅ S08: Session Expiration Handling
- ✅ S09: Cross-Tab Logout

## Next Steps (Phase 4)

1. **Password Reset Flow**
   - Implement `ForgotPasswordPage.tsx`
   - Implement `ResetPasswordPage.tsx`
   - Wire up authService methods

2. **Error Handling Enhancement**
   - Better error messages in UI
   - Toast notifications for auth errors
   - Validation error display

3. **Session Monitoring**
   - Show session expiry warning
   - Countdown timer before expiry
   - Refresh prompt

4. **Testing**
   - Unit tests for AuthContext
   - Integration tests for auth flows
   - E2E tests for login/logout

## Notes

- Access tokens are JWT format with claims: `sub`, `email`, `name`, `role`, `exp`
- Refresh tokens are opaque strings (hashed on backend)
- Token expiry is checked client-side (proactive) and server-side (reactive)
- Return URL preserved across session expiry for seamless recovery
- Cross-tab logout uses localStorage events (not recommended for sensitive data, but safe for logout signal)

## Files Modified

1. `src/frontend/src/features/auth/types/index.ts` - Type definitions
2. `src/frontend/src/features/auth/services/authService.ts` - API client
3. `src/frontend/src/core/services/api.ts` - Axios interceptors
4. `src/frontend/src/contexts/AuthContext.tsx` - State management
5. `src/frontend/src/core/components/ProtectedRoute.tsx` - Route protection

## Files Created

1. `docs/features/authentication/PHASE3_IMPLEMENTATION.md` - This document

---

**Implementation Status:** ✅ Complete and type-checked
**Breaking Changes:** Yes - `UserInfo.roles` changed to `UserInfo.role`
**Backward Compatible:** No - requires backend authentication API
