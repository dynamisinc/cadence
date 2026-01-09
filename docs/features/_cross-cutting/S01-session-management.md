# Story: S01 Session Management

> **Status**: 📋 Ready for Development  
> **Priority**: P0 (Critical)  
> **Epic**: E2 - Infrastructure  
> **Sprint Points**: 5

## User Story

**As a** Cadence user,  
**I want** my session to remain active during long exercise conduct periods without unexpected logouts,  
**So that** I don't lose my work or miss critical exercise events due to session timeout.

## Context

The EXIS analysis revealed that short session timeouts (30 minutes) are a major pain point during exercises that can last 4-8 hours. Users lose unsaved work when sessions expire, and the interruption of re-authenticating during a live exercise can cause missed injects.

Cadence must support extended sessions appropriate for exercise conduct while maintaining security best practices.

### User Impact

| Scenario | Impact Without This Feature |
|----------|----------------------------|
| Multi-hour exercise | Forced logout mid-conduct, missed injects |
| Data entry | Lost work requiring re-entry |
| Evaluator observations | Incomplete documentation |
| Multiple browser tabs | Inconsistent session state |

## Acceptance Criteria

### Session Duration

- [ ] **Given** a user is authenticated, **when** they remain active (any interaction), **then** their session remains valid for at least 4 hours of continuous activity

- [ ] **Given** a user is authenticated, **when** they are inactive for 30 minutes, **then** they see a warning modal with time remaining and "Extend Session" button

- [ ] **Given** a user sees the session warning, **when** they click "Extend Session", **then** their session is extended for another 4 hours and the warning closes

- [ ] **Given** a user sees the session warning, **when** they do not respond within 5 minutes, **then** they are logged out and redirected to login with message "Session expired due to inactivity"

- [ ] **Given** a user is logged out due to timeout, **when** they log back in, **then** they are returned to the page they were on (if still valid)

### Multi-Tab Support

- [ ] **Given** a user has multiple Cadence tabs open, **when** they extend their session in one tab, **then** all tabs reflect the extended session (no duplicate warnings)

- [ ] **Given** a user has multiple tabs open, **when** they log out in one tab, **then** all tabs redirect to login

### Offline Handling

- [ ] **Given** a user loses network connectivity, **when** they regain connectivity before session expiry, **then** their session remains valid

- [ ] **Given** a user is offline, **when** their session would expire, **then** they see offline indicator but are not logged out until connectivity returns

### Security

- [ ] **Given** a user closes their browser, **when** they reopen and navigate to Cadence within 4 hours, **then** they remain authenticated (remember me)

- [ ] **Given** security requirements, **when** implementing session management, **then** tokens are stored securely (HttpOnly cookies or secure storage)

## Out of Scope

- Single sign-on (SSO) integration (future consideration)
- Concurrent session limiting (allow unlimited tabs/devices in MVP)
- Session activity audit logging (Standard phase)
- Custom timeout configuration per organization (Standard phase)

## Dependencies

- Authentication system implementation
- Frontend state management setup
- Offline capability infrastructure (Infrastructure story)

## Open Questions

- [ ] Should "Remember Me" checkbox be offered at login, or always remember?
- [ ] What is the maximum absolute session duration (even with activity)? Recommend 12 hours.
- [ ] Should session extend on any activity, or only explicit user actions?

## Domain Terms

| Term | Definition |
|------|------------|
| Session | Authenticated period between login and logout |
| Session Timeout | Automatic logout due to inactivity |
| Token Refresh | Process of obtaining new authentication token |
| Silent Refresh | Token refresh without user interaction |

## UI/UX Notes

### Session Warning Modal

```
┌─────────────────────────────────────────────────────────────┐
│                    Session Expiring                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Your session will expire in 4:32 due to inactivity.        │
│                                                             │
│  Would you like to continue working?                        │
│                                                             │
│           ┌────────────────┐  ┌─────────────┐              │
│           │ Extend Session │  │   Log Out   │              │
│           └────────────────┘  └─────────────┘              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Visual Indicators

- Countdown timer in warning modal
- No persistent session indicator (unnecessary clutter)
- Clear feedback when session is extended

### Behavior Notes

- Warning modal should be dismissable by clicking "Extend" only (not background click)
- Warning should appear above any other modals
- Keyboard accessible (Enter = Extend, Esc = no action)

## Technical Notes

### Token Strategy

Recommend JWT with refresh token pattern:
- Access token: 15-minute expiry
- Refresh token: 4-hour expiry
- Silent refresh triggered before access token expiry
- Refresh token rotation on use

### Multi-Tab Coordination

Use BroadcastChannel API or localStorage events:
```javascript
// Notify other tabs of session extension
const channel = new BroadcastChannel('cadence-session');
channel.postMessage({ type: 'SESSION_EXTENDED', expiry: newExpiry });
```

### Activity Detection

Detect activity via:
- Mouse movement (debounced)
- Keyboard input
- Touch events
- Focus events
- API calls

### Storage

- Access token: Memory only (for security)
- Refresh token: HttpOnly cookie (preferred) or secure localStorage
- Session state: localStorage for cross-tab sync

---

## INVEST Checklist

- [x] **I**ndependent - Can be developed with mock auth, before full auth system
- [x] **N**egotiable - Timeout values and warning timing can be adjusted
- [x] **V**aluable - Critical for multi-hour exercise support
- [x] **E**stimable - Well-defined scope, ~5 points
- [x] **S**mall - Focused on session lifecycle only
- [x] **T**estable - Clear timeout and warning behaviors

## Test Scenarios

### Unit Tests
- Token refresh logic
- Activity detection debouncing
- Warning timer calculations

### Integration Tests
- Full session lifecycle
- Multi-tab synchronization
- Offline/online transitions

### E2E Tests
- Login → activity → warning → extend → continued work
- Login → inactivity → warning → timeout → redirect
- Multi-tab logout propagation

---

*Related Stories*: [S03 Auto-save](./S03-auto-save.md), [Offline Capability](../README.md)

*Last updated: 2025-01-08*
