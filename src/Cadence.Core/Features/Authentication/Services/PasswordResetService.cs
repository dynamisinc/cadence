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
/// Handles password reset flows extracted from <see cref="AuthenticationService"/>.
/// Responsible for issuing reset tokens, sending reset emails, and completing
/// the reset — including auto-login after a successful password change.
/// </summary>
public class PasswordResetService : IPasswordResetService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly AppDbContext _context;
    private readonly AuthenticationOptions _options;
    private readonly ILogger<PasswordResetService> _logger;
    private readonly IEmailService? _emailService;

    /// <summary>
    /// Initializes a new instance of <see cref="PasswordResetService"/>.
    /// </summary>
    public PasswordResetService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenStore refreshTokenStore,
        AppDbContext context,
        IOptions<AuthenticationOptions> options,
        ILogger<PasswordResetService> logger,
        IEmailService? emailService = null)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenStore = refreshTokenStore;
        _context = context;
        _options = options.Value;
        _logger = logger;
        _emailService = emailService;
    }

    /// <inheritdoc />
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

        // Construct the frontend reset URL and send email
        var resetUrl = $"{_options.FrontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";

        if (_emailService != null)
        {
            await _emailService.SendPasswordResetEmailAsync(
                email,
                user.DisplayName,
                resetUrl);
        }
        else
        {
            // Fallback: log the token when email service is not configured
            _logger.LogWarning(
                "Password reset token generated for {UserId} but no email service configured. Token: {Token}",
                user.Id, token);
        }

        return true;
    }

    /// <inheritdoc />
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
        var authResult = await GenerateAuthResponseAsync(
            user,
            rememberMe: false,
            isFirstUser: false,
            isNewAccount: false,
            ipAddress,
            deviceInfo);

        // Send password changed confirmation email (fire-and-forget, after DbContext work is done)
        if (_emailService != null)
        {
            var resetPasswordUrl = $"{_options.FrontendBaseUrl}/forgot-password";
            var supportUrl = $"{_options.FrontendBaseUrl}/support";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordChangedEmailAsync(
                        user.Email!,
                        user.DisplayName,
                        "Password reset",
                        resetPasswordUrl,
                        supportUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password changed email for {UserId}", user.Id);
                }
            });
        }

        return authResult;
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Generate authentication response with JWT tokens.
    /// Mirrors the same logic in <see cref="AuthenticationService"/> to produce
    /// the auto-login response after a successful password reset.
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

        // Get organization context if user has a current organization
        Guid? orgId = null;
        string? orgName = null;
        string? orgSlug = null;
        string? orgRole = null;

        if (user.CurrentOrganizationId.HasValue)
        {
            var org = await _context.Set<Organization>()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == user.CurrentOrganizationId.Value && !o.IsDeleted);

            if (org != null)
            {
                orgId = org.Id;
                orgName = org.Name;
                orgSlug = org.Slug;

                // Get user's role in this organization.
                // IgnoreQueryFilters: during login/token refresh the JWT org context
                // doesn't match yet, so the org-scoped filter would hide the membership.
                var membership = await _context.Set<OrganizationMembership>()
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m =>
                        m.UserId == user.Id &&
                        m.OrganizationId == org.Id &&
                        m.Status == MembershipStatus.Active &&
                        !m.IsDeleted);

                if (membership != null)
                {
                    orgRole = membership.Role.ToString();
                }
                else if (user.SystemRole == SystemRole.Admin)
                {
                    // SysAdmins get OrgAdmin access even without membership
                    orgRole = OrgRole.OrgAdmin.ToString();
                }
            }
        }

        var (accessToken, expiresIn) = _tokenService.GenerateAccessToken(userInfo, orgId, orgName, orgSlug, orgRole);
        var refreshTokenResult = await _refreshTokenStore.CreateAsync(
            user.Id,
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
