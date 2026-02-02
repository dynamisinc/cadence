# Feature: Exercise Capabilities

**Phase:** Standard
**Status:** Not Started

## Overview

Enable organizations to define, manage, and evaluate capabilities during exercises. Capabilities are the organizational competencies or functions that exercises are designed to test and improve. This feature provides a configurable capability library at the organization level, allowing exercises to target specific capabilities and observations to be tagged with relevant capabilities for performance analysis.

This feature supports multiple capability frameworks including FEMA's 32 Core Capabilities, NATO's 7 Baseline Requirements, NIST Cybersecurity Framework, and ISO 22301 process areas, as well as fully custom capability definitions.

## Problem Statement

Exercise evaluation needs to align with capability-based objectives per HSEEP methodology, but current tools either force all users to adopt FEMA's 32 Core Capabilities (limiting international and commercial use) or provide no capability framework at all. Organizations need flexibility to use industry-standard frameworks that match their sector and regulatory requirements while still getting meaningful performance metrics by capability area.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-capability-entity-api.md) | Capability Entity and API | P0 | Not Started |
| [S02](./S02-capability-library-admin-ui.md) | Capability Library Admin UI | P0 | Not Started |
| [S03](./S03-import-predefined-libraries.md) | Import Predefined Capability Libraries | P0 | Not Started |
| [S04](./S04-exercise-target-capabilities.md) | Exercise Target Capabilities | P0 | Not Started |
| [S05](./S05-observation-capability-tagging.md) | Observation Capability Tagging | P0 | Not Started |
| [S06](./S06-capability-performance-metrics.md) | Capability Performance Metrics | P1 | Not Started |

**Total Estimated Points:** 28

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Administrator** | Manages organization's capability library; imports predefined libraries (FEMA, NATO, NIST, ISO) |
| **Exercise Director** | Selects target capabilities for each exercise during Initial Planning Meeting |
| **Evaluator** | Tags observations with relevant capabilities during exercise conduct |
| **Director/Emergency Manager** | Views capability performance metrics to identify improvement priorities |

## Key Concepts

**Capability** - An organizational competency or function that can be evaluated during an exercise (e.g., "Mass Care Services", "Operational Communications")

**Capability Library** - The collection of capabilities defined for an organization, either imported from predefined frameworks or custom-defined

**Target Capability** - A capability explicitly selected for evaluation in a specific exercise (aligned with HSEEP exercise objectives)

**Capability Tag** - A capability linked to an observation, indicating which organizational function the observation relates to

**Capability Gap** - A target capability that received no observations during the exercise, indicating a scope mismatch

**Predefined Library** - A curated set of capabilities from recognized frameworks:
- **FEMA Core Capabilities** - 32 capabilities across 5 mission areas (US Emergency Management)
- **NATO Baseline Requirements** - 7 capabilities for Allied civil preparedness
- **NIST Cybersecurity Framework** - 6 functions for cyber exercises
- **ISO 22301 Process Areas** - 10 process areas for business continuity

**Capability Performance** - Aggregate P/S/M/U rating across all observations for a capability

**Coverage** - Percentage of target capabilities that have at least one observation

## Dependencies

- Organization entity (Complete)
- Exercise CRUD (Complete)
- Observation capture (Complete)
- Metrics feature (In Progress)
- Settings feature (In Progress)

## Acceptance Criteria (Feature-Level)

- [ ] Organizations can maintain a library of capabilities with names, descriptions, and categories
- [ ] Administrators can import predefined capability libraries (FEMA, NATO, NIST CSF, ISO 22301) with one click
- [ ] Duplicate capabilities are automatically detected and skipped during import
- [ ] Exercise Directors can select target capabilities when creating/editing exercises
- [ ] Evaluators can tag observations with one or more capabilities during exercise conduct
- [ ] Exercise target capabilities are prioritized at the top of the observation capability selector
- [ ] Metrics dashboard shows capability performance with P/S/M/U rating distribution
- [ ] Metrics show coverage percentage (X of Y target capabilities evaluated)
- [ ] Metrics identify capability gaps (target capabilities with no observations)
- [ ] All capability data syncs offline and reconciles on reconnect
- [ ] Capabilities can be deactivated (soft delete) without losing historical data

## Notes

### Implementation Sequence

