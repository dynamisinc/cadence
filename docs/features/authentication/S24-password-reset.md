# S24: Self-Service Password Reset

## Story

**As a** registered user who has forgotten my password,
**I want** to reset my password via email,
**So that** I can regain access to my account without contacting an administrator.

## Context

Users occasionally forget their passwords, especially for systems used intermittently (exercises may be weeks or months apart). Self-service password reset reduces admin burden and improves user experience. This feature uses Azure Communication Services (ACS) for email delivery.

## Acceptance Criteria

### Request Reset
- [ ] **Given** I am on the login page, **when** I click "Forgot Password", **then** I see a password reset request form
- [ ] **Given** I am on the reset request form, **when** I enter my email and submit, **then** I see "If an account exists, a reset link has been sent"
- [ ] **Given** I submit a reset request, **when** the email exists, **then** an email is sent with a reset link
- [ ] **Given** I submit a reset request, **when** the email does NOT exist, **then** NO email is sent (but same message shown - prevents enumeration)

### Reset Token
- [ ] **Given** a reset is requested, **when** the token is generated, **then** it expires after 1 hour
- [ ] **Given** a reset is requested, **when** a new token is generated, **then** any previous tokens are invalidated
- [ ] **Given** a reset token, **when** I inspect it, **then** it contains no sensitive information (opaque token only)

### Complete Reset
- [ ] **Given** I click the reset link in email, **when** the token is valid, **then** I see a new password form
- [ ] **Given** I click the reset link, **when** the token is expired, **then** I see "This link has expired. Please request a new reset."
- [ ] **Given** I click the reset link, **when** the token is already used, **then** I see "This link has already been used."
- [ ] **Given** I enter a new password, **when** it meets requirements, **then** my password is updated and I'm redirected to login
- [ ] **Given** I reset my password, **when** complete, **then** all existing sessions/refresh tokens are revoked

### Email Content
- [ ] **Given** a reset email is sent, **when** I read it, **then** it includes: reset link, expiration time (1 hour), and "If you didn't request this, ignore this email"
- [ ] **Given** a reset email is sent, **when** I check the sender, **then** it comes from a branded Cadence address (e.g., noreply@cadence.example.com)

### Security
- [ ] **Given** I request multiple resets, **when** I exceed 5 requests in 15 minutes, **then** I'm rate limited
- [ ] **Given** I enter wrong passwords on reset form, **when** I exceed 5 attempts, **then** the token is invalidated
- [ ] **Given** my account is deactivated, **when** I request a reset, **then** no email is sent

## Out of Scope

- Password reset via SMS/phone
- Security questions
- Admin-initiated password reset (covered in S12)
- "Magic link" passwordless login

## Dependencies

- S05 (JWT Token Issuance) - Token revocation on password change
- S17 (Identity Provider) - Password update functionality
- Azure Communication Services - Email delivery

## Domain Terms

| Term | Definition |
|------|------------|
| Reset Token | Single-use, time-limited token embedded in reset URL |
| Token Expiry | 1 hour from generation |
| Rate Limit | 5 requests per 15 minutes per email address |

## API Contract

### Request Password Reset

```http
POST /api/auth/password-reset/request
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response (200 OK - always, to prevent enumeration):**
```json
{
  "message": "If an account with that email exists, a password reset link has been sent."
}
```

### Complete Password Reset

```http
POST /api/auth/password-reset/complete
Content-Type: application/json

{
  "token": "abc123...",
  "newPassword": "NewSecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "message": "Password reset successful. Please log in with your new password."
}
```

**Response (400 Bad Request - Invalid/Expired Token):**
```json
{
  "error": {
    "code": "invalid_token",
    "message": "This reset link is invalid or has expired. Please request a new one."
  }
}
```

**Response (400 Bad Request - Password Requirements):**
```json
{
  "error": {
    "code": "validation_error",
    "message": "Password does not meet requirements",
    "details": {
      "password": ["Must be at least 8 characters", "Must contain uppercase letter"]
    }
  }
}
```

## Email Template

```
Subject: Reset your Cadence password

Hi [DisplayName],

We received a request to reset your Cadence password. Click the link below to set a new password:

[Reset Password Button/Link]

This link will expire in 1 hour.

If you didn't request this password reset, you can safely ignore this email. Your password will not be changed.

---
Cadence - HSEEP MSEL Management Platform
This is an automated message. Please do not reply.
```

## UI/UX Notes

### Forgot Password Link
```
┌─────────────────────────────────────────────────────────────┐
│                         Sign In                              │
├─────────────────────────────────────────────────────────────┤
│  Email: [________________________]                          │
│  Password: [________________________]                       │
│                                                             │
│  [Sign In]                                                  │
│                                                             │
│  Forgot your password?  ← Link to reset form                │
│                                                             │
│  ──────────────────  OR  ──────────────────                 │
│  [Sign in with Microsoft]                                   │
└─────────────────────────────────────────────────────────────┘
```

### Request Reset Form
```
┌─────────────────────────────────────────────────────────────┐
│                    Reset Password                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Enter your email address and we'll send you a link to     │
│  reset your password.                                       │
│                                                             │
│  Email: [________________________]                          │
│                                                             │
│  [Send Reset Link]                                          │
│                                                             │
│  ← Back to Sign In                                          │
└─────────────────────────────────────────────────────────────┘
```

### Success Message
```
┌─────────────────────────────────────────────────────────────┐
│                    Check Your Email                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✉️  If an account exists for user@example.com, we've       │
│      sent a password reset link.                            │
│                                                             │
│  The link will expire in 1 hour.                            │
│                                                             │
│  Didn't receive the email? Check your spam folder or        │
│  [request another link].                                    │
│                                                             │
│  ← Back to Sign In                                          │
└─────────────────────────────────────────────────────────────┘
```

### New Password Form
```
┌─────────────────────────────────────────────────────────────┐
│                  Set New Password                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  New Password: [________________________]                   │
│  Confirm Password: [________________________]               │
│                                                             │
│  Password requirements:                                     │
│  ✓ At least 8 characters                                   │
│  ✓ Contains uppercase letter                               │
│  ✓ Contains number                                         │
│                                                             │
│  [Set New Password]                                         │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

### Azure Communication Services Integration

```csharp
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string displayName, string resetUrl);
}

public class AcsEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly EmailOptions _options;

    public async Task SendPasswordResetEmailAsync(string email, string displayName, string resetUrl)
    {
        var message = new EmailMessage(
            senderAddress: _options.SenderAddress,  // e.g., "noreply@cadence.dynamis.com"
            content: new EmailContent("Reset your Cadence password")
            {
                Html = BuildResetEmailHtml(displayName, resetUrl)
            },
            recipients: new EmailRecipients(new List<EmailAddress>
            {
                new(email, displayName)
            })
        );

        await _emailClient.SendAsync(WaitUntil.Started, message);
    }
}
```

### Reset Token Storage

```csharp
public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;  // SHA256 hash of token
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
}

// Token is generated as random bytes, hashed before storage
// Only hash is stored - original token sent in email URL
```

### Configuration

```json
{
  "Authentication": {
    "PasswordReset": {
      "TokenExpiryMinutes": 60,
      "RateLimitRequests": 5,
      "RateLimitWindowMinutes": 15,
      "MaxAttempts": 5
    }
  },
  "Email": {
    "Provider": "AzureCommunicationServices",
    "ConnectionString": "endpoint=https://...",
    "SenderAddress": "noreply@cadence.dynamis.com",
    "SenderDisplayName": "Cadence"
  }
}
```

---

*Story created: 2026-01-21*
