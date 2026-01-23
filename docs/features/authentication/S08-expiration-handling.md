# S08: Token Expiration Handling

## Story

**As a** user whose session has expired,
**I want** to be redirected to login gracefully,
**So that** I don't lose my work and can quickly re-authenticate.

## Context

When both access and refresh tokens expire (user inactive for 4+ hours), the session is truly ended. The user must login again, but we should preserve their context and any unsaved work if possible.

## Acceptance Criteria

- [ ] **Given** my refresh token has expired, **when** I try to refresh, **then** I am redirected to the login page
- [ ] **Given** I am redirected to login, **when** I view the page, **then** I see "Your session has expired. Please sign in again."
- [ ] **Given** I was on a specific page, **when** I login again, **then** I am returned to that page (return URL)
- [ ] **Given** I have unsaved changes, **when** my session expires, **then** my changes are preserved locally
- [ ] **Given** I login after session expiry, **when** I return to my work, **then** I can recover my unsaved changes
- [ ] **Given** I am offline and my tokens expire, **when** I reconnect, **then** I am prompted to login

## Out of Scope

- Session extension warnings ("Your session will expire in 5 minutes")
- "Stay logged in" prompt
- Automatic save before redirect

## Dependencies

- S07 (Token Refresh)
- Offline sync service (Phase H) ✅

## Domain Terms

| Term | Definition |
|------|------------|
| Session Expiry | When both access and refresh tokens are invalid |
| Return URL | The page user was on before being redirected to login |
| Local Preservation | Storing unsaved data in IndexedDB for recovery |

## Technical Notes

```typescript
// Response interceptor for 401 handling
axios.interceptors.response.use(
  response => response,
  async (error) => {
    if (error.response?.status === 401) {
      const refreshResult = await attemptRefresh();
      if (!refreshResult.success) {
        // Save current work to IndexedDB
        await preserveUnsavedWork();
        
        // Store return URL
        sessionStorage.setItem('returnUrl', window.location.pathname);
        
        // Redirect to login
        window.location.href = '/login?expired=true';
      }
    }
    return Promise.reject(error);
  }
);
```

## UI/UX Notes

- Use toast notification for expiry message, not modal
- Return URL should exclude sensitive pages (if any)
- Recovery of unsaved work should be automatic (not prompt user)

---

*Story created: 2025-01-21*
