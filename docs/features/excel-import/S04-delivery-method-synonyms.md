# Story: S04 - Delivery Method Synonym Matching

## User Story

**As an** Administrator or Exercise Director,
**I want** common delivery method variations to be automatically recognized during Excel import,
**So that** I don't have to manually edit my existing MSEL spreadsheets to match exact system terminology.

## Context

Many organizations use different terminology for delivery methods in their MSELs. For example, "in person" instead of "Verbal", "call" instead of "Phone", or "sim" instead of "Simulation". Currently, if an exact match isn't found, the system defaults to "Other" and stores the original value in DeliveryMethodOther.

This story adds synonym matching similar to the InjectType synonym matching, allowing common variations to be automatically mapped to the correct delivery method. This reduces manual data cleanup and improves the import experience.

## Acceptance Criteria

### Verbal Synonyms
- [ ] **Given** an Excel row has "in person" in the delivery method column, **when** import runs, **then** it maps to "Verbal"
- [ ] **Given** an Excel row has "in-person" in the delivery method column, **when** import runs, **then** it maps to "Verbal"
- [ ] **Given** an Excel row has "face to face" in the delivery method column, **when** import runs, **then** it maps to "Verbal"
- [ ] **Given** an Excel row has "face-to-face" in the delivery method column, **when** import runs, **then** it maps to "Verbal"
- [ ] **Given** an Excel row has "spoken" in the delivery method column, **when** import runs, **then** it maps to "Verbal"
- [ ] **Given** an Excel row has "oral" in the delivery method column, **when** import runs, **then** it maps to "Verbal"

### Phone Synonyms
- [ ] **Given** an Excel row has "call" in the delivery method column, **when** import runs, **then** it maps to "Phone"
- [ ] **Given** an Excel row has "telephone" in the delivery method column, **when** import runs, **then** it maps to "Phone"
- [ ] **Given** an Excel row has "text" in the delivery method column, **when** import runs, **then** it maps to "Phone"
- [ ] **Given** an Excel row has "sms" in the delivery method column, **when** import runs, **then** it maps to "Phone"
- [ ] **Given** an Excel row has "message" in the delivery method column, **when** import runs, **then** it maps to "Phone"

### Email Synonyms
- [ ] **Given** an Excel row has "e-mail" in the delivery method column, **when** import runs, **then** it maps to "Email"
- [ ] **Given** an Excel row has "electronic mail" in the delivery method column, **when** import runs, **then** it maps to "Email"
- [ ] **Given** an Excel row has "msg" in the delivery method column, **when** import runs, **then** it maps to "Email"

### Written Synonyms
- [ ] **Given** an Excel row has "fax" in the delivery method column, **when** import runs, **then** it maps to "Written"
- [ ] **Given** an Excel row has "document" in the delivery method column, **when** import runs, **then** it maps to "Written"
- [ ] **Given** an Excel row has "paper" in the delivery method column, **when** import runs, **then** it maps to "Written"
- [ ] **Given** an Excel row has "memo" in the delivery method column, **when** import runs, **then** it maps to "Written"
- [ ] **Given** an Excel row has "letter" in the delivery method column, **when** import runs, **then** it maps to "Written"

### Simulation Synonyms
- [ ] **Given** an Excel row has "sim" in the delivery method column, **when** import runs, **then** it maps to "Simulation"
- [ ] **Given** an Excel row has "cax" in the delivery method column, **when** import runs, **then** it maps to "Simulation"
- [ ] **Given** an Excel row has "computer aided" in the delivery method column, **when** import runs, **then** it maps to "Simulation"
- [ ] **Given** an Excel row has "simulated" in the delivery method column, **when** import runs, **then** it maps to "Simulation"

### Case Insensitivity
- [ ] **Given** an Excel row has "IN PERSON" (uppercase), **when** import runs, **then** it maps to "Verbal"
- [ ] **Given** an Excel row has "Call" (mixed case), **when** import runs, **then** it maps to "Phone"
- [ ] **Given** an Excel row has "SIM" (uppercase), **when** import runs, **then** it maps to "Simulation"

### Fallback Behavior
- [ ] **Given** an Excel row has an unrecognized delivery method value, **when** import runs, **then** it maps to "Other" and stores the value in DeliveryMethodOther
- [ ] **Given** an Excel row has an empty delivery method value, **when** import runs, **then** it leaves DeliveryMethodId null

### Exact Match Priority
- [ ] **Given** an Excel row has "Verbal" (exact match), **when** import runs, **then** it maps directly to "Verbal" without synonym lookup
- [ ] **Given** an Excel row has "Phone" (exact match), **when** import runs, **then** it maps directly to "Phone" without synonym lookup

## Out of Scope

- Creating new delivery methods from unrecognized values (use "Other" instead)
- Custom synonym dictionaries per organization
- Suggesting corrections during import preview
- Multiple synonym matches (first match wins)
- Synonym matching for DeliveryMethodOther text field

## Dependencies

- excel-import/S01: Upload Excel (file must be uploaded)
- excel-import/S02: Map Columns (columns must be mapped)
- excel-import/S03: Validate Import Data (validation step)
- _core/inject-entity: DeliveryMethodLookup entity

## Open Questions

- [ ] Should "text" and "sms" map to Phone or should there be a separate "Text Message" delivery method?
- [ ] Should "radio" also accept synonyms like "two-way radio", "walkie talkie"?
- [ ] Should we log synonym matches for user visibility?

## Domain Terms

