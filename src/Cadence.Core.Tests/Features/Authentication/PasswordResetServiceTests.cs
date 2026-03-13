using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Models;
using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Cadence.Core.Tests.Features.Authentication;

/// <summary>
/// Tests for PasswordResetService — password reset flows with rate limiting, token validation, and auto-login.
/// </summary>
public class PasswordResetServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenStore> _refreshTokenStoreMock;
    private readonly Mock<ILogger<PasswordResetService>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthenticationOptions _options;
    private readonly PasswordResetService _sut;

    public PasswordResetServiceTests()
    {
        _context = TestDbContextFactory.Create();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenStoreMock = new Mock<IRefreshTokenStore>();
        _loggerMock = new Mock<ILogger<PasswordResetService>>();
        _emailServiceMock = new Mock<IEmailService>();

        _options = new AuthenticationOptions
        {
            FrontendBaseUrl = "https://cadence.test"
        };

        _sut = new PasswordResetService(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenStoreMock.Object,
            _context,
            Options.Create(_options),
            _loggerMock.Object,
            _emailServiceMock.Object);

        // Default mocks
        _tokenServiceMock
            .Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns((string t) => $"hashed-{t}");

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<UserInfo>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(("test-access-token", 900));

        _refreshTokenStoreMock
            .Setup(x => x.CreateAsync(
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new RefreshTokenCreateResult("test-refresh-token", 14400, false));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private ApplicationUser CreateTestUser(string email = "user@test.com", UserStatus status = UserStatus.Active)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            DisplayName = "Test User",
            SystemRole = SystemRole.User,
            Status = status
        };
        return user;
    }

    // =========================================================================
    // RequestPasswordResetAsync
    // =========================================================================

    [Fact]
    public async Task RequestPasswordResetAsync_NonexistentEmail_ReturnsTrue()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.RequestPasswordResetAsync("nobody@test.com");

        result.Should().BeTrue(); // Always returns true to prevent enumeration
    }

    [Fact]
    public async Task RequestPasswordResetAsync_DeactivatedUser_ReturnsTrue()
    {
        var user = CreateTestUser(status: UserStatus.Disabled);
        _userManagerMock.Setup(x => x.FindByEmailAsync("disabled@test.com"))
            .ReturnsAsync(user);

        var result = await _sut.RequestPasswordResetAsync("disabled@test.com");

        result.Should().BeTrue();
        // Should not generate a token for disabled users
        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ValidUser_GeneratesTokenAndSendsEmail()
    {
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token-123");

        var result = await _sut.RequestPasswordResetAsync("user@test.com", "127.0.0.1");

        result.Should().BeTrue();
        _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(
            "user@test.com",
            "Test User",
            It.Is<string>(url => url.Contains("reset-password") && url.Contains("reset-token-123"))),
            Times.Once);

        // Verify token was stored
        _context.PasswordResetTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_NoFrontendBaseUrl_DoesNotSendEmail()
    {
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync("reset-token-123");

        // Create service without FrontendBaseUrl
        var options = new AuthenticationOptions { FrontendBaseUrl = null };
        var service = new PasswordResetService(
            _userManagerMock.Object, _tokenServiceMock.Object, _refreshTokenStoreMock.Object,
            _context, Options.Create(options), _loggerMock.Object, _emailServiceMock.Object);

        var result = await service.RequestPasswordResetAsync("user@test.com");

        result.Should().BeTrue();
        _emailServiceMock.Verify(x => x.SendPasswordResetEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // =========================================================================
    // ValidateTokenAsync
    // =========================================================================

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TokenHash = "hashed-valid-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();

        var result = await _sut.ValidateTokenAsync("valid-token");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ReturnsFalse()
    {
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TokenHash = "hashed-expired-token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();

        var result = await _sut.ValidateTokenAsync("expired-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_UsedToken_ReturnsFalse()
    {
        var token = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TokenHash = "hashed-used-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UsedAt = DateTime.UtcNow.AddMinutes(-5), // Already used
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();

        var result = await _sut.ValidateTokenAsync("used-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_NonexistentToken_ReturnsFalse()
    {
        var result = await _sut.ValidateTokenAsync("nonexistent-token");

        result.Should().BeFalse();
    }

    // =========================================================================
    // ResetPasswordAsync
    // =========================================================================

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsInvalidTokenError()
    {
        var result = await _sut.ResetPasswordAsync("bad-token", "NewP@ssw0rd");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsInvalidTokenError()
    {
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TokenHash = "hashed-expired-token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.ResetPasswordAsync("expired-token", "NewP@ssw0rd");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_DeactivatedUser_ReturnsError()
    {
        var user = CreateTestUser(status: UserStatus.Disabled);
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-valid-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        var result = await _sut.ResetPasswordAsync("valid-token", "NewP@ssw0rd");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_ResetsPasswordAndAutoLogins()
    {
        var user = CreateTestUser();
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-valid-reset",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "valid-reset", "NewP@ssw0rd123!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        var result = await _sut.ResetPasswordAsync("valid-reset", "NewP@ssw0rd123!");

        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();

        // Verify token was marked as used
        var token = _context.PasswordResetTokens.First();
        token.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_LockedOutUser_ResetsLockout()
    {
        var user = CreateTestUser();
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-lockout-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "lockout-token", "NewP@ss123!"))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(true);

        await _sut.ResetPasswordAsync("lockout-token", "NewP@ss123!");

        _userManagerMock.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_IdentityFailure_ReturnsValidationErrors()
    {
        var user = CreateTestUser();
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hashed-weak-pw-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.ResetPasswordAsync(user, "weak-pw-token", "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Password must be at least 8 characters."
            }));

        var result = await _sut.ResetPasswordAsync("weak-pw-token", "weak");

        result.IsSuccess.Should().BeFalse();
    }

    // =========================================================================
    // IsRateLimitedAsync
    // =========================================================================

    [Fact]
    public async Task IsRateLimitedAsync_NonexistentEmail_ReturnsFalse()
    {
        _userManagerMock.Setup(x => x.FindByEmailAsync("nobody@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.IsRateLimitedAsync("nobody@test.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRateLimitedAsync_UnderLimit_ReturnsFalse()
    {
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(user);

        // Add 2 recent tokens (limit is 3)
        for (int i = 0; i < 2; i++)
        {
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"hash-{i}",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var result = await _sut.IsRateLimitedAsync("user@test.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsRateLimitedAsync_AtLimit_ReturnsTrue()
    {
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(user);

        // Add 3 recent tokens (limit is 3)
        for (int i = 0; i < 3; i++)
        {
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"hash-{i}",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var result = await _sut.IsRateLimitedAsync("user@test.com");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRateLimitedAsync_OldTokens_NotCounted()
    {
        var user = CreateTestUser();
        _userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com"))
            .ReturnsAsync(user);

        // Add 5 old tokens — SaveChanges auto-sets CreatedAt to UtcNow,
        // so we must save first, then backdate CreatedAt and re-save.
        var oldTime = DateTime.UtcNow.AddMinutes(-30);
        for (int i = 0; i < 5; i++)
        {
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"old-hash-{i}",
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = oldTime,
                UpdatedAt = oldTime
            });
        }
        await _context.SaveChangesAsync();

        // Backdate CreatedAt after the auto-timestamp override
        foreach (var token in _context.PasswordResetTokens.ToList())
        {
            token.CreatedAt = oldTime;
        }
        await _context.SaveChangesAsync();

        var result = await _sut.IsRateLimitedAsync("user@test.com");

        result.Should().BeFalse();
    }

    // =========================================================================
    // CleanupExpiredTokensAsync
    // =========================================================================

    [Fact]
    public async Task CleanupExpiredTokensAsync_RemovesExpiredAndUsedTokens()
    {
        var oldDate = DateTime.UtcNow.AddDays(-3);

        // Expired token
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TokenHash = "expired-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        // Used token
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-2",
            TokenHash = "used-hash",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            UsedAt = DateTime.UtcNow.AddDays(-2),
            CreatedAt = oldDate,
            UpdatedAt = oldDate
        });

        // Valid, unused token (should NOT be removed)
        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = "user-3",
            TokenHash = "valid-hash",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Backdate CreatedAt for old tokens (DbContext auto-sets to UtcNow on Add)
        var oldTokens = _context.PasswordResetTokens
            .Where(t => t.TokenHash == "expired-hash" || t.TokenHash == "used-hash")
            .ToList();
        foreach (var t in oldTokens)
        {
            t.CreatedAt = oldDate;
        }
        await _context.SaveChangesAsync();

        var removed = await _sut.CleanupExpiredTokensAsync(DateTime.UtcNow.AddDays(-1));

        removed.Should().Be(2);
        _context.PasswordResetTokens.Should().HaveCount(1);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_NoExpiredTokens_ReturnsZero()
    {
        var removed = await _sut.CleanupExpiredTokensAsync(DateTime.UtcNow.AddDays(-1));

        removed.Should().Be(0);
    }
}
