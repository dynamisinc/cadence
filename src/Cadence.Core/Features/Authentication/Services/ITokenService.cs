using Cadence.Core.Features.Authentication.Models.DTOs;

namespace Cadence.Core.Features.Authentication.Services;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a JWT access token for a user.
    /// </summary>
    /// <param name="user">User information to embed in token claims.</param>
    /// <returns>Tuple of (token string, expiration time in seconds).</returns>
    (string Token, int ExpiresIn) GenerateAccessToken(UserInfo user);

    /// <summary>
    /// Validate and parse a JWT access token.
    /// </summary>
    /// <param name="token">The JWT token string to validate.</param>
    /// <returns>Token claims if valid, null if invalid or expired.</returns>
    TokenClaims? ValidateToken(string token);

    /// <summary>
    /// Generate a cryptographically secure random refresh token.
    /// </summary>
    /// <returns>Base64-encoded random token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Hash a refresh token using SHA256.
    /// Only the hash is stored in the database for security.
    /// </summary>
    /// <param name="token">The refresh token to hash.</param>
    /// <returns>SHA256 hash of the token.</returns>
    string HashToken(string token);

    /// <summary>
    /// Verify a refresh token matches a stored hash.
    /// </summary>
    /// <param name="token">The refresh token to verify.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns>True if token matches hash, false otherwise.</returns>
    bool VerifyTokenHash(string token, string hash);
}
