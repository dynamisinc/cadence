# Authentication Testing & Security Recommendations Implementation

> **Purpose**: This document provides complete context and instructions for implementing the security and testing recommendations identified during the authentication feature code review.
> **Priority**: HIGH - These are critical gaps that must be addressed before production deployment.

---

## Executive Summary

The authentication system has been implemented with solid security fundamentals (password hashing, token rotation, HttpOnly cookies, account lockout). However, the code review identified **critical gaps in test coverage** and a few security enhancements needed.

### Priority Matrix

| Priority | Item | Effort | Impact | Status |
| -------- | ---- | ------ | ------ | ------ |
| **CRITICAL** | AuthenticationService unit tests | Medium | High - Core auth logic untested | TODO |
| **CRITICAL** | AuthController integration tests | Medium | High - API endpoints untested | TODO |
| **HIGH** | Frontend auth tests | Medium | Medium - UI/UX validation | TODO |
| **HIGH** | Rate limiting | Low | High - Brute force protection | DONE |
| **MEDIUM** | Single-flight token refresh | Low | Medium - Race condition fix | DONE |
| **MEDIUM** | Password reset implementation | Medium | Medium - Feature completion | DONE |
| **LOW** | Security headers audit | Low | Low - Defense in depth | TODO |

### Already Implemented (as of 2025-01-22)

The following items have been implemented:

1. **Rate Limiting** - Applied to auth endpoints in `Program.cs`:
   - `auth` policy: 10 requests per minute for login/register
   - `password-reset` policy: 3 requests per 15 minutes

2. **Single-Flight Token Refresh** - Implemented in `api.ts`:
   - Uses a shared promise to prevent multiple concurrent refresh requests
   - All concurrent 401 responses wait for the same refresh

3. **Password Reset** - Full implementation:
   - `RequestPasswordResetAsync` in AuthenticationService
   - `ResetPasswordAsync` in AuthenticationService
   - REST endpoints in AuthController (`/password-reset/request`, `/password-reset/complete`)

---

## Phase 1: AuthenticationService Unit Tests (CRITICAL)

### File to Create
`src/Cadence.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs`

### Test Cases Required

Based on the code in `AuthenticationService.cs`, implement these test scenarios:

#### Registration Tests
```csharp
// Test: RegisterAsync_FirstUser_AssignsAdministratorRole
// Given: No users exist in the database
// When: A user registers
// Then: They are assigned the "Administrator" global role

// Test: RegisterAsync_SubsequentUser_AssignsObserverRole
// Given: At least one user exists
// When: A new user registers
// Then: They are assigned the "Observer" global role

// Test: RegisterAsync_DuplicateEmail_ReturnsDuplicateEmailError
// Given: A user with email "test@example.com" exists
// When: Registration is attempted with the same email
// Then: Returns AuthError.DuplicateEmail

// Test: RegisterAsync_IdentityDisabled_ReturnsProviderDisabled
// Given: Identity provider is disabled in configuration
// When: Registration is attempted
// Then: Returns AuthError.ProviderDisabled

// Test: RegisterAsync_RegistrationDisabled_ReturnsRegistrationDisabled
// Given: AllowRegistration is false in configuration
// When: Registration is attempted
// Then: Returns error with code "registration_disabled"

// Test: RegisterAsync_WeakPassword_ReturnsValidationErrors
// Given: Password doesn't meet Identity requirements
// When: Registration is attempted
// Then: Returns validation errors from Identity

// Test: RegisterAsync_Success_CreatesDefaultOrganization
// Given: No organization exists
// When: First user registers
// Then: A "Default Organization" is created and user is assigned to it

// Test: RegisterAsync_Success_ReturnsTokens
// Given: Valid registration request
// When: Registration succeeds
// Then: Returns access token, refresh token, and user info
```

