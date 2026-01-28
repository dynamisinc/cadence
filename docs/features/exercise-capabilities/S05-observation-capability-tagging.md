# Story: Observation Capability Tagging

**Feature:** Exercise Capabilities  
**Story ID:** S05  
**Priority:** P0 (MVP)  
**Phase:** Standard Implementation

---

## User Story

**As an** Evaluator,  
**I want** to tag my observations with relevant capabilities,  
**So that** metrics can show performance by capability area and support improvement planning.

---

## Context

Observations capture what evaluators see during exercise conduct. Tagging observations with capabilities connects what was observed to the organizational functions being evaluated. This enables:

1. **Capability Performance Metrics** - Calculate P/S/M/U distribution by capability
2. **Gap Analysis** - Identify target capabilities with no observations
3. **AAR Organization** - Group observations by capability in reports
4. **Trend Analysis** - Track capability performance over time (future)

To streamline the evaluator workflow, the UI prioritizes exercise target capabilities at the top of the selector. Evaluators can also tag capabilities beyond the target list if their observation is relevant to other areas.

---

## Acceptance Criteria

- [ ] **Given** Observation entry form, **when** displayed, **then** I see "Capabilities" field (optional)
- [ ] **Given** the Capabilities field, **when** clicked, **then** shows multi-select with exercise target capabilities first, then other active capabilities
- [ ] **Given** the multi-select, **when** opened, **then** target capabilities are in a "Target Capabilities" section at top
- [ ] **Given** the multi-select, **when** opened, **then** non-target capabilities are in an "Other Capabilities" section below
- [ ] **Given** the multi-select, **when** I type in search, **then** all capabilities (target and other) are filtered
- [ ] **Given** selected capabilities, **when** viewing observation form, **then** they appear as chips
- [ ] **Given** an observation with capabilities, **when** I save, **then** capability tags are persisted
- [ ] **Given** an observation with capabilities, **when** viewing observation detail, **then** capability chips are displayed
- [ ] **Given** an observation with capabilities, **when** viewing observation list, **then** capability tags are shown
- [ ] **Given** API, **when** `GET /api/observations/{id}` called, **then** response includes tagged capabilities
- [ ] **Given** API, **when** creating/updating observation with capability IDs, **then** capabilities are linked
- [ ] **Given** exercise metrics, **when** observations have capability tags, **then** capability performance can be calculated
- [ ] **Given** no target capabilities on exercise, **when** tagging observation, **then** all org capabilities shown (no "Target" section)
- [ ] **Given** no capabilities in org library, **when** creating observation, **then** Capabilities field is hidden

---

## Out of Scope

- Automatic capability suggestion based on observation text
- Required capability tagging (always optional)
- Capability-specific observation templates
- Bulk tagging of multiple observations

---

## Dependencies

- S01: Capability Entity and API
- S04: Exercise Target Capabilities
- Observation CRUD feature (existing)

---

## Open Questions

- [x] Should capability tagging be required? **No, always optional**
- [x] Can evaluators tag capabilities not in target list? **Yes, "Other Capabilities" section**
- [ ] Should we show capability descriptions on hover? **Yes, if space permits**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Capability Tag | A capability linked to an observation |
| Target Capability | A capability explicitly targeted for this exercise (shown first) |
| Other Capability | Any active capability not in the exercise target list |

---

## UI/UX Notes

### Observation Entry Form - Capabilities Section

```
┌─────────────────────────────────────────────────────────────────────────┐
│  NEW OBSERVATION                                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ... (Inject selector, Description, Rating fields) ...                  │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  CAPABILITIES                                                           │
│  Which capabilities does this observation relate to?                    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [Mass Care Services ×] [Operational Communications ×]           │   │
│  │                                                         [+ Add] │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ... (other fields) ...                                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Capability Selector with Target Prioritization

```
┌─────────────────────────────────────────────────────────────┐
│  🔍 Search capabilities...                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ★ TARGET CAPABILITIES                                      │
│  ─────────────────────────────────────────────────────────  │
│  ├─ ☑ Mass Care Services                                   │
│  ├─ ☑ Operational Communications                           │
│  └─ ☐ Public Information and Warning                       │
│                                                             │
│  OTHER CAPABILITIES                                         │
│  ─────────────────────────────────────────────────────────  │
│  ├─ ☐ Critical Transportation                              │
│  ├─ ☐ Planning                                             │
│  ├─ ☐ Public Health, Medical, and Mental Health Services   │
│  ├─ ☐ Volunteer Coordination                               │
│  └─ ...                                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Observation Card with Capability Tags

