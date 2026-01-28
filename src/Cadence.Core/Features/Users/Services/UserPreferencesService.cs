using Cadence.Core.Data;
using Cadence.Core.Features.Users.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Users.Services;

/// <summary>
/// Service for user preferences operations.
/// Implements preferences read/update with automatic creation of defaults.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        AppDbContext context,
        ILogger<UserPreferencesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserPreferencesDto> GetPreferencesAsync(string userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences for new user
            preferences = UserPreferencesMapper.CreateDefault(userId);
            _context.UserPreferences.Add(preferences);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default preferences for user {UserId}", userId);
        }

        return preferences.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserPreferencesDto> UpdatePreferencesAsync(string userId, UpdateUserPreferencesRequest request)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences first, then apply updates
            preferences = UserPreferencesMapper.CreateDefault(userId);
            _context.UserPreferences.Add(preferences);
        }

        // Apply the update
        preferences.ApplyUpdate(request);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated preferences for user {UserId}: Theme={Theme}, Density={Density}, TimeFormat={TimeFormat}",
            userId,
            preferences.Theme,
            preferences.DisplayDensity,
            preferences.TimeFormat);

        return preferences.ToDto();
    }

    /// <inheritdoc />
    public async Task<UserPreferencesDto> ResetPreferencesAsync(string userId)
    {
        var preferences = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preferences == null)
        {
            // Create default preferences
            preferences = UserPreferencesMapper.CreateDefault(userId);
            _context.UserPreferences.Add(preferences);
        }
        else
        {
            // Reset to defaults
            preferences.Theme = ThemePreference.System;
            preferences.DisplayDensity = DisplayDensity.Comfortable;
            preferences.TimeFormat = TimeFormat.TwentyFourHour;
            preferences.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reset preferences to defaults for user {UserId}", userId);

        return preferences.ToDto();
    }
}
