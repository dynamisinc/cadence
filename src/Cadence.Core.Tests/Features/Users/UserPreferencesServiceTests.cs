using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Features.Users.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Users;

public class UserPreferencesServiceTests
{
    private readonly Mock<ILogger<UserPreferencesService>> _loggerMock;

    public UserPreferencesServiceTests()
    {
        _loggerMock = new Mock<ILogger<UserPreferencesService>>();
    }

    // =========================================================================
    // GetPreferencesAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetPreferencesAsync_CreatesDefaultPreferences_WhenUserHasNone()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new UserPreferencesService(context, _loggerMock.Object);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.GetPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("System");
        result.DisplayDensity.Should().Be("Comfortable");
        result.TimeFormat.Should().Be("TwentyFourHour");

        // Verify preferences were persisted
        context.UserPreferences.Should().ContainSingle(p => p.UserId == userId);
    }

    [Fact]
    public async Task GetPreferencesAsync_ReturnsExistingPreferences_WhenUserHasThem()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        // Pre-create preferences
        var preferences = new UserPreferences
        {
            UserId = userId,
            Theme = ThemePreference.Dark,
            DisplayDensity = DisplayDensity.Compact,
            TimeFormat = TimeFormat.TwelveHour,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        var service = new UserPreferencesService(context, _loggerMock.Object);

        // Act
        var result = await service.GetPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("Dark");
        result.DisplayDensity.Should().Be("Compact");
        result.TimeFormat.Should().Be("TwelveHour");
    }

    // =========================================================================
    // UpdatePreferencesAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdatePreferencesAsync_CreatesAndUpdates_WhenUserHasNoPreferences()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new UserPreferencesService(context, _loggerMock.Object);
        var userId = Guid.NewGuid().ToString();

        var request = new UpdateUserPreferencesRequest
        {
            Theme = "Dark",
            DisplayDensity = "Compact"
        };

        // Act
        var result = await service.UpdatePreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("Dark");
        result.DisplayDensity.Should().Be("Compact");
        result.TimeFormat.Should().Be("TwentyFourHour"); // Default not changed
    }

    [Fact]
    public async Task UpdatePreferencesAsync_PartialUpdate_OnlyChangesSpecifiedFields()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        // Pre-create preferences
        var preferences = new UserPreferences
        {
            UserId = userId,
            Theme = ThemePreference.Light,
            DisplayDensity = DisplayDensity.Comfortable,
            TimeFormat = TimeFormat.TwentyFourHour,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        var service = new UserPreferencesService(context, _loggerMock.Object);

        // Only update Theme
        var request = new UpdateUserPreferencesRequest
        {
            Theme = "Dark"
            // DisplayDensity and TimeFormat are null - should not change
        };

        // Act
        var result = await service.UpdatePreferencesAsync(userId, request);

        // Assert
        result.Theme.Should().Be("Dark");
        result.DisplayDensity.Should().Be("Comfortable"); // Unchanged
        result.TimeFormat.Should().Be("TwentyFourHour"); // Unchanged
    }

    [Fact]
    public async Task UpdatePreferencesAsync_IgnoresInvalidEnumValues()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        // Pre-create preferences
        var preferences = new UserPreferences
        {
            UserId = userId,
            Theme = ThemePreference.Light,
            DisplayDensity = DisplayDensity.Comfortable,
            TimeFormat = TimeFormat.TwentyFourHour,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        var service = new UserPreferencesService(context, _loggerMock.Object);

        // Try to set invalid value
        var request = new UpdateUserPreferencesRequest
        {
            Theme = "InvalidTheme"
        };

        // Act
        var result = await service.UpdatePreferencesAsync(userId, request);

        // Assert
        result.Theme.Should().Be("Light"); // Unchanged because invalid value was ignored
    }

    // =========================================================================
    // ResetPreferencesAsync Tests
    // =========================================================================

    [Fact]
    public async Task ResetPreferencesAsync_CreatesDefaults_WhenUserHasNoPreferences()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var service = new UserPreferencesService(context, _loggerMock.Object);
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = await service.ResetPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("System");
        result.DisplayDensity.Should().Be("Comfortable");
        result.TimeFormat.Should().Be("TwentyFourHour");
    }

    [Fact]
    public async Task ResetPreferencesAsync_ResetsToDefaults_WhenUserHasCustomPreferences()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        // Pre-create custom preferences
        var preferences = new UserPreferences
        {
            UserId = userId,
            Theme = ThemePreference.Dark,
            DisplayDensity = DisplayDensity.Compact,
            TimeFormat = TimeFormat.TwelveHour,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        context.UserPreferences.Add(preferences);
        await context.SaveChangesAsync();

        var service = new UserPreferencesService(context, _loggerMock.Object);

        // Act
        var result = await service.ResetPreferencesAsync(userId);

        // Assert
        result.Theme.Should().Be("System"); // Reset to default
        result.DisplayDensity.Should().Be("Comfortable"); // Reset to default
        result.TimeFormat.Should().Be("TwentyFourHour"); // Reset to default
    }
}
