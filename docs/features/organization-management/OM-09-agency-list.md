# Story OM-09: Agency List Management

**Priority:** P1 (Standard)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As an** Organization Administrator,  
**I want** to manage a list of participating agencies for my organization,  
**So that** exercises can track performance and observations by agency.

---

## Context

Agencies represent the responding organizations/departments that participate in exercises. They are **not** structural divisions of the organization - they are participant types used for:

- Filtering injects by target agency ("This inject is for Fire Department")
- Filtering observations by observed agency ("How did EMS perform?")
- Grouping exercise participants ("Who represents Hospital?")
- After-action reporting ("Agency performance summary")

**Examples by domain:**
- Emergency Management: Fire, EMS, Police, Hospital, Public Health, Red Cross
- Cybersecurity: IT Security, Legal, Communications, HR, Executive Team
- ICS-based: Operations, Planning, Logistics, Finance/Admin
- Business Continuity: Facilities, IT, HR, Customer Service

---

## Acceptance Criteria

### Agency List Display

- [ ] **Given** I am an OrgAdmin, **when** I navigate to Organization Settings, **then** I see an "Agencies" section
- [ ] **Given** I view the agencies list, **when** there are agencies, **then** I see: Name, Abbreviation, Description, Active status
- [ ] **Given** I view the agencies list, **when** there are no agencies, **then** I see an empty state with "Add your first agency"
- [ ] **Given** I view the list, **when** agencies exist, **then** they are sorted by SortOrder, then by Name

### Create Agency

- [ ] **Given** I am an OrgAdmin, **when** I click "Add Agency", **then** I see a form to create an agency
- [ ] **Given** I am creating an agency, **when** I enter a Name, **then** it is required and max 200 characters
- [ ] **Given** I am creating an agency, **when** I enter an Abbreviation, **then** it is optional and max 20 characters
- [ ] **Given** I am creating an agency, **when** I enter a Description, **then** it is optional and max 500 characters
- [ ] **Given** I submit the form, **when** the name already exists in this org, **then** I see "An agency with this name already exists"
- [ ] **Given** I submit valid data, **when** creation succeeds, **then** the agency is added to the list

### Edit Agency

- [ ] **Given** I view an agency in the list, **when** I click Edit, **then** I see a form pre-filled with current values
- [ ] **Given** I am editing an agency, **when** I change the name to an existing name, **then** I see a duplicate error
- [ ] **Given** I save changes, **when** the update succeeds, **then** the list reflects the changes

### Deactivate/Reactivate Agency

- [ ] **Given** I view an active agency, **when** I click Deactivate, **then** I see a confirmation dialog
- [ ] **Given** I confirm deactivation, **when** the action completes, **then** the agency is marked inactive
- [ ] **Given** an agency is inactive, **when** viewing it in dropdowns elsewhere, **then** it does not appear as an option
- [ ] **Given** an agency is inactive, **when** it's already assigned to injects/participants, **then** those assignments remain but display "(Inactive)"
- [ ] **Given** I view an inactive agency, **when** I click Reactivate, **then** it becomes active again

### Reorder Agencies

- [ ] **Given** I view the agency list, **when** I drag an agency, **then** I can drop it in a new position
- [ ] **Given** I reorder agencies, **when** I release, **then** the new order is saved automatically
- [ ] **Given** agencies are reordered, **when** viewing agency dropdowns elsewhere, **then** they appear in the custom order

### Seed Common Agencies

- [ ] **Given** I have no agencies, **when** I click "Add Common Agencies", **then** I see a list of common agency templates
- [ ] **Given** I view common agencies, **when** I select some and click Add, **then** they are added to my list
- [ ] **Given** I add common agencies, **when** I already have an agency with the same name, **then** it is skipped with a note

---

## Out of Scope

- Agency hierarchies (parent/child agencies)
- Agency contact information
- Agency-specific settings
- Importing agencies from external systems
- Agency logos/icons
- Cross-organization agency sharing

---

## Dependencies

- OM-03: Edit Organization (agencies are org-scoped)
- Exercise Participant model (for agency assignment)
- Inject model (for target agency)
- Observation model (for observed agency)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Agency | A responding organization or department that participates in exercises |
| Abbreviation | Short form of agency name (e.g., "EMS" for "Emergency Medical Services") |
| Common Agencies | Pre-defined agency templates for quick setup |

---

## UI/UX Notes

