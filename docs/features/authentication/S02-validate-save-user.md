# S02: Validate and Save User

## Story

**As a** new user,
**I want** my registration validated and saved securely,
**So that** I can be confident my account is protected.

## Context

When a user submits registration, the backend must validate all inputs, check for duplicate emails, hash the password securely, and create the user record. Clear error messages help users correct issues without frustration.

## Acceptance Criteria

- [ ] **Given** I submit valid registration data, **when** the API processes it, **then** a new user record is created
- [ ] **Given** I submit a duplicate email, **when** the API processes it, **then** I receive error "An account with this email already exists"
- [ ] **Given** I submit a password under 8 characters, **when** the API processes it, **then** I receive error "Password must be at least 8 characters"
- [ ] **Given** I submit a password without uppercase, **when** the API processes it, **then** I receive error "Password must contain at least one uppercase letter"
- [ ] **Given** I submit a password without a number, **when** the API processes it, **then** I receive error "Password must contain at least one number"
- [ ] **Given** registration succeeds, **when** the user is created, **then** the password is hashed (never stored in plain text)
- [ ] **Given** registration succeeds, **when** the response returns, **then** I receive a JWT access token and refresh token
- [ ] **Given** registration succeeds, **when** the user is created, **then** they are assigned the default role (Observer)

## Out of Scope

- Email verification
- Password complexity beyond stated requirements
- Account activation workflow

## Dependencies

- S01 (Registration Form)
- ASP.NET Core Identity configuration

## Domain Terms

| Term | Definition |
|------|------------|
| Password Hash | One-way encrypted version of password stored in database |
| Default Role | Role assigned to new users (Observer for standard registration) |

## API Contract

**Endpoint:** `POST /api/auth/register`

**Request:**
```json
{
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "password": "SecurePass123"
}
```

**Success Response (201 Created):**
```json
{
  "userId": "guid-here",
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "role": "Observer",
  "accessToken": "eyJ...",
  "refreshToken": "eyJ...",
  "expiresIn": 900
}
```

**Error Response (400 Bad Request):**
```json
{
  "errors": {
    "email": ["An account with this email already exists"],
    "password": ["Password must be at least 8 characters"]
  }
}
```

## Technical Notes

- Use ASP.NET Core Identity's `UserManager<T>` for user creation
- Password validation via Identity's `PasswordOptions`
- Return JWT immediately (auto-login on registration)
- Log registration events (user ID, timestamp, IP - no password)

---

*Story created: 2025-01-21*
