# Story: Organization Session Timeout

**Feature**: Settings  
**Story ID**: S11  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Administrator,  
**I want** to configure how long user sessions remain active,  
**So that** I can balance security requirements with usability during long exercises.

---

## Context

SME feedback on EXIS highlighted session timeout as a major pain point: users getting logged out mid-exercise, losing work, and having to re-authenticate repeatedly. However, organizations have legitimate security requirements.

The solution: make session timeout configurable at the organization level, with reasonable ranges that balance security and usability.

---

## Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I access organization settings, **then** I see session timeout configuration
- [ ] **Given** I am configuring timeout, **when** I view options, **then** I can select from preset durations: 30 min, 1 hour, 2 hours, 4 hours, 8 hours
- [ ] **Given** I select 4-hour timeout, **when** a user's session reaches 4 hours of inactivity, **then** they are logged out
- [ ] **Given** a user is active (making requests), **when** the timeout period passes, **then** the timeout resets (activity extends session)
- [ ] **Given** a session is about to expire (5 minutes remaining), **when** the user is active in the app, **then** a warning appears with option to extend
- [ ] **Given** session expires, **when** the user tries to perform an action, **then** they see a friendly message and login prompt
- [ ] **Given** session expires, **when** the user had unsaved work, **then** their work is preserved locally and can be submitted after re-login
- [ ] **Given** defaults, **when** a new organization is created, **then** session timeout is 4 hours (exercise-friendly default)

---

## Out of Scope

- Per-user timeout overrides
- Activity-based timeout (different timeout for idle vs active)
- "Remember me" persistent sessions
- Multi-factor authentication refresh requirements

---

## Dependencies

- Authentication system
- JWT/refresh token implementation
- Organization entity

---

## Open Questions

- [ ] Should active SignalR connections prevent timeout?
- [ ] What constitutes "activity" - any request, or only user-initiated actions?
- [ ] Should there be a maximum timeout limit for security?
- [ ] How does offline mode interact with session timeout?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Session Timeout | Duration of inactivity before user is automatically logged out |
| Inactivity | No API requests or user interactions within the timeout period |

---

## UI/UX Notes

### Organization Settings - Session Timeout

```
┌─────────────────────────────────────────────────────────────┐
│  Organization Settings                        [Admin Only]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Security                                                   │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Session Timeout                                            │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [ 4 hours ▼ ]                                      │   │
│  │                                                     │   │
│  │  Users will be logged out after this period of      │   │
│  │  inactivity. Longer timeouts are recommended for    │   │
│  │  exercise conduct.                                  │   │
│  │                                                     │   │
│  │  Options: 30 min, 1 hour, 2 hours, 4 hours, 8 hours │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ⓘ Users will see a warning 5 minutes before timeout.     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Session Expiration Warning

```
┌─────────────────────────────────────────────────────────────┐
│  Session Expiring Soon                                 [X]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ⚠ Your session will expire in 5 minutes due to           │
│  inactivity.                                               │
│                                                             │
│  Click "Stay Logged In" or perform any action to extend    │
│  your session.                                             │
│                                                             │
│                                     [Stay Logged In]        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Session Expired Screen

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│                        🔒                                   │
│                                                             │
│              Session Expired                                │
│                                                             │
│     Your session has ended due to inactivity.              │
│     Please log in again to continue.                       │
│                                                             │
│     ✓ Any unsaved observations have been preserved.       │
│       They will be synced after you log in.               │
│                                                             │
│                    [Log In Again]                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store timeout duration on Organization entity (in minutes)
- JWT access token lifetime should be shorter than session timeout
- Refresh token lifetime matches session timeout
- Frontend: track last activity timestamp, show warning at (timeout - 5 min)
- Backend: validate session on each request
- Consider: offline queue should work even if session expired (re-auth on sync)
- Do NOT log out if user has active SignalR connection (treat as activity)

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
