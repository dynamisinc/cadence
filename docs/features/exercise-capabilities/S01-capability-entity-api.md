# Story: Capability Entity and API

**Feature:** Exercise Capabilities  
**Story ID:** S01  
**Priority:** P0 (MVP)  
**Phase:** Standard Implementation

---

## User Story

**As a** Developer,  
**I want** the Capability entity and CRUD API endpoints,  
**So that** capabilities can be managed at the organization level.

---

## Context

Capabilities represent the organizational competencies or functions that exercises evaluate. This story establishes the foundational data model and API layer that all other capability-related features depend on. The design supports multiple capability frameworks (FEMA, NATO, NIST, ISO, custom) through a flexible schema with optional categorization.

Capabilities are scoped to organizations, allowing each organization to maintain their own capability library independent of others. This enables commercial clients, international agencies, and US federal organizations to each use their preferred framework.

### HSEEP Evaluation Hierarchy

The Capability entity is the foundation of a larger evaluation hierarchy:

```
Organization
└── Capability (this story - organization library)
        │
        └── CapabilityTarget (EEG feature - exercise-scoped threshold)
                │
                └── CriticalTask (EEG feature - specific assessable action)
                        │
                        └── EegEntry (EEG feature - evaluator assessment)
```

**Key Distinction:**
- **Capability** = Generic organizational function (e.g., "Operational Communications")
- **CapabilityTarget** = Exercise-specific performance threshold (e.g., "Establish communications within 30 minutes")
- **CriticalTask** = Specific observable action (e.g., "Issue activation notification")

This story creates the Capability library. The EEG feature creates CapabilityTarget and CriticalTask entities that reference capabilities from this library.

---

## Acceptance Criteria

- [ ] **Given** the database, **when** migrations run, **then** Capability table is created with all required columns
- [ ] **Given** the database, **when** migrations run, **then** ExerciseCapability junction table is created
- [ ] **Given** the database, **when** migrations run, **then** ObservationCapability junction table is created
- [ ] **Given** an authenticated Administrator, **when** calling `GET /api/organizations/{orgId}/capabilities`, **then** returns list of capabilities for that organization
- [ ] **Given** the capabilities list endpoint, **when** called with `?includeInactive=false` (default), **then** only active capabilities are returned
- [ ] **Given** the capabilities list endpoint, **when** called with `?includeInactive=true`, **then** all capabilities including inactive are returned
- [ ] **Given** an authenticated Administrator, **when** calling `POST /api/organizations/{orgId}/capabilities` with valid data, **then** creates a new capability and returns 201
- [ ] **Given** the create endpoint, **when** called with missing required field (Name), **then** returns 400 with validation error
- [ ] **Given** the create endpoint, **when** called with duplicate Name within same organization, **then** returns 409 Conflict
- [ ] **Given** an authenticated Administrator, **when** calling `PUT /api/organizations/{orgId}/capabilities/{id}` with valid data, **then** updates the capability
- [ ] **Given** an authenticated Administrator, **when** calling `DELETE /api/organizations/{orgId}/capabilities/{id}`, **then** sets IsActive to false (soft delete)
- [ ] **Given** a capability linked to observations or capability targets, **when** DELETE is called, **then** capability is deactivated but not removed (preserves referential integrity)
- [ ] **Given** a non-Administrator role, **when** calling any capability management endpoint, **then** returns 403 Forbidden

---

## Out of Scope

- Admin UI (S02)
- Predefined library import (S03)
- Exercise target capability selection (S04)
- Observation capability tagging (S05)
- Metrics integration (S06)
- Bulk operations (import/export CSV)
- CapabilityTarget entity (EEG Feature S01)
- CriticalTask entity (EEG Feature S02)

---

## Dependencies

- Organization entity exists
- Authentication and role-based authorization
- Standard API patterns established

---

## Open Questions

- [x] Should capabilities be globally unique or unique per organization? **Per organization**
- [x] Should soft-deleted capabilities be permanently deletable? **No, preserve for data integrity**
- [ ] Should we add a `Code` field for short identifiers? **Deferred to future enhancement**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Capability | An organizational competency or function that can be evaluated during an exercise (library item) |
| Category | Optional grouping for capabilities (e.g., Mission Area for FEMA capabilities) |
| Source Library | Identifier for predefined libraries (FEMA, NATO, NIST, ISO) vs custom |
| Active | Whether a capability is available for selection in exercises and observations |
| CapabilityTarget | Exercise-scoped performance threshold referencing a Capability (see EEG feature) |
| CriticalTask | Specific action required to achieve a CapabilityTarget (see EEG feature) |

---

## Technical Notes

### Entity Definition

