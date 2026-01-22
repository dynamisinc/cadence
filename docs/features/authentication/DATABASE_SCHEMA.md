# Authentication Database Schema

## Overview

This document describes the database schema for authentication in Cadence. The schema implements ASP.NET Core Identity with custom extensions for refresh tokens, password reset, and external login providers.

**Migration**: `20260122140042_AddAuthenticationEntities`

## Entities

### 1. ApplicationUser

Extends `IdentityUser` from ASP.NET Core Identity. Represents an authenticated user.

**Table**: `AspNetUsers`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | string(450) | PK | User unique identifier |
| DisplayName | nvarchar(200) | NOT NULL | Display name shown in UI |
| GlobalRole | nvarchar(50) | NOT NULL | System-wide role (Administrator, Controller, Evaluator, Observer) |
| Status | nvarchar(20) | NOT NULL | Account status (Active, Deactivated) |
| LastLoginAt | datetime2 | NULL | UTC timestamp of most recent login |
| OrganizationId | uniqueidentifier | NOT NULL, FK | Primary organization |
| UserName | nvarchar(256) | NULL | Username (typically email) |
| NormalizedUserName | nvarchar(256) | NULL, UNIQUE | Uppercase username for lookups |
| Email | nvarchar(256) | NULL | Email address |
| NormalizedEmail | nvarchar(256) | NULL | Uppercase email for lookups |
| EmailConfirmed | bit | NOT NULL | Email verification status |
| PasswordHash | nvarchar(MAX) | NULL | Hashed password |
| SecurityStamp | nvarchar(MAX) | NULL | Security stamp for invalidation |
| ConcurrencyStamp | nvarchar(MAX) | NULL | Concurrency token |
| PhoneNumber | nvarchar(MAX) | NULL | Phone number |
| PhoneNumberConfirmed | bit | NOT NULL | Phone verification status |
| TwoFactorEnabled | bit | NOT NULL | 2FA enabled flag |
| LockoutEnd | datetimeoffset | NULL | Lockout expiration |
| LockoutEnabled | bit | NOT NULL | Lockout feature enabled |
| AccessFailedCount | int | NOT NULL | Failed login attempts |

**Indexes**:
- `IX_AspNetUsers_Status` - Status filtering
- `IX_AspNetUsers_OrganizationId` - Organization lookups
- `UserNameIndex` - Unique normalized username
- `EmailIndex` - Email lookups

**Relationships**:
- `Organization` (Many-to-One via OrganizationId, RESTRICT)
- `RefreshTokens` (One-to-Many)
- `PasswordResetTokens` (One-to-Many)
- `ExternalLogins` (One-to-Many)

**Default Role**: New users receive "Observer" role unless they are the first user (Administrator).

---

### 2. RefreshToken

Stores hashed refresh tokens for JWT authentication.

**Table**: `RefreshTokens`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Token unique identifier |
| UserId | nvarchar(450) | NOT NULL, FK | User this token belongs to |
| TokenHash | nvarchar(256) | NOT NULL | SHA256 hash of the actual token |
| ExpiresAt | datetime2 | NOT NULL | UTC expiration time (4 hours or 30 days) |
| IsRevoked | bit | NOT NULL | Revoked before expiration flag |
| RevokedAt | datetime2 | NULL | UTC timestamp when revoked |
| RememberMe | bit | NOT NULL | Remember Me selection (affects expiration) |
| CreatedByIp | nvarchar(50) | NULL | IP address at creation |
| DeviceInfo | nvarchar(200) | NULL | Browser/device information |
| CreatedAt | datetime2 | NOT NULL | UTC timestamp (auto-set) |
| UpdatedAt | datetime2 | NOT NULL | UTC timestamp (auto-set) |

**Indexes**:
- `IX_RefreshTokens_UserId` - User lookups
- `IX_RefreshTokens_TokenHash` - Token validation
- `IX_RefreshTokens_ExpiresAt` - Cleanup expired tokens
- `IX_RefreshTokens_UserId_IsRevoked` - Active token queries

**Relationships**:
- `ApplicationUser` (Many-to-One via UserId, CASCADE)

**Security Notes**:
- Only SHA256 hash is stored; actual token is sent to client once
- Tokens are single-use (replaced on refresh)
- Expiration: 4 hours (default) or 30 days (RememberMe)

---

### 3. PasswordResetToken

Stores hashed password reset tokens for self-service recovery.

**Table**: `PasswordResetTokens`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Token unique identifier |
| UserId | nvarchar(450) | NOT NULL, FK | User this token belongs to |
| TokenHash | nvarchar(256) | NOT NULL | SHA256 hash of the actual token |
| ExpiresAt | datetime2 | NOT NULL | UTC expiration time (1 hour) |
| UsedAt | datetime2 | NULL | UTC timestamp when token was used |
| IpAddress | nvarchar(50) | NULL | IP address where reset was requested |
| CreatedAt | datetime2 | NOT NULL | UTC timestamp (auto-set) |
| UpdatedAt | datetime2 | NOT NULL | UTC timestamp (auto-set) |

