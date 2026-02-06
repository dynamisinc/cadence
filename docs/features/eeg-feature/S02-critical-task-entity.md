# S02: Critical Task Entity and API

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to define Critical Tasks under each Capability Target,
**So that** evaluators know exactly what actions to observe and assess.

## Context

Critical Tasks are the specific actions required to achieve a Capability Target. In HSEEP terminology, these are the observable, assessable activities that demonstrate capability performance. For example, under a Capability Target of "Activate EOC within 60 minutes," Critical Tasks might include:

1. Issue EOC activation notification to all stakeholders
2. Activate emergency communication systems
3. Staff EOC positions per assignment roster

This story creates the backend entity and API for Critical Tasks. The UI is covered in S04.

## Acceptance Criteria

### Entity Requirements

- [ ] **Given** the Cadence database, **when** migrations run, **then** a `CriticalTasks` table exists with proper schema
- [ ] **Given** a CriticalTask entity, **when** created, **then** it requires CapabilityTargetId and TaskDescription
- [ ] **Given** a CriticalTask, **when** the parent CapabilityTarget is deleted, **then** the CriticalTask is cascade deleted
- [ ] **Given** a CriticalTask, **when** created, **then** SortOrder defaults to next available within the target
- [ ] **Given** a CriticalTask, **when** created, **then** Standard field is optional

### API Requirements

- [ ] **Given** I am authenticated with Director+ role, **when** I GET `/api/capability-targets/{targetId}/critical-tasks`, **then** I receive a list of tasks
- [ ] **Given** I am authenticated with Director+ role, **when** I POST to `/api/capability-targets/{targetId}/critical-tasks`, **then** a new task is created
- [ ] **Given** I am authenticated with Director+ role, **when** I PUT to `/api/critical-tasks/{id}`, **then** the task is updated
- [ ] **Given** I am authenticated with Director+ role, **when** I DELETE `/api/critical-tasks/{id}`, **then** the task and its EEG entries are deleted
- [ ] **Given** I am authenticated with Evaluator role, **when** I GET tasks, **then** I receive the list (read access)
- [ ] **Given** I am authenticated with Evaluator role, **when** I attempt to create/update/delete, **then** I receive 403 Forbidden
- [ ] **Given** an invalid CapabilityTargetId, **when** I call any endpoint, **then** I receive 404 Not Found

### Validation Rules

- [ ] **Given** TaskDescription, **when** empty or whitespace, **then** validation fails
- [ ] **Given** TaskDescription, **when** longer than 500 characters, **then** validation fails
- [ ] **Given** Standard, **when** longer than 1000 characters, **then** validation fails
- [ ] **Given** SortOrder, **when** negative, **then** validation fails

### Sync Requirements

- [ ] **Given** a CriticalTask is created/updated/deleted, **when** other clients are connected, **then** they receive real-time updates via SignalR
- [ ] **Given** offline mode, **when** a task is created, **then** it queues for sync with proper conflict resolution

## Data Model

```csharp
public class CriticalTask : BaseEntity
{
    public Guid CapabilityTargetId { get; set; }
    public CapabilityTarget CapabilityTarget { get; set; } = null!;
    
    /// <summary>
    /// Specific action required to achieve the capability target.
    /// Example: "Issue EOC activation notification to all stakeholders"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string TaskDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Conditions and standards for task performance.
    /// Example: "Per SOP 5.2, using emergency notification system"
    /// </summary>
    [MaxLength(1000)]
    public string? Standard { get; set; }
    
    /// <summary>
    /// Display order within the capability target's tasks
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Injects that test this task (many-to-many)
    /// </summary>
    public ICollection<InjectCriticalTask> LinkedInjects { get; set; } = new List<InjectCriticalTask>();
    
    /// <summary>
    /// EEG entries recorded against this task
    /// </summary>
    public ICollection<EegEntry> EegEntries { get; set; } = new List<EegEntry>();
}

/// <summary>
/// Junction table for many-to-many relationship between Injects and Critical Tasks
/// </summary>
public class InjectCriticalTask
{
    public Guid InjectId { get; set; }
    public Inject Inject { get; set; } = null!;
    
    public Guid CriticalTaskId { get; set; }
    public CriticalTask CriticalTask { get; set; } = null!;
}
```

## API Specification

### GET /api/capability-targets/{targetId}/critical-tasks

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "capabilityTargetId": "guid",
      "taskDescription": "Issue EOC activation notification to all stakeholders",
      "standard": "Per SOP 5.2, using emergency notification system",
      "sortOrder": 1,
      "linkedInjectCount": 2,
      "eegEntryCount": 1
    }
  ],
  "totalCount": 3
}
```

### GET /api/exercises/{exerciseId}/critical-tasks

**Query Parameters:**
- `capabilityTargetId` (optional): Filter by specific target
- `hasInjects` (optional): Filter to tasks with/without linked injects
- `hasEegEntries` (optional): Filter to tasks with/without EEG entries

**Response 200:** List of all critical tasks across all targets in the exercise

### POST /api/capability-targets/{targetId}/critical-tasks

**Request:**
```json
{
  "taskDescription": "Issue EOC activation notification to all stakeholders",
  "standard": "Per SOP 5.2",
  "sortOrder": 1
}
```

**Response 201:** Created task object

### PUT /api/critical-tasks/{id}

**Request:**
```json
{
  "taskDescription": "Updated description",
  "standard": "Updated standard",
  "sortOrder": 2
}
```

**Response 200:** Updated task object

### DELETE /api/critical-tasks/{id}

**Response 204:** No content (cascades to InjectCriticalTask links and EegEntries)

## Out of Scope

- UI for managing tasks (S04)
- Linking tasks to injects (S05)
- EEG entry against tasks (S06)
- Task templates or libraries (future enhancement)
- Bulk import of tasks (future enhancement)

## Dependencies

- CapabilityTarget entity (S01)
- Sync service for real-time updates

## Technical Notes

- Create `InjectCriticalTask` junction table in this story (entity only, API in S05)
- Index on CapabilityTargetId for query performance
- Consider adding `IsEvaluated` computed property for quick coverage checks
- Cascade delete should remove junction records and EEG entries

## Test Scenarios

### Unit Tests
- CriticalTask entity validation
- CriticalTask service CRUD operations
- Authorization checks (Director can edit, Evaluator cannot)
- Cascade delete behavior

### Integration Tests
- Full API endpoint tests with authentication
- Cross-capability-target reference prevention
- Tasks returned with counts (injects, EEG entries)
- Offline sync queue and reconciliation

---

*Story created: 2026-02-03*