#### Login Tests
```csharp
// Test: AuthenticateWithPasswordAsync_ValidCredentials_ReturnsTokens
// Given: A user exists with email/password
// When: Login with correct credentials
// Then: Returns access token, refresh token, user info

// Test: AuthenticateWithPasswordAsync_InvalidEmail_ReturnsInvalidCredentials
// Given: No user exists with the email
// When: Login is attempted
// Then: Returns AuthError.InvalidCredentials

// Test: AuthenticateWithPasswordAsync_InvalidPassword_ReturnsInvalidCredentials
// Given: User exists but password is wrong
// When: Login is attempted
// Then: Returns AuthError.InvalidCredentials with attempts remaining

// Test: AuthenticateWithPasswordAsync_DeactivatedUser_ReturnsAccountDeactivated
// Given: User exists but Status = Deactivated
// When: Login is attempted
// Then: Returns AuthError.AccountDeactivated

// Test: AuthenticateWithPasswordAsync_LockedAccount_ReturnsAccountLocked
// Given: User is currently locked out
// When: Login is attempted
// Then: Returns AuthError.AccountLocked with unlock time

// Test: AuthenticateWithPasswordAsync_FailedAttempts_LocksAccount
// Given: User has 4 failed attempts (one less than max)
// When: Login fails again
// Then: Account is locked and returns AccountLocked error

// Test: AuthenticateWithPasswordAsync_IdentityDisabled_ReturnsProviderDisabled
// Given: Identity provider is disabled
// When: Login is attempted
// Then: Returns AuthError.ProviderDisabled

// Test: AuthenticateWithPasswordAsync_Success_ResetsFailedCount
// Given: User has some failed attempts
// When: Login succeeds
// Then: Failed access count is reset to 0

// Test: AuthenticateWithPasswordAsync_Success_UpdatesLastLoginAt
// Given: User exists
// When: Login succeeds
// Then: LastLoginAt is updated to current time

// Test: AuthenticateWithPasswordAsync_RememberMe_ExtendsRefreshTokenExpiry
// Given: Login request with RememberMe = true
// When: Login succeeds
// Then: Refresh token expires in 30 days (not 4 hours)
```

#### Token Refresh Tests
```csharp
// Test: RefreshTokenAsync_ValidToken_ReturnsNewTokens
// Given: Valid refresh token exists
// When: Refresh is called
// Then: Returns new access token and new refresh token

// Test: RefreshTokenAsync_InvalidToken_ReturnsInvalidToken
// Given: Token doesn't exist in database
// When: Refresh is called
// Then: Returns AuthError.InvalidToken

// Test: RefreshTokenAsync_RevokedToken_ReturnsInvalidToken
// Given: Token exists but is revoked
// When: Refresh is called
// Then: Returns AuthError.InvalidToken

// Test: RefreshTokenAsync_ExpiredToken_ReturnsInvalidToken
// Given: Token exists but is expired
// When: Refresh is called
// Then: Returns AuthError.InvalidToken

// Test: RefreshTokenAsync_DeactivatedUser_ReturnsAccountDeactivated
// Given: Valid token but user is deactivated
// When: Refresh is called
// Then: Returns AuthError.AccountDeactivated

// Test: RefreshTokenAsync_Success_RevokesOldToken
// Given: Valid refresh token
// When: Refresh succeeds
// Then: Old token is revoked (token rotation)

// Test: RefreshTokenAsync_PreservesRememberMe_BasedOnTokenLifetime
// Given: Original token had 30-day expiry (RememberMe)
// When: Refresh is called
// Then: New token also has extended expiry
```

#### Logout Tests
```csharp
// Test: RevokeTokenAsync_ValidToken_RevokesIt
// Given: Valid refresh token
// When: RevokeTokenAsync is called
// Then: Token is marked as revoked

// Test: RevokeTokensAsync_RevokesAllUserTokens
// Given: User has multiple refresh tokens
// When: RevokeTokensAsync is called
// Then: All tokens are revoked
```