**Indexes**:
- `IX_PasswordResetTokens_UserId` - User lookups
- `IX_PasswordResetTokens_TokenHash` - Token validation
- `IX_PasswordResetTokens_ExpiresAt` - Cleanup expired tokens
- `IX_PasswordResetTokens_UserId_UsedAt` - Single-use enforcement

**Relationships**:
- `ApplicationUser` (Many-to-One via UserId, CASCADE)

**Security Notes**:
- Only SHA256 hash is stored; actual token is sent in email link
- Tokens are single-use (UsedAt != NULL after use)
- Expiration: 1 hour per HSEEP security requirements
- Rate limit: 5 requests per 15 minutes (enforced in service layer)

---

### 4. ExternalLogin

Links external SSO providers (e.g., Entra, Google) to Cadence users.

**Table**: `ExternalLogins`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PK | Login unique identifier |
| UserId | nvarchar(450) | NOT NULL, FK | Cadence user this login is linked to |
| Provider | nvarchar(50) | NOT NULL | Provider name (e.g., "Entra", "Google") |
| ProviderUserId | nvarchar(200) | NOT NULL | User's ID in external provider system |
| LinkedAt | datetime2 | NOT NULL | UTC timestamp when linked |
| CreatedAt | datetime2 | NOT NULL | UTC timestamp (auto-set) |
| UpdatedAt | datetime2 | NOT NULL | UTC timestamp (auto-set) |

**Indexes**:
- `IX_ExternalLogins_UserId` - User lookups
- `IX_ExternalLogins_Provider_ProviderUserId` - UNIQUE composite for duplicate prevention

**Relationships**:
- `ApplicationUser` (Many-to-One via UserId, CASCADE)

**Future Providers**:
- Entra (Microsoft 365) - Priority
- Google Workspace
- Okta
- Generic SAML/OIDC

---

### 5. UserStatus Enum

**Values**:
- `Active` (1) - Account is active and can authenticate
- `Deactivated` (2) - Account has been deactivated and cannot authenticate

**Storage**: String representation in database (nvarchar(20))

---

## ASP.NET Core Identity Tables

These standard Identity tables are automatically created by `IdentityDbContext`:

### AspNetRoles
Standard ASP.NET Core Identity roles table (not used in Cadence; we use GlobalRole field instead).

### AspNetUserRoles
Many-to-many join table for user-role assignments (not used in Cadence).

### AspNetUserClaims
User claims table for additional user properties.

### AspNetUserLogins
External login provider table (separate from our custom ExternalLogin entity).

### AspNetUserTokens
Token storage for Identity features (password reset, email confirmation, etc.).

### AspNetRoleClaims
Role claims table.

---

## Automatic Timestamps

All entities implementing `IHasTimestamps` have their `CreatedAt` and `UpdatedAt` fields automatically set by `AppDbContext.SaveChanges()`:

- **CreatedAt**: Set on `EntityState.Added`
- **UpdatedAt**: Set on `EntityState.Added` and `EntityState.Modified`

Entities with automatic timestamps:
- RefreshToken
- PasswordResetToken
- ExternalLogin

---

## Column Type: datetime2

All `DateTime` and `DateTime?` properties use the `datetime2` column type, configured globally in `AppDbContext.OnModelCreating()`. This provides better precision and range than `datetime`.

---

## Query Filters

No global query filters are applied to authentication entities (they are not soft-deletable). The existing User entity is soft-deletable and has the filter applied.

---

## Migration Commands

### Create Migration
```bash
cd src/Cadence.Core
dotnet ef migrations add AddAuthenticationEntities \
  --startup-project ../Cadence.WebApi \
  --context AppDbContext \
  --output-dir Migrations
```

### Apply Migration
```bash
cd src/Cadence.WebApi
dotnet ef database update
```

### Rollback Migration
```bash
cd src/Cadence.WebApi
dotnet ef database update <PreviousMigrationName>
```

### Remove Last Migration (if not applied)
```bash
cd src/Cadence.Core
dotnet ef migrations remove --startup-project ../Cadence.WebApi
```

---

## First User Bootstrap

The first user to register automatically receives the `Administrator` role. This is enforced in the service layer by checking `_userManager.Users.CountAsync() == 0` within a database transaction to prevent race conditions.

---

## Security Considerations

1. **Password Storage**: Hashed using ASP.NET Core Identity's password hasher (PBKDF2)
2. **Token Storage**: Only SHA256 hashes are stored; actual tokens sent once to client
3. **Token Expiration**:
   - Access tokens: 15 minutes
   - Refresh tokens: 4 hours (default) or 30 days (RememberMe)
   - Password reset tokens: 1 hour
4. **Rate Limiting**: Enforced in service layer (5 password reset requests per 15 minutes)
5. **Lockout**: Configurable via Identity options (default: 5 failed attempts = 15 minute lockout)
6. **Concurrency**: Identity uses `ConcurrencyStamp` for optimistic concurrency

---

## Related Stories

- S02: Validate and Save User
- S03: First User Becomes Administrator
- S05: JWT Token Issuance
- S07: Automatic Token Refresh
- S24: Self-Service Password Reset

---

*Schema created: 2026-01-22*
