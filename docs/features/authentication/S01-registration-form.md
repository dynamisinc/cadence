# S01: Registration Form

## Story

**As a** new user,
**I want** to enter my account details in a registration form,
**So that** I can create an account and access Cadence.

## Context

Users need a clear, simple form to create their account. The form should provide immediate validation feedback and guide users toward creating a secure password. This is often the first interaction a user has with Cadence, so the experience should be welcoming and frictionless.

## Acceptance Criteria

- [ ] **Given** I am on the login page, **when** I click "Create Account", **then** I see the registration form
- [ ] **Given** I am on the registration form, **when** I view it, **then** I see fields for: Display Name, Email, Password, Confirm Password
- [ ] **Given** I am entering a password, **when** I type, **then** I see real-time feedback on password requirements
- [ ] **Given** I have entered mismatched passwords, **when** I blur the confirm field, **then** I see an inline error "Passwords do not match"
- [ ] **Given** I have entered an invalid email format, **when** I blur the email field, **then** I see an inline error "Please enter a valid email address"
- [ ] **Given** all fields are valid, **when** I click "Create Account", **then** the form submits
- [ ] **Given** the form is submitting, **when** I view the button, **then** I see a loading indicator and the button is disabled
- [ ] **Given** I click the password visibility toggle, **when** I view the field, **then** the password is shown/hidden

## Out of Scope

- Email verification flow
- CAPTCHA or bot protection
- Social login options
- Terms of service acceptance checkbox

## Dependencies

- MUI component library (from Phase A) ✅

## Domain Terms

| Term | Definition |
|------|------------|
| Display Name | User's preferred name shown in the UI (not username) |
| Registration | Process of creating a new user account |

## UI/UX Notes

```
┌─────────────────────────────────────────┐
│              CADENCE                     │
│                                         │
│     ┌─────────────────────────────┐     │
│     │      Create Account         │     │
│     ├─────────────────────────────┤     │
│     │                             │     │
│     │  Display Name               │     │
│     │  ┌───────────────────────┐  │     │
│     │  │                       │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  Email Address              │     │
│     │  ┌───────────────────────┐  │     │
│     │  │                       │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  Password                   │     │
│     │  ┌───────────────────────┐  │     │
│     │  │ ••••••••          👁  │  │     │
│     │  └───────────────────────┘  │     │
│     │  Min 8 chars, 1 uppercase,  │     │
│     │  1 number                   │     │
│     │                             │     │
│     │  Confirm Password           │     │
│     │  ┌───────────────────────┐  │     │
│     │  │ ••••••••          👁  │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  ┌───────────────────────┐  │     │
│     │  │    Create Account    │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  Already have an account?   │     │
│     │  Sign in                    │     │
│     └─────────────────────────────┘     │
│                                         │
└─────────────────────────────────────────┘
```

- Use MUI TextField components with validation states
- Password strength indicator (optional visual, not blocking)
- Clear error messages in plain language
- "Sign in" link for existing users
- Form should be centered, max-width 400px

## Technical Notes

- Client-side validation before API call
- Debounce email uniqueness check (if implementing)
- Store form state in React (useState or form library)

---

*Story created: 2025-01-21*
