# Story: Exercise Target Capabilities

**Feature:** Exercise Capabilities  
**Story ID:** S04  
**Priority:** P0 (MVP)  
**Phase:** Standard Implementation

---

## User Story

**As an** Exercise Director,  
**I want** to select which capabilities my exercise will evaluate,  
**So that** evaluators know what to observe and metrics show performance against our goals.

---

## Context

Target capabilities define the scope of evaluation for an exercise. Per HSEEP methodology, exercise objectives should be aligned to capabilities during the Initial Planning Meeting (IPM). Selecting target capabilities:

1. Focuses evaluators on what matters for this exercise
2. Enables meaningful "capability gap" analysis (what was targeted but not observed)
3. Provides structure for the After-Action Report (AAR/IP)
4. Allows metrics to show performance by capability

Target capabilities are selected from the organization's capability library (which may include FEMA, NATO, NIST, ISO, or custom capabilities). An exercise can target any number of capabilities, though HSEEP guidance suggests focusing on 3-5 key capabilities per exercise.

---

## Acceptance Criteria

- [ ] **Given** Exercise Create form, **when** displayed, **then** I see "Target Capabilities" section (optional)
- [ ] **Given** Exercise Edit form, **when** displayed, **then** I see "Target Capabilities" section with current selections
- [ ] **Given** the Target Capabilities field, **when** clicked, **then** shows multi-select with active capabilities from org library
- [ ] **Given** the multi-select, **when** opened, **then** capabilities are grouped by Category
- [ ] **Given** the multi-select, **when** I type in search, **then** capabilities are filtered by name match
- [ ] **Given** the multi-select, **when** I select capabilities, **then** they appear as chips in the field
- [ ] **Given** selected capability chips, **when** I click X on a chip, **then** capability is deselected
- [ ] **Given** an exercise with target capabilities, **when** I save, **then** selections are persisted
- [ ] **Given** an exercise with target capabilities, **when** viewing exercise detail, **then** target capabilities are displayed
- [ ] **Given** API, **when** `GET /api/exercises/{id}/capabilities` called, **then** returns list of target capabilities for that exercise
- [ ] **Given** API, **when** `PUT /api/exercises/{id}/capabilities` called with capability IDs, **then** updates target capabilities
- [ ] **Given** no capabilities in org library, **when** viewing exercise form, **then** Target Capabilities section shows "No capabilities defined" with link to Settings
- [ ] **Given** exercise with target capabilities, **when** Evaluator creates observation, **then** target capabilities are shown prominently in capability selector (S05)

---

## Out of Scope

- Capability performance metrics (S06)
- Suggesting capabilities based on exercise type
- Limiting number of target capabilities
- Capability objectives/targets (quantifiable goals)

---

## Dependencies

- S01: Capability Entity and API
- S02: Capability Library Admin UI (capabilities must exist)
- Exercise CRUD feature (existing)

---

## Open Questions

- [x] Is target capability selection required? **No, optional**
- [x] Should there be a recommended limit? **Show guidance but don't enforce**
- [ ] Should we show capability descriptions in the selector? **Yes, on hover/expand**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Target Capability | A capability explicitly selected for evaluation in a specific exercise |
| Capability Gap | A target capability that received no observations during the exercise |
| Exercise Scope | The defined boundaries of what an exercise will evaluate |

---

## UI/UX Notes

