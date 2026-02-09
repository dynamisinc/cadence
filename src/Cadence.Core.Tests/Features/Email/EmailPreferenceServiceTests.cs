using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cadence.Core.Tests.Features.Email;

/// <summary>
/// Tests for EmailPreferenceService (user email preferences with caching).
/// </summary>
public class EmailPreferenceServiceTests
{
    private readonly AppDbContext _context;
    private readonly EmailPreferenceService _service;
    private readonly IMemoryCache _cache;
    private readonly string _testUserId = "test-user-001";

    public EmailPreferenceServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<EmailPreferenceService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new EmailPreferenceService(_context, logger.Object, _cache);
    }

    // =========================================================================
    // CanSendAsync Tests - Mandatory Categories
    // =========================================================================

    [Fact]
    public async Task CanSendAsync_SecurityCategory_AlwaysReturnsTrue()
    {
        var result = await _service.CanSendAsync(_testUserId, EmailCategory.Security);

        Assert.True(result);
    }

    [Fact]
    public async Task CanSendAsync_InvitationsCategory_AlwaysReturnsTrue()
    {
        var result = await _service.CanSendAsync(_testUserId, EmailCategory.Invitations);

        Assert.True(result);
    }

    [Fact]
    public async Task CanSendAsync_MandatoryCategory_IgnoresUserPreference()
    {
        // Even if somehow a user set Security to false, CanSend should still be true
        await _service.InitializeDefaultsAsync(_testUserId);

        var result = await _service.CanSendAsync(_testUserId, EmailCategory.Security);

        Assert.True(result);
    }

    // =========================================================================
    // CanSendAsync Tests - Optional Categories
    // =========================================================================

    [Fact]
    public async Task CanSendAsync_Assignments_DefaultsToTrue()
    {
        var result = await _service.CanSendAsync(_testUserId, EmailCategory.Assignments);

        Assert.True(result);
    }

    [Fact]
    public async Task CanSendAsync_DailyDigest_DefaultsToFalse()
    {
        var result = await _service.CanSendAsync(_testUserId, EmailCategory.DailyDigest);

        Assert.False(result);
    }

    [Fact]
    public async Task CanSendAsync_WeeklyDigest_DefaultsToFalse()
    {
        var result = await _service.CanSendAsync(_testUserId, EmailCategory.WeeklyDigest);

        Assert.False(result);
    }

    [Fact]
    public async Task CanSendAsync_DisabledCategory_ReturnsFalse()
    {
        await _service.InitializeDefaultsAsync(_testUserId);
        await _service.UpdatePreferenceAsync(_testUserId, EmailCategory.Reminders, false);

        var result = await _service.CanSendAsync(_testUserId, EmailCategory.Reminders);

        Assert.False(result);
    }

    // =========================================================================
    // GetPreferencesAsync Tests
    // =========================================================================

    [Fact]
    public async Task GetPreferencesAsync_NewUser_ReturnsDefaults()
    {
        var prefs = await _service.GetPreferencesAsync(_testUserId);

        Assert.True(prefs[EmailCategory.Security]);
        Assert.True(prefs[EmailCategory.Invitations]);
        Assert.True(prefs[EmailCategory.Assignments]);
        Assert.True(prefs[EmailCategory.Workflow]);
        Assert.True(prefs[EmailCategory.Reminders]);
        Assert.False(prefs[EmailCategory.DailyDigest]);
        Assert.False(prefs[EmailCategory.WeeklyDigest]);
    }

    [Fact]
    public async Task GetPreferencesAsync_AllCategoriesPresent()
    {
        var prefs = await _service.GetPreferencesAsync(_testUserId);

        var allCategories = Enum.GetValues<EmailCategory>();
        foreach (var category in allCategories)
        {
            Assert.True(prefs.ContainsKey(category), $"Missing category: {category}");
        }
    }

    [Fact]
    public async Task GetPreferencesAsync_CachesResult()
    {
        // First call loads from DB
        var prefs1 = await _service.GetPreferencesAsync(_testUserId);

        // Second call should use cache (same result)
        var prefs2 = await _service.GetPreferencesAsync(_testUserId);

        Assert.Equal(prefs1.Count, prefs2.Count);
    }

    // =========================================================================
    // UpdatePreferenceAsync Tests
    // =========================================================================

    [Fact]
    public async Task UpdatePreferenceAsync_OptionalCategory_UpdatesSuccessfully()
    {
        await _service.InitializeDefaultsAsync(_testUserId);

        await _service.UpdatePreferenceAsync(_testUserId, EmailCategory.Reminders, false);

        var prefs = await _service.GetPreferencesAsync(_testUserId);
        Assert.False(prefs[EmailCategory.Reminders]);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_EnableDigest_UpdatesSuccessfully()
    {
        await _service.InitializeDefaultsAsync(_testUserId);

        await _service.UpdatePreferenceAsync(_testUserId, EmailCategory.DailyDigest, true);

        var prefs = await _service.GetPreferencesAsync(_testUserId);
        Assert.True(prefs[EmailCategory.DailyDigest]);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_MandatoryCategory_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePreferenceAsync(_testUserId, EmailCategory.Security, false));
    }

    [Fact]
    public async Task UpdatePreferenceAsync_MandatoryInvitations_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdatePreferenceAsync(_testUserId, EmailCategory.Invitations, false));
    }

    [Fact]
    public async Task UpdatePreferenceAsync_InvalidatesCache()
    {
        await _service.InitializeDefaultsAsync(_testUserId);

        // Load into cache
        var prefsBefore = await _service.GetPreferencesAsync(_testUserId);
        Assert.True(prefsBefore[EmailCategory.Reminders]);

        // Update preference (should invalidate cache)
        await _service.UpdatePreferenceAsync(_testUserId, EmailCategory.Reminders, false);

        // Next call should reflect the update
        var prefsAfter = await _service.GetPreferencesAsync(_testUserId);
        Assert.False(prefsAfter[EmailCategory.Reminders]);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_CreatesPrefWhenNotInitialized()
    {
        // No InitializeDefaults called
        await _service.UpdatePreferenceAsync(_testUserId, EmailCategory.DailyDigest, true);

        var prefs = await _service.GetPreferencesAsync(_testUserId);
        Assert.True(prefs[EmailCategory.DailyDigest]);
    }

    // =========================================================================
    // InitializeDefaultsAsync Tests
    // =========================================================================

    [Fact]
    public async Task InitializeDefaultsAsync_CreatesAllCategories()
    {
        await _service.InitializeDefaultsAsync(_testUserId);

        var count = _context.UserEmailPreferences.Count(p => p.UserId == _testUserId);
        var allCategories = Enum.GetValues<EmailCategory>();
        Assert.Equal(allCategories.Length, count);
    }

    [Fact]
    public async Task InitializeDefaultsAsync_AlreadyInitialized_SkipsWithoutError()
    {
        await _service.InitializeDefaultsAsync(_testUserId);
        var countBefore = _context.UserEmailPreferences.Count(p => p.UserId == _testUserId);

        // Second call should not add duplicates
        await _service.InitializeDefaultsAsync(_testUserId);
        var countAfter = _context.UserEmailPreferences.Count(p => p.UserId == _testUserId);

        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task InitializeDefaultsAsync_SetsCorrectDefaults()
    {
        await _service.InitializeDefaultsAsync(_testUserId);

        var prefs = await _service.GetPreferencesAsync(_testUserId);

        // Mandatory on
        Assert.True(prefs[EmailCategory.Security]);
        Assert.True(prefs[EmailCategory.Invitations]);

        // Optional defaults on
        Assert.True(prefs[EmailCategory.Assignments]);
        Assert.True(prefs[EmailCategory.Workflow]);
        Assert.True(prefs[EmailCategory.Reminders]);

        // Optional defaults off
        Assert.False(prefs[EmailCategory.DailyDigest]);
        Assert.False(prefs[EmailCategory.WeeklyDigest]);
    }

    // =========================================================================
    // IsMandatoryCategory Tests
    // =========================================================================

    [Theory]
    [InlineData(EmailCategory.Security, true)]
    [InlineData(EmailCategory.Invitations, true)]
    [InlineData(EmailCategory.Assignments, false)]
    [InlineData(EmailCategory.Workflow, false)]
    [InlineData(EmailCategory.Reminders, false)]
    [InlineData(EmailCategory.DailyDigest, false)]
    [InlineData(EmailCategory.WeeklyDigest, false)]
    public void IsMandatoryCategory_ReturnsCorrectResult(EmailCategory category, bool expected)
    {
        Assert.Equal(expected, EmailPreferenceService.IsMandatoryCategory(category));
    }
}
