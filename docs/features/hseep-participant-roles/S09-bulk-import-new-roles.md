# Story: S09 - Bulk Import Support for New Roles

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As an** Exercise Director,
**I want** to bulk import participants with the new HSEEP roles,
**So that** I can efficiently assign specialized staff to large-scale exercises.

## Context

The bulk participant import feature (bulk-participant-import/S01-S06) currently supports the original five roles (Administrator, ExerciseDirector, Controller, Evaluator, Observer). With the addition of five new HSEEP roles (Player, Simulator, Facilitator, SafetyOfficer, TrustedAgent), the bulk import system must:

1. Recognize new role names in CSV/XLSX files
2. Support role name synonyms (e.g., "Safety Officer", "SafetyOfficer", "Safety")
3. Validate role assignments during preview
4. Update template files to include new roles
5. Handle role-specific validation (e.g., warn if no Safety Officer for FSE)

This story extends existing bulk import functionality rather than creating new features.

## Acceptance Criteria

### Template Updates

- [ ] **AC-01**: Given I download the participant import template, when I open it, then the Role column includes all ten roles
  - Test: `BulkImportService.test.ts::GenerateTemplate_IncludesAllRoles`
  - CSV: Plain text list
  - XLSX: Dropdown validation with all ten roles

- [ ] **AC-02**: Given the XLSX template, when I view the Role column, then it has data validation restricting to the ten role names
  - Test: Manual verification of XLSX template
  - Prevents typos during manual entry

### Role Name Recognition

- [ ] **AC-03**: Given I upload a file with "Player" in the role column, when the system parses it, then it maps to ExerciseRole.Player
  - Test: `BulkImportService.test.ts::ParseFile_RecognizesNewRoleNames`

- [ ] **AC-04**: Given I upload a file with role synonyms, when the system parses it, then it recognizes all common variations
  - Test: `BulkImportService.test.ts::ParseFile_RecognizesSynonyms`
  - Synonyms:
    - Player: "Player", "Participant"
    - Simulator: "Simulator", "SimCell", "Sim Cell"
    - Facilitator: "Facilitator", "Moderator"
    - SafetyOfficer: "Safety Officer", "SafetyOfficer", "Safety", "Safety Lead"
    - TrustedAgent: "Trusted Agent", "TrustedAgent", "SME", "Subject Matter Expert"

- [ ] **AC-05**: Given I upload a file with an unrecognized role name, when the system validates it, then it shows an error with suggestions
  - Test: `BulkImportService.test.ts::ParseFile_SuggestsRoleForTypo`
  - Example: "Safty" → "Did you mean 'Safety Officer'?"

### Preview & Validation

- [ ] **AC-06**: Given I upload a file with new roles, when I see the preview, then participants are grouped by role including new roles
  - Test: `ImportPreviewStep.test.tsx::Preview_GroupsByAllRoles`

- [ ] **AC-07**: Given I upload a file for an FSE exercise with no Safety Officer, when I see the preview, then I see a warning
  - Test: `ImportPreviewStep.test.tsx::Preview_WarnsMissingSafetyOfficerForFSE`
  - "⚠️ Full-Scale Exercises typically require a Safety Officer. Consider adding one."

- [ ] **AC-08**: Given I upload a file with many Players, when I see the preview, then the Player section is collapsible
  - Test: `ImportPreviewStep.test.tsx::Preview_CollapsesLargePlayerList`
  - Helpful for exercises with 50+ players

### Import Processing

- [ ] **AC-09**: Given I confirm import with new roles, when processing completes, then all roles are assigned correctly
  - Test: `ExerciseParticipantService.test.cs::BulkImport_AssignsNewRoles`

- [ ] **AC-10**: Given I confirm import, when results are shown, then new roles appear in the summary
  - Test: `ImportResultsStep.test.tsx::Results_ShowsAllRoleAssignments`
  - "5 Players assigned, 2 Simulators invited, 1 Safety Officer assigned"

## Out of Scope

- Role-specific validation rules beyond safety warnings (future enhancement)
- Automatic role suggestions based on email domain (future enhancement)
- Bulk role reassignment (separate story)