| Term | Definition |
|------|------------|
| Delivery Method | How an inject is delivered to players (Verbal, Phone, Email, Radio, Written, Simulation, Other) |
| Synonym | Alternative term that means the same as the canonical value |
| DeliveryMethodOther | Text field for custom delivery methods when "Other" is selected |
| Exact Match | Value matches the database Name field exactly (case-insensitive) |

## UI/UX Notes

Synonym matching happens automatically during import. Users see the canonical value in the validation preview:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Step 3 of 4: Validate Data                                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ✓ 47 rows validated successfully                                      │
│                                                                         │
│  Synonym matches found:                                                │
│  • Row 3: "in person" → Verbal                                        │
│  • Row 7: "call" → Phone                                              │
│  • Row 12: "sim" → Simulation                                         │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Row │ Title                │ Delivery Method │ Status          │   │
│  │ ────┼──────────────────────┼─────────────────┼──────────────   │   │
│  │  3  │ Evacuation order     │ Verbal ✓        │ Valid           │   │
│  │  7  │ Shelter status       │ Phone ✓         │ Valid           │   │
│  │ 12  │ CAX input            │ Simulation ✓    │ Valid           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ℹ Note: Common variations were automatically matched to standard      │
│    delivery methods. Review the mapping above to confirm.              │
│                                                                         │
│                                       [← Back]  [Cancel]  [Import →]   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

### Implementation Pattern

Follow the same pattern as InjectTypeSynonyms in ExcelImportService.cs:

```csharp
// Add to ExcelImportService.cs after InjectTypeSynonyms

// Synonyms for DeliveryMethod lookup values
private static readonly Dictionary<string, string> DeliveryMethodSynonyms = new(StringComparer.OrdinalIgnoreCase)
{
    // Verbal synonyms
    { "in person", "Verbal" },
    { "in-person", "Verbal" },
    { "face to face", "Verbal" },
    { "face-to-face", "Verbal" },
    { "spoken", "Verbal" },
    { "oral", "Verbal" },

    // Phone synonyms
    { "call", "Phone" },
    { "telephone", "Phone" },
    { "text", "Phone" },
    { "sms", "Phone" },
    { "message", "Phone" },

    // Email synonyms
    { "e-mail", "Email" },
    { "electronic mail", "Email" },
    { "msg", "Email" },

    // Written synonyms
    { "fax", "Written" },
    { "document", "Written" },
    { "paper", "Written" },
    { "memo", "Written" },
    { "letter", "Written" },

    // Simulation synonyms
    { "sim", "Simulation" },
    { "cax", "Simulation" },
    { "computer aided", "Simulation" },
    { "simulated", "Simulation" },
};
```

### Matching Logic

In MapRowToInject method, update the DeliveryMethod case:

```csharp
case "DeliveryMethod":
    if (!string.IsNullOrEmpty(stringValue))
    {
        // First try exact match
        if (deliveryMethods.TryGetValue(stringValue.ToLowerInvariant(), out var method))
        {
            inject.DeliveryMethodId = method.Id;
        }
        // Then try synonym match
        else if (DeliveryMethodSynonyms.TryGetValue(stringValue, out var canonicalName))
        {
            if (deliveryMethods.TryGetValue(canonicalName.ToLowerInvariant(), out var synonymMethod))
            {
                inject.DeliveryMethodId = synonymMethod.Id;
                // Optional: Log synonym match for user visibility
                warnings.Add($"Row delivery method '{stringValue}' matched to '{canonicalName}'.");
            }
        }
        // Finally fall back to "Other"
        else
        {
            var otherMethod = deliveryMethods.Values.FirstOrDefault(m => m.IsOther);
            if (otherMethod != null)
            {
                inject.DeliveryMethodId = otherMethod.Id;
                inject.DeliveryMethodOther = stringValue;
            }
        }
    }
    break;
```

### Testing Strategy

Follow TDD approach:

1. Write tests for each synonym category (Verbal, Phone, Email, Written, Simulation)
2. Test case insensitivity
3. Test exact match priority
4. Test fallback to "Other" for unrecognized values
5. Verify DeliveryMethodOther is populated correctly

Example test:

```csharp
[Fact]
public async Task ImportInjects_VerbalSynonyms_MapsToVerbalDeliveryMethod()
{
    // Arrange
    var testData = new[]
    {
        new Dictionary<string, object?> { ["Title"] = "Test 1", ["DeliveryMethod"] = "in person" },
        new Dictionary<string, object?> { ["Title"] = "Test 2", ["DeliveryMethod"] = "face-to-face" },
        new Dictionary<string, object?> { ["Title"] = "Test 3", ["DeliveryMethod"] = "spoken" },
    };

    // Act
    var result = await _service.ImportInjectsAsync(request);

    // Assert
    var injects = await _context.Injects.ToListAsync();
    Assert.All(injects, inject =>
    {
        Assert.Equal(VerbalDeliveryMethodId, inject.DeliveryMethodId);
        Assert.Null(inject.DeliveryMethodOther);
    });
}
```

### Performance Considerations

- Dictionary lookups are O(1), so synonym matching adds negligible overhead
- No database queries needed for synonym matching
- Consider caching deliveryMethods dictionary if importing large files

### Migration Notes

No database migration needed - this is a code-only change to the import service.

---

**Status:** 📋 Ready for Development

**Priority:** Standard Phase (P2)

**Estimated Effort:** Small (1-2 days)
- 4 hours: Implement synonym dictionary and matching logic
- 2 hours: Write comprehensive unit tests
- 1 hour: Update validation preview to show synonym matches
- 1 hour: Manual testing with real MSELs
