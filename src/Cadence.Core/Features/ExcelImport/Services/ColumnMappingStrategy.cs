using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Provides static column-pattern dictionaries and the auto-mapping algorithm used to
/// suggest Cadence field mappings from Excel column headers.
/// </summary>
internal static class ColumnMappingStrategy
{
    // -----------------------------------------------------------------------
    // Pattern dictionaries
    // -----------------------------------------------------------------------

    /// <summary>
    /// Maps Cadence field names to arrays of column header patterns to match against.
    /// Matching is case-insensitive. The first exact match wins (confidence 100),
    /// followed by a contains match (confidence 80).
    /// </summary>
    public static readonly Dictionary<string, string[]> ColumnPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "InjectNumber",          new[] { "#", "number", "inject number", "inj #", "inject #", "inject no", "no", "id", "msel number", "msel #", "msel no", "inj#" } },
        { "Title",                 new[] { "title", "inject title", "name", "inject name", "event", "event title", "inject", "subject" } },
        { "Description",           new[] { "description", "desc", "details", "inject description", "narrative", "inject details", "text", "description/script", "detailed statement", "script" } },
        { "ScheduledTime",         new[] { "time", "scheduled time", "scheduled", "wall clock", "wall time", "delivery time", "inject dtg", "dtg", "date time", "actual dtg" } },
        { "ScenarioDay",           new[] { "day", "scenario day", "exercise day", "sim day" } },
        { "ScenarioTime",          new[] { "scenario time", "sim time", "story time", "exercise time" } },
        { "Source",                new[] { "source", "from", "sender", "originator", "sent by", "send from", "sent from", "initiated by" } },
        { "Target",                new[] { "target", "to", "recipient", "receiver", "sent to", "for", "send to" } },
        { "Track",                 new[] { "track", "lane", "functional area", "area", "storyline/thread", "storyline/ thread", "storyline", "thread", "esf" } },
        { "DeliveryMethod",        new[] { "method", "delivery method", "delivery", "means", "inject mode", "modality" } },
        { "ExpectedAction",        new[] { "expected action", "expected response", "response", "action", "anticipated response", "remarks/expected outcome", "expected outcome" } },
        { "Notes",                 new[] { "notes", "comments", "remarks", "controller notes", "notes/remarks", "comments/notes" } },
        { "Phase",                 new[] { "phase", "exercise phase", "phase name" } },
        { "Priority",              new[] { "priority", "importance" } },
        { "LocationName",          new[] { "location", "location name", "place", "venue" } },
        { "LocationType",          new[] { "location type", "venue type" } },
        { "ResponsibleController", new[] { "controller", "responsible controller", "assigned to", "owner", "injected by", "poc", "inject author" } },
        { "InjectType",            new[] { "inject type", "category", "inject category" } },
        { "TriggerType",           new[] { "trigger", "trigger type", "activation", "fire mode" } },
    };

    /// <summary>
    /// Maps common legacy MSEL vocabulary to <see cref="InjectType"/> enum values.
    /// </summary>
    public static readonly Dictionary<string, InjectType> InjectTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Standard synonyms
        { "standard",       InjectType.Standard },
        { "normal",         InjectType.Standard },
        { "scheduled",      InjectType.Standard },
        { "planned",        InjectType.Standard },
        { "regular",        InjectType.Standard },
        { "primary",        InjectType.Standard },
        // Contingency synonyms
        { "contingency",    InjectType.Contingency },
        { "backup",         InjectType.Contingency },
        { "alternate",      InjectType.Contingency },
        { "fallback",       InjectType.Contingency },
        { "reserve",        InjectType.Contingency },
        // Adaptive synonyms
        { "adaptive",       InjectType.Adaptive },
        { "branch",         InjectType.Adaptive },
        { "branching",      InjectType.Adaptive },
        { "conditional",    InjectType.Adaptive },
        { "decision",       InjectType.Adaptive },
        // Complexity synonyms
        { "complexity",     InjectType.Complexity },
        { "advanced",       InjectType.Complexity },
        { "challenge",      InjectType.Complexity },
        { "escalation",     InjectType.Complexity },
        { "difficult",      InjectType.Complexity },
        // Legacy MSEL type values
        { "administrative", InjectType.Standard },
        { "contextual",     InjectType.Standard },
        { "expected action",InjectType.Standard },
        { "contingent",     InjectType.Contingency },
        { "information",    InjectType.Standard },
        { "operational",    InjectType.Standard },
    };

    /// <summary>
    /// Maps common trigger-type vocabulary to <see cref="TriggerType"/> enum values.
    /// </summary>
    public static readonly Dictionary<string, TriggerType> TriggerTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Manual synonyms - controller/staff initiated
        { "manual",           TriggerType.Manual },
        { "controller",       TriggerType.Manual },
        { "controller action",TriggerType.Manual },
        { "actor action",     TriggerType.Manual },
        { "staff action",     TriggerType.Manual },
        { "human",            TriggerType.Manual },
        { "hand",             TriggerType.Manual },
        // Scheduled synonyms - time-based automatic
        { "scheduled",        TriggerType.Scheduled },
        { "auto",             TriggerType.Scheduled },
        { "automatic",        TriggerType.Scheduled },
        { "timed",            TriggerType.Scheduled },
        { "time-based",       TriggerType.Scheduled },
        { "time",             TriggerType.Scheduled },
        // Conditional synonyms - triggered by events/actions
        { "conditional",      TriggerType.Conditional },
        { "triggered",        TriggerType.Conditional },
        { "event",            TriggerType.Conditional },
        { "event-based",      TriggerType.Conditional },
        { "player action",    TriggerType.Conditional },
        { "inject",           TriggerType.Conditional },
        { "dependent",        TriggerType.Conditional },
        { "contingent",       TriggerType.Conditional },
    };

    /// <summary>
    /// Values that look like delivery methods — used to warn the user when they
    /// may have mapped a Delivery Method column to Inject Type.
    /// </summary>
    public static readonly HashSet<string> DeliveryMethodLikeValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "radio", "phone", "email", "verbal", "written", "in person", "in-person", "face to face",
        "text", "sms", "fax", "simulation", "sim", "messenger", "call"
    };

    /// <summary>
    /// Values that look like trigger types — used to warn when mapped to Inject Type.
    /// </summary>
    public static readonly HashSet<string> TriggerTypeLikeValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "controller action", "actor action", "player action", "staff action", "inject",
        "manual", "automatic", "auto", "scheduled", "timed", "conditional", "triggered",
        "time-based", "event-based", "dependent", "contingent"
    };

    /// <summary>
    /// Maps common legacy delivery-method vocabulary to canonical Cadence delivery-method names.
    /// </summary>
    public static readonly Dictionary<string, string> DeliveryMethodSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Verbal synonyms
        { "in person",        "Verbal" },
        { "in-person",        "Verbal" },
        { "face to face",     "Verbal" },
        { "face-to-face",     "Verbal" },
        { "spoken",           "Verbal" },
        { "oral",             "Verbal" },
        { "runner",           "Verbal" },
        // Phone synonyms
        { "call",             "Phone" },
        { "telephone",        "Phone" },
        { "text",             "Phone" },
        { "sms",              "Phone" },
        { "message",          "Phone" },
        { "cell phone call",  "Phone" },
        { "cell phone",       "Phone" },
        { "mobile",           "Phone" },
        // Email synonyms
        { "e-mail",           "Email" },
        { "electronic mail",  "Email" },
        { "msg",              "Email" },
        // Written synonyms
        { "fax",              "Written" },
        { "document",         "Written" },
        { "paper",            "Written" },
        { "memo",             "Written" },
        { "letter",           "Written" },
        { "handout",          "Written" },
        { "courier",          "Written" },
        // Simulation synonyms
        { "sim",              "Simulation" },
        { "cax",              "Simulation" },
        { "computer aided",   "Simulation" },
        { "simulated",        "Simulation" },
        { "emits",            "Simulation" },
        { "jiee",             "Simulation" },
    };

    // -----------------------------------------------------------------------
    // Auto-mapping algorithm
    // -----------------------------------------------------------------------

    /// <summary>
    /// Finds the best-matching column index for a given Cadence field name by scanning
    /// <paramref name="columns"/> against <see cref="ColumnPatterns"/>.
    /// </summary>
    /// <returns>
    /// A tuple of (<c>index</c>, <c>confidence</c>) where <c>index</c> is -1 when no
    /// match is found, and <c>confidence</c> is 100 for exact matches or 80 for
    /// contains matches.
    /// </returns>
    public static (int Index, int Confidence) FindBestMatchingColumn(
        string fieldName,
        IReadOnlyList<ColumnInfoDto> columns)
    {
        if (!ColumnPatterns.TryGetValue(fieldName, out var patterns))
        {
            return (-1, 0);
        }

        foreach (var column in columns)
        {
            var headerLower = column.Header.ToLowerInvariant();

            foreach (var pattern in patterns)
            {
                if (headerLower == pattern)
                {
                    return (column.Index, 100); // Exact match
                }

                if (headerLower.Contains(pattern))
                {
                    return (column.Index, 80); // Contains match
                }
            }
        }

        return (-1, 0);
    }
}
