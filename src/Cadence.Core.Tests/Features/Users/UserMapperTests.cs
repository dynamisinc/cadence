using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Models.Entities;
using FluentAssertions;

namespace Cadence.Core.Tests.Features.Users;

/// <summary>
/// Tests for UserMapper extension methods — entity-to-DTO mapping for ApplicationUser.
/// </summary>
public class UserMapperTests
{
    private static ApplicationUser CreateUser(
        SystemRole role = SystemRole.User,
        UserStatus status = UserStatus.Active,
        string? email = "test@example.com",
        string? phoneNumber = "555-0100")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            DisplayName = "Test User",
            SystemRole = role,
            Status = status,
            PhoneNumber = phoneNumber,
            LastLoginAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    // =========================================================================
    // ToDto
    // =========================================================================

    [Fact]
    public void ToDto_AllFields_MapsCorrectly()
    {
        var user = CreateUser(SystemRole.Admin, UserStatus.Active);

        var dto = user.ToDto();

        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be("test@example.com");
        dto.DisplayName.Should().Be("Test User");
        dto.SystemRole.Should().Be("Admin");
        dto.Status.Should().Be("Active");
        dto.LastLoginAt.Should().Be(user.LastLoginAt);
        dto.CreatedAt.Should().Be(user.CreatedAt);
    }

    [Fact]
    public void ToDto_NullEmail_DefaultsToEmptyString()
    {
        var user = CreateUser(email: null);

        var dto = user.ToDto();

        dto.Email.Should().BeEmpty();
    }

    [Theory]
    [InlineData(SystemRole.Admin, "Admin")]
    [InlineData(SystemRole.Manager, "Manager")]
    [InlineData(SystemRole.User, "User")]
    public void ToDto_SystemRoles_MapsToString(SystemRole role, string expected)
    {
        var user = CreateUser(role: role);

        user.ToDto().SystemRole.Should().Be(expected);
    }

    [Theory]
    [InlineData(UserStatus.Active, "Active")]
    [InlineData(UserStatus.Disabled, "Disabled")]
    [InlineData(UserStatus.Pending, "Pending")]
    public void ToDto_UserStatuses_MapsToString(UserStatus status, string expected)
    {
        var user = CreateUser(status: status);

        user.ToDto().Status.Should().Be(expected);
    }

    // =========================================================================
    // ToProfileDto
    // =========================================================================

    [Fact]
    public void ToProfileDto_IncludesPhoneNumber()
    {
        var user = CreateUser(phoneNumber: "555-1234");

        var dto = user.ToProfileDto();

        dto.PhoneNumber.Should().Be("555-1234");
        dto.Email.Should().Be("test@example.com");
        dto.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public void ToProfileDto_NullEmail_DefaultsToEmptyString()
    {
        var user = CreateUser(email: null);

        var dto = user.ToProfileDto();

        dto.Email.Should().BeEmpty();
    }

    // =========================================================================
    // ToContactDto
    // =========================================================================

    [Fact]
    public void ToContactDto_MapsAllFieldsIncludingUpdatedAt()
    {
        var user = CreateUser();
        var updatedAt = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

        var dto = user.ToContactDto(updatedAt);

        dto.Id.Should().Be(user.Id);
        dto.DisplayName.Should().Be("Test User");
        dto.Email.Should().Be("test@example.com");
        dto.PhoneNumber.Should().Be("555-0100");
        dto.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void ToContactDto_NullPhoneNumber_MapsAsNull()
    {
        var user = CreateUser(phoneNumber: null);

        var dto = user.ToContactDto(DateTime.UtcNow);

        dto.PhoneNumber.Should().BeNull();
    }
}