## Dependencies

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- bulk-participant-import/S01-S06: Bulk import feature (existing)

## Technical Implementation

### Backend: Role Synonym Mapping

**File**: `src/Cadence.Core/Features/Exercises/Services/ExerciseParticipantService.cs`

```csharp
private static readonly Dictionary<string, ExerciseRole> RoleSynonyms = new(StringComparer.OrdinalIgnoreCase)
{
    // Original roles
    { "Administrator", ExerciseRole.Administrator },
    { "Admin", ExerciseRole.Administrator },
    { "Exercise Director", ExerciseRole.ExerciseDirector },
    { "ExerciseDirector", ExerciseRole.ExerciseDirector },
    { "Director", ExerciseRole.ExerciseDirector },
    { "Controller", ExerciseRole.Controller },
    { "Evaluator", ExerciseRole.Evaluator },
    { "Observer", ExerciseRole.Observer },

    // New roles
    { "Player", ExerciseRole.Player },
    { "Participant", ExerciseRole.Player },
    { "Simulator", ExerciseRole.Simulator },
    { "SimCell", ExerciseRole.Simulator },
    { "Sim Cell", ExerciseRole.Simulator },
    { "Facilitator", ExerciseRole.Facilitator },
    { "Moderator", ExerciseRole.Facilitator },
    { "Safety Officer", ExerciseRole.SafetyOfficer },
    { "SafetyOfficer", ExerciseRole.SafetyOfficer },
    { "Safety", ExerciseRole.SafetyOfficer },
    { "Safety Lead", ExerciseRole.SafetyOfficer },
    { "Trusted Agent", ExerciseRole.TrustedAgent },
    { "TrustedAgent", ExerciseRole.TrustedAgent },
    { "SME", ExerciseRole.TrustedAgent },
    { "Subject Matter Expert", ExerciseRole.TrustedAgent },
};

public ExerciseRole? ParseRoleName(string roleName)
{
    if (RoleSynonyms.TryGetValue(roleName.Trim(), out var role))
    {
        return role;
    }

    return null;
}

public string SuggestRoleName(string invalidRole)
{
    // Simple Levenshtein distance matching
    var suggestions = RoleSynonyms.Keys
        .Select(k => new { Role = k, Distance = LevenshteinDistance(invalidRole, k) })
        .OrderBy(x => x.Distance)
        .Take(3)
        .Where(x => x.Distance <= 3)
        .Select(x => x.Role)
        .ToList();

    if (suggestions.Any())
    {
        return $"Did you mean: {string.Join(", ", suggestions)}?";
    }

    return "Must be one of: Administrator, Exercise Director, Controller, Evaluator, Observer, Player, Simulator, Facilitator, Safety Officer, Trusted Agent";
}
```

### Backend: Exercise Type Warning

**File**: `src/Cadence.Core/Features/Exercises/Services/ExerciseParticipantService.cs`

```csharp
public BulkImportPreviewDto GeneratePreview(Guid exerciseId, List<BulkImportRowDto> rows)
{
    var exercise = await _context.Exercises.FindAsync(exerciseId);
    var preview = new BulkImportPreviewDto();

    // ... existing preview logic ...

    // Check for safety officer in operations-based exercises
    if (exercise.Type is ExerciseType.FSE or ExerciseType.FE)
    {
        var hasSafetyOfficer = rows.Any(r => r.Role == ExerciseRole.SafetyOfficer);
        if (!hasSafetyOfficer)
        {
            preview.Warnings.Add(new ImportWarningDto
            {
                Severity = WarningSeverity.Recommendation,
                Message = $"{exercise.Type} exercises typically require a Safety Officer. Consider adding one to ensure safety oversight.",
                AffectedRows = new List<int>()
            });
        }
    }

    return preview;
}
```

### Frontend: Template Generation

**File**: `src/frontend/src/features/bulk-participant-import/services/bulkImportService.ts`