### Test Setup Requirements

```csharp
// Use these mocks/fakes:
// - InMemory database for AppDbContext
// - Mock<UserManager<ApplicationUser>> with proper setup
// - Real JwtTokenService (already tested separately)
// - InMemory RefreshTokenStore
// - IOptions<AuthenticationOptions> with configurable values

// Example test setup:
public class AuthenticationServiceTests : IAsyncLifetime
{
    private AppDbContext _context;
    private Mock<UserManager<ApplicationUser>> _userManager;
    private ITokenService _tokenService;
    private IRefreshTokenStore _refreshTokenStore;
    private AuthenticationService _sut;
    private AuthenticationOptions _options;

    public async Task InitializeAsync()
    {
        // Setup InMemory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Setup UserManager mock
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        // Setup real token services
        var jwtOptions = Options.Create(new JwtOptions { /* ... */ });
        _tokenService = new JwtTokenService(jwtOptions);
        _refreshTokenStore = new RefreshTokenStore(_context, _tokenService, Options.Create(_options));

        // Setup auth options
        _options = new AuthenticationOptions
        {
            Identity = new IdentityProviderOptions
            {
                Enabled = true,
                AllowRegistration = true,
                LockoutMaxAttempts = 5,
                LockoutMinutes = 15
            }
        };

        _sut = new AuthenticationService(
            _userManager.Object,
            _tokenService,
            _refreshTokenStore,
            _context,
            Options.Create(_options),
            Mock.Of<ILogger<AuthenticationService>>()
        );
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
```

---

## Phase 2: AuthController Integration Tests (CRITICAL)

### File to Create
`src/Cadence.WebApi.Tests/Controllers/AuthControllerIntegrationTests.cs`

### Test Cases Required

```csharp
// Test: POST_Register_ValidRequest_Returns201WithTokens
// Given: Valid registration request
// When: POST /api/auth/register
// Then: Returns 201 with AuthResponseDto and sets refresh token cookie

// Test: POST_Register_InvalidRequest_Returns400WithValidationErrors
// Given: Invalid registration (missing fields)
// When: POST /api/auth/register
// Then: Returns 400 with validation errors

// Test: POST_Register_DuplicateEmail_Returns409
// Given: Email already registered
// When: POST /api/auth/register
// Then: Returns 409 Conflict

// Test: POST_Login_ValidCredentials_Returns200WithTokens
// Given: Registered user
// When: POST /api/auth/login with correct credentials
// Then: Returns 200 with tokens and sets HttpOnly cookie

// Test: POST_Login_InvalidCredentials_Returns401
// Given: Registered user
// When: POST /api/auth/login with wrong password
// Then: Returns 401 with InvalidCredentials error

// Test: POST_Login_LockedAccount_Returns423
// Given: User account is locked
// When: POST /api/auth/login
// Then: Returns 423 Locked with unlock time

// Test: POST_Refresh_ValidCookie_Returns200WithNewTokens
// Given: Valid refresh token in HttpOnly cookie
// When: POST /api/auth/refresh
// Then: Returns 200 with new tokens

// Test: POST_Refresh_MissingCookie_Returns401
// Given: No refresh token cookie
// When: POST /api/auth/refresh
// Then: Returns 401

// Test: POST_Logout_ValidToken_Returns200AndClearsCookie
// Given: Authenticated user with refresh token
// When: POST /api/auth/logout
// Then: Returns 200 and clears refresh_token cookie

// Test: GET_Methods_ReturnsAvailableAuthMethods
// Given: Identity provider enabled
// When: GET /api/auth/methods
// Then: Returns list including "Identity" method

// Test: GET_Me_Authenticated_ReturnsUserInfo
// Given: Valid access token in Authorization header
// When: GET /api/auth/me
// Then: Returns current user info

// Test: GET_Me_Unauthenticated_Returns401
// Given: No Authorization header
// When: GET /api/auth/me
// Then: Returns 401
```

