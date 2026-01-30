using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Orchestrates authentication across multiple providers (local Identity, Azure Entra, etc.).
/// All authentication flows ultimately issue Cadence JWTs regardless of original auth method.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly AppDbContext _context;
    private readonly AuthenticationOptions _options;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenStore refreshTokenStore,
        AppDbContext context,
        IOptions<AuthenticationOptions> options,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenStore = refreshTokenStore;
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate with email/password via Identity provider.
    /// Issues Cadence JWT on success.
    /// </summary>
    public async Task<AuthResponse> AuthenticateWithPasswordAsync(
        LoginRequest request,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        if (!_options.Identity.Enabled)
        {
            _logger.LogWarning("Identity provider disabled but login attempted");
            return AuthResponse.Failure(AuthError.ProviderDisabled);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
            return AuthResponse.Failure(AuthError.InvalidCredentials);
        }

        // Check if deactivated
        if (user.Status == UserStatus.Disabled)
        {
            _logger.LogWarning("Login attempt for deactivated user: {UserId}", user.Id);
            return AuthResponse.Failure(AuthError.AccountDeactivated);
        }

        // Check lockout status first
        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var minutesRemaining = lockoutEnd.HasValue
                ? (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)
                : _options.Identity.LockoutMinutes;

            _logger.LogWarning("Login attempt for locked account: {UserId}", user.Id);
            return AuthResponse.Failure(AuthError.AccountLocked(
                lockoutEnd?.UtcDateTime ?? DateTime.UtcNow.AddMinutes(_options.Identity.LockoutMinutes),
                minutesRemaining));
        }

        // Validate password
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            // Increment failed access count
            await _userManager.AccessFailedAsync(user);

            // Check if now locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var minutesRemaining = lockoutEnd.HasValue
                    ? (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)
                    : _options.Identity.LockoutMinutes;

                _logger.LogWarning("Account locked after failed attempt: {UserId}", user.Id);
                return AuthResponse.Failure(AuthError.AccountLocked(
                    lockoutEnd?.UtcDateTime ?? DateTime.UtcNow.AddMinutes(_options.Identity.LockoutMinutes),
                    minutesRemaining));
            }

            // Get remaining attempts
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var attemptsRemaining = _options.Identity.LockoutMaxAttempts - failedCount;

            _logger.LogWarning(
                "Failed login attempt for: {UserId}, Attempts remaining: {AttemptsRemaining}",
                user.Id, attemptsRemaining);

            var error = AuthError.InvalidCredentials;
            return AuthResponse.Failure(new AuthError
            {
                Code = error.Code,
                Message = error.Message,
                AttemptsRemaining = attemptsRemaining
            });
        }

        // Reset failed access count on successful login
        await _userManager.ResetAccessFailedCountAsync(user);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Successful login: {UserId}", user.Id);

        return await GenerateAuthResponseAsync(
            user,
            request.RememberMe,
            isFirstUser: false,
            isNewAccount: false,
            ipAddress,
            deviceInfo);
    }

    /// <summary>
    /// Complete authentication from external OAuth callback (e.g., Azure Entra).
    /// Creates or links local account as needed, then issues Cadence JWT.
    /// </summary>
    public Task<AuthResponse> AuthenticateWithExternalAsync(
        ExternalAuthRequest request,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        // External authentication not yet implemented
        _logger.LogWarning("External authentication not yet enabled: {Provider}", request.Provider);
        return Task.FromResult(AuthResponse.Failure(
            AuthError.ExternalAuthFailed("External authentication is not yet enabled")));
    }

    /// <summary>
    /// Register a new local account (Identity provider only).
    /// Auto-authenticates on success, returning JWT tokens.
    /// First user becomes Administrator, subsequent users become Observer role.
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(
        RegistrationRequest request,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        if (!_options.Identity.Enabled)
        {
            _logger.LogWarning("Identity provider disabled but registration attempted");
            return AuthResponse.Failure(AuthError.ProviderDisabled);
        }

        if (!_options.Identity.AllowRegistration)
        {
            _logger.LogWarning("Registration disabled but registration attempted");
            return AuthResponse.Failure(new AuthError
            {
                Code = "registration_disabled",
                Message = "User registration is not currently enabled"
            });
        }

        // Check for duplicate email
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
            return AuthResponse.Failure(AuthError.DuplicateEmail);
        }

        // Use execution strategy for transactions with retry logic
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if first user (gets Admin role, subsequent users get User role)
                var isFirstUser = !await _userManager.Users.AnyAsync();
                var defaultSystemRole = isFirstUser ? SystemRole.Admin : SystemRole.User;

                // Get or create default organization
                // For MVP, all users belong to a single default organization
                var organization = await _context.Organizations.FirstOrDefaultAsync();
                if (organization == null)
                {
                    organization = new Organization
                    {
                        Id = Guid.NewGuid(),
                        Name = "Default Organization",
                        Description = "Default organization for Cadence users",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Organizations.Add(organization);
                    await _context.SaveChangesAsync();
                }

                var user = new ApplicationUser
                {
                    Email = request.Email,
                    UserName = request.Email, // ASP.NET Identity requires UserName
                    DisplayName = request.DisplayName,
                    SystemRole = defaultSystemRole,
                    Status = UserStatus.Active,
                    EmailConfirmed = true, // For MVP, skip email verification
                    OrganizationId = organization.Id
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.ToDictionary(
                        e => ToCamelCase(e.Code),
                        e => new[] { e.Description }
                    );

                    _logger.LogWarning(
                        "Registration failed for {Email}: {Errors}",
                        request.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));

                    return AuthResponse.Failure(AuthError.ValidationFailed(errors));
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "User registered: {UserId}, Email: {Email}, Role: {Role}, IsFirstUser: {IsFirst}",
                    user.Id, user.Email, defaultSystemRole, isFirstUser);

                // Auto-login: generate tokens
                return await GenerateAuthResponseAsync(
                    user,
                    rememberMe: false,
                    isFirstUser,
                    isNewAccount: true,
                    ipAddress,
                    deviceInfo);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during registration for {Email}", request.Email);
                return AuthResponse.Failure(new AuthError
                {
                    Code = "registration_failed",
                    Message = "An error occurred during registration. Please try again."
                });
            }
        });
    }

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// Works regardless of original authentication method.
    /// </summary>
    public async Task<AuthResponse> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null)
    {
        // Hash the token to look it up
        var tokenHash = _tokenService.HashToken(refreshToken);
        var storedToken = await _refreshTokenStore.GetByHashAsync(tokenHash);

        if (storedToken == null || storedToken.IsRevoked)
        {
            _logger.LogWarning("Refresh token invalid or revoked");
            return AuthResponse.Failure(AuthError.InvalidToken);
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token expired: {TokenId}", storedToken.Id);
            return AuthResponse.Failure(AuthError.InvalidToken);
        }

        // Get user
        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found for refresh token: {UserId}", storedToken.UserId);
            return AuthResponse.Failure(AuthError.InvalidToken);
        }

        // Check if user is deactivated
        if (user.Status == UserStatus.Disabled)
        {
            _logger.LogWarning("Refresh token used for deactivated user: {UserId}", user.Id);
            return AuthResponse.Failure(AuthError.AccountDeactivated);
        }

        // Revoke old token (token rotation for security)
        await _refreshTokenStore.RevokeAsync(tokenHash);

        _logger.LogInformation("Refresh token used: {UserId}", user.Id);

        // Use the stored RememberMe value from the original login
        // This preserves the user's original preference across token rotations
        var rememberMe = storedToken.RememberMe;

        // Generate new tokens
        return await GenerateAuthResponseAsync(
            user,
            rememberMe,
            isFirstUser: false,
            isNewAccount: false,
            ipAddress,
            storedToken.DeviceInfo);
    }

    /// <summary>
    /// Revoke all tokens for a user (logout from all devices).
    /// </summary>
    public async Task RevokeTokensAsync(Guid userId)
    {
        await _refreshTokenStore.RevokeAllForUserAsync(userId);
        _logger.LogInformation("All tokens revoked for user: {UserId}", userId);
    }

    /// <summary>
    /// Revoke a specific refresh token (single device logout).
    /// </summary>
    public async Task RevokeTokenAsync(string refreshToken)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);
        await _refreshTokenStore.RevokeAsync(tokenHash);
        _logger.LogInformation("Single refresh token revoked");
    }

    /// <summary>
    /// Get user information by ID.
    /// </summary>
    public async Task<UserInfo?> GetUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        return new UserInfo
        {
            Id = Guid.Parse(user.Id),
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = user.SystemRole.ToString(),
            Status = user.Status.ToString(),
            LastLoginAt = user.LastLoginAt
        };
    }

    /// <summary>
    /// Get all enabled authentication methods.
    /// Used by login UI to show available sign-in options.
    /// </summary>
    public IReadOnlyList<AuthMethod> GetAvailableMethods()
    {
        var methods = new List<AuthMethod>();

        if (_options.Identity.Enabled)
        {
            methods.Add(new AuthMethod
            {
                Provider = "Identity",
                DisplayName = "Email & Password",
                Icon = "envelope",
                IsEnabled = true,
                IsExternal = false
            });
        }

        // Future: Add Entra when enabled
        // if (_options.Entra.Enabled)
        // {
        //     methods.Add(new AuthMethod
        //     {
        //         Provider = "Entra",
        //         DisplayName = "Sign in with Microsoft",
        //         Icon = "microsoft",
        //         IsEnabled = true,
        //         IsExternal = true
        //     });
        // }

        return methods;
    }

    /// <summary>
    /// Get OAuth redirect URL for external provider.
    /// Frontend redirects user to this URL to initiate OAuth flow.
    /// </summary>
    public string GetExternalLoginUrl(string provider, string returnUrl)
    {
        // External authentication not yet implemented
        _logger.LogWarning("External authentication not yet enabled: {Provider}", provider);
        throw new NotSupportedException("External authentication is not yet enabled");
    }

    /// <summary>
    /// Request a password reset for the specified email address.
    /// Always returns success to prevent email enumeration attacks.
    /// </summary>
    public async Task<bool> RequestPasswordResetAsync(string email, string? ipAddress = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal whether email exists - always return success
            _logger.LogInformation("Password reset requested for non-existent email");
            return true;
        }

        // Check if user is deactivated
        if (user.Status == UserStatus.Disabled)
        {
            _logger.LogWarning("Password reset requested for deactivated user: {UserId}", user.Id);
            return true; // Still return success to prevent enumeration
        }

        // Generate password reset token using ASP.NET Identity
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Store hashed token in database
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(token),
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        // TODO: Send email with reset link
        // For MVP, log the token (remove in production!)
        _logger.LogWarning(
            "Password reset token generated for {UserId}: {Token}",
            user.Id, token);

        return true;
    }

    /// <summary>
    /// Complete a password reset using the reset token.
    /// Auto-authenticates the user on success.
    /// </summary>
    public async Task<AuthResponse> ResetPasswordAsync(
        string token,
        string newPassword,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        // Hash the token to look it up
        var tokenHash = _tokenService.HashToken(token);
        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.UsedAt == null);

        if (resetToken == null)
        {
            _logger.LogWarning("Password reset attempted with invalid token");
            return AuthResponse.Failure(AuthError.InvalidToken);
        }

        if (resetToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset attempted with expired token: {TokenId}", resetToken.Id);
            return AuthResponse.Failure(AuthError.InvalidToken);
        }

        // Get the user
        var user = await _userManager.FindByIdAsync(resetToken.UserId);
        if (user == null)
        {
            _logger.LogWarning("User not found for password reset: {UserId}", resetToken.UserId);
            return AuthResponse.Failure(AuthError.InvalidToken);
        }

        // Check if user is deactivated
        if (user.Status == UserStatus.Disabled)
        {
            _logger.LogWarning("Password reset attempted for deactivated user: {UserId}", user.Id);
            return AuthResponse.Failure(AuthError.AccountDeactivated);
        }

        // Reset the password using Identity
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(
                e => ToCamelCase(e.Code),
                e => new[] { e.Description }
            );

            _logger.LogWarning(
                "Password reset failed for {UserId}: {Errors}",
                user.Id,
                string.Join(", ", result.Errors.Select(e => e.Description)));

            return AuthResponse.Failure(AuthError.ValidationFailed(errors));
        }

        // Mark token as used
        resetToken.UsedAt = DateTime.UtcNow;
        resetToken.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Reset lockout if user was locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        _logger.LogInformation("Password reset completed for user: {UserId}", user.Id);

        // Auto-login after password reset
        return await GenerateAuthResponseAsync(
            user,
            rememberMe: false,
            isFirstUser: false,
            isNewAccount: false,
            ipAddress,
            deviceInfo);
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Generate authentication response with JWT tokens.
    /// </summary>
    private async Task<AuthResponse> GenerateAuthResponseAsync(
        ApplicationUser user,
        bool rememberMe,
        bool isFirstUser,
        bool isNewAccount,
        string? ipAddress,
        string? deviceInfo)
    {
        var userInfo = new UserInfo
        {
            Id = Guid.Parse(user.Id),
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = user.SystemRole.ToString(),
            Status = user.Status.ToString(),
            LastLoginAt = user.LastLoginAt
        };

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(userInfo);
        var refreshTokenResult = await _refreshTokenStore.CreateAsync(
            Guid.Parse(user.Id),
            rememberMe,
            ipAddress,
            deviceInfo);

        return AuthResponse.Success(
            userId: Guid.Parse(user.Id),
            displayName: user.DisplayName,
            email: user.Email!,
            role: user.SystemRole.ToString(),
            accessToken: accessToken,
            refreshToken: refreshTokenResult.Token,
            expiresIn: expiresIn,
            status: user.Status.ToString(),
            isNewAccount: isNewAccount,
            isFirstUser: isFirstUser,
            rememberMe: refreshTokenResult.RememberMe,
            refreshTokenExpiresIn: refreshTokenResult.ExpiresIn);
    }

    /// <summary>
    /// Convert PascalCase to camelCase for error field names.
    /// </summary>
    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
