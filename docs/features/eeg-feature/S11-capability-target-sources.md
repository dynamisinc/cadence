# S11: Capability Target Sources Field

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 2

## User Story

**As an** Exercise Director,
**I want** to document the plans, policies, and SOPs that each Capability Target is based on,
**So that** evaluators understand the authoritative basis for the target and generated EEG documents include source references per HSEEP standards.

## Context

The official HSEEP EEG template includes a "Source(s)" line under each Capability Target. This field references the authoritative documents the target is derived from — emergency operations plans, standard operating procedures, mutual aid agreements, frameworks, regulations, etc.

Example sources:
- "Metro County Emergency Operations Plan, Annex F"
- "SOP 5.2 — Communications Activation Procedures"
- "State of Virginia CEMP; NIMS"

This information is critical for two reasons:
1. **Evaluator context:** Evaluators need to know what standards they are assessing against
2. **HSEEP compliance:** The official EEG template requires source documentation for each target

Currently, `CriticalTask.Standard` captures the *performance condition* for individual tasks (e.g., "Per SOP 5.2, using emergency notification system"), but there is no field for the *authoritative document references* at the parent Capability Target level. These are complementary — the Standard says *how* to measure success, while Sources says *where the requirement comes from*.

### HSEEP Template Reference

From the HSEEP EEG template, each Capability Target block includes:

```
Organizational Capability Target 1: [Insert customized target based on plans and assessments]

Critical Task: [Insert task from frameworks, plans, or SOPs]
Critical Task: [Insert task from frameworks, plans, or SOPs]

Source(s): [Insert name of plan, policy, procedure, or reference]    ← THIS FIELD
```

## Acceptance Criteria

### Entity Changes

- [ ] **Given** the Cadence database, **when** migrations run, **then** CapabilityTargets table has a new `Sources` column (nvarchar(500), nullable)
- [ ] **Given** an existing CapabilityTarget, **when** the migration runs, **then** the Sources field defaults to null (non-breaking change)

### API Changes

- [ ] **Given** the CapabilityTarget create endpoint, **when** called with a `sources` field, **then** the value is persisted
- [ ] **Given** the CapabilityTarget update endpoint, **when** called with a `sources` field, **then** the value is updated
- [ ] **Given** the CapabilityTarget list/get endpoints, **when** called, **then** `sources` is included in the response DTO
- [ ] **Given** `sources` is not provided on create, **when** saved, **then** the field is null (optional)
- [ ] **Given** `sources` exceeds 500 characters, **when** submitted, **then** returns 400 with validation error

### UI Changes

- [ ] **Given** the Create Capability Target dialog (S03), **when** displayed, **then** I see a "Sources" text field below the Target Description
- [ ] **Given** the Sources field, **when** displayed, **then** it shows placeholder text: "e.g., County EOP Annex F; SOP 5.2; NIMS"
- [ ] **Given** the Sources field, **when** displayed, **then** it shows helper text: "Plans, policies, SOPs, or frameworks this target is based on"
- [ ] **Given** the Edit Capability Target dialog (S03), **when** displayed, **then** the Sources field shows the current value
- [ ] **Given** the Capability Target list view (S03), **when** a target has sources, **then** sources are visible in the expanded detail
- [ ] **Given** the EEG Coverage Dashboard (S09), **when** a target has sources, **then** sources are displayed with the target

### Sync Requirements

- [ ] **Given** a CapabilityTarget sources field is updated, **when** other clients are connected, **then** they receive the update via SignalR
- [ ] **Given** offline mode, **when** sources are edited, **then** changes queue for sync

## Data Model

```csharp
// Addition to existing CapabilityTarget entity (S01)
public class CapabilityTarget : BaseEntity
{
    // ... existing fields ...

    /// <summary>
    /// References to plans, policies, SOPs, or frameworks this target is based on.
    /// Corresponds to the "Source(s)" field in the HSEEP EEG template.
    /// Example: "Metro County EOP, Annex F; SOP 5.2; NIMS"
    /// </summary>
    [MaxLength(500)]
    public string? Sources { get; set; }
}
```

### Updated DTO

```csharp
// Add to existing CreateCapabilityTargetRequest
public record CreateCapabilityTargetRequest(
    Guid CapabilityId,
    string TargetDescription,
    string? Sources,        // NEW
    int? SortOrder
);

// Add to existing UpdateCapabilityTargetRequest
public record UpdateCapabilityTargetRequest(
    string TargetDescription,
    string? Sources,        // NEW
    int? SortOrder
);

// Add to existing CapabilityTargetDto
public record CapabilityTargetDto(
    Guid Id,
    Guid ExerciseId,
    Guid CapabilityId,
    CapabilityDto Capability,
    string TargetDescription,
    string? Sources,        // NEW
    int SortOrder,
    int CriticalTaskCount
);
```

## Out of Scope

- Structured source references (linking to a "Documents" entity) — future enhancement
- Source document upload/attachment — future enhancement
- Auto-suggesting sources based on capability framework — future enhancement

## Dependencies

- S01: CapabilityTarget entity exists
- S03: Define Capability Targets UI exists (to add the field)

## Technical Notes

- This is a non-breaking additive change — nullable column with no default required
- Migration should be simple `AddColumn` operation
- Consider placing the Sources field below TargetDescription in the form, with smaller font/muted style to keep visual hierarchy clean
- The field is free-text by design; structured document references are a future enhancement

## Test Scenarios

### Unit Tests
- CapabilityTarget entity accepts null Sources
- CapabilityTarget entity accepts valid Sources string
- CapabilityTarget entity rejects Sources > 500 characters
- DTO mapping includes Sources field

### Integration Tests
- Create target with Sources → persists correctly
- Create target without Sources → null stored
- Update target Sources → value changes
- API response includes Sources in list and detail endpoints

---

*Story created: 2026-02-05*
*Origin: EEG Template Gap Analysis — Gap #1*