```typescript
export const generateTemplate = (format: 'csv' | 'xlsx'): string => {
  if (format === 'csv') {
    const roles = [
      'Administrator',
      'Exercise Director',
      'Controller',
      'Evaluator',
      'Observer',
      'Player',
      'Simulator',
      'Facilitator',
      'Safety Officer',
      'Trusted Agent',
    ];

    return `Name,Email,Role
John Smith,john.smith@agency.gov,Controller
Jane Doe,jane.doe@agency.gov,Evaluator
Bob Player,bob@example.com,Player

Available Roles: ${roles.join(', ')}`;
  }

  // XLSX template URL from backend
  return `/api/exercises/bulk-import/template?format=xlsx`;
};
```

### Frontend: Preview Grouping

**File**: `src/frontend/src/features/bulk-participant-import/components/ImportPreviewStep.tsx`

```typescript
const groupedByRole = useMemo(() => {
  const groups = new Map<ExerciseRole, BulkImportRow[]>();

  preview.rows.forEach((row) => {
    if (!groups.has(row.role)) {
      groups.set(row.role, []);
    }
    groups.get(row.role)!.push(row);
  });

  // Sort groups by role hierarchy
  const roleOrder = [
    ExerciseRole.Administrator,
    ExerciseRole.ExerciseDirector,
    ExerciseRole.Controller,
    ExerciseRole.Evaluator,
    ExerciseRole.Observer,
    ExerciseRole.Facilitator,
    ExerciseRole.Simulator,
    ExerciseRole.SafetyOfficer,
    ExerciseRole.TrustedAgent,
    ExerciseRole.Player,
  ];

  return new Map(
    roleOrder
      .filter((role) => groups.has(role))
      .map((role) => [role, groups.get(role)!])
  );
}, [preview]);

return (
  <>
    {preview.warnings.map((warning) => (
      <Alert severity="warning" key={warning.message} sx={{ mb: 2 }}>
        {warning.message}
      </Alert>
    ))}

    {Array.from(groupedByRole.entries()).map(([role, rows]) => (
      <Accordion key={role}>
        <AccordionSummary>
          <RoleBadge role={role} />
          <Typography sx={{ ml: 2 }}>
            {rows.length} {rows.length === 1 ? 'person' : 'people'}
          </Typography>
        </AccordionSummary>
        <AccordionDetails>
          {rows.map((row, index) => (
            <ParticipantPreviewRow key={index} row={row} />
          ))}
        </AccordionDetails>
      </Accordion>
    ))}
  </>
);
```

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Features/Exercises/ExerciseParticipantServiceTests.cs`
  - `BulkImport_RecognizesNewRoleNames`
  - `BulkImport_RecognizesRoleSynonyms`
  - `BulkImport_SuggestsRoleForTypo`
  - `BulkImport_WarnsNoSafetyOfficerForFSE`

### Frontend Tests
- `src/frontend/src/features/bulk-participant-import/components/ImportPreviewStep.test.tsx`
- `src/frontend/src/features/bulk-participant-import/services/bulkImportService.test.ts`

## Example Import File

**CSV Template:**
```csv
Name,Email,Role
John Smith,john.smith@agency.gov,Controller
Jane Evaluator,jane@agency.gov,Evaluator
Bob Safety,bob@agency.gov,Safety Officer
Alice SimCell,alice@agency.gov,Simulator
Charlie Player,charlie@example.com,Player
Dana Player,dana@example.com,Player
```

**Preview Result:**
```
CONTROLLER (1)
  ✓ John Smith (john.smith@agency.gov) - Existing member, will be assigned

EVALUATOR (1)
  ✓ Jane Evaluator (jane@agency.gov) - Existing member, will be assigned

SAFETY OFFICER (1)
  ✓ Bob Safety (bob@agency.gov) - Existing member, will be assigned

SIMULATOR (1)
  ⚠️ Alice SimCell (alice@agency.gov) - Not in organization, will be invited

PLAYER (2)
  ⚠️ Charlie Player (charlie@example.com) - Not in organization, will be invited
  ⚠️ Dana Player (dana@example.com) - Not in organization, will be invited
```

## Related Stories

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- bulk-participant-import/S02: Preview and Validate Import

---

*Last updated: 2026-02-09*
