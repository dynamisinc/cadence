# Feature: Inject CRUD Operations

**Parent Epic:** MSEL Authoring (E4)

## Description

Injects are the core content of a MSEL - they are the events, messages, and scenarios delivered during exercise conduct. This feature covers the basic create, read, update, and delete operations for injects, including Cadence's dual-time tracking capability.

Each inject represents something that happens during the exercise: a phone call, an email, a simulated news report, a resource request, etc. Controllers deliver injects to exercise players at scheduled times.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-inject.md) | Create Inject | P0 | 📋 Ready |
| [S02](./S02-edit-inject.md) | Edit Inject | P0 | 📋 Ready |
| [S03](./S03-view-inject-detail.md) | View Inject Detail | P0 | 📋 Ready |
| [S04](./S04-delete-inject.md) | Delete Inject | P1 | 📋 Ready |
| [S05](./S05-dual-time-tracking.md) | Dual Time Tracking | P0 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full CRUD access to all injects |
| Exercise Director | Full CRUD access to injects in their exercises |
| Controller | Create, edit, view injects; limited delete |
| Evaluator | View injects only |
| Observer | View injects only |

## Key Concepts

### Dual Time Tracking

Cadence supports two time concepts for each inject:

| Time Type | Purpose | Example |
|-----------|---------|---------|
| **Scheduled Time** | When to deliver the inject (wall clock) | "10:30 AM EST" |
| **Scenario Time** | When it happens in the story | "Day 2, 14:00" |

This allows exercises to compress multi-day scenarios into shorter conduct periods. A "Day 3" scenario event might be delivered at 11:00 AM on the actual exercise day.

### Inject Fields

| Field | Required | Description |
|-------|----------|-------------|
| Inject Number | Yes (auto) | Unique identifier within MSEL |
| Title | Yes | Brief description |
| Scheduled Time | Yes | Wall-clock delivery time |
| Scenario Day | No | Story day (1, 2, 3...) |
| Scenario Time | No | Story time (HH:MM) |
| Description | No | Full inject content |
| From | No | Simulated sender |
| To | No | Target recipient(s) |
| Method | No | Delivery channel |
| Expected Action | No | What players should do |
| Notes | No | Controller notes |

## Dependencies

- exercise-crud/S01: Create Exercise (injects belong to exercises)
- exercise-objectives/S03: Link Objective to Inject (objectives can be linked)
- exercise-phases/S02: Assign Inject to Phase (phases can be assigned)

## Acceptance Criteria (Feature-Level)

- [ ] Users can create injects with required and optional fields
- [ ] Users can view inject details including all time information
- [ ] Users can edit inject content before and during conduct
- [ ] Users can delete injects (with confirmation)
- [ ] Dual time (Scheduled + Scenario) is supported on all injects

## Wireframes/Mockups

### MSEL List View

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  MSEL - Hurricane Response 2025                    [+ New Inject] [Import]   │
├──────────────────────────────────────────────────────────────────────────────┤
│  [Filter ▼]  [Group ▼]  [Search injects...]                                 │
├──────────────────────────────────────────────────────────────────────────────┤
│  #  │ Scheduled  │ Scenario   │ Title                      │ Status │       │
│ ────┼────────────┼────────────┼────────────────────────────┼────────┼────── │
│  1  │ 09:00 AM   │ D1 08:00   │ Hurricane warning issued   │ Pending│ •••  │
│  2  │ 09:15 AM   │ D1 10:00   │ EOC activation ordered     │ Pending│ •••  │
│  3  │ 09:30 AM   │ D1 14:00   │ Evacuation order issued    │ Pending│ •••  │
│  4  │ 09:45 AM   │ D2 08:00   │ Landfall + 6 hours        │ Pending│ •••  │
│  5  │ 10:00 AM   │ D2 14:00   │ Shelter capacity exceeded  │ Pending│ •••  │
└──────────────────────────────────────────────────────────────────────────────┘

D1, D2 = Scenario Day 1, Day 2
```

### Inject Detail View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  INJ-003: Evacuation order issued                    [Edit] [Delete]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  TIME                                                                   │
│  Scheduled: 09:30 AM EST (Jan 15, 2025)                                │
│  Scenario:  Day 1, 14:00                                               │
│                                                                         │
│  TARGETING                                                              │
│  From:    County Emergency Manager                                     │
│  To:      All Municipalities                                           │
│  Method:  Phone Call                                                    │
│                                                                         │
│  CONTENT                                                                │
│  The County Emergency Manager contacts all municipal coordinators to   │
│  issue a mandatory evacuation order for Zones A and B, effective       │
│  immediately. Transportation resources should be activated.             │
│                                                                         │
│  EXPECTED ACTION                                                        │
│  • Acknowledge evacuation order                                        │
│  • Activate transportation plan                                        │
│  • Begin shelter notifications                                         │
│                                                                         │
│  OBJECTIVES                                                             │
│  1. Demonstrate EOC activation    2. Test multi-agency communication   │
│                                                                         │
│  Phase: Initial Response                                               │
│  Status: Pending                                                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Notes

- Inject numbering is automatic and sequential within the MSEL
- Consider soft delete to allow recovery
- During conduct, some fields may become read-only
