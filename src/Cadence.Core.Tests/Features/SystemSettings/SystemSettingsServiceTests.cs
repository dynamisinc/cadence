using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.SystemSettings.Services;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Options;

namespace Cadence.Core.Tests.Features.SystemSettings;

public class SystemSettingsServiceTests
{
    private readonly EmailServiceOptions _defaults = new()
    {
        DefaultSenderAddress = "noreply@default.com",
        DefaultSenderName = "Default Cadence",
        SupportAddress = "support@default.com"
    };

    private SystemSettingsService CreateService(string? dbName = null)
    {
        var context = TestDbContextFactory.Create(dbName);
        return new SystemSettingsService(context, Options.Create(_defaults));
    }

    // =========================================================================
    // GetSettingsAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetSettings_NoRowExists_ReturnsDefaultEffectiveValues()
    {
        var service = CreateService();

        var result = await service.GetSettingsAsync();

        Assert.Null(result.Id);
        Assert.Null(result.SupportAddress);
        Assert.Null(result.DefaultSenderAddress);
        Assert.Null(result.DefaultSenderName);
        Assert.Equal("support@default.com", result.EffectiveSupportAddress);
        Assert.Equal("noreply@default.com", result.EffectiveDefaultSenderAddress);
        Assert.Equal("Default Cadence", result.EffectiveDefaultSenderName);
    }

    [Fact]
    public async Task GetSettings_RowExists_ReturnsOverriddenValues()
    {
        var dbName = Guid.NewGuid().ToString();
        var service = CreateService(dbName);

        // Seed a settings row
        await service.UpdateSettingsAsync(new()
        {
            SupportAddress = "custom-support@org.com",
            DefaultSenderAddress = null,
            DefaultSenderName = "Custom Name"
        }, "admin@test.com");

        // Re-create service against same DB to simulate fresh request
        var service2 = new SystemSettingsService(
            TestDbContextFactory.Create(dbName),
            Options.Create(_defaults));

        var result = await service2.GetSettingsAsync();

        Assert.NotNull(result.Id);
        Assert.Equal("custom-support@org.com", result.SupportAddress);
        Assert.Null(result.DefaultSenderAddress);
        Assert.Equal("Custom Name", result.DefaultSenderName);
        Assert.Equal("custom-support@org.com", result.EffectiveSupportAddress);
        Assert.Equal("noreply@default.com", result.EffectiveDefaultSenderAddress); // Falls back to default
        Assert.Equal("Custom Name", result.EffectiveDefaultSenderName);
    }

    // =========================================================================
    // UpdateSettingsAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdateSettings_NoRowExists_CreatesNewRow()
    {
        var service = CreateService();

        var result = await service.UpdateSettingsAsync(new()
        {
            SupportAddress = "new-support@test.com",
            DefaultSenderAddress = "new-sender@test.com",
            DefaultSenderName = "New Name"
        }, "admin@test.com");

        Assert.NotNull(result.Id);
        Assert.Equal("new-support@test.com", result.SupportAddress);
        Assert.Equal("new-sender@test.com", result.DefaultSenderAddress);
        Assert.Equal("New Name", result.DefaultSenderName);
        Assert.Equal("admin@test.com", result.UpdatedBy);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateSettings_RowExists_UpdatesExistingRow()
    {
        var service = CreateService();

        // Create initial
        var first = await service.UpdateSettingsAsync(new()
        {
            SupportAddress = "first@test.com",
        }, "admin1@test.com");

        // Update
        var second = await service.UpdateSettingsAsync(new()
        {
            SupportAddress = "second@test.com",
        }, "admin2@test.com");

        Assert.Equal(first.Id, second.Id); // Same row
        Assert.Equal("second@test.com", second.SupportAddress);
        Assert.Equal("admin2@test.com", second.UpdatedBy);
    }

    [Fact]
    public async Task UpdateSettings_EmptyString_NormalizesToNull()
    {
        var service = CreateService();

        var result = await service.UpdateSettingsAsync(new()
        {
            SupportAddress = "  ",
            DefaultSenderAddress = "",
            DefaultSenderName = null
        }, "admin@test.com");

        Assert.Null(result.SupportAddress);
        Assert.Null(result.DefaultSenderAddress);
        Assert.Null(result.DefaultSenderName);
        // Effective values fall back to defaults
        Assert.Equal("support@default.com", result.EffectiveSupportAddress);
        Assert.Equal("noreply@default.com", result.EffectiveDefaultSenderAddress);
        Assert.Equal("Default Cadence", result.EffectiveDefaultSenderName);
    }

    [Fact]
    public async Task UpdateSettings_ClearOverride_RevertsToDefault()
    {
        var service = CreateService();

        // Set override
        await service.UpdateSettingsAsync(new()
        {
            SupportAddress = "custom@test.com",
        }, "admin@test.com");

        // Clear it
        var result = await service.UpdateSettingsAsync(new()
        {
            SupportAddress = null,
        }, "admin@test.com");

        Assert.Null(result.SupportAddress);
        Assert.Equal("support@default.com", result.EffectiveSupportAddress);
    }
}