```csharp
namespace Cadence.Core.Entities;

/// <summary>
/// Represents an organizational capability that can be evaluated during exercises.
/// Capabilities are scoped to organizations and support multiple frameworks
/// (FEMA Core Capabilities, NATO Baseline Requirements, NIST CSF, ISO 22301, custom).
/// 
/// This is the "library" entity. For exercise-specific performance thresholds,
/// see CapabilityTarget in the EEG feature.
/// </summary>
public class Capability
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The organization that owns this capability definition.
    /// </summary>
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Display name of the capability (e.g., "Mass Care Services", "Cybersecurity").
    /// Required, max 200 characters.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of what this capability encompasses.
    /// Optional, max 1000 characters.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Grouping category (e.g., "Response", "Protection" for FEMA Mission Areas).
    /// Optional, max 100 characters.
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Display order within category. Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>
    /// Whether this capability is available for selection.
    /// Inactive capabilities are hidden from UIs but preserved for historical data.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Identifies the predefined library this was imported from (FEMA, NATO, NIST, ISO).
    /// Null for custom capabilities. Used for display and potential updates.
    /// </summary>
    public string? SourceLibrary { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Organization Organization { get; set; } = null!;
    
    /// <summary>
    /// Exercises that have selected this capability as a target (simple selection).
    /// </summary>
    public ICollection<ExerciseCapability> ExerciseCapabilities { get; set; } = [];
    
    /// <summary>
    /// Observations tagged with this capability (ad-hoc tagging).
    /// </summary>
    public ICollection<ObservationCapability> ObservationCapabilities { get; set; } = [];
    
    /// <summary>
    /// Capability Targets that reference this capability (EEG feature).
    /// Each CapabilityTarget has a measurable threshold and Critical Tasks.
    /// </summary>
    public ICollection<CapabilityTarget> CapabilityTargets { get; set; } = [];
}
```

### Junction Tables

```csharp
/// <summary>
/// Links exercises to their target capabilities for evaluation (simple selection).
/// For detailed performance thresholds, see CapabilityTarget in EEG feature.
/// </summary>
public class ExerciseCapability
{
    public Guid ExerciseId { get; set; }
    public Guid CapabilityId { get; set; }
    
    public Exercise Exercise { get; set; } = null!;
    public Capability Capability { get; set; } = null!;
}

/// <summary>
/// Links observations to evaluated capabilities (ad-hoc tagging).
/// For structured assessments against Critical Tasks, see EegEntry in EEG feature.
/// </summary>
public class ObservationCapability
{
    public Guid ObservationId { get; set; }
    public Guid CapabilityId { get; set; }
    
    public Observation Observation { get; set; } = null!;
    public Capability Capability { get; set; } = null!;
}
```

### Relationship to EEG Feature

The EEG feature defines additional entities that reference Capability:

```csharp
// Defined in EEG Feature - shown here for relationship context
public class CapabilityTarget
{
    public Guid ExerciseId { get; set; }
    public Guid CapabilityId { get; set; }  // References this Capability entity
    public string TargetDescription { get; set; }  // e.g., "Activate EOC within 60 min"
    
    public Capability Capability { get; set; } = null!;  // Navigation to library item
    public ICollection<CriticalTask> CriticalTasks { get; set; } = [];
}
```

### DTOs

```csharp
public record CapabilityDto(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    int SortOrder,
    bool IsActive,
    string? SourceLibrary
);

public record CreateCapabilityRequest(
    string Name,
    string? Description,
    string? Category,
    int? SortOrder
);

public record UpdateCapabilityRequest(
    string Name,
    string? Description,
    string? Category,
    int? SortOrder,
    bool? IsActive
);
```

### API Controller

```csharp
[ApiController]
[Route("api/organizations/{organizationId}/capabilities")]
[Authorize(Roles = "Administrator")]
public class CapabilitiesController : ControllerBase
{
    // GET /api/organizations/{orgId}/capabilities?includeInactive=false
    // POST /api/organizations/{orgId}/capabilities
    // PUT /api/organizations/{orgId}/capabilities/{id}
    // DELETE /api/organizations/{orgId}/capabilities/{id}
}
```

### Database Migration

```csharp
migrationBuilder.CreateTable(
    name: "Capabilities",
    columns: table => new
    {
        Id = table.Column<Guid>(nullable: false),
        OrganizationId = table.Column<Guid>(nullable: false),
        Name = table.Column<string>(maxLength: 200, nullable: false),
        Description = table.Column<string>(maxLength: 1000, nullable: true),
        Category = table.Column<string>(maxLength: 100, nullable: true),
        SortOrder = table.Column<int>(nullable: false, defaultValue: 0),
        IsActive = table.Column<bool>(nullable: false, defaultValue: true),
        SourceLibrary = table.Column<string>(maxLength: 50, nullable: true),
        CreatedAt = table.Column<DateTime>(nullable: false),
        UpdatedAt = table.Column<DateTime>(nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Capabilities", x => x.Id);
        table.ForeignKey(
            name: "FK_Capabilities_Organizations",
            column: x => x.OrganizationId,
            principalTable: "Organizations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    });

migrationBuilder.CreateIndex(
    name: "IX_Capabilities_OrganizationId_Name",
    table: "Capabilities",
    columns: new[] { "OrganizationId", "Name" },
    unique: true);
```

---

## UI/UX Notes

This story is API-only. See S02 for Admin UI.

---

## Estimation

**T-Shirt Size:** S  
**Story Points:** 3

---

## Testing Requirements

### Unit Tests
- [ ] Capability entity validation (Name required, max lengths)
- [ ] CapabilityService CRUD operations
- [ ] Duplicate name detection within organization

### Integration Tests
- [ ] API endpoint authorization (Admin only)
- [ ] Create/Read/Update/Delete flow
- [ ] Soft delete preserves linked data (observations, capability targets)
- [ ] Query filtering (active/inactive)

---

## Related Features

| Feature | Relationship |
|---------|--------------|
| Exercise Capabilities S04 | Uses Capability for simple target selection |
| Exercise Capabilities S05 | Uses Capability for observation tagging |
| Exercise Capabilities S06 | Uses Capability for metrics grouping |
| **EEG Feature** | Creates CapabilityTarget entities that reference Capability with measurable thresholds |
