# Story: EM-04-S03 - Account Verification Email

**As a** new User,  
**I want** to verify my email address,  
**So that** my account is fully activated and I can receive important notifications.

## Context

Email verification confirms the user owns the email address they registered with. This prevents typos, ensures deliverability, and is a security best practice. Users can access the system with limited functionality until verified.

## Acceptance Criteria

### Verification Flow

- [ ] **Given** user registers/accepts invitation, **when** account created, **then** verification email sent automatically
- [ ] **Given** verification email, **when** received, **then** contains unique verification link
- [ ] **Given** verification link clicked, **when** valid, **then** email marked as verified
- [ ] **Given** successful verification, **when** complete, **then** user sees "Email verified!" confirmation

### Token Security

- [ ] **Given** verification token, **when** generated, **then** expires in 24 hours
- [ ] **Given** expired token, **when** clicked, **then** user sees "Link expired" with resend option
- [ ] **Given** verification token, **when** used, **then** it cannot be used again

### Unverified Experience

- [ ] **Given** unverified user, **when** signed in, **then** they see "Verify your email" banner
- [ ] **Given** unverified user, **when** viewing banner, **then** they can request new verification email
- [ ] **Given** unverified user, **when** using app, **then** core features work (not blocked)

### Resend Verification

- [ ] **Given** user requests resend, **when** submitted, **then** new email sent with fresh token
- [ ] **Given** resend requested, **when** sent, **then** previous token is invalidated
- [ ] **Given** multiple resend requests, **when** rate limited, **then** max 3 per hour

## Out of Scope

- Blocking unverified users from app access
- Phone verification alternative
- Verification for email address changes

## Dependencies

- EM-01-S01: ACS Email Configuration
- User registration flow

## UI/UX Notes

### Verification Banner

```
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️ Please verify your email (jane@example.com) to ensure you   │
│    receive important notifications.        [Resend Email]      │
└─────────────────────────────────────────────────────────────────┘
```

### Email Preview

```
Subject: Verify your Cadence email

---

[Cadence Logo]

Verify Your Email

Hi Jane,

Please verify your email address to complete your 
Cadence account setup.

        [Verify Email Address]

This link expires in 24 hours.

If you didn't create a Cadence account, you can 
safely ignore this email.

---

Cadence - Exercise Management Platform
```

## Domain Terms

| Term | Definition |
|------|------------|
| Email Verification | Process confirming user owns their registered email |
| Unverified User | User who hasn't confirmed email ownership |

## Effort Estimate

**3 story points** - Token management, UI banner, resend flow

---

*Feature: EM-04 Authentication Emails*  
*Priority: P0*
