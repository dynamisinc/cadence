# S07: Automatic Token Refresh

## Story

**As an** authenticated user,
**I want** my session to refresh automatically,
**So that** I can work uninterrupted during long exercise sessions.

## Context

Exercise conduct sessions can last several hours. Users shouldn't be interrupted by authentication timeouts during critical moments. The system should proactively refresh tokens before they expire, making authentication invisible to the user.

## Acceptance Criteria

- [ ] **Given** my access token expires in less than 2 minutes, **when** I make an API call, **then** a refresh happens automatically first
- [ ] **Given** a refresh is in progress, **when** multiple API calls are made, **then** they queue until refresh completes (no duplicate refreshes)
- [ ] **Given** refresh succeeds, **when** I check my session, **then** I have a new access token valid for 15 minutes
- [ ] **Given** refresh succeeds, **when** the queued API calls execute, **then** they use the new access token
- [ ] **Given** I am actively using Cadence, **when** 4 hours pass without logging out, **then** I am still authenticated (refresh token renewed)
- [ ] **Given** I have multiple browser tabs open, **when** one tab refreshes the token, **then** all tabs use the new token

## Out of Scope

- Background refresh when app is not in use
- Refresh token rotation on every refresh (consider for future)
- Push notification of imminent session expiry

## Dependencies

- S05 (JWT Issuance)
- Axios interceptors

## Domain Terms

| Term | Definition |
|------|------------|
| Proactive Refresh | Refreshing token before it expires, not after |
| Token Queue | Holding API requests while refresh is in progress |
| Refresh Window | Time before expiration when refresh should occur (2 minutes) |

## API Contract

**Endpoint:** `POST /api/auth/refresh`

**Request:**
*No body - refresh token sent via HttpOnly cookie*

**Success Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

**Failed Response (401 Unauthorized):**
```json
{
  "error": "invalid_refresh_token",
  "message": "Session expired. Please login again."
}
```

## Technical Notes

```typescript
// Axios interceptor pseudo-code
let isRefreshing = false;
let refreshSubscribers: ((token: string) => void)[] = [];

axios.interceptors.request.use(async (config) => {
  const token = getAccessToken();
  const expiresAt = getTokenExpiration(token);
  
  // Proactive refresh if expiring within 2 minutes
  if (expiresAt - Date.now() < 120000) {
    if (!isRefreshing) {
      isRefreshing = true;
      try {
        const newToken = await refreshToken();
        refreshSubscribers.forEach(cb => cb(newToken));
        refreshSubscribers = [];
      } finally {
        isRefreshing = false;
      }
    } else {
      // Queue this request until refresh completes
      await new Promise(resolve => {
        refreshSubscribers.push((token) => {
          config.headers.Authorization = `Bearer ${token}`;
          resolve(config);
        });
      });
    }
  }
  
  config.headers.Authorization = `Bearer ${token}`;
  return config;
});
```

- Use localStorage for cross-tab token sync (access token only)
- Consider `BroadcastChannel` API for tab communication
- Log refresh events for debugging session issues

---

*Story created: 2025-01-21*
