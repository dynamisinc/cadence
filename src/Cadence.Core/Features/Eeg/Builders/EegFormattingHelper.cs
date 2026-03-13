using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.Eeg.Builders;

/// <summary>
/// Shared formatting helpers and rating display utilities for EEG Excel exports.
/// Contains color dictionaries and text-formatting functions used by all worksheet builders.
/// </summary>
internal static class EegFormattingHelper
{
    /// <summary>
    /// Foreground (font) colors keyed by performance rating.
    /// </summary>
    internal static readonly Dictionary<PerformanceRating, XLColor> RatingColors = new()
    {
        { PerformanceRating.Performed, XLColor.FromHtml("#4caf50") },           // Green
        { PerformanceRating.SomeChallenges, XLColor.FromHtml("#ff9800") },      // Orange
        { PerformanceRating.MajorChallenges, XLColor.FromHtml("#f44336") },     // Red
        { PerformanceRating.UnableToPerform, XLColor.FromHtml("#9e9e9e") },     // Grey
    };

    /// <summary>
    /// Background (fill) colors keyed by performance rating.
    /// </summary>
    internal static readonly Dictionary<PerformanceRating, XLColor> RatingBackgroundColors = new()
    {
        { PerformanceRating.Performed, XLColor.FromHtml("#e8f5e9") },
        { PerformanceRating.SomeChallenges, XLColor.FromHtml("#fff3e0") },
        { PerformanceRating.MajorChallenges, XLColor.FromHtml("#ffebee") },
        { PerformanceRating.UnableToPerform, XLColor.FromHtml("#fafafa") },
    };

    /// <summary>
    /// Returns the full human-readable label for a performance rating
    /// (e.g., "P - Performed").
    /// </summary>
    /// <param name="rating">The performance rating to display.</param>
    /// <returns>Display string suitable for summary tables.</returns>
    internal static string GetRatingDisplay(PerformanceRating rating) =>
        rating switch
        {
            PerformanceRating.Performed => "P - Performed",
            PerformanceRating.SomeChallenges => "S - Some Challenges",
            PerformanceRating.MajorChallenges => "M - Major Challenges",
            PerformanceRating.UnableToPerform => "U - Unable to Perform",
            _ => ""
        };

    /// <summary>
    /// Returns the single-letter short code for a performance rating
    /// (e.g., "P", "S", "M", "U").
    /// </summary>
    /// <param name="rating">The performance rating to abbreviate.</param>
    /// <returns>Single-character code suitable for compact columns.</returns>
    internal static string GetRatingShortCode(PerformanceRating rating) =>
        rating switch
        {
            PerformanceRating.Performed => "P",
            PerformanceRating.SomeChallenges => "S",
            PerformanceRating.MajorChallenges => "M",
            PerformanceRating.UnableToPerform => "U",
            _ => ""
        };

    /// <summary>
    /// Truncates text to the specified maximum length, appending "..." when truncated.
    /// Returns an empty string for null or empty input.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum character length including the ellipsis.</param>
    /// <returns>Truncated string, or the original string if within the limit.</returns>
    internal static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }
}
