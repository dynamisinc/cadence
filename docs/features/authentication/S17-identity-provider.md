# S17: Identity Provider Implementation

## Story

**As a** developer,
**I want** a complete Identity provider implementation,
**So that** Cadence works with local authentication for MVP.

## Context

The Identity provider implements `IAuthenticationProvider` using ASP.NET Core Identity. This is the primary authentication mechanism for MVP and serves as the reference implementation for future providers.

## Acceptance Criteria

- [ ] **Given** the interface, **when** I implement IdentityAuthProvider, **then** all methods are fully functional
- [ ] **Given** a valid login, **when** AuthenticateAsync is called, **then** tokens are returned
- [ ] **Given** a valid registration, **when** RegisterAsync is called, **then** user is created and tokens returned
- [ ] **Given** a valid refresh token, **when** RefreshTokenAsync is called, **then** new access token is returned
- [ ] **Given** logout is called, **when** RevokeTokensAsync executes, **then** refresh tokens are invalidated
- [ ] **Given** the provider, **when** I check capabilities, **then** SupportsRegistration and SupportsPasswordAuth are true

## Out of Scope

- Entra implementation (separate story)
- Email verification
- Password reset flow

## Dependencies

- S16 (Interface Design)
- ASP.NET Core Identity NuGet packages

## Implementation

```csharp
/// <summary>
/// Authentication provider using ASP.NET Core Identity.
/// Stores users locally in the application database.
/// </summary>
public class IdentityAuthenticationProvider : IAuthenticationProvider
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ILogger<IdentityAuthenticationProvider> _logger;

    public string ProviderType => "Identity";
    public bool SupportsRegistration => true;
    public bool SupportsPasswordAuth => true;

    public IdentityAuthenticationProvider(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IRefreshTokenStore refreshTokenStore,
        ILogger<IdentityAuthenticationProvider> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _refreshTokenStore = refreshTokenStore;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
            return AuthResult.Failure(AuthError.InvalidCredentials);
        }

        // Check if deactivated
        if (user.Status == UserStatus.Deactivated)
        {
            _logger.LogWarning("Login attempt for deactivated user: {UserId}", user.Id);
            return AuthResult.Failure(new AuthError 
            { 
                Code = "account_deactivated", 
                Message = "Account deactivated. Contact administrator." 
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            _logger.LogWarning("Account locked: {UserId}", user.Id);
            return AuthResult.Failure(AuthError.AccountLocked(lockoutEnd?.DateTime ?? DateTime.UtcNow));
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login for: {UserId}", user.Id);
            return AuthResult.Failure(AuthError.InvalidCredentials);
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Successful login: {UserId}", user.Id);
        return await GenerateTokensAsync(user, request.RememberMe);
    }

    public async Task<AuthResult> RegisterAsync(RegistrationRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return AuthResult.Failure(AuthError.DuplicateEmail);
        }

        // First user becomes Administrator
        var isFirstUser = !await _userManager.Users.AnyAsync();
        var defaultRole = isFirstUser ? Roles.Administrator : Roles.Observer;

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            DisplayName = request.DisplayName,
            Role = defaultRole,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(
                e => e.Code,
                e => new[] { e.Description }
            );
            return AuthResult.Failure(new AuthError
            {
                Code = "registration_failed",
                Message = "Registration failed",
                ValidationErrors = errors
            });
        }

        _logger.LogInformation("User registered: {UserId}, Role: {Role}, IsFirstUser: {IsFirst}", 
            user.Id, defaultRole, isFirstUser);
            
        return await GenerateTokensAsync(user, rememberMe: false);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var stored = await _refreshTokenStore.GetAsync(refreshToken);
        if (stored == null || stored.ExpiresAt < DateTime.UtcNow || stored.IsRevoked)
        {
            return AuthResult.Failure(AuthError.InvalidToken);
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user == null || user.Status == UserStatus.Deactivated)
        {
            return AuthResult.Failure(AuthError.InvalidToken);
        }

        // Revoke old refresh token (rotation)
        await _refreshTokenStore.RevokeAsync(refreshToken);

        return await GenerateTokensAsync(user, stored.RememberMe);
    }

    public async Task RevokeTokensAsync(Guid userId)
    {
        await _refreshTokenStore.RevokeAllForUserAsync(userId);
        _logger.LogInformation("All tokens revoked for: {UserId}", userId);
    }

    public async Task<UserInfo?> GetUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        return new UserInfo
        {
            Id = Guid.Parse(user.Id),
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    public Task<TokenClaims?> ValidateTokenAsync(string accessToken)
    {
        return Task.FromResult(_tokenService.ValidateToken(accessToken));
    }

    private async Task<AuthResult> GenerateTokensAsync(ApplicationUser user, bool rememberMe)
    {
        var userInfo = new UserInfo
        {
            Id = Guid.Parse(user.Id),
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
        
        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(userInfo);
        var refreshToken = await _refreshTokenStore.CreateAsync(
            Guid.Parse(user.Id),
            rememberMe
        );

        return AuthResult.Success(userInfo, accessToken, refreshToken, expiresIn);
    }
}
```

## DI Registration

```csharp
// In Program.cs or extension method
public static IServiceCollection AddIdentityAuthentication(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Configure Identity
    services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        var identityConfig = configuration.GetSection("Authentication:Identity");
        options.Password.RequireDigit = identityConfig.GetValue<bool>("PasswordRequireDigit");
        options.Password.RequireUppercase = identityConfig.GetValue<bool>("PasswordRequireUppercase");
        options.Password.RequiredLength = identityConfig.GetValue<int>("PasswordMinLength");
        options.Lockout.MaxFailedAccessAttempts = identityConfig.GetValue<int>("LockoutMaxAttempts");
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityConfig.GetValue<int>("LockoutMinutes"));
    })
    .AddEntityFrameworkStores<CadenceDbContext>()
    .AddDefaultTokenProviders();
    
    // Register provider
    services.AddScoped<IAuthenticationProvider, IdentityAuthenticationProvider>();
    services.AddScoped<ITokenService, JwtTokenService>();
    services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();
    
    return services;
}
```

## Technical Notes

- `ApplicationUser` extends `IdentityUser` with `DisplayName`, `Role`, `Status`, `LastLoginAt`
- `IRefreshTokenStore` stores refresh tokens in database
- `ITokenService` handles JWT generation/validation
- Use transactions for first-user check to prevent race conditions

---

*Story created: 2025-01-21*
