# S06: Failed Login Handling

## Story

**As a** user who enters incorrect credentials,
**I want** clear feedback and protection against brute force,
**So that** I understand what went wrong and my account remains secure.

## Context

Failed logins need to balance helpful feedback with security. We don't want to reveal whether an email exists (enumeration attack), but we do want to help legitimate users. Account lockout after repeated failures prevents brute force attacks.

## Acceptance Criteria

- [ ] **Given** I enter incorrect password, **when** I submit, **then** I see "Invalid email or password" (generic message)
- [ ] **Given** I enter non-existent email, **when** I submit, **then** I see "Invalid email or password" (same message, no enumeration)
- [ ] **Given** I fail login 4 times, **when** I view the form, **then** I see "X attempts remaining before lockout"
- [ ] **Given** I fail login 5 times, **when** I try again, **then** I see "Account locked. Try again in 15 minutes."
- [ ] **Given** my account is locked, **when** 15 minutes pass, **then** I can attempt login again
- [ ] **Given** I login successfully after failures, **when** authenticated, **then** my failed attempt counter resets
- [ ] **Given** login fails, **when** the form resets, **then** the password field is cleared but email is preserved
- [ ] **Given** I am offline, **when** I try to login, **then** I see "You're offline. Please check your connection."

## Out of Scope

- CAPTCHA after failed attempts
- Admin notification of lockouts
- IP-based rate limiting
- "Forgot password" flow

## Dependencies

- S04 (Login Form)
- S05 (JWT Issuance)

## Domain Terms

| Term | Definition |
|------|------------|
| Account Lockout | Temporary block on login attempts after too many failures |
| Lockout Duration | Time before locked account can attempt login again (15 minutes) |
| Failed Attempt Counter | Number of consecutive failed logins for an account |

## API Contract

**Failed Login Response (401 Unauthorized):**
```json
{
  "error": "invalid_credentials",
  "message": "Invalid email or password",
  "attemptsRemaining": 3
}
```

**Locked Account Response (429 Too Many Requests):**
```json
{
  "error": "account_locked",
  "message": "Account locked. Try again in 15 minutes.",
  "lockoutEnd": "2025-01-21T14:30:00Z"
}
```

## Technical Notes

- Use ASP.NET Core Identity's built-in lockout feature
- Configure in `IdentityOptions`:
  ```csharp
  options.Lockout.MaxFailedAccessAttempts = 5;
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
  options.Lockout.AllowedForNewUsers = true;
  ```
- Log failed attempts (userId if known, IP, timestamp)
- Consider progressive delays (1s, 2s, 4s) before lockout

## UI/UX Notes

- Error message uses warning color (amber/orange), not error (red) for "attempts remaining"
- Locked state uses error color
- Countdown timer for lockout (optional, nice-to-have)
- Clear, actionable language

---

*Story created: 2025-01-21*
