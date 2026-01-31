# Story OM-11: Capability Library Selection

**Priority:** P2 (Future Enhancement)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As an** Organization Administrator,  
**I want** to select which capability frameworks my organization uses,  
**So that** exercises can be evaluated against the standards relevant to our domain.

---

## Context

The 2020 HSEEP doctrine explicitly allows organizations to use alternative capability frameworks beyond FEMA's Core Capabilities. Cadence supports multiple frameworks to serve diverse markets:

| Framework | Source | Domain | Capabilities |
|-----------|--------|--------|--------------|
| FEMA Core Capabilities | FEMA | US Emergency Management | 32 capabilities across 5 mission areas |
| NATO Baseline Requirements | NATO | International Defense | 7 baseline capability requirements |
| NIST CSF Functions | NIST | Cybersecurity | 6 core functions |
| ISO Process Areas | ISO 22301 | Business Continuity | 10 process areas |

Organizations can enable one or more frameworks and customize them by adding organization-specific capabilities.

**Key design principle:** Capability libraries are **copied** to the organization, not referenced. This allows customization without affecting other organizations.

---

## Acceptance Criteria

### Initial Framework Selection

- [ ] **Given** I am creating a new organization (SysAdmin), **when** I reach the capabilities step, **then** I can select which frameworks to enable
- [ ] **Given** I am selecting frameworks, **when** I view the options, **then** I see: FEMA Core Capabilities, NATO Baseline, NIST CSF, ISO 22301
- [ ] **Given** I select a framework, **when** I see its details, **then** I see a description and list of included capabilities
- [ ] **Given** I don't select any framework, **when** the organization is created, **then** it starts with an empty capability library (can add later)

### Framework Seeding

- [ ] **Given** I select FEMA Core Capabilities, **when** the organization is created, **then** all 32 capabilities are copied to the organization's library
- [ ] **Given** capabilities are seeded, **when** viewing them, **then** each shows its original source framework
- [ ] **Given** capabilities are seeded, **when** I edit one, **then** my changes don't affect other organizations
- [ ] **Given** I selected multiple frameworks, **when** seeding completes, **then** all selected frameworks' capabilities are added

### Post-Creation Framework Management

- [ ] **Given** my organization exists, **when** I navigate to Capability Settings, **then** I can add additional frameworks
- [ ] **Given** I add a framework after creation, **when** I confirm, **then** its capabilities are added to my existing library
- [ ] **Given** I have framework capabilities, **when** I want to remove them, **then** I can bulk-remove by framework (with warning if in use)

### Custom Capabilities

- [ ] **Given** I am an OrgAdmin, **when** I view my capability library, **then** I see an "Add Custom Capability" option
- [ ] **Given** I add a custom capability, **when** I enter details, **then** I provide: Name, Description, Category (optional)
- [ ] **Given** I create a custom capability, **when** viewing it, **then** it shows "Custom" as the source framework
- [ ] **Given** I have custom capabilities, **when** viewing the library, **then** they appear alongside framework capabilities

### Capability Library Management

- [ ] **Given** I am viewing my capability library, **when** I see the list, **then** capabilities are grouped by framework/source
- [ ] **Given** I am viewing a capability, **when** I click Edit, **then** I can modify its name, description, and active status
- [ ] **Given** I deactivate a capability, **when** viewing exercises, **then** it no longer appears in capability selection dropdowns
- [ ] **Given** a capability is used in exercises, **when** I deactivate it, **then** existing assignments remain but display "(Inactive)"

### Capability Search and Filter

- [ ] **Given** I have many capabilities, **when** I search, **then** I can find capabilities by name or description
- [ ] **Given** I am viewing capabilities, **when** I filter by framework, **then** I see only capabilities from that framework
- [ ] **Given** I am viewing capabilities, **when** I filter by status, **then** I can show Active, Inactive, or All

---

## Out of Scope

- Capability hierarchies (parent/child capabilities)
- Capability mapping across frameworks
- Automatic framework updates (capabilities are point-in-time copies)
- Capability performance metrics/thresholds
- Capability certification/compliance tracking
- Multi-language capability names

---

## Dependencies

- OM-02: Create Organization (optional framework selection during creation)
- Exercise Capabilities feature (uses the library)
- Observation feature (links to capabilities)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Capability | A function or activity that must be performed to manage an emergency |
| Capability Framework | A standardized set of capabilities from an authoritative source |
| Capability Library | An organization's collection of capabilities (seeded + custom) |
| Mission Area | FEMA grouping: Prevention, Protection, Mitigation, Response, Recovery |

---

## UI/UX Notes

