using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Service for refresh token persistence and management.
/// </summary>
public class RefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _options;
    private readonly ILogger<RefreshTokenStore> _logger;

    public RefreshTokenStore(
        AppDbContext context,
        ITokenService tokenService,
        IOptions<JwtOptions> options,
        ILogger<RefreshTokenStore> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create and store a new refresh token for a user.
    /// </summary>
    /// <param name="userId">User who owns this token.</param>
    /// <param name="rememberMe">If true, token expires in 30 days; otherwise 4 hours.</param>
    /// <param name="ipAddress">IP address where token was issued (for audit).</param>
    /// <param name="deviceInfo">Device/user agent string (for audit).</param>
    /// <returns>The unhashed refresh token string (to be sent to client).</returns>
    public async Task<string> CreateAsync(
        Guid userId,
        bool rememberMe,
        string? ipAddress = null,
        string? deviceInfo = null)
    {
        // Generate cryptographically secure random token
        var rawToken = _tokenService.GenerateRefreshToken();
        var tokenHash = _tokenService.HashToken(rawToken);

        // Determine expiration based on RememberMe
        var expiresAt = rememberMe
            ? DateTime.UtcNow.AddDays(_options.RememberMeDays)
            : DateTime.UtcNow.AddHours(_options.RefreshTokenHours);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId.ToString(),
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            RememberMe = rememberMe,
            IsRevoked = false,
            CreatedByIp = ipAddress,
            DeviceInfo = deviceInfo
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created refresh token for user {UserId}, RememberMe={RememberMe}, ExpiresAt={ExpiresAt}",
            userId,
            rememberMe,
            expiresAt);

        // Return the unhashed token to send to client
        return rawToken;
    }

    /// <summary>
    /// Retrieve refresh token information by its hash.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the refresh token.</param>
    /// <returns>Token information if found and valid, null otherwise.</returns>
    public async Task<RefreshTokenInfo?> GetByHashAsync(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            _logger.LogWarning("GetByHashAsync called with null or empty token hash");
            return null;
        }

        var token = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token == null)
        {
            _logger.LogDebug("Refresh token not found for hash");
            return null;
        }

        return new RefreshTokenInfo
        {
            Id = token.Id,
            UserId = Guid.Parse(token.UserId),
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            IsRevoked = token.IsRevoked,
            RememberMe = token.RememberMe,
            IpAddress = token.CreatedByIp,
            DeviceInfo = token.DeviceInfo,
            CreatedAt = token.CreatedAt
        };
    }

    /// <summary>
    /// Revoke a specific refresh token.
    /// </summary>
    /// <param name="tokenHash">SHA256 hash of the token to revoke.</param>
    public async Task RevokeAsync(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            _logger.LogWarning("RevokeAsync called with null or empty token hash");
            return;
        }

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (token == null)
        {
            _logger.LogDebug("Attempted to revoke non-existent token");
            return;
        }

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked refresh token {TokenId} for user {UserId}",
            token.Id,
            token.UserId);
    }

    /// <summary>
    /// Revoke all refresh tokens for a user (logout from all devices).
    /// </summary>
    /// <param name="userId">User whose tokens should be revoked.</param>
    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var userIdString = userId.ToString();
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userIdString && !t.IsRevoked)
            .ToListAsync();

        if (tokens.Count == 0)
        {
            _logger.LogDebug("No active tokens found for user {UserId}", userId);
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked all {Count} refresh tokens for user {UserId}",
            tokens.Count,
            userId);
    }

    /// <summary>
    /// Clean up expired or revoked tokens (called by maintenance job).
    /// </summary>
    /// <param name="olderThan">Delete tokens expired before this date.</param>
    /// <returns>Number of tokens deleted.</returns>
    public async Task<int> CleanupExpiredTokensAsync(DateTime olderThan)
    {
        var tokensToDelete = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < olderThan)
            .ToListAsync();

        if (tokensToDelete.Count == 0)
        {
            _logger.LogDebug("No expired tokens to clean up");
            return 0;
        }

        _context.RefreshTokens.RemoveRange(tokensToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleaned up {Count} expired refresh tokens older than {OlderThan}",
            tokensToDelete.Count,
            olderThan);

        return tokensToDelete.Count;
    }
}
