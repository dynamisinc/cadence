# Story OM-10: Agency Assignment

**Priority:** P1 (Standard)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As an** Exercise Director or Controller,  
**I want** to assign agencies to exercise participants, injects, and observations,  
**So that** I can track and report on performance by responding agency.

---

## Context

With the agency list established (OM-09), this story covers how agencies are actually used throughout the exercise lifecycle:

1. **Exercise Participants** - "Jane represents Fire Department in this exercise"
2. **Injects** - "This inject is targeted at EMS"
3. **Observations** - "This observation is about Police response"

Agency assignment enables filtering, grouping, and reporting capabilities that are essential for after-action review.

---

## Acceptance Criteria

### Exercise Participant Agency Assignment

- [ ] **Given** I am adding a participant to an exercise, **when** I see the form, **then** I see an optional Agency dropdown
- [ ] **Given** I am assigning an agency, **when** I view the dropdown, **then** I see only active agencies from my organization
- [ ] **Given** I assign an agency to a participant, **when** viewing the participant list, **then** I see their agency displayed
- [ ] **Given** a participant has no agency assigned, **when** viewing the list, **then** the agency column shows "-" or is blank
- [ ] **Given** I am editing a participant, **when** I change their agency, **then** the change is saved

### Inject Target Agency

- [ ] **Given** I am creating an inject, **when** I see the form, **then** I see an optional "Target Agency" field
- [ ] **Given** I am editing an inject, **when** I set a target agency, **then** the inject indicates which agency should respond
- [ ] **Given** an inject has a target agency, **when** viewing the MSEL, **then** I see the agency abbreviation or name
- [ ] **Given** I am filtering the MSEL, **when** I filter by agency, **then** I see only injects targeting that agency

### Observation Agency Assignment

- [ ] **Given** I am creating an observation, **when** I see the form, **then** I see an optional "Observed Agency" field
- [ ] **Given** I am creating an observation linked to an inject, **when** the inject has a target agency, **then** the agency is pre-selected (but editable)
- [ ] **Given** I save an observation with an agency, **when** viewing observations, **then** the agency is displayed
- [ ] **Given** I am filtering observations, **when** I filter by agency, **then** I see only observations about that agency

### Agency Filter in Lists

- [ ] **Given** I am on the MSEL view, **when** I click the Agency filter, **then** I see a dropdown of all agencies used in this exercise
- [ ] **Given** I select an agency filter, **when** the filter applies, **then** only items for that agency are shown
- [ ] **Given** I clear the agency filter, **when** the filter clears, **then** all items are shown again

### Agency Column in Tables

- [ ] **Given** I am viewing the participant list, **when** looking at columns, **then** Agency is a visible/toggleable column
- [ ] **Given** I am viewing the MSEL, **when** looking at columns, **then** Target Agency is a visible/toggleable column
- [ ] **Given** agency is a column, **when** I click the header, **then** I can sort by agency

### Inactive Agency Display

- [ ] **Given** an agency was deactivated after being assigned, **when** viewing the assignment, **then** it shows with "(Inactive)" label
- [ ] **Given** an inactive agency is displayed, **when** I try to reassign, **then** the inactive agency is not in the dropdown
- [ ] **Given** I am creating a new assignment, **when** viewing the dropdown, **then** inactive agencies are not shown

### Agency in Reports (Preview)

- [ ] **Given** observations have agencies assigned, **when** viewing the exercise summary, **then** I can see observation counts by agency
- [ ] **Given** I am on the post-exercise view, **when** I request a report, **then** agencies are included in grouping options

---

## Out of Scope

- Multiple agencies per inject (single target agency only)
- Multiple agencies per participant (represent one agency per exercise)
- Agency-specific permissions (agency admins, etc.)
- Agency contact during exercise (notifications to agency POC)
- Agency capability mapping

---

## Dependencies

- OM-09: Agency List Management (agencies must exist)
- Exercise Participant feature
- Inject CRUD feature
- Observation feature (if exists)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Target Agency | The agency an inject is directed at or expects a response from |
| Observed Agency | The agency whose performance is being documented in an observation |
| Participant Agency | The agency a participant represents during the exercise |

---

## UI/UX Notes

