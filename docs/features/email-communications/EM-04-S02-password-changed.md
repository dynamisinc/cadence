# Story: EM-04-S02 - Password Changed Confirmation

**As a** User,  
**I want** to receive confirmation when my password is changed,  
**So that** I'm alerted if someone else changed it without my knowledge.

## Context

Password change confirmations are a security best practice. They alert users to potential unauthorized access and provide a path to secure their account if the change wasn't intentional.

## Acceptance Criteria

- [ ] **Given** password is changed (any method), **when** change saved, **then** confirmation email sent immediately
- [ ] **Given** confirmation email, **when** received, **then** it shows when the change occurred
- [ ] **Given** confirmation email, **when** received, **then** it shows method (reset link vs. account settings)
- [ ] **Given** confirmation email, **when** received, **then** it includes "Wasn't you?" instructions
- [ ] **Given** confirmation email, **when** received, **then** it includes link to secure account
- [ ] **Given** this email type, **when** checking preferences, **then** it's mandatory (cannot disable)

## Out of Scope

- Automatic account lockout on suspicious changes
- Password change reversal

## Dependencies

- EM-01-S01: ACS Email Configuration
- Password change functionality

## UI/UX Notes

### Email Preview

```
Subject: Your Cadence password was changed

---

[Cadence Logo]

Password Changed

Hi Jane,

Your Cadence password was successfully changed.

Change details:
• Time: February 6, 2026 at 3:15 PM EST  
• Method: Account settings

If you made this change, no action is needed.

If you didn't change your password:
1. Reset your password immediately
2. Review your account for unauthorized changes
3. Contact support if you need help

        [Reset Password]     [Contact Support]

---

This is an automated security email from Cadence.
```

## Domain Terms

| Term | Definition |
|------|------------|
| Password Change Confirmation | Security notification sent after password modification |

## Effort Estimate

**2 story points** - Trigger implementation, template

---

*Feature: EM-04 Authentication Emails*  
*Priority: P0*
