using Cadence.Core.Data;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Email.Services;

/// <summary>
/// Manages user email preferences with in-memory caching for fast lookups.
/// Mandatory categories (Security, Invitations) always return true.
/// </summary>
public class EmailPreferenceService : IEmailPreferenceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmailPreferenceService> _logger;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Categories that cannot be disabled by users.
    /// </summary>
    private static readonly HashSet<EmailCategory> MandatoryCategories =
    [
        EmailCategory.Security,
        EmailCategory.Invitations
    ];

    /// <summary>
    /// Default enabled state for each category (for new users).
    /// </summary>
    private static readonly Dictionary<EmailCategory, bool> DefaultPreferences = new()
    {
        [EmailCategory.Security] = true,
        [EmailCategory.Invitations] = true,
        [EmailCategory.Assignments] = true,
        [EmailCategory.Workflow] = true,
        [EmailCategory.Reminders] = true,
        [EmailCategory.DailyDigest] = false,
        [EmailCategory.WeeklyDigest] = false
    };

    public EmailPreferenceService(
        AppDbContext context,
        ILogger<EmailPreferenceService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<bool> CanSendAsync(string userId, EmailCategory category, CancellationToken ct = default)
    {
        // Mandatory categories always send
        if (MandatoryCategories.Contains(category))
        {
            return true;
        }

        var prefs = await GetPreferencesAsync(userId, ct);
        return prefs.TryGetValue(category, out var isEnabled) && isEnabled;
    }

    public async Task<IReadOnlyDictionary<EmailCategory, bool>> GetPreferencesAsync(
        string userId,
        CancellationToken ct = default)
    {
        var cacheKey = $"email_prefs_{userId}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<EmailCategory, bool>? cached) && cached != null)
        {
            return cached;
        }

        var dbPrefs = await _context.UserEmailPreferences
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        // Start with defaults, override with user preferences
        var result = new Dictionary<EmailCategory, bool>(DefaultPreferences);
        foreach (var pref in dbPrefs)
        {
            result[pref.Category] = pref.IsEnabled;
        }

        // Enforce mandatory categories
        foreach (var mandatory in MandatoryCategories)
        {
            result[mandatory] = true;
        }

        var frozen = (IReadOnlyDictionary<EmailCategory, bool>)result;
        _cache.Set(cacheKey, frozen, CacheDuration);

        return frozen;
    }

    public async Task UpdatePreferenceAsync(
        string userId,
        EmailCategory category,
        bool isEnabled,
        CancellationToken ct = default)
    {
        if (MandatoryCategories.Contains(category) && !isEnabled)
        {
            throw new InvalidOperationException(
                $"Cannot disable mandatory email category '{category}'. " +
                "Security and Invitation emails are required for account functionality.");
        }

        var existing = await _context.UserEmailPreferences
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category == category, ct);

        if (existing != null)
        {
            existing.IsEnabled = isEnabled;
        }
        else
        {
            _context.UserEmailPreferences.Add(new UserEmailPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Category = category,
                IsEnabled = isEnabled
            });
        }

        await _context.SaveChangesAsync(ct);

        // Invalidate cache
        _cache.Remove($"email_prefs_{userId}");

        _logger.LogDebug(
            "Updated email preference for user {UserId}: {Category} = {IsEnabled}",
            userId, category, isEnabled);
    }

    public async Task InitializeDefaultsAsync(string userId, CancellationToken ct = default)
    {
        var existingCount = await _context.UserEmailPreferences
            .IgnoreQueryFilters()
            .CountAsync(p => p.UserId == userId, ct);

        if (existingCount > 0)
        {
            _logger.LogDebug("User {UserId} already has email preferences, skipping initialization.", userId);
            return;
        }

        foreach (var (category, isEnabled) in DefaultPreferences)
        {
            _context.UserEmailPreferences.Add(new UserEmailPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Category = category,
                IsEnabled = isEnabled
            });
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Initialized default email preferences for user {UserId}", userId);
    }

    /// <summary>
    /// Check if a category is mandatory (cannot be disabled).
    /// </summary>
    public static bool IsMandatoryCategory(EmailCategory category) => MandatoryCategories.Contains(category);
}