### Agency List Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Agencies                                         [+ Add Agency] │
├─────────────────────────────────────────────────────────────────┤
│ Manage the list of agencies that participate in your exercises. │
│ Drag to reorder. [Add Common Agencies]                          │
├─────────────────────────────────────────────────────────────────┤
│ ⠿ │ Name                    │ Abbrev │ Status │ Actions        │
├───┼─────────────────────────┼────────┼────────┼────────────────┤
│ ⠿ │ Fire Department         │ FD     │ 🟢     │ [Edit] [...]  │
│ ⠿ │ Emergency Medical Svcs  │ EMS    │ 🟢     │ [Edit] [...]  │
│ ⠿ │ Police Department       │ PD     │ 🟢     │ [Edit] [...]  │
│ ⠿ │ Public Health           │ PH     │ 🟢     │ [Edit] [...]  │
│ ⠿ │ Hospital Network        │ HOSP   │ 🔴     │ [Edit] [...]  │
└───┴─────────────────────────┴────────┴────────┴────────────────┘
                                              ⠿ = drag handle
```

### Add/Edit Agency Dialog
```
┌─────────────────────────────────────────────────┐
│ Add Agency                                [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Agency Name *                                   │
│ ┌─────────────────────────────────────────┐    │
│ │ Emergency Medical Services              │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Abbreviation                                    │
│ ┌─────────────────────────────────────────┐    │
│ │ EMS                                      │    │
│ └─────────────────────────────────────────┘    │
│ Used in compact views and reports              │
│                                                 │
│ Description                                     │
│ ┌─────────────────────────────────────────┐    │
│ │ County emergency medical services        │    │
│ │ including ambulance and paramedics       │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│                          [Cancel]  [Save Agency]│
│                                                 │
└─────────────────────────────────────────────────┘
```

### Agency Actions Menu (...)
```
┌────────────────────┐
│ ✏️ Edit           │
│ 🔴 Deactivate     │  (or 🟢 Reactivate if inactive)
│ ─────────────────  │
│ 🗑️ Delete        │  (only if unused)
└────────────────────┘
```

### Add Common Agencies Dialog
```
┌─────────────────────────────────────────────────┐
│ Add Common Agencies                       [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Select agencies to add to your organization:   │
│                                                 │
│ Emergency Response                              │
│ ┌─────────────────────────────────────────┐    │
│ │ ☑ Fire Department (FD)                  │    │
│ │ ☑ Emergency Medical Services (EMS)      │    │
│ │ ☑ Police Department (PD)                │    │
│ │ ☑ Sheriff's Office (SO)                 │    │
│ │ ☐ Search and Rescue (SAR)               │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Public Health & Medical                         │
│ ┌─────────────────────────────────────────┐    │
│ │ ☑ Hospital (HOSP)                       │    │
│ │ ☑ Public Health (PH)                    │    │
│ │ ☐ Medical Examiner (ME)                 │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Government & Support                            │
│ ┌─────────────────────────────────────────┐    │
│ │ ☐ Emergency Management (EM)             │    │
│ │ ☐ Public Works (PW)                     │    │
│ │ ☐ Communications/Dispatch (COMM)        │    │
│ │ ☐ Red Cross (RC)                        │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Selected: 6 agencies                           │
│ ⚠️ 1 agency already exists and will be skipped│
│                                                 │
│                    [Cancel]  [Add 5 Agencies]   │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Empty State
```
┌─────────────────────────────────────────────────────────────────┐
│ Agencies                                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│     🏢                                                          │
│     No agencies yet                                             │
│                                                                  │
│     Add agencies to track exercise participation               │
│     and performance by responding organization.                │
│                                                                  │
│     [+ Add Agency]    [Add Common Agencies]                    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoints

**List Agencies:**
```
GET /api/organizations/current/agencies
Authorization: Bearer {token}

Query Parameters:
  - includeInactive: bool (default: false)

Response:
{
  "items": [
    {
      "id": "guid",
      "name": "Fire Department",
      "abbreviation": "FD",
      "description": "County fire services",
      "isActive": true,
      "sortOrder": 0
    }
  ]
}
```

**Create Agency:**
```
POST /api/organizations/current/agencies
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "name": "Emergency Medical Services",
  "abbreviation": "EMS",
  "description": "County EMS including ambulance"
}

Response (201 Created):
{
  "id": "guid",
  "name": "Emergency Medical Services",
  "abbreviation": "EMS",
  "description": "County EMS including ambulance",
  "isActive": true,
  "sortOrder": 5
}
```

**Update Agency:**
```
PUT /api/organizations/current/agencies/{id}
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "name": "Emergency Medical Services",
  "abbreviation": "EMS",
  "description": "Updated description",
  "isActive": true
}

Response (200 OK):
{
  "id": "guid",
  "name": "Emergency Medical Services",
  ...
}
```

**Reorder Agencies:**
```
PUT /api/organizations/current/agencies/reorder
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "orderedIds": ["guid1", "guid2", "guid3", ...]
}

Response (200 OK):
{
  "success": true
}
```

**Delete Agency:**
```
DELETE /api/organizations/current/agencies/{id}
Authorization: Bearer {token} (OrgAdmin only)

Response (200 OK - if unused):
{
  "deleted": true
}

