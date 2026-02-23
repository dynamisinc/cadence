using System.Text.RegularExpressions;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Helper for parsing time and date values from various legacy MSEL formats.
/// Supports standard time strings, Excel serial dates, military DTG, and DateTime+timezone strings.
/// </summary>
public static partial class TimeParsingHelper
{
    private static readonly Dictionary<string, int> MonthAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        { "JAN", 1 }, { "FEB", 2 }, { "MAR", 3 }, { "APR", 4 },
        { "MAY", 5 }, { "JUN", 6 }, { "JUL", 7 }, { "AUG", 8 },
        { "SEP", 9 }, { "OCT", 10 }, { "NOV", 11 }, { "DEC", 12 }
    };

    private static readonly string[] TimeFormats =
    [
        "h:mm tt", "hh:mm tt", "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
        "h:mm:ss tt", "hh:mm:ss tt"
    ];

    /// <summary>
    /// Attempts to parse a military DTG (Date-Time Group) format string.
    /// Format: DDHHmmMMMYYYY or DDHHmmMMMYY (e.g., "210900AUG2011", "210900AUG11").
    /// </summary>
    public static bool TryParseMilitaryDtg(string? value, out DateTime result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var match = MilitaryDtgRegex().Match(value.Trim());
        if (!match.Success)
            return false;

        var day = int.Parse(match.Groups[1].Value);
        var timeStr = match.Groups[2].Value;
        var monthStr = match.Groups[3].Value;
        var yearStr = match.Groups[4].Value;

        var hours = int.Parse(timeStr[..2]);
        var minutes = int.Parse(timeStr[2..]);

        if (!MonthAbbreviations.TryGetValue(monthStr, out var month))
            return false;

        var year = yearStr.Length == 2
            ? 2000 + int.Parse(yearStr)
            : int.Parse(yearStr);

        if (day < 1 || day > 31 || hours > 23 || minutes > 59)
            return false;

        try
        {
            result = new DateTime(year, month, day, hours, minutes, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse a time value from various formats.
    /// Supports: DateTime, TimeSpan, double (Excel fraction), string time formats,
    /// DateTime+timezone strings, and military DTG.
    /// </summary>
    public static bool TryParseTime(object? value, out TimeOnly result)
    {
        result = default;

        if (value == null) return false;

        if (value is DateTime dt)
        {
            result = TimeOnly.FromDateTime(dt);
            return true;
        }

        if (value is TimeSpan ts)
        {
            result = TimeOnly.FromTimeSpan(ts);
            return true;
        }

        if (value is double d)
        {
            var time = TimeSpan.FromDays(d);
            result = TimeOnly.FromTimeSpan(time);
            return true;
        }

        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;

            // Try standard time formats first
            foreach (var format in TimeFormats)
            {
                if (TimeOnly.TryParseExact(s, format, null, System.Globalization.DateTimeStyles.None, out result))
                    return true;
            }

            // Try general TimeOnly parsing
            if (TimeOnly.TryParse(s, out result))
                return true;

            // Strip timezone abbreviation (CDT, EST, PST, etc.) and try DateTime parse
            var tzStripped = TimezoneAbbrevRegex().Replace(s.Trim(), "").Trim();
            if (DateTime.TryParse(tzStripped, out var parsedDt))
            {
                result = TimeOnly.FromDateTime(parsedDt);
                return true;
            }

            // Try military DTG format
            if (TryParseMilitaryDtg(s, out var militaryDt))
            {
                result = TimeOnly.FromDateTime(militaryDt);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse a full DateTime value from various formats.
    /// Supports: DateTime, double (Excel OADate), military DTG, and DateTime+timezone strings.
    /// </summary>
    public static bool TryParseDateTime(object? value, out DateTime result)
    {
        result = default;

        if (value == null) return false;

        if (value is DateTime dt)
        {
            result = dt;
            return true;
        }

        if (value is double d)
        {
            try
            {
                result = DateTime.FromOADate(d);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;

            // Try military DTG first (most specific)
            if (TryParseMilitaryDtg(s, out result))
                return true;

            // Strip timezone abbreviation and try DateTime parse
            var tzStripped = TimezoneAbbrevRegex().Replace(s.Trim(), "").Trim();
            if (DateTime.TryParse(tzStripped, out result))
                return true;

            // Try standard parse
            if (DateTime.TryParse(s, out result))
                return true;
        }

        return false;
    }

    [GeneratedRegex(@"^(\d{2})(\d{4})([A-Za-z]{3})(\d{2,4})$")]
    private static partial Regex MilitaryDtgRegex();

    [GeneratedRegex(@"\s+[A-Z]{2,5}$")]
    private static partial Regex TimezoneAbbrevRegex();
}