```
┌─────────────────────────────────────────────────────────────────────────┐
│  OBS-007                                              14:32  [Marginal] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Communication between EOC and field teams was delayed by 15 minutes   │
│  due to radio frequency congestion.                                     │
│                                                                         │
│  Inject: INJ-012 - Initial Shelter Setup                               │
│                                                                         │
│  [Operational Communications] [Mass Care Services]                      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Observation Detail View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  OBSERVATION OBS-007                                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Recorded: 14:32:15                          Rating: ● Marginal (M)    │
│  Evaluator: Jane Smith                                                  │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  DESCRIPTION                                                            │
│  Communication between EOC and field teams was delayed by 15 minutes   │
│  due to radio frequency congestion. Multiple teams reported being      │
│  unable to reach the command post during the initial response phase.   │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  RELATED INJECT                                                         │
│  INJ-012 - Initial Shelter Setup                        14:15          │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  CAPABILITIES                                                           │
│  [Operational Communications]  [Mass Care Services]                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### Observation DTO Update

```csharp
public record ObservationDto(
    Guid Id,
    string Description,
    string? Rating,
    DateTime RecordedAt,
    // ... existing fields ...
    List<CapabilityDto> Capabilities  // Add this
);

public record CreateObservationRequest(
    string Description,
    string? Rating,
    Guid? InjectId,
    // ... existing fields ...
    List<Guid>? CapabilityIds  // Add this
);

public record UpdateObservationRequest(
    string Description,
    string? Rating,
    // ... existing fields ...
    List<Guid>? CapabilityIds  // Add this
);
```

### API Changes

Observation endpoints already exist - update to include capabilities:

```csharp
// POST /api/exercises/{exerciseId}/observations
// - Accept CapabilityIds in request body
// - Return Capabilities in response

// PUT /api/observations/{id}
// - Accept CapabilityIds in request body
// - Return Capabilities in response

// GET /api/observations/{id}
// - Include Capabilities in response
```

### Service Layer

```csharp
public class ObservationService
{
    public async Task<Observation> CreateObservationAsync(
        Guid exerciseId,
        CreateObservationRequest request)
    {
        var observation = new Observation
        {
            ExerciseId = exerciseId,
            Description = request.Description,
            Rating = request.Rating,
            // ... other fields ...
        };
        
        _context.Observations.Add(observation);
        await _context.SaveChangesAsync();
        
        // Link capabilities
        if (request.CapabilityIds?.Any() == true)
        {
            await SetObservationCapabilitiesAsync(
                observation.Id, 
                request.CapabilityIds);
        }
        
        return observation;
    }
    
    public async Task SetObservationCapabilitiesAsync(
        Guid observationId,
        List<Guid> capabilityIds)
    {
        // Remove existing
        var existing = await _context.ObservationCapabilities
            .Where(oc => oc.ObservationId == observationId)
            .ToListAsync();
        _context.ObservationCapabilities.RemoveRange(existing);
        
        // Add new
        var newLinks = capabilityIds.Select(cId => new ObservationCapability
        {
            ObservationId = observationId,
            CapabilityId = cId
        });
        _context.ObservationCapabilities.AddRange(newLinks);
        
        await _context.SaveChangesAsync();
    }
}
```

### Frontend Component

```typescript
// ObservationCapabilitySelector.tsx
interface ObservationCapabilitySelectorProps {
  exerciseId: string;
  selectedCapabilityIds: string[];
  onChange: (capabilityIds: string[]) => void;
}

// Fetches:
// 1. Exercise target capabilities (shown first)
// 2. All other active org capabilities (shown second)
// Groups into "Target Capabilities" and "Other Capabilities" sections
```

---

## Estimation

**T-Shirt Size:** M  
**Story Points:** 5

---

## Testing Requirements

### Unit Tests
- [ ] ObservationService capability linking
- [ ] CapabilitySelector prioritizes target capabilities
- [ ] Search filtering across both sections

### Integration Tests
- [ ] Create observation with capabilities
- [ ] Update observation capabilities
- [ ] Get observation includes capabilities
- [ ] Deactivated capabilities excluded from selector

### E2E Tests
- [ ] Create observation and tag capabilities
- [ ] Edit observation to add/remove capabilities
- [ ] View observation shows capability tags
- [ ] Observation list shows capability tags
