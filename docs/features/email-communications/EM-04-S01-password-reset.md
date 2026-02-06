# Story: EM-04-S01 - Password Reset Email

**As a** User,  
**I want** to receive a password reset email when I request one,  
**So that** I can regain access to my account if I forget my password.

## Context

Password reset is a critical security feature. The email must be delivered quickly, contain a secure time-limited link, and provide clear instructions. This is a mandatory email that cannot be disabled in preferences.

## Acceptance Criteria

### Request Flow

- [ ] **Given** I'm on login page, **when** I click "Forgot Password", **then** I see email input form
- [ ] **Given** valid email entered, **when** submitted, **then** reset email is sent within 30 seconds
- [ ] **Given** email not in system, **when** submitted, **then** same success message shown (security)
- [ ] **Given** success message, **when** displayed, **then** it says "If an account exists, you'll receive an email"

### Email Content

- [ ] **Given** reset email, **when** received, **then** it contains secure reset link
- [ ] **Given** reset link, **when** generated, **then** token expires in 1 hour
- [ ] **Given** reset email, **when** received, **then** it shows when the request was made
- [ ] **Given** reset email, **when** received, **then** it explains what to do if not requested
- [ ] **Given** reset email, **when** received, **then** it includes account email for verification

### Security

- [ ] **Given** reset token, **when** used once, **then** it cannot be used again
- [ ] **Given** multiple reset requests, **when** made, **then** only latest token is valid
- [ ] **Given** expired token, **when** clicking link, **then** user sees "Link expired" with option to request new
- [ ] **Given** invalid token, **when** clicking link, **then** user sees generic error (don't reveal token validity)

### Rate Limiting

- [ ] **Given** same email, **when** reset requested 3 times in 1 hour, **then** subsequent requests are blocked
- [ ] **Given** rate limited, **when** attempting again, **then** user sees "Please wait before requesting again"

## Out of Scope

- Security questions as alternative
- SMS-based reset
- Admin-triggered password reset

## Dependencies

- EM-01-S01: ACS Email Configuration
- EM-01-S02: Email Template System
- ASP.NET Core Identity (token generation)

## Technical Notes

### Token Generation

```csharp
// Using ASP.NET Core Identity
var token = await _userManager.GeneratePasswordResetTokenAsync(user);
var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
var resetUrl = $"{_baseUrl}/reset-password?email={email}&token={encodedToken}";
```

### Email Template Model

```csharp
public class PasswordResetEmailModel
{
    public string Email { get; set; }
    public string ResetUrl { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; }      // For "not you?" context
}
```

## UI/UX Notes

### Email Preview

```
Subject: Reset your Cadence password

---

[Cadence Logo]

Password Reset Request

Hi,

We received a request to reset the password for your 
Cadence account (jane@example.com).

        [Reset Password]

This link expires in 1 hour.

Request details:
• Time: February 6, 2026 at 2:34 PM EST
• IP: 192.168.1.xxx

If you didn't request this, you can safely ignore this email.
Your password won't change unless you click the link above.

---

This is an automated security email from Cadence.
You cannot unsubscribe from security notifications.
```

## Domain Terms

| Term | Definition |
|------|------------|
| Password Reset Token | Secure, single-use code that authorizes password change |
| Rate Limiting | Restriction on how often an action can be performed |

## Effort Estimate

**3 story points** - Token generation, security considerations, template

---

*Feature: EM-04 Authentication Emails*  
*Priority: P0*
