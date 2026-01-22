# Authentication Feature

This feature provides authentication and authorization services for Cadence, supporting both local email/password authentication (MVP) and external OAuth providers (future Azure Entra integration).

## Architecture

The authentication system follows a **hybrid provider pattern** where:
- Multiple authentication methods can be enabled simultaneously
- All authentication flows ultimately issue Cadence-owned JWTs
- External providers are used for authentication, but local accounts store user data
- User linking allows the same user to authenticate via multiple methods

## Directory Structure

```
Authentication/
├── Models/
│   └── DTOs/
│       └── AuthDtos.cs           # All DTOs for auth operations
└── Services/
    ├── IAuthenticationService.cs # Main orchestrator interface
    ├── ITokenService.cs          # JWT generation and validation
    ├── IRefreshTokenStore.cs     # Refresh token persistence
    ├── IPasswordResetService.cs  # Password reset flows
    └── IEmailService.cs          # Email notifications (via ACS)
```

## Core Interfaces

### IAuthenticationService
The main orchestrator for all authentication operations:
- `AuthenticateWithPasswordAsync()` - Local email/password login
- `AuthenticateWithExternalAsync()` - OAuth callback handling
- `RegisterAsync()` - New user registration
- `RefreshTokenAsync()` - Token refresh
- `RevokeTokensAsync()` - Logout operations
- `GetUserAsync()` - User information retrieval
- `GetAvailableMethods()` - List enabled auth methods
- `GetExternalLoginUrl()` - OAuth redirect URL generation

### ITokenService
JWT token operations:
- `GenerateAccessToken()` - Create JWT access token
- `ValidateToken()` - Parse and validate JWT
- `GenerateRefreshToken()` - Create random refresh token
- `HashToken()` - SHA256 hash for storage
- `VerifyTokenHash()` - Verify token against hash

### IRefreshTokenStore
Refresh token persistence:
- `CreateAsync()` - Store new refresh token
- `GetByHashAsync()` - Retrieve token by hash
- `RevokeAsync()` - Revoke single token
- `RevokeAllForUserAsync()` - Revoke all user tokens
- `CleanupExpiredTokensAsync()` - Maintenance operation

### IPasswordResetService
Self-service password reset:
- `RequestResetAsync()` - Initiate password reset
- `ValidateTokenAsync()` - Check reset token validity
- `CompleteResetAsync()` - Set new password
- `IsRateLimitedAsync()` - Check rate limiting
- `CleanupExpiredTokensAsync()` - Maintenance operation

### IEmailService
Email notifications via Azure Communication Services:
- `SendPasswordResetEmailAsync()` - Password reset link
- `SendWelcomeEmailAsync()` - New user welcome
- `SendAccountDeactivatedEmailAsync()` - Account deactivation notice
- `SendAccountReactivatedEmailAsync()` - Account reactivation notice

## Key DTOs

### Request DTOs
- `LoginRequest` - Email, password, rememberMe
- `RegistrationRequest` - Email, password, displayName
- `PasswordResetRequest` - Email
- `PasswordResetCompleteRequest` - Token, newPassword
- `RefreshTokenRequest` - (token from cookie)
- `ExternalAuthRequest` - Provider, code, state, returnUrl

### Response DTOs
- `AuthResponse` - Success/failure, tokens, user info, errors
- `AuthError` - Error code, message, validation errors
- `UserInfo` - User details, roles, linked providers
- `AuthMethod` - Available authentication methods
- `TokenClaims` - Parsed JWT claims
- `RefreshTokenInfo` - Stored token metadata
- `PasswordResetValidation` - Token validation result

## Token Strategy

### Access Tokens (JWT)
- **Lifespan:** 15 minutes (configurable)
- **Storage:** Memory only (never localStorage)
- **Claims:** userId (sub), email, displayName (name), role
- **Usage:** Authorization header: `Bearer {token}`

### Refresh Tokens
- **Lifespan:** 4 hours (standard) or 30 days (remember me)
- **Storage:** HttpOnly, Secure, SameSite=Strict cookie
- **Format:** Cryptographically secure random bytes (Base64)
- **Persistence:** SHA256 hash stored in database
- **Security:** Original token never stored, only hash

## Security Features

### Account Lockout
- 5 failed attempts → 15 minute lockout
- Attempt counter resets on successful login
- Clear messaging to user with attempts remaining

### Rate Limiting
- Password reset: 5 requests per 15 minutes per email
- Prevents enumeration and abuse

### Token Security
- Refresh tokens hashed with SHA256 before storage
- IP address and device info logged for audit
- All tokens revoked on password change
- Expired tokens cleaned up by maintenance job

### Email Enumeration Prevention
- Password reset always returns same success message
- No indication whether email exists or not
- Email only sent if account exists and is active

## Authentication Flow Examples

### Password Login
```
1. User submits LoginRequest
2. IAuthenticationService validates credentials
3. If valid:
   - Generate access token (JWT)
   - Create refresh token
   - Store refresh token hash
   - Return AuthResponse with tokens
4. If invalid:
   - Increment failed attempt counter
   - Check for lockout condition
   - Return AuthError with attempts remaining
```

### Registration
```
1. User submits RegistrationRequest
2. IAuthenticationService validates input
3. Check if email already exists
4. Create user account:
   - First user → Administrator role
   - Subsequent users → User role
5. Auto-login:
   - Generate tokens
   - Return AuthResponse with isNewAccount=true
```

### Token Refresh
```
1. Frontend detects token expiring (< 2 min)
2. Sends refresh token (from cookie)
3. ITokenService validates refresh token hash
4. If valid:
   - Generate new access token
   - Return new token
5. If invalid/expired:
   - Return error
   - Frontend redirects to login
```

### Password Reset
```
1. User requests reset with email
2. Generate random token, hash and store
3. Send email with reset link (if account exists)
4. User clicks link with token
5. Validate token (not expired, not used)
6. User sets new password
7. Revoke all sessions
8. Mark token as used
9. Auto-login with new credentials
```

## Configuration

See `appsettings.json` for:
- JWT signing key (use dotnet secrets in dev)
- Token lifespans
- Lockout settings
- Rate limit configuration
- Azure Communication Services connection string

## Implementation Status

**Phase 1: Contracts (Current)**
- [x] DTOs defined
- [x] Service interfaces defined
- [ ] Implementation (Phase 2)
- [ ] Controllers (Phase 2)
- [ ] Tests (Phase 2)

## Related Stories

- S16: Auth Service Interface Design
- S02: Validate and Save User
- S05: JWT Token Issuance
- S06: Failed Login Handling
- S07: Automatic Token Refresh
- S24: Self-Service Password Reset

## Future Enhancements

- Azure Entra SSO (post-MVP)
- Multi-factor authentication
- Email verification on registration
- Session management UI
- Security event logging
- Account linking UI