### Framework Selection (During Org Creation)
```
┌─────────────────────────────────────────────────────────────────┐
│ Create Organization - Step 3 of 3                               │
│ Capability Frameworks                                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Select the capability frameworks your organization will use     │
│ for exercise evaluation. You can add more frameworks later.     │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ ☑ FEMA Core Capabilities                                  │   │
│ │   32 capabilities across 5 mission areas                  │   │
│ │   Best for: US emergency management agencies              │   │
│ │                                            [View List →]  │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ ☐ NATO Baseline Requirements                              │   │
│ │   7 baseline capability requirements                      │   │
│ │   Best for: International defense organizations           │   │
│ │                                            [View List →]  │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ ☐ NIST Cybersecurity Framework (CSF)                      │   │
│ │   6 core functions                                        │   │
│ │   Best for: IT security and cyber exercises               │   │
│ │                                            [View List →]  │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ ☐ ISO 22301 Process Areas                                 │   │
│ │   10 process areas                                        │   │
│ │   Best for: Business continuity exercises                 │   │
│ │                                            [View List →]  │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ ☐ Skip for now - I'll configure capabilities later             │
│                                                                  │
│                              [← Back]  [Create Organization]    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Capability Library Settings
```
┌─────────────────────────────────────────────────────────────────┐
│ Capability Library                            [+ Add Capability]│
├─────────────────────────────────────────────────────────────────┤
│ 🔍 Search capabilities...   Framework: [All ▼]  Status: [All ▼]│
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ FEMA Core Capabilities (32)                      [Remove All ▼] │
│ ├─────────────────────────────────────────────────────────────┤ │
│ │ ▼ Response Mission Area                                     │ │
│ │   ├─ Planning                              🟢 Active [Edit] │ │
│ │   ├─ Public Information and Warning        🟢 Active [Edit] │ │
│ │   ├─ Operational Coordination              🟢 Active [Edit] │ │
│ │   ├─ Critical Transportation               🟢 Active [Edit] │ │
│ │   └─ ... (more capabilities)                                │ │
│ │                                                              │ │
│ │ ▶ Prevention Mission Area (7 capabilities)                  │ │
│ │ ▶ Protection Mission Area (6 capabilities)                  │ │
│ │ ▶ Mitigation Mission Area (3 capabilities)                  │ │
│ │ ▶ Recovery Mission Area (6 capabilities)                    │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│ Custom Capabilities (2)                                         │
│ ├─────────────────────────────────────────────────────────────┤ │
│ │   ├─ Regional Coordination                 🟢 Active [Edit] │ │
│ │   └─ Multi-Agency Communication            🟢 Active [Edit] │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│ [+ Add Framework]                                               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Add/Edit Capability Dialog
```
┌─────────────────────────────────────────────────┐
│ Add Custom Capability                     [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Capability Name *                               │
│ ┌─────────────────────────────────────────┐    │
│ │ Regional Coordination                    │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Description                                     │
│ ┌─────────────────────────────────────────┐    │
│ │ Ability to coordinate response across   │    │
│ │ multiple jurisdictions in the region.   │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Category (optional)                             │
│ ┌─────────────────────────────────────────┐    │
│ │ Response                             ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Status                                          │
│ ● Active    ○ Inactive                         │
│                                                 │
│                    [Cancel]  [Save Capability]  │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Add Framework Dialog
```
┌─────────────────────────────────────────────────┐
│ Add Capability Framework                  [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Select a framework to add to your library:     │
│                                                 │
│ ┌─────────────────────────────────────────┐    │
│ │ ☐ NATO Baseline Requirements            │    │
│ │   7 capabilities                        │    │
│ │                                         │    │
│ │ ☐ NIST Cybersecurity Framework          │    │
│ │   6 capabilities                        │    │
│ │                                         │    │
│ │ ☐ ISO 22301 Process Areas               │    │
│ │   10 capabilities                       │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ ℹ️ FEMA Core Capabilities already added        │
│                                                 │
│ Selected capabilities will be copied to your   │
│ library. You can customize them after adding.  │
│                                                 │
│                    [Cancel]  [Add Frameworks]   │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## Technical Notes

### Data Model

**Capability Entity:**
```csharp
public class Capability
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
    
    public string Name { get; set; }  // max 200
    public string? Description { get; set; }  // max 2000
    public string? Category { get; set; }  // e.g., "Response", "Prevention"
    
    public CapabilityFramework SourceFramework { get; set; }  // FEMA, NATO, NIST, ISO, Custom
    public string? SourceCapabilityId { get; set; }  // Original ID from framework
    
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum CapabilityFramework
{
    Custom,
    FemaCore,
    NatoBaseline,
    NistCsf,
    Iso22301
}
```

**Capability Template (Seed Data):**
```csharp
public class CapabilityTemplate
{
    public string Id { get; set; }  // e.g., "FEMA-CORE-01"
    public CapabilityFramework Framework { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public int SortOrder { get; set; }
}
```

### API Endpoints

**Get Available Frameworks:**
```
GET /api/capabilities/frameworks
Authorization: Bearer {token}

Response:
{
  "frameworks": [
    {
      "id": "FemaCore",
      "name": "FEMA Core Capabilities",
      "description": "32 capabilities across 5 mission areas",
      "capabilityCount": 32,
      "categories": ["Prevention", "Protection", "Mitigation", "Response", "Recovery"]
    },
    ...
  ]
}
```

**Get Framework Capabilities (Preview):**
```
GET /api/capabilities/frameworks/{frameworkId}/capabilities
Authorization: Bearer {token}

Response:
{
  "framework": "FemaCore",
  "capabilities": [
    {
      "id": "FEMA-CORE-01",
      "name": "Planning",
      "description": "Conduct a systematic process...",
      "category": "Response"
    },
    ...
  ]
}
```

**Get Organization's Capability Library:**
```
GET /api/organizations/current/capabilities
Authorization: Bearer {token}

Query Parameters:
  - framework: FemaCore|NatoBaseline|NistCsf|Iso22301|Custom
  - status: Active|Inactive|All
  - search: string

Response:
{
  "items": [
    {
      "id": "guid",
      "name": "Planning",
      "description": "...",
      "category": "Response",
      "sourceFramework": "FemaCore",
      "isActive": true
    }
  ],
  "byFramework": {
    "FemaCore": 32,
    "Custom": 2
  }
}
```

**Seed Framework Capabilities:**
```
POST /api/organizations/current/capabilities/seed
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "frameworks": ["FemaCore", "NistCsf"]
}

Response (200 OK):
{
  "seeded": {
    "FemaCore": 32,
    "NistCsf": 6
  },
  "totalAdded": 38
}
```

**Create Custom Capability:**
```
POST /api/organizations/current/capabilities
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "name": "Regional Coordination",
  "description": "Ability to coordinate...",
  "category": "Response"
}

Response (201 Created):
{
  "id": "guid",
  "name": "Regional Coordination",
  "sourceFramework": "Custom",
  "isActive": true
}
```

**Update Capability:**
```
PUT /api/organizations/current/capabilities/{id}
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "name": "Regional Coordination (Updated)",
  "description": "Updated description",
  "category": "Response",
  "isActive": true
}
```

**Remove Framework Capabilities:**
```
DELETE /api/organizations/current/capabilities/framework/{frameworkId}
Authorization: Bearer {token} (OrgAdmin only)

Response (200 OK):
{
  "removed": 32,
  "inUseWarning": {
    "exerciseCount": 3,
    "observationCount": 15
  }
}
```

### FEMA Core Capabilities Seed Data

```json
{
  "framework": "FemaCore",
  "version": "2020",
  "capabilities": [
    {
      "id": "FEMA-CORE-01",
      "name": "Planning",
      "category": "Response",
      "description": "Conduct a systematic process engaging the whole community as appropriate in the development of executable strategic, operational, and/or tactical-level approaches to meet defined objectives."
    },
    {
      "id": "FEMA-CORE-02",
      "name": "Public Information and Warning",
      "category": "Response",
      "description": "Deliver coordinated, prompt, reliable, and actionable information to the whole community through the use of clear, consistent, accessible, and culturally and linguistically appropriate methods to effectively relay information regarding any threat or hazard, as well as the actions being taken and the assistance being made available."
    },
    // ... remaining 30 capabilities
  ]
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Select framework during org creation | Integration | P0 |
| Framework capabilities are seeded | Integration | P0 |
| Create custom capability | Integration | P0 |
| Edit capability | Integration | P0 |
| Deactivate capability | Integration | P0 |
| Add framework post-creation | Integration | P1 |
| Remove framework capabilities | Integration | P1 |
| Search capabilities | Integration | P1 |
| Filter by framework | Integration | P1 |
| Inactive capability not in dropdowns | Integration | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `Capability` entity
- [ ] Create `CapabilityFramework` enum
- [ ] Create seed data for all frameworks (JSON files)
- [ ] Create `GET /api/capabilities/frameworks` endpoint
- [ ] Create `GET /api/capabilities/frameworks/{id}/capabilities` endpoint
- [ ] Create `GET /api/organizations/current/capabilities` endpoint
- [ ] Create `POST /api/organizations/current/capabilities/seed` endpoint
- [ ] Create `POST /api/organizations/current/capabilities` endpoint
- [ ] Create `PUT /api/organizations/current/capabilities/{id}` endpoint
- [ ] Create `DELETE /api/organizations/current/capabilities/framework/{id}` endpoint
- [ ] Add capability selection to org creation flow
- [ ] Unit tests for seeding logic
- [ ] Integration tests for endpoints

### Frontend
- [ ] Add framework selection step to org creation wizard
- [ ] Create `CapabilityLibraryPage` component
- [ ] Create `FrameworkCapabilityList` component (collapsible groups)
- [ ] Create `AddCapabilityDialog` component
- [ ] Create `AddFrameworkDialog` component
- [ ] Create `FrameworkPreviewDialog` component
- [ ] Add capability search and filters
- [ ] Component tests

### Seed Data
- [ ] FEMA Core Capabilities (32)
- [ ] NATO Baseline Requirements (7)
- [ ] NIST CSF Functions (6)
- [ ] ISO 22301 Process Areas (10)

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
