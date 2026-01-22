# S09: Secure Logout

## Story

**As an** authenticated user,
**I want** to securely logout,
**So that** my session is fully terminated and my account is protected.

## Context

Logout must clear all authentication state both client-side and server-side. On shared computers, users need confidence that the next person can't access their account. Logout should also clear cached data to protect sensitive exercise information.

## Acceptance Criteria

- [ ] **Given** I am logged in, **when** I click "Logout", **then** I see a brief confirmation before logout executes
- [ ] **Given** I confirm logout, **when** the action completes, **then** my access token is cleared from memory
- [ ] **Given** I confirm logout, **when** the action completes, **then** my refresh token cookie is invalidated
- [ ] **Given** I confirm logout, **when** the action completes, **then** I am redirected to the login page
- [ ] **Given** I have logged out, **when** I use the back button, **then** I cannot see protected content
- [ ] **Given** I logout, **when** I check IndexedDB, **then** cached exercise data is cleared (optional: prompt user)
- [ ] **Given** I have multiple tabs open, **when** I logout in one tab, **then** all tabs redirect to login
- [ ] **Given** I am offline, **when** I click logout, **then** I am logged out locally (server invalidation on reconnect)

## Out of Scope

- "Logout all devices" option
- Session history / active sessions list
- Inactivity auto-logout

## Dependencies

- S05 (JWT Issuance)
- IndexedDB sync service (Phase H) ✅

## Domain Terms

| Term | Definition |
|------|------------|
| Token Invalidation | Server-side marking of refresh token as revoked |
| Local Logout | Clearing client-side auth state (works offline) |
| Cross-tab Logout | Synchronizing logout across multiple browser tabs |

## API Contract

**Endpoint:** `POST /api/auth/logout`

**Request:**
*No body - refresh token sent via HttpOnly cookie*

**Success Response (204 No Content):**
*Empty response, cookie cleared via `Set-Cookie` header*

## Technical Notes

```typescript
// Logout flow
async function logout(clearCache: boolean = false) {
  try {
    // Server-side invalidation (if online)
    await axios.post('/api/auth/logout');
  } catch {
    // Offline - queue for later
    queueOfflineAction({ type: 'logout' });
  }
  
  // Client-side cleanup (always)
  clearAccessToken();
  
  if (clearCache) {
    await clearIndexedDB();
  }
  
  // Notify other tabs
  localStorage.setItem('logout', Date.now().toString());
  
  // Redirect
  window.location.href = '/login';
}

// Listen for logout in other tabs
window.addEventListener('storage', (e) => {
  if (e.key === 'logout') {
    window.location.href = '/login';
  }
});
```

## UI/UX Notes

- Logout button in user menu (top-right)
- Brief confirmation: "Are you sure you want to sign out?"
- Option: "Clear cached data" checkbox (default unchecked)
- Success toast: "You have been signed out"

## Open Questions

- [x] Should logout clear IndexedDB by default? **Decision: No, prompt user with checkbox**
- [x] Should offline logout queue server invalidation? **Decision: Yes, security best practice**

---

*Story created: 2025-01-21*