### Participant Form with Agency
```
┌─────────────────────────────────────────────────┐
│ Add Exercise Participant                  [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ User *                                          │
│ ┌─────────────────────────────────────────┐    │
│ │ Select user...                       ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Exercise Role *                                 │
│ ┌─────────────────────────────────────────┐    │
│ │ Controller                           ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Representing Agency                             │
│ ┌─────────────────────────────────────────┐    │
│ │ Fire Department (FD)                 ▼  │    │
│ └─────────────────────────────────────────┘    │
│ Optional: Which agency does this participant   │
│ represent during this exercise?                │
│                                                 │
│                    [Cancel]  [Add Participant]  │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Inject Form with Target Agency
```
┌─────────────────────────────────────────────────┐
│ Create Inject                             [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Inject Number *         Scheduled Time *        │
│ ┌─────────────────┐    ┌─────────────────┐     │
│ │ INJ-001          │    │ 09:30           │     │
│ └─────────────────┘    └─────────────────┘     │
│                                                 │
│ Title *                                         │
│ ┌─────────────────────────────────────────┐    │
│ │ Multi-vehicle accident reported         │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Description                                     │
│ ┌─────────────────────────────────────────┐    │
│ │ 911 call reports MVA with injuries...   │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Target Agency                                   │
│ ┌─────────────────────────────────────────┐    │
│ │ Emergency Medical Services (EMS)     ▼  │    │
│ └─────────────────────────────────────────┘    │
│ Which agency should respond to this inject?    │
│                                                 │
│ [More fields...]                                │
│                                                 │
│                          [Cancel]  [Create]     │
│                                                 │
└─────────────────────────────────────────────────┘
```

### MSEL View with Agency Column
```
┌─────────────────────────────────────────────────────────────────────────┐
│ MSEL - Hurricane Response Exercise                                       │
├─────────────────────────────────────────────────────────────────────────┤
│ Filter: Agency [All ▼]  Status [All ▼]  Phase [All ▼]    🔍 Search...  │
├─────────────────────────────────────────────────────────────────────────┤
│ # │ Time  │ Title                           │ Agency │ Status │ Actions │
├───┼───────┼─────────────────────────────────┼────────┼────────┼─────────┤
│ 1 │ 09:00 │ Exercise start announcement     │ -      │ ⏳     │ [...]   │
│ 2 │ 09:15 │ Initial storm damage report     │ EM     │ ⏳     │ [...]   │
│ 3 │ 09:30 │ MVA with injuries               │ EMS    │ ✅     │ [...]   │
│ 4 │ 09:45 │ Structure fire reported         │ FD     │ ⏳     │ [...]   │
│ 5 │ 10:00 │ Hospital surge notification     │ HOSP   │ ⏳     │ [...]   │
└───┴───────┴─────────────────────────────────┴────────┴────────┴─────────┘
```

### Observation Form with Agency
```
┌─────────────────────────────────────────────────┐
│ Record Observation                        [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Related Inject                                  │
│ ┌─────────────────────────────────────────┐    │
│ │ INJ-003: MVA with injuries (EMS)     ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Observed Agency                                 │
│ ┌─────────────────────────────────────────┐    │
│ │ Emergency Medical Services (EMS)     ▼  │    │  ← Pre-filled from inject
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Observation *                                   │
│ ┌─────────────────────────────────────────┐    │
│ │ EMS response time was 8 minutes.        │    │
│ │ Dispatch communication was clear.       │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Rating                                          │
│ ┌─────────────────────────────────────────┐    │
│ │ Performed (P)                        ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│                    [Cancel]  [Save Observation] │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Agency Filter Dropdown
```
┌────────────────────────┐
│ Filter by Agency       │
├────────────────────────┤
│ ○ All Agencies         │
├────────────────────────┤
│ ● EMS (12 items)       │
│ ○ Fire (8 items)       │
│ ○ Police (5 items)     │
│ ○ Hospital (3 items)   │
│ ○ No Agency (4 items)  │
└────────────────────────┘
```

### Exercise Summary - By Agency
```
┌─────────────────────────────────────────────────────────────────┐
│ Exercise Summary                                                 │
├─────────────────────────────────────────────────────────────────┤
│ Observations by Agency                                          │
├─────────────────────────────────────────────────────────────────┤
│ Agency      │ Total │ P  │ S  │ M  │ U  │ Performance          │
├─────────────┼───────┼────┼────┼────┼────┼──────────────────────┤
│ EMS         │  12   │ 8  │ 3  │ 1  │ 0  │ ████████░░ 83%       │
│ Fire        │   8   │ 5  │ 2  │ 1  │ 0  │ ███████░░░ 75%       │
│ Police      │   5   │ 3  │ 1  │ 1  │ 0  │ ██████░░░░ 60%       │
│ Hospital    │   3   │ 2  │ 1  │ 0  │ 0  │ █████████░ 83%       │
└─────────────┴───────┴────┴────┴────┴────┴──────────────────────┘
P=Performed, S=Performed with Some Gaps, M=Performed with Major Gaps, U=Unable to Perform
```

---

## Technical Notes

### Data Model Updates

**ExerciseParticipant (add field):**
```csharp
public class ExerciseParticipant
{
    // ... existing fields
    public Guid? AgencyId { get; set; }
    public Agency? Agency { get; set; }
}
```

**Inject (add field):**
```csharp
public class Inject
{
    // ... existing fields
    public Guid? TargetAgencyId { get; set; }
    public Agency? TargetAgency { get; set; }
}
```

**Observation (add field):**
```csharp
public class Observation
{
    // ... existing fields
    public Guid? AgencyId { get; set; }
    public Agency? Agency { get; set; }
}
```

### API Updates

**Participant endpoints - add agency:**
```
POST /api/exercises/{id}/participants
{
  "userId": "guid",
  "exerciseRole": "Controller",
  "agencyId": "guid"  // optional
}

GET /api/exercises/{id}/participants
Response includes agency info:
{
  "items": [{
    "id": "guid",
    "user": { ... },
    "exerciseRole": "Controller",
    "agency": {
      "id": "guid",
      "name": "Fire Department",
      "abbreviation": "FD"
    }
  }]
}
```

**Inject endpoints - add agency:**
```
POST /api/exercises/{exerciseId}/injects
{
  // ... existing fields
  "targetAgencyId": "guid"  // optional
}

GET /api/exercises/{exerciseId}/injects
Query parameters:
  - agencyId: guid (filter by target agency)

Response includes agency:
{
  "items": [{
    "id": "guid",
    // ... existing fields
    "targetAgency": {
      "id": "guid",
      "name": "EMS",
      "abbreviation": "EMS"
    }
  }]
}
```

**Observation endpoints - add agency:**
```
POST /api/observations
{
  // ... existing fields
  "agencyId": "guid"  // optional, defaults from inject if linked
}

GET /api/exercises/{id}/observations
Query parameters:
  - agencyId: guid (filter by observed agency)
```

### Agency Statistics Endpoint

```
GET /api/exercises/{id}/statistics/by-agency
Authorization: Bearer {token}

Response:
{
  "items": [
    {
      "agency": {
        "id": "guid",
        "name": "EMS",
        "abbreviation": "EMS"
      },
      "injectCount": 12,
      "observationCount": 15,
      "ratings": {
        "P": 8,
        "S": 3,
        "M": 3,
        "U": 1
      },
      "participantCount": 3
    }
  ]
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Assign agency to participant | Integration | P0 |
| Assign agency to inject | Integration | P0 |
| Assign agency to observation | Integration | P0 |
| Filter MSEL by agency | Integration | P0 |
| Filter observations by agency | Integration | P0 |
| Agency pre-fills from inject to observation | Integration | P1 |
| Inactive agency displays with label | Integration | P1 |
| Inactive agency not in dropdowns | Integration | P0 |
| Agency statistics endpoint | Integration | P1 |
| Sort by agency column | Component | P1 |

---

## Implementation Checklist

### Backend
- [ ] Add `AgencyId` to `ExerciseParticipant` entity
- [ ] Add `TargetAgencyId` to `Inject` entity
- [ ] Add `AgencyId` to `Observation` entity
- [ ] Create database migration
- [ ] Update participant endpoints to include agency
- [ ] Update inject endpoints to include agency
- [ ] Update observation endpoints to include agency
- [ ] Add agency filter to list endpoints
- [ ] Create agency statistics endpoint
- [ ] Include agency in response DTOs
- [ ] Unit tests for updated services
- [ ] Integration tests for agency filtering

### Frontend
- [ ] Add agency dropdown to participant form
- [ ] Add agency dropdown to inject form
- [ ] Add agency dropdown to observation form
- [ ] Add agency column to participant table
- [ ] Add agency column to MSEL table
- [ ] Add agency column to observations table
- [ ] Add agency filter dropdown to lists
- [ ] Implement agency pre-fill from inject to observation
- [ ] Handle inactive agency display
- [ ] Create agency statistics component (for summary)
- [ ] Component tests

### Database
- [ ] Migration for ExerciseParticipant.AgencyId
- [ ] Migration for Inject.TargetAgencyId
- [ ] Migration for Observation.AgencyId
- [ ] Foreign key constraints with SET NULL on delete

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