### Target Capabilities in Exercise Form

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CREATE EXERCISE                                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Exercise Name *                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Hurricane Response TTX 2026                                     │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Exercise Type *                        Status                          │
│  ┌──────────────────────┐              ┌──────────────────────┐        │
│  │ Tabletop Exercise ▼  │              │ Planning         ▼   │        │
│  └──────────────────────┘              └──────────────────────┘        │
│                                                                         │
│  ... (other fields) ...                                                 │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  TARGET CAPABILITIES                                                    │
│  Select the capabilities this exercise will evaluate                    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Mass Care Services ×] [Operational Communications ×]           │   │
│  │ [Public Information and Warning ×]                              │   │
│  │                                                         [+ Add] │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  💡 HSEEP recommends focusing on 3-5 key capabilities per exercise     │
│                                                                         │
│                                              [Cancel]  [Create Exercise]│
└─────────────────────────────────────────────────────────────────────────┘
```

### Capability Selector Popover

```
┌─────────────────────────────────────────────────────────────┐
│  🔍 Search capabilities...                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  RESPONSE                                                   │
│  ├─ ☑ Mass Care Services                                   │
│  ├─ ☐ Critical Transportation                              │
│  ├─ ☑ Operational Communications                           │
│  ├─ ☐ Public Health, Medical, and Mental Health Services   │
│  └─ ...                                                    │
│                                                             │
│  PREVENTION                                                 │
│  ├─ ☑ Public Information and Warning                       │
│  ├─ ☐ Planning                                             │
│  └─ ...                                                    │
│                                                             │
│  CUSTOM                                                     │
│  ├─ ☐ Volunteer Coordination                               │
│  └─ ☐ Media Relations                                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Exercise Detail View - Target Capabilities Section

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TARGET CAPABILITIES                                                    │
│                                                                         │
│  [Mass Care Services]  [Operational Communications]                     │
│  [Public Information and Warning]                                       │
│                                                                         │
│  3 capabilities targeted for evaluation                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

### Empty State (No Org Capabilities)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TARGET CAPABILITIES                                                    │
│                                                                         │
│  ⚠️ No capabilities defined for your organization                       │
│                                                                         │
│  Set up your capability library to enable capability-based evaluation.  │
│                                                                         │
│  [Go to Capability Library →]                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoints

```csharp
// Get target capabilities for an exercise
[HttpGet("{exerciseId}/capabilities")]
public async Task<ActionResult<List<CapabilityDto>>> GetExerciseCapabilities(
    Guid exerciseId)

// Set target capabilities for an exercise
[HttpPut("{exerciseId}/capabilities")]
public async Task<ActionResult> SetExerciseCapabilities(
    Guid exerciseId,
    [FromBody] SetExerciseCapabilitiesRequest request)

public record SetExerciseCapabilitiesRequest(List<Guid> CapabilityIds);
```

### Exercise DTO Update

```csharp
public record ExerciseDto(
    Guid Id,
    string Name,
    // ... existing fields ...
    List<CapabilityDto> TargetCapabilities  // Add this
);

public record ExerciseDetailDto(
    Guid Id,
    string Name,
    // ... existing fields ...
    List<CapabilityDto> TargetCapabilities  // Add this
);
```

### Frontend Component

```typescript
// TargetCapabilitiesSelector.tsx
interface TargetCapabilitiesSelectorProps {
  organizationId: string;
  selectedCapabilityIds: string[];
  onChange: (capabilityIds: string[]) => void;
  disabled?: boolean;
}

// Groups capabilities by category
// Shows checkboxes for selection
// Supports search filtering
// Shows selected as chips above selector
```

### Service Layer

```csharp
public class ExerciseCapabilityService
{
    public async Task<List<Capability>> GetTargetCapabilitiesAsync(Guid exerciseId)
    {
        return await _context.ExerciseCapabilities
            .Where(ec => ec.ExerciseId == exerciseId)
            .Include(ec => ec.Capability)
            .Select(ec => ec.Capability)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.SortOrder)
            .ToListAsync();
    }
    
    public async Task SetTargetCapabilitiesAsync(
        Guid exerciseId, 
        List<Guid> capabilityIds)
    {
        // Remove existing
        var existing = await _context.ExerciseCapabilities
            .Where(ec => ec.ExerciseId == exerciseId)
            .ToListAsync();
        _context.ExerciseCapabilities.RemoveRange(existing);
        
        // Add new
        var newLinks = capabilityIds.Select(cId => new ExerciseCapability
        {
            ExerciseId = exerciseId,
            CapabilityId = cId
        });
        _context.ExerciseCapabilities.AddRange(newLinks);
        
        await _context.SaveChangesAsync();
    }
}
```

---

## Estimation

**T-Shirt Size:** M  
**Story Points:** 5

---

## Testing Requirements

### Unit Tests
- [ ] ExerciseCapabilityService CRUD operations
- [ ] TargetCapabilitiesSelector groups by category
- [ ] Search filtering works correctly

### Integration Tests
- [ ] Set target capabilities persists correctly
- [ ] Get target capabilities returns correct data
- [ ] Deactivated capabilities excluded from selector

### E2E Tests
- [ ] Create exercise with target capabilities
- [ ] Edit exercise to add/remove capabilities
- [ ] View exercise shows target capabilities
