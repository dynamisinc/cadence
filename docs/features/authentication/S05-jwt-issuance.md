# S05: JWT Token Issuance

## Story

**As a** user with valid credentials,
**I want** to receive secure tokens upon login,
**So that** I can access protected resources without re-entering my password.

## Context

Upon successful authentication, the backend issues two tokens: a short-lived access token for API requests and a longer-lived refresh token for obtaining new access tokens. This balance provides security (short exposure window) with convenience (infrequent re-authentication).

## Acceptance Criteria

- [ ] **Given** I login with valid credentials, **when** authentication succeeds, **then** I receive an access token valid for 15 minutes
- [ ] **Given** I login with valid credentials, **when** authentication succeeds, **then** I receive a refresh token valid for 4 hours
- [ ] **Given** I login with "Remember me" checked, **when** authentication succeeds, **then** my refresh token is valid for 30 days
- [ ] **Given** I receive tokens, **when** I inspect the access token, **then** it contains my userId, email, displayName, and role claims
- [ ] **Given** I receive tokens, **when** the frontend stores them, **then** access token is in memory only (not localStorage)
- [ ] **Given** I receive tokens, **when** the frontend stores them, **then** refresh token is in HttpOnly cookie
- [ ] **Given** I am authenticated, **when** I make API requests, **then** the access token is sent in Authorization header

## Out of Scope

- Token revocation list (blacklist)
- Multiple device session management
- Token introspection endpoint

## Dependencies

- S04 (Login Form)
- Backend JWT configuration

## Domain Terms

| Term | Definition |
|------|------------|
| Access Token | Short-lived JWT used to authenticate API requests |
| Refresh Token | Longer-lived token used to obtain new access tokens |
| Claims | Data embedded in JWT (userId, role, etc.) |
| Bearer Token | Token type sent in Authorization header |

## API Contract

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "email": "jane@example.com",
  "password": "SecurePass123",
  "rememberMe": false
}
```

**Success Response (200 OK):**
```json
{
  "userId": "guid-here",
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "role": "Controller",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

*Note: Refresh token set via `Set-Cookie` header with `HttpOnly; Secure; SameSite=Strict`*

**JWT Access Token Claims:**
```json
{
  "sub": "user-guid",
  "email": "jane@example.com",
  "name": "Jane Smith",
  "role": "Controller",
  "iat": 1705849200,
  "exp": 1705850100
}
```

## Technical Notes

```csharp
// Token generation pseudo-code
var accessToken = new JwtSecurityToken(
    issuer: _config["Jwt:Issuer"],
    audience: _config["Jwt:Audience"],
    claims: new[] {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("name", user.DisplayName),
        new Claim(ClaimTypes.Role, user.Role)
    },
    expires: DateTime.UtcNow.AddMinutes(15),
    signingCredentials: credentials
);
```

- Use `appsettings.json` for JWT configuration (issuer, audience)
- Use dotnet secrets for signing key (never in source control)
- Consider asymmetric keys for production (RS256)

---

*Story created: 2025-01-21*