### Test Infrastructure Required

```csharp
// Create WebApplicationFactory for integration tests
public class CadenceWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace database with InMemory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    }
}

// Base class for auth integration tests
public class AuthControllerIntegrationTests : IClassFixture<CadenceWebApplicationFactory>
{
    private readonly CadenceWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(CadenceWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // Helper to register and get tokens
    protected async Task<(string AccessToken, string RefreshToken)> RegisterUserAsync(
        string email = "test@example.com",
        string password = "Test123!@#")
    {
        // Implementation
    }
}
```

---

## Phase 3: Frontend Auth Tests (HIGH)

### Files to Create

1. `src/frontend/src/contexts/AuthContext.test.tsx`
2. `src/frontend/src/features/auth/pages/LoginPage.test.tsx`
3. `src/frontend/src/features/auth/pages/RegisterPage.test.tsx`

### AuthContext Tests

```typescript
// src/frontend/src/contexts/AuthContext.test.tsx

describe('AuthContext', () => {
  describe('login', () => {
    it('stores access token in memory after successful login');
    it('updates user state with returned user info');
    it('stores return URL in sessionStorage if provided');
    it('handles login failure with correct error state');
    it('clears previous errors on new login attempt');
  });

  describe('logout', () => {
    it('clears access token from memory');
    it('clears user state');
    it('calls logout endpoint');
    it('broadcasts logout event to other tabs');
  });

  describe('token refresh', () => {
    it('automatically refreshes token before expiry');
    it('updates access token after successful refresh');
    it('logs out user if refresh fails');
    it('handles concurrent refresh requests (single-flight)');
  });

  describe('cross-tab synchronization', () => {
    it('logs out all tabs when one tab logs out');
    it('syncs login state across tabs');
  });

  describe('initialization', () => {
    it('attempts token refresh on mount');
    it('sets loading state during initialization');
    it('handles initialization failure gracefully');
  });
});
```

### LoginPage Tests

```typescript
// src/frontend/src/features/auth/pages/LoginPage.test.tsx

describe('LoginPage', () => {
  it('renders email and password fields');
  it('renders remember me checkbox');
  it('renders login button');
  it('shows validation errors for empty fields');
  it('shows validation error for invalid email format');
  it('disables login button while submitting');
  it('shows loading indicator while submitting');
  it('displays error message on login failure');
  it('displays attempts remaining on invalid credentials');
  it('displays lockout message when account is locked');
  it('redirects to dashboard on successful login');
  it('redirects to returnUrl if present');
  it('shows session expired message when expired=true in URL');
  it('navigates to register page when link clicked');
  it('navigates to forgot password when link clicked');
});
```

### RegisterPage Tests

```typescript
// src/frontend/src/features/auth/pages/RegisterPage.test.tsx

describe('RegisterPage', () => {
  it('renders all required fields');
  it('shows validation errors for empty fields');
  it('shows error when passwords do not match');
  it('shows password strength requirements');
  it('disables register button while submitting');
  it('displays server validation errors');
  it('displays duplicate email error');
  it('redirects to dashboard on successful registration');
  it('shows first user welcome message when isFirstUser=true');
  it('navigates to login page when link clicked');
});
```

---

## Phase 4: Rate Limiting (HIGH)

### Implementation Location
`src/Cadence.WebApi/Program.cs` - Add rate limiting configuration

### Required Changes

```csharp
// In Program.cs, add rate limiting services
builder.Services.AddRateLimiter(options =>
{
    // Strict rate limit for auth endpoints
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10; // 10 requests per minute
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Even stricter for password reset
    options.AddFixedWindowLimiter("password-reset", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(15);
        opt.PermitLimit = 3; // 3 requests per 15 minutes
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "rate_limit_exceeded",
            message = "Too many requests. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.TotalSeconds
                : 60
        });
    };
});

// In middleware pipeline
app.UseRateLimiter();

// In AuthController, apply rate limiters
[HttpPost("login")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Login([FromBody] LoginRequest request) { }

[HttpPost("register")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Register([FromBody] RegistrationRequest request) { }

[HttpPost("forgot-password")]
[EnableRateLimiting("password-reset")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request) { }
```

