# Feature: Exercise Capabilities

**Parent Epic:** Exercise Evaluation & Metrics  
**Feature ID:** F-CAP  
**Priority:** P1 (Standard Implementation)  
**Phase:** Post-MVP Enhancement

---

## Description

Enable organizations to define, manage, and evaluate capabilities during exercises. Capabilities are the organizational competencies or functions that exercises are designed to test and improve. This feature provides a configurable capability library at the organization level, allowing exercises to target specific capabilities and observations to be tagged with relevant capabilities for performance analysis.

This feature supports multiple capability frameworks including FEMA's 32 Core Capabilities, NATO's 7 Baseline Requirements, NIST Cybersecurity Framework, and ISO 22301 process areas, as well as fully custom capability definitions.

---

## Business Value

| Value | Description |
|-------|-------------|
| HSEEP Compliance | Aligns exercises with capability-based evaluation per HSEEP doctrine |
| Framework Flexibility | Supports US federal (FEMA/CISA/TSA), international (NATO), and commercial (ISO/NIST) frameworks |
| Gap Analysis | Identifies which capabilities were evaluated vs. which were targeted but not observed |
| Improvement Planning | Enables capability-focused corrective actions in AAR/IP |
| Market Differentiation | Serves broader market than FEMA-only tools |

---

## User Personas

| Persona | Role in Feature |
|---------|-----------------|
| **Administrator** | Manages organization's capability library; imports predefined libraries |
| **Exercise Director** | Selects target capabilities for each exercise |
| **Evaluator** | Tags observations with relevant capabilities |
| **Director/Emergency Manager** | Views capability performance metrics |

---

## User Stories

| Story ID | Title | Priority | Points | Status |
|----------|-------|----------|--------|--------|
| S01 | Capability Entity and API | P0 | 3 | 🔲 |
| S02 | Capability Library Admin UI | P0 | 5 | 🔲 |
| S03 | Import Predefined Capability Libraries | P0 | 5 | 🔲 |
| S04 | Exercise Target Capabilities | P0 | 5 | 🔲 |
| S05 | Observation Capability Tagging | P0 | 5 | 🔲 |
| S06 | Capability Performance Metrics | P1 | 5 | 🔲 |

**Total Estimated Points:** 28

---

## Feature-Level Acceptance Criteria

- [ ] Organizations can maintain a library of capabilities with names, descriptions, and categories
- [ ] Administrators can import predefined capability libraries (FEMA, NATO, NIST CSF, ISO 22301)
- [ ] Exercise Directors can select target capabilities when creating/editing exercises
- [ ] Evaluators can tag observations with one or more capabilities
- [ ] Metrics dashboard shows capability performance with P/S/M/U ratings
- [ ] Capability data syncs offline and reconciles on reconnect

---

## Dependencies

| Dependency | Type | Status |
|------------|------|--------|
| Organization entity | Internal | ✅ Complete |
| Exercise CRUD | Internal | ✅ Complete |
| Observation capture | Internal | ✅ Complete |
| Metrics feature | Internal | 🔲 In Progress |
| Settings feature | Internal | 🔲 In Progress |

---

## Out of Scope

- Capability trend analysis across multiple exercises (Organization-level metrics - future)
- Automated capability inference from observation text (AI/ML - future)
- Capability-specific recommendations or best practices content
- Cross-organization capability mapping or benchmarking
- Capability hierarchy beyond single-level categories

---

## Technical Architecture

### Data Model

