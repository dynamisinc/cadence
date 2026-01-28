# Story: Organization Core Capability List

**Feature**: Settings  
**Story ID**: S14  
**Priority**: P2 (Future)  
**Phase**: Future Enhancement

---

## User Story

**As an** Administrator,  
**I want** to customize the Core Capability list available for my organization,  
**So that** evaluators can tag observations with capabilities relevant to our jurisdiction and mission.

---

## Context

FEMA defines 32 Core Capabilities, but not all are relevant to every organization:

- Local fire department may focus on 8-10 capabilities
- State emergency management may use all 32
- Healthcare coalitions may add healthcare-specific capabilities

Organizations need to configure which capabilities are available and potentially add custom ones.

---

## Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I access organization settings, **then** I see Core Capability configuration
- [ ] **Given** the capability list, **when** displayed, **then** I see all FEMA Core Capabilities with enable/disable toggles
- [ ] **Given** I disable a capability, **when** saved, **then** that capability is not available for observation tagging
- [ ] **Given** I want custom capabilities, **when** I click "Add Custom", **then** I can create organization-specific capabilities
- [ ] **Given** custom capabilities, **when** created, **then** they appear alongside FEMA capabilities in observation forms
- [ ] **Given** I delete a custom capability, **when** observations exist with that tag, **then** I am warned and must confirm
- [ ] **Given** capability changes, **when** saved, **then** changes apply to new observations (existing tags preserved)
- [ ] **Given** defaults, **when** a new organization is created, **then** all FEMA capabilities are enabled

---

## Out of Scope

- Capability definitions/descriptions
- Capability hierarchy (parent/child)
- Capability mapping to objectives (automatic)
- Capability import from external source

---

## Dependencies

- Core Capability reference data
- Observation tagging system (Phase E)
- S06: Core Capability Performance metrics

---

## Open Questions

- [ ] Should disabled capabilities still appear in historical reports?
- [ ] Can we group capabilities by Mission Area?
- [ ] Should custom capabilities have a different visual indicator?
- [ ] Do we need capability synonyms or aliases?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Core Capability | FEMA-defined or custom capability area for evaluation |
| Mission Area | FEMA grouping: Prevention, Protection, Mitigation, Response, Recovery |

---

## UI/UX Notes

### Core Capability Configuration

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Organization Settings                                   [Admin Only]   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Core Capabilities                                                      │
│  ─────────────────────────────────────────────                          │
│                                                                         │
│  Select which capabilities are available for observation tagging.       │
│                                                                         │
│  Filter: [ All ▼ ]  [🔍 Search capabilities...]                        │
│                                                                         │
│  RESPONSE (Mission Area)                                                │
│  ───────────────────────                                                │
│  [✓] Critical Transportation                                           │
│  [✓] Environmental Response/Health and Safety                          │
│  [ ] Fatality Management Services                                      │
│  [✓] Fire Management and Suppression                                   │
│  [✓] Infrastructure Systems                                            │
│  [✓] Logistics and Supply Chain Management                             │
│  [✓] Mass Care Services                                                │
│  [✓] Mass Search and Rescue Operations                                 │
│  [✓] On-scene Security, Protection, and Law Enforcement                │
│  [✓] Operational Communications                                        │
│  [✓] Operational Coordination                                          │
│  [✓] Planning                                                          │
│  [✓] Public Health, Healthcare, and Emergency Medical Services         │
│  [✓] Public Information and Warning                                    │
│  [✓] Situational Assessment                                            │
│                                                                         │
│  CUSTOM CAPABILITIES                                                    │
│  ──────────────────                                                     │
│  [✓] Hospital Surge Capacity                              [Edit] [🗑]  │
│  [✓] Volunteer Coordination                               [Edit] [🗑]  │
│                                                                         │
│                                        [+ Add Custom Capability]        │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  15 of 32 FEMA capabilities enabled                                    │
│  2 custom capabilities                                                  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store enabled capabilities as join table: `OrganizationCapabilities`
- Custom capabilities in separate table with `OrganizationId` foreign key
- Seed FEMA capabilities as reference data
- Soft-delete custom capabilities (preserve for historical observations)
- API: `GET/PUT /api/organizations/{id}/capabilities`

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