### Test Cases for Rate Limiting

```csharp
// Test: Login_ExceedsRateLimit_Returns429
// Given: 10 login attempts in the last minute
// When: 11th login attempt is made
// Then: Returns 429 with retry-after header

// Test: PasswordReset_ExceedsRateLimit_Returns429
// Given: 3 password reset requests in the last 15 minutes
// When: 4th request is made
// Then: Returns 429
```

---

## Phase 5: Single-Flight Token Refresh (MEDIUM)

### File to Modify
`src/frontend/src/core/services/api.ts`

### Current Issue
Multiple concurrent 401 responses can trigger multiple refresh attempts, causing race conditions.

### Solution

```typescript
// Add at module level
let refreshPromise: Promise<void> | null = null;

// Modify response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest?._retry && refreshAccessToken) {
      originalRequest._retry = true;

      try {
        // Single-flight: reuse existing refresh promise if one is in progress
        if (!refreshPromise) {
          refreshPromise = refreshAccessToken().finally(() => {
            refreshPromise = null;
          });
        }

        await refreshPromise;

        // Retry original request with new token
        const token = getAccessToken?.();
        if (token && originalRequest) {
          originalRequest.headers = originalRequest.headers || {};
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        }
      } catch (refreshError) {
        // Refresh failed - redirect to login
        const returnUrl = window.location.pathname;
        if (returnUrl !== '/login' && returnUrl !== '/register') {
          sessionStorage.setItem('returnUrl', returnUrl);
        }
        window.location.href = '/login?expired=true';
        return Promise.reject(error);
      }
    }

    console.error('API Error:', error.response?.data || error.message);
    return Promise.reject(error);
  }
);
```

### Test Cases

```typescript
// Test: concurrent 401 responses use single refresh
// Given: Two API calls that both return 401
// When: Both trigger token refresh
// Then: Only one refresh request is made to the server
```

---

## Phase 6: Password Reset Implementation (MEDIUM)

### Files to Modify/Create

1. `src/Cadence.Core/Features/Authentication/Services/IAuthenticationService.cs` - Add methods
2. `src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs` - Implement methods
3. `src/Cadence.WebApi/Controllers/AuthController.cs` - Implement endpoints
4. `src/frontend/src/features/auth/pages/ForgotPasswordPage.tsx` - Complete UI
5. `src/frontend/src/features/auth/pages/ResetPasswordPage.tsx` - Complete UI

### Backend Implementation

