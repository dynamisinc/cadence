using Cadence.Core.Data;
using Cadence.Core.Features.Authentication.Services;
using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Features.Users.Services;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Users;

/// <summary>
/// Tests for user management service.
/// Covers: S10 (User List), S11 (Edit User), S12 (Deactivate), S13 (Role Assignment),
/// CreateUserAsync, UpdateCurrentOrganizationAsync, GetUserInfoAsync,
/// GetCurrentOrganizationIdAsync, GetCurrentUserProfileAsync, UpdatePhoneNumberAsync.
/// </summary>
public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IRefreshTokenStore> _refreshTokenStoreMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<ICurrentOrganizationContext> _orgContextMock;

    public UserServiceTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _refreshTokenStoreMock = new Mock<IRefreshTokenStore>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _orgContextMock = new Mock<ICurrentOrganizationContext>();

        // Default: SysAdmin so tests bypass org filtering
        _orgContextMock.Setup(x => x.IsSysAdmin).Returns(true);
        _orgContextMock.Setup(x => x.HasContext).Returns(true); // Simulate HTTP context exists
    }

    private (AppDbContext context, Organization org) CreateTestContext()
    {
        var context = TestDbContextFactory.Create();

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            CreatedBy = Guid.NewGuid().ToString(),
            ModifiedBy = Guid.NewGuid().ToString()
        };
        context.Organizations.Add(org);
        context.SaveChanges();

        return (context, org);
    }

    #region S10: View User List

    [Fact]
    public async Task GetUsersAsync_WithNoFilters_ReturnsAllActiveUsers()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user1 = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id);
        var user2 = CreateUser("bob@example.com", "Bob", ExerciseRole.Evaluator, org.Id);

        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user1, user2 }.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Users.Should().HaveCount(2);
        result.Users.Should().Contain(u => u.Email == "alice@example.com");
        result.Users.Should().Contain(u => u.Email == "bob@example.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithSearchFilter_ReturnsMatchingUsers()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user1 = CreateUser("alice@example.com", "Alice Smith", ExerciseRole.Controller, org.Id);
        var user2 = CreateUser("bob@example.com", "Bob Jones", ExerciseRole.Evaluator, org.Id);

        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user1, user2 }.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUsersAsync(search: "alice");

        // Assert
        result.Users.Should().HaveCount(1);
        result.Users.First().Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GetUsersAsync_WithRoleFilter_ReturnsUsersWithThatRole()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var user1 = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id);
        var user2 = CreateUser("bob@example.com", "Bob", ExerciseRole.Evaluator, org.Id);
        var user3 = CreateUser("charlie@example.com", "Charlie", ExerciseRole.Controller, org.Id);

        // Set SystemRoles for testing role filter
        user1.SystemRole = SystemRole.Manager;
        user3.SystemRole = SystemRole.Manager;
        user2.SystemRole = SystemRole.User;

        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user1, user2, user3 }.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUsersAsync(role: "Manager");

        // Assert
        result.Users.Should().HaveCount(2);
        result.Users.Should().AllSatisfy(u => u.SystemRole.Should().Be("Manager"));
    }

    [Fact]
    public async Task GetUsersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var users = Enumerable.Range(1, 25)
            .Select(i => CreateUser($"user{i}@example.com", $"User {i}", ExerciseRole.Observer, org.Id))
            .ToArray();

        _userManagerMock.Setup(x => x.Users)
            .Returns(users.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUsersAsync(page: 2, pageSize: 10);

        // Assert
        result.Pagination.Page.Should().Be(2);
        result.Pagination.PageSize.Should().Be(10);
        result.Pagination.TotalCount.Should().Be(25);
        result.Pagination.TotalPages.Should().Be(3);
        result.Users.Should().HaveCount(10);
    }

    #endregion

    #region S11: Edit User Details

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("alice@example.com");
        result.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid();
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_ValidRequest_UpdatesDisplayName()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);
        var request = new UpdateUserRequest { DisplayName = "Alice Smith" };

        // Act
        var result = await sut.UpdateUserAsync(userId, request);

        // Assert
        result.DisplayName.Should().Be("Alice Smith");
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.DisplayName == "Alice Smith")), Times.Once);
    }

    #endregion

    #region S12: Deactivate User Account

    [Fact]
    public async Task DeactivateUserAsync_ActiveUser_SetsStatusToDeactivated()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateUser("bob@example.com", "Bob", ExerciseRole.Observer, org.Id, userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.DeactivateUserAsync(userId, "Left organization", adminId.ToString());

        // Assert
        result.Status.Should().Be("Disabled");
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.Status == UserStatus.Disabled)), Times.Once);
        _refreshTokenStoreMock.Verify(x => x.RevokeAllForUserAsync(userId.ToString()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_NonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.DeactivateUserAsync(userId, null, adminId.ToString()));
    }

    [Fact]
    public async Task ReactivateUserAsync_DeactivatedUser_SetsStatusToActive()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateUser("bob@example.com", "Bob", ExerciseRole.Observer, org.Id, userId);
        user.Status = UserStatus.Disabled;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.ReactivateUserAsync(userId, adminId.ToString());

        // Assert
        result.Status.Should().Be("Active");
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.Status == UserStatus.Active)), Times.Once);
    }

    #endregion

    #region S13: Global Role Assignment

    [Fact]
    public async Task ChangeRoleAsync_ValidRole_UpdatesUserRole()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Observer, org.Id, userId);
        var admin = CreateUser("admin@example.com", "Admin", ExerciseRole.Administrator, org.Id, adminId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user, admin }.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act
        var result = await sut.ChangeRoleAsync(userId, "Manager", adminId.ToString());

        // Assert
        result.SystemRole.Should().Be("Manager");
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.SystemRole == SystemRole.Manager)), Times.Once);
        _refreshTokenStoreMock.Verify(x => x.RevokeAllForUserAsync(userId.ToString()), Times.Once);
    }

    [Fact]
    public async Task ChangeRoleAsync_InvalidRole_ThrowsArgumentException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Observer, org.Id, userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ChangeRoleAsync(userId, "InvalidRole", adminId.ToString()));
    }

    [Fact]
    public async Task ChangeRoleAsync_LastAdministrator_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateUser("admin@example.com", "Admin", ExerciseRole.Administrator, org.Id, userId);
        user.SystemRole = SystemRole.Admin;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user }.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ChangeRoleAsync(userId, "User", adminId.ToString()));
    }

    [Fact]
    public async Task ChangeRoleAsync_NotLastAdministrator_Succeeds()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = CreateUser("admin1@example.com", "Admin 1", ExerciseRole.Administrator, org.Id, userId);
        var admin2 = CreateUser("admin2@example.com", "Admin 2", ExerciseRole.Administrator, org.Id, adminId);
        user.SystemRole = SystemRole.Admin;
        admin2.SystemRole = SystemRole.Admin;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.Users)
            .Returns(new[] { user, admin2 }.AsAsyncQueryable());

        var sut = CreateUserService(context);

        // Act
        var result = await sut.ChangeRoleAsync(userId, "Manager", adminId.ToString());

        // Assert
        result.SystemRole.Should().Be("Manager");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region CreateUserAsync

    [Fact]
    public async Task CreateUserAsync_ValidRequest_ReturnsUserDto()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var request = new CreateUserRequest
        {
            DisplayName = "New User",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.CreateUserAsync(request, creatorId, isCreatorAdmin: false);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("newuser@example.com");
        result.DisplayName.Should().Be("New User");
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var existingUser = CreateUser("existing@example.com", "Existing", ExerciseRole.Observer, org.Id);
        var request = new CreateUserRequest
        {
            DisplayName = "Duplicate",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateUserAsync(request, creatorId, isCreatorAdmin: false));
    }

    [Fact]
    public async Task CreateUserAsync_MissingDisplayName_ThrowsArgumentException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var request = new CreateUserRequest
        {
            DisplayName = "",
            Email = "user@example.com",
            Password = "Password123!"
        };

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.CreateUserAsync(request, "creator", isCreatorAdmin: false));
    }

    [Fact]
    public async Task CreateUserAsync_MissingEmail_ThrowsArgumentException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var request = new CreateUserRequest
        {
            DisplayName = "Valid Name",
            Email = "",
            Password = "Password123!"
        };

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.CreateUserAsync(request, "creator", isCreatorAdmin: false));
    }

    [Fact]
    public async Task CreateUserAsync_MissingPassword_ThrowsArgumentException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var request = new CreateUserRequest
        {
            DisplayName = "Valid Name",
            Email = "user@example.com",
            Password = ""
        };

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.CreateUserAsync(request, "creator", isCreatorAdmin: false));
    }

    [Fact]
    public async Task CreateUserAsync_AdminSetsRole_UsesRequestedRole()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var request = new CreateUserRequest
        {
            DisplayName = "New Manager",
            Email = "manager@example.com",
            Password = "Password123!",
            SystemRole = SystemRole.Manager
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.CreateUserAsync(request, creatorId, isCreatorAdmin: true);

        // Assert
        result.SystemRole.Should().Be("Manager");
        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u => u.SystemRole == SystemRole.Manager),
            request.Password), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_NonAdminIgnoresRole_DefaultsToUser()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var request = new CreateUserRequest
        {
            DisplayName = "Attempted Admin",
            Email = "notadmin@example.com",
            Password = "Password123!",
            SystemRole = SystemRole.Admin
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.CreateUserAsync(request, creatorId, isCreatorAdmin: false);

        // Assert — role escalation was silently ignored
        result.SystemRole.Should().Be("User");
        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u => u.SystemRole == SystemRole.User),
            request.Password), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_PasswordValidationFails_ThrowsArgumentException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var creatorId = Guid.NewGuid().ToString();
        var request = new CreateUserRequest
        {
            DisplayName = "New User",
            Email = "newuser@example.com",
            Password = "weak"
        };

        _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Passwords must be at least 6 characters."
            }));

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.CreateUserAsync(request, creatorId, isCreatorAdmin: false));
    }

    #endregion

    #region UpdateCurrentOrganizationAsync

    [Fact]
    public async Task UpdateCurrentOrganizationAsync_ValidUser_UpdatesOrgId()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);
        var newOrgId = Guid.NewGuid();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        await sut.UpdateCurrentOrganizationAsync(userId.ToString(), newOrgId);

        // Assert
        _userManagerMock.Verify(x => x.UpdateAsync(
            It.Is<ApplicationUser>(u => u.CurrentOrganizationId == newOrgId)), Times.Once);
    }

    [Fact]
    public async Task UpdateCurrentOrganizationAsync_NonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.UpdateCurrentOrganizationAsync(userId, Guid.NewGuid()));
    }

    #endregion

    #region GetUserInfoAsync

    [Fact]
    public async Task GetUserInfoAsync_ExistingUser_ReturnsUserInfo()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);
        user.SystemRole = SystemRole.Manager;
        user.LastLoginAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetUserInfoAsync(userId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("alice@example.com");
        result.DisplayName.Should().Be("Alice");
        result.Role.Should().Be("Manager");
        result.Status.Should().Be("Active");
        result.LastLoginAt.Should().Be(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetUserInfoAsync_NonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.GetUserInfoAsync(userId));
    }

    #endregion

    #region GetCurrentOrganizationIdAsync

    [Fact]
    public async Task GetCurrentOrganizationIdAsync_UserWithOrg_ReturnsOrgId()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);
        user.CurrentOrganizationId = org.Id;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetCurrentOrganizationIdAsync(userId.ToString());

        // Assert
        result.Should().Be(org.Id);
    }

    [Fact]
    public async Task GetCurrentOrganizationIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetCurrentOrganizationIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCurrentUserProfileAsync

    [Fact]
    public async Task GetCurrentUserProfileAsync_ExistingUser_ReturnsProfile()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);
        user.PhoneNumber = "555-1234";
        user.SystemRole = SystemRole.Manager;

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetCurrentUserProfileAsync(userId.ToString());

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId.ToString());
        result.Email.Should().Be("alice@example.com");
        result.DisplayName.Should().Be("Alice");
        result.PhoneNumber.Should().Be("555-1234");
        result.SystemRole.Should().Be("Manager");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetCurrentUserProfileAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.GetCurrentUserProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdatePhoneNumberAsync

    [Fact]
    public async Task UpdatePhoneNumberAsync_ValidPhone_UpdatesPhone()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.UpdatePhoneNumberAsync(userId.ToString(), "555-9876");

        // Assert
        result.Should().NotBeNull();
        result.PhoneNumber.Should().Be("555-9876");
        _userManagerMock.Verify(x => x.UpdateAsync(
            It.Is<ApplicationUser>(u => u.PhoneNumber == "555-9876")), Times.Once);
    }

    [Fact]
    public async Task UpdatePhoneNumberAsync_TooLongPhone_ThrowsArgumentException()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);
        var tooLong = new string('1', 26); // 26 chars — exceeds the 25-char limit

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.UpdatePhoneNumberAsync(userId.ToString(), tooLong));
    }

    [Fact]
    public async Task UpdatePhoneNumberAsync_EmptyString_NormalizesToNull()
    {
        // Arrange
        var (context, org) = CreateTestContext();
        var userId = Guid.NewGuid();
        var user = CreateUser("alice@example.com", "Alice", ExerciseRole.Controller, org.Id, userId);
        user.PhoneNumber = "555-0000";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateUserService(context);

        // Act
        var result = await sut.UpdatePhoneNumberAsync(userId.ToString(), "");

        // Assert
        result.PhoneNumber.Should().BeNull();
        _userManagerMock.Verify(x => x.UpdateAsync(
            It.Is<ApplicationUser>(u => u.PhoneNumber == null)), Times.Once);
    }

    [Fact]
    public async Task UpdatePhoneNumberAsync_NonExistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateUserService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => sut.UpdatePhoneNumberAsync(userId, "555-1234"));
    }

    #endregion

    #region Helper Methods

    private UserService CreateUserService(AppDbContext context)
    {
        return new UserService(
            _userManagerMock.Object,
            _refreshTokenStoreMock.Object,
            _loggerMock.Object,
            context,
            _orgContextMock.Object);
    }

    private ApplicationUser CreateUser(
        string email,
        string displayName,
        ExerciseRole role,
        Guid organizationId,
        Guid? id = null)
    {
        return new ApplicationUser
        {
            Id = (id ?? Guid.NewGuid()).ToString(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            DisplayName = displayName,
            SystemRole = SystemRole.User,
            Status = UserStatus.Active,
            OrganizationId = organizationId,
            EmailConfirmed = true
        };
    }

    #endregion
}
