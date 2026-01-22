using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Authentication;

/// <summary>
/// Tests for RefreshTokenStore - Refresh token persistence and management.
/// </summary>
public class RefreshTokenStoreTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ILogger<RefreshTokenStore>> _loggerMock;
    private readonly JwtOptions _jwtOptions;
    private readonly RefreshTokenStore _sut;
    private readonly string _testUserId;

    public RefreshTokenStoreTests()
    {
        _context = TestDbContextFactory.Create();
        _tokenServiceMock = new Mock<ITokenService>();
        _loggerMock = new Mock<ILogger<RefreshTokenStore>>();
        _jwtOptions = new JwtOptions
        {
            RefreshTokenHours = 4,
            RememberMeDays = 30
        };

        _sut = new RefreshTokenStore(
            _context,
            _tokenServiceMock.Object,
            Options.Create(_jwtOptions),
            _loggerMock.Object);

        // Create a test user
        _testUserId = Guid.NewGuid().ToString();
        _context.ApplicationUsers.Add(new ApplicationUser
        {
            Id = _testUserId,
            Email = "test@example.com",
            DisplayName = "Test User",
            UserName = "test@example.com"
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_StandardLogin_CreatesTokenWith4HourExpiration()
    {
        // Arrange
        var rawToken = "test-refresh-token-123";
        var tokenHash = "hashed-token-123";
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns(rawToken);
        _tokenServiceMock.Setup(x => x.HashToken(rawToken)).Returns(tokenHash);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _sut.CreateAsync(
            Guid.Parse(_testUserId),
            rememberMe: false,
            ipAddress: "127.0.0.1",
            deviceInfo: "Mozilla/5.0");

        var afterCreation = DateTime.UtcNow;

        // Assert
        result.Should().Be(rawToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        storedToken.Should().NotBeNull();
        storedToken!.UserId.Should().Be(_testUserId);
        storedToken.RememberMe.Should().BeFalse();
        storedToken.IsRevoked.Should().BeFalse();
        storedToken.CreatedByIp.Should().Be("127.0.0.1");
        storedToken.DeviceInfo.Should().Be("Mozilla/5.0");

        // Should expire in 4 hours
        storedToken.ExpiresAt.Should().BeAfter(beforeCreation.AddHours(3).AddMinutes(59));
        storedToken.ExpiresAt.Should().BeBefore(afterCreation.AddHours(4).AddMinutes(1));
    }

    [Fact]
    public async Task CreateAsync_RememberMe_CreatesTokenWith30DayExpiration()
    {
        // Arrange
        var rawToken = "test-refresh-token-123";
        var tokenHash = "hashed-token-123";
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns(rawToken);
        _tokenServiceMock.Setup(x => x.HashToken(rawToken)).Returns(tokenHash);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _sut.CreateAsync(
            Guid.Parse(_testUserId),
            rememberMe: true,
            ipAddress: "192.168.1.1",
            deviceInfo: "Chrome");

        var afterCreation = DateTime.UtcNow;

        // Assert
        result.Should().Be(rawToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        storedToken.Should().NotBeNull();
        storedToken!.RememberMe.Should().BeTrue();

        // Should expire in 30 days
        storedToken.ExpiresAt.Should().BeAfter(beforeCreation.AddDays(29).AddHours(23));
        storedToken.ExpiresAt.Should().BeBefore(afterCreation.AddDays(30).AddHours(1));
    }

    [Fact]
    public async Task CreateAsync_NoIpOrDevice_StoresTokenWithNullValues()
    {
        // Arrange
        var rawToken = "test-refresh-token-123";
        var tokenHash = "hashed-token-123";
        _tokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns(rawToken);
        _tokenServiceMock.Setup(x => x.HashToken(rawToken)).Returns(tokenHash);

        // Act
        await _sut.CreateAsync(Guid.Parse(_testUserId), rememberMe: false);

        // Assert
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        storedToken.Should().NotBeNull();
        storedToken!.CreatedByIp.Should().BeNull();
        storedToken.DeviceInfo.Should().BeNull();
    }

    #endregion

    #region GetByHashAsync Tests

    [Fact]
    public async Task GetByHashAsync_ValidNonRevokedToken_ReturnsTokenInfo()
    {
        // Arrange
        var tokenHash = "valid-token-hash";
        var expiresAt = DateTime.UtcNow.AddHours(4);

        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedByIp = "127.0.0.1",
            DeviceInfo = "Test Device"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByHashAsync(tokenHash);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(Guid.Parse(_testUserId));
        result.TokenHash.Should().Be(tokenHash);
        result.ExpiresAt.Should().Be(expiresAt);
        result.IsRevoked.Should().BeFalse();
        result.RememberMe.Should().BeFalse();
        result.IpAddress.Should().Be("127.0.0.1");
        result.DeviceInfo.Should().Be("Test Device");
    }

    [Fact]
    public async Task GetByHashAsync_RevokedToken_ReturnsTokenInfoWithRevokedFlag()
    {
        // Arrange
        var tokenHash = "revoked-token-hash";
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(4),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByHashAsync(tokenHash);

        // Assert
        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task GetByHashAsync_NonExistentToken_ReturnsNull()
    {
        // Arrange
        var nonExistentHash = "non-existent-hash";

        // Act
        var result = await _sut.GetByHashAsync(nonExistentHash);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RevokeAsync Tests

    [Fact]
    public async Task RevokeAsync_ValidToken_MarksTokenAsRevoked()
    {
        // Arrange
        var tokenHash = "token-to-revoke";
        var tokenId = Guid.NewGuid();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = tokenId,
            UserId = _testUserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(4),
            IsRevoked = false
        });
        await _context.SaveChangesAsync();

        var beforeRevoke = DateTime.UtcNow;

        // Act
        await _sut.RevokeAsync(tokenHash);

        var afterRevoke = DateTime.UtcNow;

        // Assert
        var revokedToken = await _context.RefreshTokens.FindAsync(tokenId);
        revokedToken.Should().NotBeNull();
        revokedToken!.IsRevoked.Should().BeTrue();
        revokedToken.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedAt.Should().BeAfter(beforeRevoke.AddSeconds(-1));
        revokedToken.RevokedAt.Should().BeBefore(afterRevoke.AddSeconds(1));
    }

    [Fact]
    public async Task RevokeAsync_NonExistentToken_DoesNotThrow()
    {
        // Arrange
        var nonExistentHash = "non-existent-token";

        // Act
        var act = async () => await _sut.RevokeAsync(nonExistentHash);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRevokedToken_UpdatesRevokedAt()
    {
        // Arrange
        var tokenHash = "already-revoked-token";
        var originalRevokedAt = DateTime.UtcNow.AddHours(-2);
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(4),
            IsRevoked = true,
            RevokedAt = originalRevokedAt
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.RevokeAsync(tokenHash);

        // Assert
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        token!.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().BeAfter(originalRevokedAt); // Updated
    }

    #endregion

    #region RevokeAllForUserAsync Tests

    [Fact]
    public async Task RevokeAllForUserAsync_MultipleTokens_RevokesAllUserTokens()
    {
        // Arrange
        var userId = Guid.Parse(_testUserId);
        var token1Id = Guid.NewGuid();
        var token2Id = Guid.NewGuid();

        _context.RefreshTokens.AddRange(
            new RefreshToken
            {
                Id = token1Id,
                UserId = _testUserId,
                TokenHash = "token-1",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                IsRevoked = false
            },
            new RefreshToken
            {
                Id = token2Id,
                UserId = _testUserId,
                TokenHash = "token-2",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _sut.RevokeAllForUserAsync(userId);

        // Assert
        var userTokens = await _context.RefreshTokens
            .Where(t => t.UserId == _testUserId)
            .ToListAsync();

        userTokens.Should().HaveCount(2);
        userTokens.Should().OnlyContain(t => t.IsRevoked);
        userTokens.Should().OnlyContain(t => t.RevokedAt != null);
    }

    [Fact]
    public async Task RevokeAllForUserAsync_NoTokens_DoesNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await _sut.RevokeAllForUserAsync(userId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_DoesNotRevokeOtherUsersTokens()
    {
        // Arrange
        var userId = Guid.Parse(_testUserId);
        var otherUserId = Guid.NewGuid().ToString();

        _context.ApplicationUsers.Add(new ApplicationUser
        {
            Id = otherUserId,
            Email = "other@example.com",
            DisplayName = "Other User",
            UserName = "other@example.com"
        });
        await _context.SaveChangesAsync();

        _context.RefreshTokens.AddRange(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "user-1-token",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                IsRevoked = false
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                TokenHash = "other-user-token",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                IsRevoked = false
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _sut.RevokeAllForUserAsync(userId);

        // Assert
        var otherUserToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == otherUserId);

        otherUserToken.Should().NotBeNull();
        otherUserToken!.IsRevoked.Should().BeFalse();
    }

    #endregion

    #region CleanupExpiredTokensAsync Tests

    [Fact]
    public async Task CleanupExpiredTokensAsync_ExpiredTokens_DeletesOldTokens()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-60);
        var recentDate = DateTime.UtcNow.AddDays(-1);

        _context.RefreshTokens.AddRange(
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "old-expired-1",
                ExpiresAt = oldDate,
                IsRevoked = false
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "old-expired-2",
                ExpiresAt = oldDate,
                IsRevoked = true,
                RevokedAt = oldDate
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "recent-expired",
                ExpiresAt = recentDate,
                IsRevoked = false
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TokenHash = "active-token",
                ExpiresAt = DateTime.UtcNow.AddHours(4),
                IsRevoked = false
            }
        );
        await _context.SaveChangesAsync();

        // Act - Delete tokens expired before 30 days ago
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var deletedCount = await _sut.CleanupExpiredTokensAsync(cutoffDate);

        // Assert
        deletedCount.Should().Be(2); // old-expired-1 and old-expired-2

        var remainingTokens = await _context.RefreshTokens.ToListAsync();
        remainingTokens.Should().HaveCount(2);
        remainingTokens.Should().Contain(t => t.TokenHash == "recent-expired");
        remainingTokens.Should().Contain(t => t.TokenHash == "active-token");
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_NoExpiredTokens_ReturnsZero()
    {
        // Arrange
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TokenHash = "active-token",
            ExpiresAt = DateTime.UtcNow.AddHours(4),
            IsRevoked = false
        });
        await _context.SaveChangesAsync();

        // Act
        var deletedCount = await _sut.CleanupExpiredTokensAsync(DateTime.UtcNow.AddDays(-30));

        // Assert
        deletedCount.Should().Be(0);
    }

    #endregion
}