```csharp
// IAuthenticationService - Add methods
Task<Result> RequestPasswordResetAsync(string email);
Task<AuthResponse> ResetPasswordAsync(string token, string newPassword);

// AuthenticationService - Implement
public async Task<Result> RequestPasswordResetAsync(string email)
{
    var user = await _userManager.FindByEmailAsync(email);
    if (user == null)
    {
        // Don't reveal whether email exists
        _logger.LogInformation("Password reset requested for non-existent email");
        return Result.Success(); // Still return success
    }

    // Generate reset token
    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

    // Store token with expiry (1 hour)
    var resetToken = new PasswordResetToken
    {
        UserId = Guid.Parse(user.Id),
        TokenHash = _tokenService.HashToken(token),
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        CreatedAt = DateTime.UtcNow
    };
    _context.PasswordResetTokens.Add(resetToken);
    await _context.SaveChangesAsync();

    // TODO: Send email with reset link
    // For MVP, log the token (remove in production!)
    _logger.LogWarning("Password reset token generated for {UserId}: {Token}", user.Id, token);

    return Result.Success();
}

public async Task<AuthResponse> ResetPasswordAsync(string token, string newPassword)
{
    // Find token by hash
    var tokenHash = _tokenService.HashToken(token);
    var resetToken = await _context.PasswordResetTokens
        .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsUsed);

    if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
    {
        return AuthResponse.Failure(AuthError.InvalidToken);
    }

    var user = await _userManager.FindByIdAsync(resetToken.UserId.ToString());
    if (user == null)
    {
        return AuthResponse.Failure(AuthError.InvalidToken);
    }

    // Reset password
    var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
    if (!result.Succeeded)
    {
        var errors = result.Errors.ToDictionary(
            e => ToCamelCase(e.Code),
            e => new[] { e.Description }
        );
        return AuthResponse.Failure(AuthError.ValidationFailed(errors));
    }

    // Mark token as used
    resetToken.IsUsed = true;
    resetToken.UsedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    // Auto-login after password reset
    return await GenerateAuthResponseAsync(user, false, false, false, null, null);
}

// AuthController endpoints
[HttpPost("forgot-password")]
[EnableRateLimiting("password-reset")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    await _authService.RequestPasswordResetAsync(request.Email);
    return Ok(new { message = "If an account exists with that email, a reset link has been sent." });
}

[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
    if (!result.IsSuccess)
    {
        return result.Error!.Code switch
        {
            "invalid_token" => Unauthorized(result.Error),
            "validation_failed" => BadRequest(result.Error),
            _ => BadRequest(result.Error)
        };
    }

    SetRefreshTokenCookie(result.RefreshToken!, result.User!.Id);
    return Ok(result.ToDto());
}
```

---

## Execution Order

1. **Phase 1**: AuthenticationService Unit Tests (backend-agent + testing-agent)
2. **Phase 2**: AuthController Integration Tests (backend-agent + testing-agent)
3. **Phase 3**: Frontend Auth Tests (frontend-agent + testing-agent)
4. **Phase 4**: Rate Limiting (backend-agent)
5. **Phase 5**: Single-Flight Token Refresh (frontend-agent)
6. **Phase 6**: Password Reset (orchestrator - spans backend + frontend)

### Verification Commands

```bash
# Backend tests
cd src/Cadence.Core.Tests && dotnet test --filter "Authentication"

# Integration tests (create project first)
cd src/Cadence.WebApi.Tests && dotnet test --filter "AuthController"

# Frontend tests
cd src/frontend && npm test -- --grep "Auth"

# Full test suite
cd src && dotnet test
cd src/frontend && npm test
```

---

## Success Criteria

- [ ] All AuthenticationService scenarios have unit tests (100% code coverage of auth paths)
- [ ] All AuthController endpoints have integration tests
- [ ] AuthContext has unit tests for all major flows
- [ ] LoginPage and RegisterPage have component tests
- [ ] Rate limiting is applied to auth endpoints
- [ ] Token refresh uses single-flight pattern
- [ ] Password reset is fully functional
- [ ] All tests pass in CI

---

## Reference Files

### Existing Code to Read First
1. `src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs` - Main service
2. `src/Cadence.WebApi/Controllers/AuthController.cs` - API endpoints
3. `src/frontend/src/contexts/AuthContext.tsx` - React auth context
4. `src/frontend/src/core/services/api.ts` - Axios configuration

### Existing Tests to Follow Patterns From
1. `src/Cadence.Core.Tests/Features/Authentication/JwtTokenServiceTests.cs`
2. `src/Cadence.Core.Tests/Features/Authentication/RefreshTokenStoreTests.cs`
3. `src/frontend/src/features/exercises/*.test.tsx` - Frontend test patterns

### Configuration Files
1. `src/Cadence.WebApi/appsettings.json` - Auth configuration structure
2. `src/Cadence.WebApi/Program.cs` - Service registration

---

*Document created: 2025-01-22*
*Based on security code review of authentication implementation*
