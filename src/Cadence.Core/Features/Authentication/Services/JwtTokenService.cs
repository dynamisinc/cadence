using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> options, ILogger<JwtTokenService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(_options.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is required. Configure it in user secrets or Azure Key Vault.");
        }

        if (_options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters for HS256.");
        }

        // Disable default inbound claim type mapping to preserve JWT claim names
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        // Cache validation parameters for performance
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(5), // Small clock skew for clock sync tolerance
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = ClaimTypes.Role
        };
    }

    /// <summary>
    /// Generate a JWT access token for a user.
    /// </summary>
    /// <param name="user">User information to embed in token claims.</param>
    /// <returns>Tuple of (token string, expiration time in seconds).</returns>
    public (string Token, int ExpiresIn) GenerateAccessToken(UserInfo user)
    {
        return GenerateAccessToken(user, null, null, null, null);
    }

    /// <summary>
    /// Generate a JWT access token for a user with organization context.
    /// </summary>
    /// <param name="user">User information to embed in token claims.</param>
    /// <param name="organizationId">Current organization ID.</param>
    /// <param name="orgName">Current organization name.</param>
    /// <param name="orgSlug">Current organization slug.</param>
    /// <param name="orgRole">Role in current organization.</param>
    /// <returns>Tuple of (token string, expiration time in seconds).</returns>
    public (string Token, int ExpiresIn) GenerateAccessToken(UserInfo user, Guid? organizationId, string? orgName, string? orgSlug, string? orgRole)
    {
        ArgumentNullException.ThrowIfNull(user);

        var claimsList = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("SystemRole", user.Role), // Custom claim for authorization policies
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };

        // Add organization context claims if provided
        if (organizationId.HasValue)
        {
            claimsList.Add(new Claim("org_id", organizationId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(orgName))
        {
            claimsList.Add(new Claim("org_name", orgName));
        }

        if (!string.IsNullOrEmpty(orgSlug))
        {
            claimsList.Add(new Claim("org_slug", orgSlug));
        }

        if (!string.IsNullOrEmpty(orgRole))
        {
            claimsList.Add(new Claim("org_role", orgRole));
        }

        var claims = claimsList.ToArray();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresIn = _options.AccessTokenMinutes * 60; // Convert to seconds

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug(
            "Generated access token for user {UserId} ({Email}), expires in {ExpiresIn} seconds",
            user.Id,
            user.Email,
            expiresIn);

        return (tokenString, expiresIn);
    }

    /// <summary>
    /// Validate and parse a JWT access token.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>Token claims if valid, null if invalid or expired.</returns>
    public TokenClaims? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation failed: Token is null or empty");
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Token validation failed: Token is not a valid JWT");
                return null;
            }

            // Extract claims
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var nameClaim = principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                ?? principal.FindFirst("name")?.Value; // Fallback to "name" claim
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
            var iatClaim = principal.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;

            if (userIdClaim == null || emailClaim == null || nameClaim == null || roleClaim == null)
            {
                _logger.LogWarning("Token validation failed: Missing required claims");
                return null;
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Token validation failed: Invalid user ID format");
                return null;
            }

            // Parse issued at timestamp
            var issuedAt = DateTime.UtcNow;
            if (iatClaim != null && long.TryParse(iatClaim, out var iatSeconds))
            {
                issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;
            }

            var claims = new TokenClaims
            {
                UserId = userId,
                Email = emailClaim,
                DisplayName = nameClaim,
                Role = roleClaim,
                IssuedAt = issuedAt,
                ExpiresAt = jwtToken.ValidTo
            };

            _logger.LogDebug(
                "Token validated successfully for user {UserId} ({Email})",
                claims.UserId,
                claims.Email);

            return claims;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogDebug("Token validation failed: Token has expired");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating token: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Generate a cryptographically secure random refresh token.
    /// </summary>
    /// <returns>Base64-encoded random token string.</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        _logger.LogDebug("Generated new refresh token");

        return token;
    }

    /// <summary>
    /// Hash a refresh token using SHA256.
    /// Only the hash is stored in the database for security.
    /// </summary>
    /// <param name="token">The refresh token to hash.</param>
    /// <returns>SHA256 hash of the token.</returns>
    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty", nameof(token));
        }

        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verify a refresh token matches a stored hash.
    /// </summary>
    /// <param name="token">The refresh token to verify.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns>True if token matches hash, false otherwise.</returns>
    public bool VerifyTokenHash(string token, string hash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            var computedHash = HashToken(token);
            return computedHash == hash;
        }
        catch
        {
            return false;
        }
    }
}
