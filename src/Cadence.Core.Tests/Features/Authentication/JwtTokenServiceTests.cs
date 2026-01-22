using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Authentication.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Authentication;

/// <summary>
/// Tests for JwtTokenService - JWT token generation and validation.
/// </summary>
public class JwtTokenServiceTests
{
    private readonly Mock<ILogger<JwtTokenService>> _loggerMock;
    private readonly JwtOptions _jwtOptions;
    private readonly JwtTokenService _sut;

    public JwtTokenServiceTests()
    {
        _loggerMock = new Mock<ILogger<JwtTokenService>>();
        _jwtOptions = new JwtOptions
        {
            Issuer = "Cadence",
            Audience = "Cadence",
            SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123!",
            AccessTokenMinutes = 15,
            RefreshTokenHours = 4,
            RememberMeDays = 30
        };
        _sut = new JwtTokenService(Options.Create(_jwtOptions), _loggerMock.Object);
    }

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsTokenWithCorrectExpiration()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "jane@example.com",
            DisplayName = "Jane Smith",
            Role = "Controller",
            Status = "Active"
        };

        // Act
        var (token, expiresIn) = _sut.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        expiresIn.Should().Be(15 * 60); // 15 minutes in seconds
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_TokenContainsRequiredClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserInfo
        {
            Id = userId,
            Email = "jane@example.com",
            DisplayName = "Jane Smith",
            Role = "Controller",
            Status = "Active"
        };

        // Act
        var (token, _) = _sut.GenerateAccessToken(user);
        var claims = _sut.ValidateToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.UserId.Should().Be(userId);
        claims.Email.Should().Be("jane@example.com");
        claims.DisplayName.Should().Be("Jane Smith");
        claims.Role.Should().Be("Controller");
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_TokenExpiresAt15Minutes()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "jane@example.com",
            DisplayName = "Jane Smith",
            Role = "Controller",
            Status = "Active"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var (token, _) = _sut.GenerateAccessToken(user);
        var claims = _sut.ValidateToken(token);

        var afterGeneration = DateTime.UtcNow;

        // Assert
        claims.Should().NotBeNull();
        claims!.ExpiresAt.Should().BeAfter(beforeGeneration.AddMinutes(14));
        claims.ExpiresAt.Should().BeBefore(afterGeneration.AddMinutes(16));
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public void ValidateToken_ValidToken_ReturnsTokenClaims()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "jane@example.com",
            DisplayName = "Jane Smith",
            Role = "Controller",
            Status = "Active"
        };
        var (token, _) = _sut.GenerateAccessToken(user);

        // Act
        var claims = _sut.ValidateToken(token);

        // Assert
        claims.Should().NotBeNull();
        claims!.UserId.Should().Be(user.Id);
        claims.Email.Should().Be(user.Email);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var claims = _sut.ValidateToken(invalidToken);

        // Assert
        claims.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange - Generate a token with very short expiration (1 millisecond)
        var expiredOptions = new JwtOptions
        {
            Issuer = "Cadence",
            Audience = "Cadence",
            SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123!",
            AccessTokenMinutes = 0 // Effectively immediate expiration
        };
        var expiredService = new JwtTokenService(Options.Create(expiredOptions), _loggerMock.Object);
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "jane@example.com",
            DisplayName = "Jane Smith",
            Role = "Controller",
            Status = "Active"
        };
        var (token, _) = expiredService.GenerateAccessToken(user);

        // Wait to ensure token expires (beyond the 5 second clock skew)
        await Task.Delay(6000);

        // Act
        var claims = _sut.ValidateToken(token);

        // Assert
        claims.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WrongIssuer_ReturnsNull()
    {
        // Arrange - Generate token with different issuer
        var wrongOptions = new JwtOptions
        {
            Issuer = "WrongIssuer",
            Audience = "Cadence",
            SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123!",
            AccessTokenMinutes = 15
        };
        var wrongService = new JwtTokenService(Options.Create(wrongOptions), _loggerMock.Object);
        var user = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "jane@example.com",
            DisplayName = "Jane Smith",
            Role = "Controller",
            Status = "Active"
        };
        var (token, _) = wrongService.GenerateAccessToken(user);

        // Act
        var claims = _sut.ValidateToken(token);

        // Assert
        claims.Should().BeNull();
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachTime()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert - Should be able to convert back from Base64
        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(32); // 32 random bytes
    }

    #endregion

    #region HashToken Tests

    [Fact]
    public void HashToken_ValidToken_ReturnsSHA256Hash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash = _sut.HashToken(token);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(token); // Hash should be different from original
    }

    [Fact]
    public void HashToken_SameTokenTwice_ReturnsSameHash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash1 = _sut.HashToken(token);
        var hash2 = _sut.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentTokens_ReturnsDifferentHashes()
    {
        // Arrange
        var token1 = "test-refresh-token-1";
        var token2 = "test-refresh-token-2";

        // Act
        var hash1 = _sut.HashToken(token1);
        var hash2 = _sut.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region VerifyTokenHash Tests

    [Fact]
    public void VerifyTokenHash_MatchingTokenAndHash_ReturnsTrue()
    {
        // Arrange
        var token = "test-refresh-token";
        var hash = _sut.HashToken(token);

        // Act
        var result = _sut.VerifyTokenHash(token, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyTokenHash_NonMatchingTokenAndHash_ReturnsFalse()
    {
        // Arrange
        var token = "test-refresh-token";
        var wrongToken = "wrong-refresh-token";
        var hash = _sut.HashToken(token);

        // Act
        var result = _sut.VerifyTokenHash(wrongToken, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyTokenHash_EmptyToken_ReturnsFalse()
    {
        // Arrange
        var token = "test-refresh-token";
        var hash = _sut.HashToken(token);

        // Act
        var result = _sut.VerifyTokenHash("", hash);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