Response (409 Conflict - if in use):
{
  "error": "AgencyInUse",
  "message": "This agency is assigned to exercises and cannot be deleted. Deactivate it instead.",
  "usageCount": {
    "participants": 5,
    "injects": 12,
    "observations": 8
  }
}
```

**Get Common Agency Templates:**
```
GET /api/agencies/templates
Authorization: Bearer {token}

Response:
{
  "categories": [
    {
      "name": "Emergency Response",
      "agencies": [
        { "name": "Fire Department", "abbreviation": "FD" },
        { "name": "Emergency Medical Services", "abbreviation": "EMS" }
      ]
    }
  ]
}
```

**Add Common Agencies:**
```
POST /api/organizations/current/agencies/bulk
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "agencies": [
    { "name": "Fire Department", "abbreviation": "FD" },
    { "name": "Emergency Medical Services", "abbreviation": "EMS" }
  ]
}

Response (200 OK):
{
  "added": 2,
  "skipped": [
    { "name": "Fire Department", "reason": "Already exists" }
  ]
}
```

### Common Agency Templates (Seed Data)

```json
{
  "categories": [
    {
      "name": "Emergency Response",
      "agencies": [
        { "name": "Fire Department", "abbreviation": "FD" },
        { "name": "Emergency Medical Services", "abbreviation": "EMS" },
        { "name": "Police Department", "abbreviation": "PD" },
        { "name": "Sheriff's Office", "abbreviation": "SO" },
        { "name": "Search and Rescue", "abbreviation": "SAR" }
      ]
    },
    {
      "name": "Public Health & Medical",
      "agencies": [
        { "name": "Hospital", "abbreviation": "HOSP" },
        { "name": "Public Health", "abbreviation": "PH" },
        { "name": "Medical Examiner", "abbreviation": "ME" },
        { "name": "Mental Health Services", "abbreviation": "MHS" }
      ]
    },
    {
      "name": "Government & Support",
      "agencies": [
        { "name": "Emergency Management", "abbreviation": "EM" },
        { "name": "Public Works", "abbreviation": "PW" },
        { "name": "Communications/Dispatch", "abbreviation": "COMM" },
        { "name": "Red Cross", "abbreviation": "RC" },
        { "name": "National Guard", "abbreviation": "NG" }
      ]
    },
    {
      "name": "Business/Corporate",
      "agencies": [
        { "name": "IT/Information Security", "abbreviation": "IT" },
        { "name": "Human Resources", "abbreviation": "HR" },
        { "name": "Legal", "abbreviation": "LEGAL" },
        { "name": "Communications/PR", "abbreviation": "COMM" },
        { "name": "Facilities", "abbreviation": "FAC" },
        { "name": "Executive Team", "abbreviation": "EXEC" }
      ]
    },
    {
      "name": "ICS Sections",
      "agencies": [
        { "name": "Operations Section", "abbreviation": "OPS" },
        { "name": "Planning Section", "abbreviation": "PLAN" },
        { "name": "Logistics Section", "abbreviation": "LOG" },
        { "name": "Finance/Admin Section", "abbreviation": "FIN" }
      ]
    }
  ]
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| OrgAdmin can create agency | Integration | P0 |
| Duplicate name rejected | Integration | P0 |
| OrgAdmin can edit agency | Integration | P0 |
| OrgAdmin can deactivate agency | Integration | P0 |
| Inactive agency hidden from dropdowns | Integration | P0 |
| OrgAdmin can reactivate agency | Integration | P0 |
| Cannot delete agency in use | Integration | P0 |
| Reorder persists correctly | Integration | P1 |
| Add common agencies works | Integration | P1 |
| Common agencies skip duplicates | Integration | P1 |
| OrgManager cannot manage agencies | Integration | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `Agency` entity (if not already stubbed)
- [ ] Create `GET /api/organizations/current/agencies` endpoint
- [ ] Create `POST /api/organizations/current/agencies` endpoint
- [ ] Create `PUT /api/organizations/current/agencies/{id}` endpoint
- [ ] Create `DELETE /api/organizations/current/agencies/{id}` endpoint
- [ ] Create `PUT /api/organizations/current/agencies/reorder` endpoint
- [ ] Create `GET /api/agencies/templates` endpoint
- [ ] Create `POST /api/organizations/current/agencies/bulk` endpoint
- [ ] Implement usage check for delete
- [ ] Add common agency seed data
- [ ] Unit tests for service
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `AgencyListSection` component
- [ ] Create `AgencyDialog` component (add/edit)
- [ ] Create `AddCommonAgenciesDialog` component
- [ ] Implement drag-and-drop reordering (react-beautiful-dnd or similar)
- [ ] Add deactivate/reactivate confirmation
- [ ] Add delete confirmation with usage warning
- [ ] Component tests

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
