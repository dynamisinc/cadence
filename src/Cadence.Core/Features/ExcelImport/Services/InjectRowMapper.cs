using Cadence.Core.Features.ExcelImport.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.ExcelImport.Services;

/// <summary>
/// Result of mapping a single import row to an <see cref="Inject"/> entity.
/// Contains any new phases that need to be added to the DbContext and any warnings generated.
/// </summary>
internal record MappingResult(List<Phase> NewPhases, List<string> Warnings);

/// <summary>
/// Pure-function mapper that applies column mappings from an import row onto an <see cref="Inject"/> entity.
/// Returns side-effect data (new phases, warnings) for the caller to apply.
/// </summary>
internal static class InjectRowMapper
{
    /// <summary>
    /// Maps row values into the properties of an <see cref="Inject"/> entity
    /// according to the configured column mappings.
    /// </summary>
    /// <returns>A <see cref="MappingResult"/> containing any new phases created and warnings generated.</returns>
    public static MappingResult MapRowToInject(
        Inject inject,
        Dictionary<string, object?> values,
        IReadOnlyList<ColumnMappingDto> mappings,
        Dictionary<string, Phase> phases,
        Dictionary<string, DeliveryMethodLookup> deliveryMethods,
        Guid exerciseId,
        Guid organizationId,
        bool createMissingPhases)
    {
        var newPhases = new List<Phase>();
        var warnings = new List<string>();

        foreach (var mapping in mappings.Where(m => m.SourceColumnIndex.HasValue))
        {
            if (!values.TryGetValue(mapping.CadenceField, out var value) || RowValidationService.IsEmpty(value))
            {
                continue;
            }

            var stringValue = value?.ToString()?.Trim();

            switch (mapping.CadenceField)
            {
                case "InjectNumber":
                    // InjectNumber is auto-assigned; store the source value as a reference
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        inject.SourceReference = stringValue;
                    }
                    break;

                case "Title":
                    inject.Title = stringValue ?? "";
                    break;

                case "Description":
                    inject.Description = stringValue ?? "";
                    break;

                case "ScheduledTime":
                    if (TimeParsingHelper.TryParseTime(value, out var time))
                    {
                        inject.ScheduledTime = time;
                    }
                    else if (TimeParsingHelper.TryParseDateTime(value, out var fullDt))
                    {
                        inject.ScheduledTime = TimeOnly.FromDateTime(fullDt);
                        // Populate ScenarioDay from the date portion if not already set
                        if (inject.ScenarioDay == null)
                        {
                            inject.ScenarioDay = fullDt.Day;
                        }
                    }
                    break;

                case "ScenarioDay":
                    if (int.TryParse(stringValue, out var day))
                    {
                        inject.ScenarioDay = day;
                    }
                    break;

                case "ScenarioTime":
                    if (TimeParsingHelper.TryParseTime(value, out var scenarioTime))
                    {
                        inject.ScenarioTime = scenarioTime;
                    }
                    break;

                case "Source":
                    inject.Source = stringValue;
                    break;

                case "Target":
                    inject.Target = stringValue ?? "";
                    break;

                case "Track":
                    inject.Track = stringValue;
                    break;

                case "DeliveryMethod":
                    if (stringValue != null
                        && deliveryMethods.TryGetValue(stringValue.ToLowerInvariant(), out var method))
                    {
                        inject.DeliveryMethodId = method.Id;
                    }
                    else if (!string.IsNullOrEmpty(stringValue)
                             && ColumnMappingStrategy.DeliveryMethodSynonyms.TryGetValue(stringValue, out var canonicalName)
                             && deliveryMethods.TryGetValue(canonicalName.ToLowerInvariant(), out var synonymMethod))
                    {
                        inject.DeliveryMethodId = synonymMethod.Id;
                    }
                    else if (!string.IsNullOrEmpty(stringValue))
                    {
                        var otherMethod = deliveryMethods.Values.FirstOrDefault(m => m.IsOther);
                        if (otherMethod != null)
                        {
                            inject.DeliveryMethodId = otherMethod.Id;
                            inject.DeliveryMethodOther = stringValue;
                        }
                    }
                    break;

                case "ExpectedAction":
                    inject.ExpectedAction = stringValue;
                    break;

                case "Notes":
                    inject.ControllerNotes = stringValue;
                    break;

                case "Phase":
                    if (stringValue != null)
                    {
                        var phaseLower = stringValue.ToLowerInvariant();
                        if (phases.TryGetValue(phaseLower, out var phase))
                        {
                            inject.PhaseId = phase.Id;
                        }
                        else if (createMissingPhases)
                        {
                            var newPhase = new Phase
                            {
                                Id = Guid.NewGuid(),
                                ExerciseId = exerciseId,
                                OrganizationId = organizationId,
                                Name = stringValue,
                                Sequence = phases.Count + 1
                            };
                            phases[phaseLower] = newPhase;
                            inject.PhaseId = newPhase.Id;
                            newPhases.Add(newPhase);
                        }
                        else
                        {
                            warnings.Add($"Phase '{stringValue}' not found and will not be assigned.");
                        }
                    }
                    break;

                case "Priority":
                    if (int.TryParse(stringValue, out var priority))
                    {
                        inject.Priority = Math.Clamp(priority, 1, 5);
                    }
                    break;

                case "LocationName":
                    inject.LocationName = stringValue;
                    break;

                case "LocationType":
                    inject.LocationType = stringValue;
                    break;

                case "ResponsibleController":
                    inject.ResponsibleController = stringValue;
                    break;

                case "InjectType":
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        if (ColumnMappingStrategy.InjectTypeSynonyms.TryGetValue(stringValue, out var injectType))
                        {
                            inject.InjectType = injectType;
                        }
                        else if (ColumnMappingStrategy.TriggerTypeLikeValues.Contains(stringValue)
                                 || ColumnMappingStrategy.TriggerTypeSynonyms.ContainsKey(stringValue))
                        {
                            warnings.Add(
                                $"Row value '{stringValue}' in Inject Type column looks like a trigger type " +
                                "(e.g., Controller Action, Player Action). Consider mapping this column to " +
                                "Trigger Type instead. Defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                        else if (ColumnMappingStrategy.DeliveryMethodLikeValues.Contains(stringValue))
                        {
                            warnings.Add(
                                $"Row value '{stringValue}' in Inject Type column looks like a delivery method. " +
                                "Consider mapping to Delivery Method instead. Defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                        else
                        {
                            warnings.Add($"Unrecognized inject type '{stringValue}', defaulting to Standard.");
                            inject.InjectType = InjectType.Standard;
                        }
                    }
                    break;

                case "TriggerType":
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        if (ColumnMappingStrategy.TriggerTypeSynonyms.TryGetValue(stringValue, out var triggerType))
                        {
                            inject.TriggerType = triggerType;
                        }
                        else
                        {
                            warnings.Add($"Unrecognized trigger type '{stringValue}', defaulting to Manual.");
                            inject.TriggerType = TriggerType.Manual;
                        }
                    }
                    break;
            }
        }

        return new MappingResult(newPhases, warnings);
    }
}
