using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.SystemSettings.Services;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SystemSettingsEntity = Cadence.Core.Features.SystemSettings.Models.Entities.SystemSettings;

namespace Cadence.Core.Tests.Features.SystemSettings;

public class EmailConfigurationProviderTests
{
    private readonly EmailServiceOptions _defaults = new()
    {
        DefaultSenderAddress = "noreply@default.com",
        DefaultSenderName = "Default Cadence",
        SupportAddress = "support@default.com"
    };

    [Fact]
    public async Task GetConfiguration_NoOverrides_ReturnsDefaults()
    {
        var context = TestDbContextFactory.Create();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new EmailConfigurationProvider(context, Options.Create(_defaults), cache);

        var config = await provider.GetConfigurationAsync();

        Assert.Equal("support@default.com", config.SupportAddress);
        Assert.Equal("noreply@default.com", config.DefaultSenderAddress);
        Assert.Equal("Default Cadence", config.DefaultSenderName);
    }

    [Fact]
    public async Task GetConfiguration_WithOverrides_ReturnsOverriddenValues()
    {
        var context = TestDbContextFactory.Create();
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            SupportAddress = "custom-support@org.com",
            DefaultSenderAddress = "custom-sender@org.com",
            DefaultSenderName = "Custom Org",
            UpdatedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new EmailConfigurationProvider(context, Options.Create(_defaults), cache);

        var config = await provider.GetConfigurationAsync();

        Assert.Equal("custom-support@org.com", config.SupportAddress);
        Assert.Equal("custom-sender@org.com", config.DefaultSenderAddress);
        Assert.Equal("Custom Org", config.DefaultSenderName);
    }

    [Fact]
    public async Task GetConfiguration_PartialOverrides_MergesWithDefaults()
    {
        var context = TestDbContextFactory.Create();
        context.SystemSettings.Add(new SystemSettingsEntity
        {
            Id = Guid.NewGuid(),
            SupportAddress = "custom-support@org.com",
            DefaultSenderAddress = null, // Not overridden
            DefaultSenderName = null,     // Not overridden
            UpdatedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new EmailConfigurationProvider(context, Options.Create(_defaults), cache);

        var config = await provider.GetConfigurationAsync();

        Assert.Equal("custom-support@org.com", config.SupportAddress);
        Assert.Equal("noreply@default.com", config.DefaultSenderAddress);  // Default
        Assert.Equal("Default Cadence", config.DefaultSenderName);          // Default
    }

    [Fact]
    public async Task GetConfiguration_CachesResult()
    {
        var context = TestDbContextFactory.Create();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new EmailConfigurationProvider(context, Options.Create(_defaults), cache);

        // First call hits DB
        var config1 = await provider.GetConfigurationAsync();
        // Second call should return cached
        var config2 = await provider.GetConfigurationAsync();

        Assert.Same(config1, config2);
    }
}
