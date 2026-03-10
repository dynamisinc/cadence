using System.Globalization;
using Cadence.Core.Models.Entities;
using ClosedXML.Excel;

namespace Cadence.Core.Features.ExcelExport.Builders;

/// <summary>
/// Shared column definitions and formatting utilities used across Excel worksheet builders.
/// </summary>
internal static class ExcelFormattingHelper
{
    /// <summary>
    /// Column definitions for the core MSEL worksheet fields.
    /// </summary>
    internal static readonly (string Field, string Header, int Width)[] MselColumns =
    {
        ("InjectNumber", "Inject #", 10),
        ("Title", "Title", 40),
        ("Description", "Description", 60),
        ("ScheduledTime", "Scheduled Time", 15),
        ("ScenarioDay", "Scenario Day", 12),
        ("ScenarioTime", "Scenario Time", 15),
        ("Source", "From / Source", 20),
        ("Target", "To / Target", 20),
        ("DeliveryMethod", "Delivery Method", 18),
        ("Track", "Track", 15),
        ("Phase", "Phase", 20),
        ("ExpectedAction", "Expected Action", 50),
        ("ControllerNotes", "Notes", 40),
        ("Priority", "Priority", 10),
        ("LocationName", "Location", 20),
        ("ResponsibleController", "Responsible Controller", 20),
    };

    /// <summary>
    /// Additional columns appended when conduct data (firing status) is included in an export.
    /// </summary>
    internal static readonly (string Field, string Header, int Width)[] ConductColumns =
    {
        ("Status", "Status", 12),
        ("FiredAt", "Fired At", 20),
        ("FiredBy", "Fired By", 20),
    };

    /// <summary>
    /// Column definitions for the observations worksheet.
    /// </summary>
    internal static readonly (string Field, string Header, int Width)[] ObservationColumns =
    {
        ("ObservedAt", "Timestamp", 20),
        ("Observer", "Observer", 25),
        ("RelatedInject", "Related Inject", 30),
        ("Content", "Observation", 60),
        ("Rating", "Rating (P/S/M/U)", 18),
        ("Recommendation", "Recommendation", 50),
        ("Location", "Location", 20),
        ("RelatedObjective", "Related Objective", 30),
    };

    /// <summary>
    /// Returns the display string for the delivery method on an inject,
    /// resolving custom "Other" values and lookup names.
    /// </summary>
    /// <param name="inject">The inject whose delivery method is resolved.</param>
    /// <returns>A human-readable delivery method string.</returns>
    internal static string GetDeliveryMethodDisplay(Inject inject)
    {
        if (inject.DeliveryMethodLookup != null)
        {
            if (inject.DeliveryMethodLookup.IsOther && !string.IsNullOrEmpty(inject.DeliveryMethodOther))
            {
                return inject.DeliveryMethodOther;
            }
            return inject.DeliveryMethodLookup.Name;
        }
        return inject.DeliveryMethod?.ToString() ?? "";
    }

    /// <summary>
    /// Returns the display string for an observation rating using HSEEP P/S/M/U notation.
    /// </summary>
    /// <param name="rating">The rating value to display.</param>
    /// <returns>A formatted rating string, or an empty string when no rating is set.</returns>
    internal static string GetRatingDisplay(ObservationRating? rating)
    {
        return rating switch
        {
            ObservationRating.Performed => "P - Performed",
            ObservationRating.Satisfactory => "S - Satisfactory",
            ObservationRating.Marginal => "M - Marginal",
            ObservationRating.Unsatisfactory => "U - Unsatisfactory",
            _ => ""
        };
    }

    /// <summary>
    /// Escapes a field value for inclusion in a CSV file, wrapping in quotes when necessary.
    /// </summary>
    /// <param name="field">The field value to escape.</param>
    /// <returns>A CSV-safe string.</returns>
    internal static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        // If field contains comma, quote, or newline, wrap in quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    /// <summary>
    /// Applies the standard bold header formatting to a row of cells.
    /// </summary>
    /// <param name="ws">The worksheet being formatted.</param>
    /// <param name="columns">Column definitions providing headers and widths.</param>
    /// <param name="headerColor">Background color for header cells.</param>
    /// <param name="includeFormatting">When false, no styles are applied.</param>
    internal static void ApplyHeaderRow(
        IXLWorksheet ws,
        (string Field, string Header, int Width)[] columns,
        XLColor headerColor,
        bool includeFormatting)
    {
        for (int i = 0; i < columns.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = columns[i].Header;

            if (includeFormatting)
            {
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Column(i + 1).Width = columns[i].Width;
            }
        }
    }

    /// <summary>
    /// Generates a filename-safe version of an exercise name by replacing invalid characters and spaces.
    /// </summary>
    /// <param name="name">The raw exercise name.</param>
    /// <returns>A sanitised string suitable for use in a filename.</returns>
    internal static string GenerateSafeFilename(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return safeName.Replace(" ", "_");
    }

    /// <summary>
    /// Generates a dated MSEL export filename from an exercise name.
    /// </summary>
    /// <param name="exerciseName">The exercise name.</param>
    /// <returns>A filename base string (without extension).</returns>
    internal static string GenerateMselFilename(string exerciseName)
    {
        var safeName = GenerateSafeFilename(exerciseName);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{safeName}_MSEL_{date}";
    }

    /// <summary>
    /// Generates a dated observations export filename from an exercise name.
    /// </summary>
    /// <param name="exerciseName">The exercise name.</param>
    /// <returns>A filename base string (without extension).</returns>
    internal static string GenerateObservationsFilename(string exerciseName)
    {
        var safeName = GenerateSafeFilename(exerciseName);
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"{safeName}_Observations_{date}";
    }
}
