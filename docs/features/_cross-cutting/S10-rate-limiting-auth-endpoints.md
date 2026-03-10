# Story: S10 Rate Limiting on Authentication Endpoints

> **Status**: Proposed
> **Priority**: P2 (Medium - Security)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 5
> **Deferred From**: Code hardening review (CD-P02)

## User Story

**As an** Administrator,
**I want** authentication endpoints protected by rate limiting,
**So that** brute-force login attempts and credential stuffing attacks against exercise participant accounts are automatically throttled before they can compromise an active exercise.

## Context

Cadence's authentication endpoints — login, register, password reset, and token refresh — are publicly accessible. Without rate limiting, an attacker can make thousands of login attempts per second against a known email address, or use credential-stuffing lists to attempt automated logins before an exercise begins.

Because Cadence manages real emergency management exercises with sensitive participant information and MSEL content, protecting accounts from automated attacks is a security baseline requirement.

### Why Deferred

Two viable implementation approaches exist and the right choice depends on deployment architecture:

| Option | Implementation | Best For |
|--------|---------------|----------|
| ASP.NET Core built-in rate limiting | `Microsoft.AspNetCore.RateLimiting` middleware | Self-hosted / App Service without Azure Front Door |
| Azure Front Door WAF rules | Azure portal / Bicep configuration | Production deployments behind Azure Front Door |

The deferred decision is: which layer should own rate limiting? Using both provides defense-in-depth but requires coordination. This story should be picked up after the production deployment architecture is finalized.

### Endpoints in Scope

| Endpoint | Method | Rate Limit Rationale |
|----------|--------|----------------------|
| `POST /api/auth/login` | Write | Primary brute-force target |
| `POST /api/auth/register` | Write | Spam account creation |
| `POST /api/auth/forgot-password` | Write | Email enumeration / abuse |
| `POST /api/auth/reset-password` | Write | Token brute-force |
| `POST /api/auth/refresh` | Write | Refresh token cycling attacks |

Read endpoints (`GET /api/auth/...`) are lower risk and out of scope for this story.

## Acceptance Criteria

- [ ] **AC-01**: Given a client that sends more than 10 login requests within 60 seconds from the same IP address, when the 11th request arrives, then the server returns HTTP 429 (Too Many Requests) with a `Retry-After` header
  - Test: `AuthRateLimitingTests.cs::Login_ExceedsRateLimit_Returns429WithRetryAfter`

- [ ] **AC-02**: Given a client that has been rate-limited, when the retry window expires, then subsequent requests are processed normally
  - Test: `AuthRateLimitingTests.cs::Login_AfterRetryWindowExpires_RequestSucceeds`

- [ ] **AC-03**: Given the password reset endpoint, when more than 5 requests are made from the same IP within 60 seconds, then HTTP 429 is returned
  - Test: `AuthRateLimitingTests.cs::ForgotPassword_ExceedsRateLimit_Returns429`

- [ ] **AC-04**: Given an authenticated user refreshing their token, when token refresh is rate-limited per user identity (not just IP), then excessive refresh calls from the same user are throttled regardless of IP address
  - Test: `AuthRateLimitingTests.cs::TokenRefresh_PerUserLimit_ThrottlesExcessiveRefresh`

- [ ] **AC-05**: Given a legitimate user who reaches the rate limit, when they receive the 429 response, then the response body includes a human-readable message explaining the limit and when to retry (not a generic error)

- [ ] **AC-06**: Given rate limiting middleware is active, when normal single-user activity occurs (login once, refresh occasionally), then no 429 responses are encountered in normal usage

- [ ] **AC-07**: Given the chosen implementation approach, when it is documented, then a decision record in this story's Implementation Notes explains whether ASP.NET Core middleware or Azure Front Door WAF is used and why

## Out of Scope

- Rate limiting non-auth API endpoints (separate decision; lower priority)
- CAPTCHA integration (complementary but separate story)
- Account lockout after N failed attempts (separate from rate limiting; consider for future story)
- Geographic IP blocking (Azure Front Door WAF concern, not application code)
- Rate limiting the MSEL or inject APIs (exercise conduct must remain responsive)

## Dependencies

- Deployment architecture decision: App Service only vs. Azure Front Door in front of App Service
- _cross-cutting/S01 (Session Management) — understand token refresh patterns before rate-limiting refresh endpoint

## Implementation Notes

### Option A: ASP.NET Core Built-In (Preferred for MVP)

Available in .NET 7+ via `Microsoft.AspNetCore.RateLimiting`. No additional packages required for .NET 10.

```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth-login", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromSeconds(60);
        limiterOptions.QueueLimit = 0; // No queue — reject immediately
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// On controller or action
[EnableRateLimiting("auth-login")]
[HttpPost("login")]
public async Task<IActionResult> Login(...)
```

Partition by IP address using `HttpContext.Connection.RemoteIpAddress` as the partition key. Ensure `ForwardedHeaders` middleware is configured so the real client IP is used when running behind a reverse proxy or App Service.

### Option B: Azure Front Door WAF

Configure a custom WAF rule in the Azure Front Door profile to limit requests to `/api/auth/*` by client IP. This approach requires no application code changes but is environment-specific and not testable in unit/integration tests.

### Decision Record (To Be Filled In During Implementation)

> **Decision (TBD):** [ASP.NET Core middleware | Azure Front Door WAF | Both]
> **Rationale:** [Explain based on deployment architecture]
> **Implementation Date:** [TBD]

### Retry-After Header

The built-in ASP.NET Core rate limiter does not set `Retry-After` automatically. Add an `OnRejected` callback:

```csharp
options.OnRejected = async (context, token) =>
{
    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
    {
        context.HttpContext.Response.Headers.RetryAfter =
            ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
    }
    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    await context.HttpContext.Response.WriteAsync(
        "Too many requests. Please try again later.", cancellationToken: token);
};
```

## Domain Terms

| Term | Definition |
|------|------------|
| Rate Limiting | Capping the number of requests a client can make in a time window |
| Credential Stuffing | Automated attack using leaked username/password pairs from other breaches |
| Brute Force | Automated attack trying many passwords against one account |
| 429 Too Many Requests | HTTP status code indicating rate limit exceeded |
| Retry-After | HTTP response header telling the client when to retry |

## Test Scenarios

### Unit / Integration Tests
- Send N+1 requests within the window, assert 429 on N+1
- Send N requests within window, assert 200 on request N
- Wait for window to expire, assert next request succeeds
- Verify `Retry-After` header is present on 429 responses

### Manual / Exploratory Tests
- Normal login flow: no 429 encountered
- Simulate attacker with looping curl — confirm throttling kicks in

---

## INVEST Checklist

- [x] **I**ndependent - Rate limiting is self-contained middleware; no feature dependencies
- [x] **N**egotiable - Limits (10/minute, 5/minute) are configurable and can be tuned
- [x] **V**aluable - Protects exercise participant accounts from automated attacks
- [x] **E**stimable - ~5 points (implementation + tests + deployment decision)
- [x] **S**mall - Scoped to auth endpoints only
- [x] **T**estable - Rate limit behavior is deterministic and testable

---

*Related Stories*: [S01 Session Management](./S01-session-management.md), [S09 Multi-Tenant Integration Tests](./S09-multi-tenant-integration-tests.md)

*Last updated: 2026-03-09*