```
Organization (existing)
  └── Capability (new)
        ├── Id (GUID)
        ├── OrganizationId (FK)
        ├── Name (string, required, max 200)
        ├── Description (string, optional, max 1000)
        ├── Category (string, optional, max 100)
        ├── SortOrder (int)
        ├── IsActive (bool, default true)
        ├── SourceLibrary (string, optional - e.g., "FEMA", "NATO")
        └── CreatedAt / UpdatedAt

Exercise (existing)
  └── ExerciseCapability (new junction)
        ├── ExerciseId (FK)
        └── CapabilityId (FK)

Observation (existing)
  └── ObservationCapability (new junction)
        ├── ObservationId (FK)
        └── CapabilityId (FK)
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/organizations/{orgId}/capabilities` | List capabilities |
| POST | `/api/organizations/{orgId}/capabilities` | Create capability |
| PUT | `/api/organizations/{orgId}/capabilities/{id}` | Update capability |
| DELETE | `/api/organizations/{orgId}/capabilities/{id}` | Deactivate capability |
| POST | `/api/organizations/{orgId}/capabilities/import` | Import predefined library |
| GET | `/api/exercises/{exerciseId}/capabilities` | Get exercise target capabilities |
| PUT | `/api/exercises/{exerciseId}/capabilities` | Set exercise target capabilities |
| GET | `/api/exercises/{exerciseId}/metrics/capabilities` | Get capability performance metrics |

---

## Wireframes/Mockups

### Capability Library (Admin Settings)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CAPABILITY LIBRARY                                    [Import ▼] [Add] │
├─────────────────────────────────────────────────────────────────────────┤
│  Filter: [● All] [○ Active] [○ Inactive]    Search: [____________]     │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  RESPONSE (15)                                            [Expand ▼]││
│  ├─────────────────────────────────────────────────────────────────────┤│
│  │  ● Critical Transportation                        Active     [Edit] ││
│  │  ● Mass Care Services                             Active     [Edit] ││
│  │  ● Operational Communications                     Active     [Edit] ││
│  │  ...                                                                ││
│  └─────────────────────────────────────────────────────────────────────┘│
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────────┐│
│  │  CUSTOM (3)                                               [Expand ▼]││
│  └─────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────┘
```

### Import Library Dialog

```
┌─────────────────────────────────────────────────────────────┐
│  IMPORT CAPABILITY LIBRARY                              [X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Select a predefined library to import:                     │
│                                                             │
│  ○ FEMA Core Capabilities (32 capabilities)                 │
│      US National Preparedness Goal - 5 Mission Areas        │
│                                                             │
│  ○ NATO Baseline Requirements (7 capabilities)              │
│      Allied civil preparedness requirements                 │
│                                                             │
│  ○ NIST Cybersecurity Framework (6 capabilities)            │
│      CSF 2.0 Functions for cyber exercises                  │
│                                                             │
│  ○ ISO 22301 Process Areas (10 capabilities)                │
│      Business continuity management                         │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  ⚠ Existing capabilities will not be duplicated            │
│                                                             │
│                              [Cancel]  [Import Selected]    │
└─────────────────────────────────────────────────────────────┘
```

---

## Risks & Assumptions

| Risk/Assumption | Mitigation/Validation |
|-----------------|----------------------|
| Users may not understand capability frameworks | Provide clear descriptions and onboarding guidance |
| Large capability lists may slow selection UIs | Implement virtualized lists, category grouping |
| Offline capability sync could conflict | Use standard sync service with last-write-wins |
| Organizations may want to customize imported libraries | Allow editing of imported capabilities |

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Organizations with capabilities defined | >80% of active orgs |
| Exercises with target capabilities | >70% of exercises |
| Observations with capability tags | >50% of observations |
| Time to import predefined library | <5 seconds |

---

## References

- [HSEEP 2020 Doctrine](https://www.fema.gov/sites/default/files/2020-04/Homeland-Security-Exercise-and-Evaluation-Program-Doctrine-2020-Revision-2-2-25.pdf)
- [FEMA Core Capabilities](https://www.fema.gov/emergency-managers/national-preparedness/mission-core-capabilities)
- [Predefined Capability Libraries](./predefined-capability-libraries.md)

---

## Changelog

| Date | Author | Change |
|------|--------|--------|
| 2026-01-28 | Claude | Initial feature documentation |
