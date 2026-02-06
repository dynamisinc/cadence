# S01: Capability Target Entity and API

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to define Capability Targets for my exercise,
**So that** I can establish measurable performance thresholds aligned with HSEEP methodology.

## Context

HSEEP evaluation requires exercises to define specific, measurable capability targets—not just reference generic capabilities from a library. A Capability Target takes an organizational capability (e.g., "Operational Communications") and adds an exercise-specific performance threshold (e.g., "Establish interoperable communications within 30 minutes of EOC activation").

This story creates the backend entity and API for Capability Targets. The UI is covered in S03.

## Acceptance Criteria

### Entity Requirements

- [ ] **Given** the Cadence database, **when** migrations run, **then** a `CapabilityTargets` table exists with proper schema
- [ ] **Given** a CapabilityTarget entity, **when** created, **then** it requires ExerciseId, CapabilityId, and TargetDescription
- [ ] **Given** a CapabilityTarget, **when** the parent Exercise is deleted, **then** the CapabilityTarget is cascade deleted
- [ ] **Given** a CapabilityTarget, **when** the referenced Capability is deactivated, **then** the CapabilityTarget remains (soft reference)
- [ ] **Given** a CapabilityTarget, **when** created, **then** SortOrder defaults to next available within the exercise

### API Requirements

- [ ] **Given** I am authenticated with Director+ role, **when** I GET `/api/exercises/{exerciseId}/capability-targets`, **then** I receive a list of targets with their capabilities
- [ ] **Given** I am authenticated with Director+ role, **when** I POST to `/api/exercises/{exerciseId}/capability-targets`, **then** a new target is created
- [ ] **Given** I am authenticated with Director+ role, **when** I PUT to `/api/exercises/{exerciseId}/capability-targets/{id}`, **then** the target is updated
- [ ] **Given** I am authenticated with Director+ role, **when** I DELETE `/api/exercises/{exerciseId}/capability-targets/{id}`, **then** the target and its Critical Tasks are deleted
- [ ] **Given** I am authenticated with Evaluator or Observer role, **when** I attempt to create/update/delete, **then** I receive 403 Forbidden
- [ ] **Given** an invalid ExerciseId, **when** I call any endpoint, **then** I receive 404 Not Found
- [ ] **Given** a CapabilityId not in the organization's library, **when** I create a target, **then** I receive 400 Bad Request

### Validation Rules

- [ ] **Given** TargetDescription, **when** empty or whitespace, **then** validation fails
- [ ] **Given** TargetDescription, **when** longer than 500 characters, **then** validation fails
- [ ] **Given** CapabilityId, **when** not a valid GUID, **then** validation fails
- [ ] **Given** CapabilityId, **when** capability belongs to different organization, **then** validation fails

### Sync Requirements

- [ ] **Given** a CapabilityTarget is created/updated/deleted, **when** other clients are connected, **then** they receive real-time updates via SignalR
- [ ] **Given** offline mode, **when** a target is created, **then** it queues for sync with proper conflict resolution

## Data Model

```csharp
public class CapabilityTarget : BaseEntity
{
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    
    public Guid CapabilityId { get; set; }
    public Capability Capability { get; set; } = null!;
    
    /// <summary>
    /// Measurable performance threshold for this capability in this exercise.
    /// Example: "Activate EOC within 60 minutes of notification"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TargetDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Display order within the exercise's capability targets
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Critical tasks required to achieve this target
    /// </summary>
    public ICollection<CriticalTask> CriticalTasks { get; set; } = new List<CriticalTask>();
}
```

## API Specification

### GET /api/exercises/{exerciseId}/capability-targets

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "exerciseId": "guid",
      "capabilityId": "guid",
      "capability": {
        "id": "guid",
        "name": "Operational Communications",
        "category": "Response"
      },
      "targetDescription": "Establish interoperable communications within 30 minutes",
      "sortOrder": 1,
      "criticalTaskCount": 3
    }
  ],
  "totalCount": 5
}
```

### POST /api/exercises/{exerciseId}/capability-targets

**Request:**
```json
{
  "capabilityId": "guid",
  "targetDescription": "Establish interoperable communications within 30 minutes",
  "sortOrder": 1
}
```

**Response 201:** Created target object

### PUT /api/exercises/{exerciseId}/capability-targets/{id}

**Request:**
```json
{
  "targetDescription": "Updated description",
  "sortOrder": 2
}
```

**Response 200:** Updated target object

### DELETE /api/exercises/{exerciseId}/capability-targets/{id}

**Response 204:** No content (cascades to Critical Tasks)

## Out of Scope

- Critical Task entity (S02)
- UI for managing targets (S03)
- Linking to objectives (future enhancement)
- Importing targets from EEG templates (future enhancement)
- Bulk operations (future enhancement)

## Dependencies

- Exercise entity exists
- Capability entity and library exists (Exercise Capabilities feature)
- Sync service for real-time updates

## Technical Notes

- Use existing repository pattern from Cadence
- Include Capability navigation property in responses (CapabilityId + expanded Capability object)
- Cascade delete to CriticalTasks when target is deleted
- Index on ExerciseId for query performance

## Test Scenarios

### Unit Tests
- CapabilityTarget entity validation
- CapabilityTarget service CRUD operations
- Authorization checks

### Integration Tests
- Full API endpoint tests with authentication
- Cascade delete verification
- Cross-organization capability reference prevention
- Offline sync queue and reconciliation

---

*Story created: 2026-02-03*