The stories should be implemented in order:
1. **S01** - Backend entity and API foundation
2. **S02** - Admin UI for manual capability management
3. **S03** - Import functionality (requires S01 API + S02 UI)
4. **S04** - Exercise targeting (depends on capability library existing)
5. **S05** - Observation tagging (depends on exercise targets)
6. **S06** - Metrics (depends on tagged observations)

### Predefined Libraries

Full capability definitions are documented in [predefined-capability-libraries.md](./predefined-capability-libraries.md).

### HSEEP Alignment

Per HSEEP 2020 Doctrine, exercise objectives should be tied to organizational capabilities that need assessment. This feature enables that alignment while supporting international and commercial frameworks beyond FEMA.

### Out of Scope Items

- Capability trend analysis across multiple exercises (Organization-level metrics - future)
- Automated capability inference from observation text (AI/ML - future)
- Capability-specific recommendations or best practices content
- Cross-organization capability mapping or benchmarking
- Capability hierarchy beyond single-level categories
- Export to AAR/IP format (future enhancement)
- Capability objectives/targets with quantifiable goals
- Bulk import from CSV (future enhancement)

### Business Value

| Value | Description |
|-------|-------------|
| HSEEP Compliance | Aligns exercises with capability-based evaluation per HSEEP doctrine |
| Framework Flexibility | Supports US federal (FEMA/CISA/TSA), international (NATO), and commercial (ISO/NIST) frameworks |
| Gap Analysis | Identifies which capabilities were evaluated vs. which were targeted but not observed |
| Improvement Planning | Enables capability-focused corrective actions in AAR/IP |
| Market Differentiation | Serves broader market than FEMA-only tools |
| Time Savings | One-click import of 32 FEMA capabilities vs. manual entry |

### Risks & Assumptions

| Risk/Assumption | Mitigation/Validation |
|-----------------|----------------------|
| Users may not understand capability frameworks | Provide clear descriptions and onboarding guidance for each library |
| Large capability lists may slow selection UIs | Implement virtualized lists, category grouping, and search filtering |
| Offline capability sync could conflict | Use standard sync service with last-write-wins reconciliation |
| Organizations may want to customize imported libraries | Allow editing of all imported capabilities |
| Users may import the same library multiple times | Duplicate detection prevents data pollution |

### Success Metrics

| Metric | Target |
|--------|--------|
| Organizations with capabilities defined | >80% of active organizations |
| Exercises with target capabilities | >70% of exercises |
| Observations with capability tags | >50% of observations |
| Time to import predefined library | <5 seconds |
| Capability library setup time (FEMA import) | <2 minutes (vs. 30+ minutes manual entry) |

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
| GET | `/api/organizations/{orgId}/capabilities` | List capabilities (with includeInactive filter) |
| POST | `/api/organizations/{orgId}/capabilities` | Create capability |
| PUT | `/api/organizations/{orgId}/capabilities/{id}` | Update capability |
| DELETE | `/api/organizations/{orgId}/capabilities/{id}` | Deactivate capability (soft delete) |
| POST | `/api/organizations/{orgId}/capabilities/import` | Import predefined library |
| GET | `/api/exercises/{exerciseId}/capabilities` | Get exercise target capabilities |
| PUT | `/api/exercises/{exerciseId}/capabilities` | Set exercise target capabilities |
| GET | `/api/exercises/{exerciseId}/metrics/capabilities` | Get capability performance metrics |

### References

- [HSEEP 2020 Doctrine](https://www.fema.gov/sites/default/files/2020-04/Homeland-Security-Exercise-and-Evaluation-Program-Doctrine-2020-Revision-2-2-25.pdf)
- [FEMA Core Capabilities](https://www.fema.gov/emergency-managers/national-preparedness/mission-core-capabilities)
- [Predefined Capability Libraries](./predefined-capability-libraries.md)
- [NATO Baseline Requirements for National Resilience](https://www.nato.int/cps/en/natohq/topics_132722.htm)
- [NIST Cybersecurity Framework 2.0](https://www.nist.gov/cyberframework)
- [ISO 22301:2019 Business Continuity Management](https://www.iso.org/standard/75106.html)

### Changelog

| Date | Author | Change |
|------|--------|--------|
| 2026-02-02 | Claude | Standardized FEATURE.md format |
| 2026-01-28 | Claude | Initial feature documentation |
