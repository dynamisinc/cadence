using Cadence.Core.Features.SystemSettings.Models.DTOs;
using Cadence.Core.Features.SystemSettings.Models.Entities;
using Cadence.Core.Features.SystemSettings.Services;
using Cadence.Core.Tests.Helpers;
using FluentAssertions;
using SystemSettingsEntity = Cadence.Core.Features.SystemSettings.Models.Entities.SystemSettings;

namespace Cadence.Core.Tests.Features.SystemSettings;

public class EulaServiceTests
{
    private EulaService CreateService(string? dbName = null)
    {
        var context = TestDbContextFactory.Create(dbName);
        return new EulaService(context);
    }

    // =========================================================================
    // GetStatusAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetStatusAsync_NoSystemSettings_ReturnsNotRequired()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetStatusAsync("user-123");

        // Assert
        result.Required.Should().BeFalse();
        result.Version.Should().BeNull();
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_NoEulaConfigured_ReturnsNotRequired()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = null,
            EulaContent = null
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));

        // Act
        var result = await service.GetStatusAsync("user-123");

        // Assert
        result.Required.Should().BeFalse();
        result.Version.Should().BeNull();
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_EulaConfiguredNotAccepted_ReturnsRequired()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = "1.0",
            EulaContent = "# Terms\nPlease agree."
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));

        // Act
        var result = await service.GetStatusAsync("user-123");

        // Assert
        result.Required.Should().BeTrue();
        result.Version.Should().Be("1.0");
        result.Content.Should().Be("# Terms\nPlease agree.");
    }

    [Fact]
    public async Task GetStatusAsync_EulaConfiguredAlreadyAccepted_ReturnsNotRequired()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = "1.0",
            EulaContent = "# Terms\nPlease agree."
        });
        context.EulaAcceptances.Add(new EulaAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            EulaVersion = "1.0",
            AcceptedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));

        // Act
        var result = await service.GetStatusAsync("user-123");

        // Assert
        result.Required.Should().BeFalse();
        result.Version.Should().Be("1.0");
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_DifferentVersionAccepted_ReturnsRequired()
    {
        // Arrange — user accepted v1.0 but current EULA is v2.0
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = "2.0",
            EulaContent = "# Updated Terms\nNew content."
        });
        context.EulaAcceptances.Add(new EulaAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            EulaVersion = "1.0",
            AcceptedAt = DateTime.UtcNow.AddDays(-30)
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));

        // Act
        var result = await service.GetStatusAsync("user-123");

        // Assert
        result.Required.Should().BeTrue();
        result.Version.Should().Be("2.0");
        result.Content.Should().Be("# Updated Terms\nNew content.");
    }

    // =========================================================================
    // AcceptAsync Tests
    // =========================================================================

    [Fact]
    public async Task AcceptAsync_EmptyVersion_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.AcceptAsync("user-123", "   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("version");
    }

    [Fact]
    public async Task AcceptAsync_NoEulaConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.AcceptAsync("user-123", "1.0");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No EULA is currently configured*");
    }

    [Fact]
    public async Task AcceptAsync_WrongVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = "2.0",
            EulaContent = "# Terms"
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));

        // Act
        var act = async () => await service.AcceptAsync("user-123", "1.0");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not match the current version*");
    }

    [Fact]
    public async Task AcceptAsync_ValidVersion_CreatesAcceptanceRecord()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = "1.0",
            EulaContent = "# Terms"
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));

        // Act
        await service.AcceptAsync("user-123", "1.0");

        // Assert — verify record persisted by querying a fresh context
        var verifyContext = TestDbContextFactory.Create(dbName);
        var acceptance = verifyContext.EulaAcceptances
            .SingleOrDefault(a => a.UserId == "user-123" && a.EulaVersion == "1.0");

        acceptance.Should().NotBeNull();
        acceptance!.UserId.Should().Be("user-123");
        acceptance.EulaVersion.Should().Be("1.0");
        acceptance.AcceptedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AcceptAsync_AlreadyAccepted_IsIdempotent()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var context = TestDbContextFactory.Create(dbName);
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            EulaVersion = "1.0",
            EulaContent = "# Terms"
        });
        await context.SaveChangesAsync();

        var service = new EulaService(TestDbContextFactory.Create(dbName));
        await service.AcceptAsync("user-123", "1.0");

        // Act — accept again with a fresh service instance
        var service2 = new EulaService(TestDbContextFactory.Create(dbName));
        var act = async () => await service2.AcceptAsync("user-123", "1.0");

        // Assert — no exception thrown, still only one record
        await act.Should().NotThrowAsync();

        var verifyContext = TestDbContextFactory.Create(dbName);
        var acceptanceCount = verifyContext.EulaAcceptances
            .Count(a => a.UserId == "user-123" && a.EulaVersion == "1.0");

        acceptanceCount.Should().Be(1);
    }
}
