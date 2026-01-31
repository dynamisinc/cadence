using System.Linq.Expressions;
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
using Xunit;

namespace Cadence.Core.Tests.Features.Authentication;

/// <summary>
/// Tests for AuthenticationService - Core authentication orchestration.
/// </summary>
public class AuthenticationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenStore> _refreshTokenStoreMock;
    private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
    private readonly AuthenticationOptions _options;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _context = TestDbContextFactory.Create();

        // Setup UserManager mock
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenStoreMock = new Mock<IRefreshTokenStore>();
        _loggerMock = new Mock<ILogger<AuthenticationService>>();

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
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenStoreMock.Object,
            _context,
            Options.Create(_options),
            _loggerMock.Object);

        // Setup default token generation behavior
        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<UserInfo>()))
            .Returns(("test-access-token", 900));

        // Setup overload with organization context (used by GenerateAuthResponseAsync)
        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<UserInfo>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .Returns(("test-access-token", 900));

        _tokenServiceMock
            .Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns((string t) => $"hashed-{t}");

        _refreshTokenStoreMock
            .Setup(x => x.CreateAsync(
                It.IsAny<Guid>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync((Guid _, bool rememberMe, string? _, string? _) =>
                new RefreshTokenCreateResult("test-refresh-token", rememberMe ? 30 * 24 * 3600 : 4 * 3600, rememberMe));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // =========================================================================
    // Registration Tests
    // =========================================================================

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_FirstUser_AssignsAdministratorRole()
    {
        // Arrange
        var request = new RegistrationRequest("admin@example.com", "Password123!", "Admin User");
        ApplicationUser? capturedUser = null;

        // Ensure no ApplicationUsers exist in the database (seed data only creates Organization and User, not ApplicationUser)
        _context.ApplicationUsers.RemoveRange(_context.ApplicationUsers);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Use the actual DbSet - it should be empty since we cleared it
        _userManagerMock
            .Setup(x => x.Users)
            .Returns(_context.ApplicationUsers);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) =>
            {
                user.Id = Guid.NewGuid().ToString();
                capturedUser = user;
            });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFirstUser.Should().BeTrue();
        result.Role.Should().Be("Admin");
        capturedUser.Should().NotBeNull();
        capturedUser!.SystemRole.Should().Be(SystemRole.Admin);
    }

    [Fact]
    public async Task RegisterAsync_SubsequentUser_AssignsObserverRole()
    {
        // Arrange
        var existingUser = CreateTestUser();
        var request = new RegistrationRequest("new@example.com", "Password123!", "New User");
        ApplicationUser? capturedUser = null;

        // Add an existing user to the database so it's not the first user
        _context.ApplicationUsers.Add(existingUser);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.Users)
            .Returns(_context.ApplicationUsers);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) =>
            {
                user.Id = Guid.NewGuid().ToString();
                capturedUser = user;
            });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFirstUser.Should().BeFalse();
        result.Role.Should().Be("User");
        capturedUser.Should().NotBeNull();
        capturedUser!.SystemRole.Should().Be(SystemRole.User);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsDuplicateEmailError()
    {
        // Arrange
        var existingUser = CreateTestUser("existing@example.com");
        var request = new RegistrationRequest("existing@example.com", "Password123!", "New User");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("duplicate_email");
    }

    [Fact]
    public async Task RegisterAsync_IdentityDisabled_ReturnsProviderDisabled()
    {
        // Arrange
        _options.Identity.Enabled = false;
        var request = new RegistrationRequest("test@example.com", "Password123!", "Test User");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("provider_disabled");
    }

    [Fact]
    public async Task RegisterAsync_RegistrationDisabled_ReturnsRegistrationDisabled()
    {
        // Arrange
        _options.Identity.AllowRegistration = false;
        var request = new RegistrationRequest("test@example.com", "Password123!", "Test User");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("registration_disabled");
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ReturnsValidationErrors()
    {
        // Arrange
        var request = new RegistrationRequest("test@example.com", "weak", "Test User");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.Users)
            .Returns(_context.ApplicationUsers);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 8 characters." }));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("validation_error");
        result.Error!.ValidationErrors.Should().NotBeNull();
        result.Error!.ValidationErrors.Should().ContainKey("passwordTooShort");
    }

    [Fact]
    public async Task RegisterAsync_Success_UsesExistingOrganization()
    {
        // Arrange
        var request = new RegistrationRequest("test@example.com", "Password123!", "Test User");
        ApplicationUser? capturedUser = null;

        // The database is seeded with a default organization
        var initialOrgCount = _context.Organizations.Count();
        initialOrgCount.Should().BeGreaterThan(0, "Database should be seeded with default organization");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.Users)
            .Returns(_context.ApplicationUsers);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) =>
            {
                user.Id = Guid.NewGuid().ToString();
                capturedUser = user;
            });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should not create new organizations if one already exists
        _context.Organizations.Should().HaveCount(initialOrgCount);
        // User should be assigned to the default organization
        capturedUser.Should().NotBeNull();
        capturedUser!.OrganizationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_Success_ReturnsTokens()
    {
        // Arrange
        var request = new RegistrationRequest("test@example.com", "Password123!", "Test User");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.Users)
            .Returns(_context.ApplicationUsers);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) =>
            {
                user.Id = Guid.NewGuid().ToString();
            });

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.UserId.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.DisplayName.Should().Be(request.DisplayName);
        result.IsNewAccount.Should().BeTrue();
    }

    #endregion

    // =========================================================================
    // Login Tests
    // =========================================================================

    #region Login Tests

    [Fact]
    public async Task AuthenticateWithPasswordAsync_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest(user.Email!, "Password123!");

        SetupUserManagerForLogin(user, passwordValid: true);

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.UserId.Should().Be(Guid.Parse(user.Id));
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_InvalidEmail_ReturnsInvalidCredentials()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password123!");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_InvalidPassword_ReturnsInvalidCredentials()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest(user.Email!, "WrongPassword");

        SetupUserManagerForLogin(user, passwordValid: false, failedAccessCount: 1);

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("invalid_credentials");
        result.Error!.AttemptsRemaining.Should().Be(4); // 5 max - 1 failed
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_DeactivatedUser_ReturnsAccountDeactivated()
    {
        // Arrange
        var user = CreateTestUser();
        user.Status = UserStatus.Disabled;
        var request = new LoginRequest(user.Email!, "Password123!");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("account_deactivated");
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_LockedAccount_ReturnsAccountLocked()
    {
        // Arrange
        var user = CreateTestUser();
        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        var request = new LoginRequest(user.Email!, "Password123!");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync(lockoutEnd);

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("account_locked");
        result.Error!.LockoutEnd.Should().NotBeNull();
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_FailedAttempts_LocksAccount()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest(user.Email!, "WrongPassword");

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .SetupSequence(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false)  // First check: not locked
            .ReturnsAsync(true);  // Second check after failed attempt: now locked

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, request.Password))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.AccessFailedAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        _userManagerMock
            .Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync(lockoutEnd);

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("account_locked");
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_IdentityDisabled_ReturnsProviderDisabled()
    {
        // Arrange
        _options.Identity.Enabled = false;
        var request = new LoginRequest("test@example.com", "Password123!");

        // Act
        var result = await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("provider_disabled");
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_Success_ResetsFailedCount()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest(user.Email!, "Password123!");

        SetupUserManagerForLogin(user, passwordValid: true);

        // Act
        await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        _userManagerMock.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_Success_UpdatesLastLoginAt()
    {
        // Arrange
        var user = CreateTestUser();
        user.LastLoginAt = null;
        var request = new LoginRequest(user.Email!, "Password123!");

        SetupUserManagerForLogin(user, passwordValid: true);

        // Act
        await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task AuthenticateWithPasswordAsync_RememberMe_CreatesLongLivedToken()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new LoginRequest(user.Email!, "Password123!", RememberMe: true);

        SetupUserManagerForLogin(user, passwordValid: true);

        // Act
        await _sut.AuthenticateWithPasswordAsync(request);

        // Assert
        _refreshTokenStoreMock.Verify(x => x.CreateAsync(
            Guid.Parse(user.Id),
            true, // rememberMe should be true
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    #endregion

    // =========================================================================
    // Token Refresh Tests
    // =========================================================================

    #region Token Refresh Tests

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = "valid-refresh-token";
        var tokenInfo = CreateRefreshTokenInfo(user.Id, false, DateTime.UtcNow.AddHours(4));

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-token");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-token"))
            .ReturnsAsync(tokenInfo);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _refreshTokenStoreMock
            .Setup(x => x.RevokeAsync("hashed-token"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ReturnsInvalidToken()
    {
        // Arrange
        var refreshToken = "invalid-refresh-token";

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-invalid");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-invalid"))
            .ReturnsAsync((RefreshTokenInfo?)null);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("invalid_token");
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ReturnsInvalidToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = "revoked-refresh-token";
        var tokenInfo = CreateRefreshTokenInfo(user.Id, isRevoked: true, DateTime.UtcNow.AddHours(4));

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-revoked");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-revoked"))
            .ReturnsAsync(tokenInfo);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("invalid_token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsInvalidToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = "expired-refresh-token";
        var tokenInfo = CreateRefreshTokenInfo(user.Id, false, DateTime.UtcNow.AddHours(-1)); // Expired

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-expired");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-expired"))
            .ReturnsAsync(tokenInfo);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("invalid_token");
    }

    [Fact]
    public async Task RefreshTokenAsync_DeactivatedUser_ReturnsAccountDeactivated()
    {
        // Arrange
        var user = CreateTestUser();
        user.Status = UserStatus.Disabled;
        var refreshToken = "valid-refresh-token";
        var tokenInfo = CreateRefreshTokenInfo(user.Id, false, DateTime.UtcNow.AddHours(4));

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-token");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-token"))
            .ReturnsAsync(tokenInfo);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("account_deactivated");
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_RevokesOldToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = "valid-refresh-token";
        var tokenInfo = CreateRefreshTokenInfo(user.Id, false, DateTime.UtcNow.AddHours(4));

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-token");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-token"))
            .ReturnsAsync(tokenInfo);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        _refreshTokenStoreMock.Verify(x => x.RevokeAsync("hashed-token"), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_PreservesRememberMe_FromStoredToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = "remember-me-token";
        // Token info with RememberMe=true stored (Bug #4 fix: use stored value, not inferred)
        var tokenInfo = CreateRefreshTokenInfo(
            user.Id,
            false,
            DateTime.UtcNow.AddDays(30),
            createdAt: DateTime.UtcNow,
            rememberMe: true); // Stored RememberMe value

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-token");

        _refreshTokenStoreMock
            .Setup(x => x.GetByHashAsync("hashed-token"))
            .ReturnsAsync(tokenInfo);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _refreshTokenStoreMock
            .Setup(x => x.RevokeAsync("hashed-token"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RefreshTokenAsync(refreshToken);

        // Assert - Verify RememberMe is taken from stored token, not inferred from lifetime
        _refreshTokenStoreMock.Verify(x => x.CreateAsync(
            Guid.Parse(user.Id),
            true, // Should use stored RememberMe value (true)
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    #endregion

    // =========================================================================
    // Logout Tests
    // =========================================================================

    #region Logout Tests

    [Fact]
    public async Task RevokeTokenAsync_ValidToken_RevokesIt()
    {
        // Arrange
        var refreshToken = "token-to-revoke";

        _tokenServiceMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns("hashed-token");

        // Act
        await _sut.RevokeTokenAsync(refreshToken);

        // Assert
        _refreshTokenStoreMock.Verify(x => x.RevokeAsync("hashed-token"), Times.Once);
    }

    [Fact]
    public async Task RevokeTokensAsync_RevokesAllUserTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _sut.RevokeTokensAsync(userId);

        // Assert
        _refreshTokenStoreMock.Verify(x => x.RevokeAllForUserAsync(userId), Times.Once);
    }

    #endregion

    // =========================================================================
    // GetAvailableMethods Tests
    // =========================================================================

    #region GetAvailableMethods Tests

    [Fact]
    public void GetAvailableMethods_IdentityEnabled_ReturnsIdentityMethod()
    {
        // Arrange
        _options.Identity.Enabled = true;

        // Act
        var methods = _sut.GetAvailableMethods();

        // Assert
        methods.Should().HaveCount(1);
        methods[0].Provider.Should().Be("Identity");
        methods[0].DisplayName.Should().Be("Email & Password");
        methods[0].IsEnabled.Should().BeTrue();
        methods[0].IsExternal.Should().BeFalse();
    }

    [Fact]
    public void GetAvailableMethods_IdentityDisabled_ReturnsEmptyList()
    {
        // Arrange
        _options.Identity.Enabled = false;

        // Act
        var methods = _sut.GetAvailableMethods();

        // Assert
        methods.Should().BeEmpty();
    }

    #endregion

    // =========================================================================
    // GetUserAsync Tests
    // =========================================================================

    #region GetUserAsync Tests

    [Fact]
    public async Task GetUserAsync_ExistingUser_ReturnsUserInfo()
    {
        // Arrange
        var user = CreateTestUser();
        var userId = Guid.Parse(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Email.Should().Be(user.Email);
        result.DisplayName.Should().Be(user.DisplayName);
        result.Role.Should().Be(user.SystemRole.ToString());
    }

    [Fact]
    public async Task GetUserAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static ApplicationUser CreateTestUser(string? email = null)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email ?? "test@example.com",
            UserName = email ?? "test@example.com",
            DisplayName = "Test User",
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            EmailConfirmed = true
        };
    }

    private void SetupUserManagerForLogin(
        ApplicationUser user,
        bool passwordValid,
        int failedAccessCount = 0)
    {
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        _userManagerMock
            .Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(passwordValid);

        if (passwordValid)
        {
            _userManagerMock
                .Setup(x => x.ResetAccessFailedCountAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);
        }
        else
        {
            _userManagerMock
                .Setup(x => x.AccessFailedAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            _userManagerMock
                .Setup(x => x.GetAccessFailedCountAsync(user))
                .ReturnsAsync(failedAccessCount);
        }
    }

    private static RefreshTokenInfo CreateRefreshTokenInfo(
        string userId,
        bool isRevoked,
        DateTime expiresAt,
        DateTime? createdAt = null,
        bool rememberMe = false)
    {
        var created = createdAt ?? DateTime.UtcNow;
        return new RefreshTokenInfo
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(userId),
            TokenHash = "test-hash",
            ExpiresAt = expiresAt,
            IsRevoked = isRevoked,
            RememberMe = rememberMe,
            CreatedAt = created
        };
    }
}

/// <summary>
/// Extension to enable mocking IQueryable with async support.
/// </summary>
public static class MockQueryableExtensions
{
    public static IQueryable<T> BuildMock<T>(this IQueryable<T> data) where T : class
    {
        return new TestAsyncEnumerable<T>(data);
    }
}

/// <summary>
/// Test implementation for async enumerable support in mocked IQueryable.
/// </summary>
public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }
}

public class TestAsyncQueryProvider<T> : IQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<T>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }
}
