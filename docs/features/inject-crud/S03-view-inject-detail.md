# Story: S03 - View Inject Detail

## User Story

**As a** Controller, Evaluator, or Observer,
**I want** to view the full details of an inject,
**So that** I can understand the inject content and context for delivery or evaluation.

## Context

The MSEL list view shows summary information, but users need to see complete inject details including full description, expected actions, linked objectives, and controller notes. This detail view is the primary interface for Controllers preparing to deliver an inject.

## Acceptance Criteria

### Navigation
- [ ] **Given** I am viewing the MSEL list, **when** I click on an inject row, **then** I navigate to the inject detail view
- [ ] **Given** I am on inject detail, **when** I view the header, **then** I see: Inject Number, Title, Status
- [ ] **Given** I am on inject detail, **when** I click "Back to MSEL", **then** I return to the MSEL list at my previous scroll position

### Time Information
- [ ] **Given** I am viewing inject detail, **when** I look at the Time section, **then** I see Scheduled Time in exercise time zone
- [ ] **Given** the inject has Scenario Time, **when** I view the Time section, **then** I see "Scenario: Day X, HH:MM"
- [ ] **Given** the inject has no Scenario Time, **when** I view the Time section, **then** the Scenario field shows "Not set"

### Targeting Information
- [ ] **Given** I am viewing inject detail, **when** I look at Targeting, **then** I see: From, To, Method
- [ ] **Given** any targeting field is empty, **when** I view it, **then** it shows "—" (dash)

### Content Information
- [ ] **Given** I am viewing inject detail, **when** I look at Content, **then** I see: Description, Expected Action
- [ ] **Given** Description is long, **when** I view it, **then** it displays fully (not truncated)

### Organization Information
- [ ] **Given** I am viewing inject detail, **when** I look at Organization, **then** I see: Phase, Objectives
- [ ] **Given** the inject has linked objectives, **when** I click an objective, **then** I can view objective details
- [ ] **Given** the inject has controller notes, **when** I view Notes, **then** I see the full notes text

### Role-Based Access
- [ ] **Given** I am an Administrator or Exercise Director, **when** I view inject detail, **then** I see Edit and Delete buttons
- [ ] **Given** I am a Controller, **when** I view inject detail, **then** I see Edit button but Delete may be restricted
- [ ] **Given** I am an Evaluator or Observer, **when** I view inject detail, **then** I see no Edit or Delete buttons

### Conduct Information
- [ ] **Given** the inject has been fired, **when** I view detail, **then** I see: Fired At timestamp, Fired By user
- [ ] **Given** the inject was skipped, **when** I view detail, **then** I see: Skipped At timestamp, Skipped By user, Skip Reason

## Out of Scope

- Inject preview/print mode
- Inject comparison view
- Navigation between injects (next/previous)
- Related injects display

## Dependencies

- inject-crud/S01: Create Inject (inject must exist)
- exercise-objectives/S03: Link Objective to Inject (objectives displayed)
- exercise-phases/S02: Assign Inject to Phase (phase displayed)
- exercise-conduct: Exercise Conduct (conduct information)

## Open Questions

- [ ] Should there be prev/next inject navigation?
- [ ] Should we show related injects (same phase or objectives)?
- [ ] Should Notes be visible to all roles or just Controllers?

## Domain Terms

| Term | Definition |
|------|------------|
| Inject Detail | Full view of all inject properties and relationships |
| Controller Notes | Internal notes for the person delivering the inject |
| Fired At | Timestamp when the inject was actually delivered |

## UI/UX Notes

### Full Inject Detail View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ← Back to MSEL                                                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  INJ-003                                                        │   │
│  │  County issues mandatory evacuation order                       │   │
│  │                                                                 │   │
│  │  Status: 🟡 Pending                    [Edit] [Delete]          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  TIME                                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Scheduled:  09:30 AM EST (January 15, 2025)                   │   │
│  │  Scenario:   Day 1, 14:00                                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  TARGETING                                                              │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  From:    County Emergency Manager                              │   │
│  │  To:      All Municipalities                                    │   │
│  │  Method:  📞 Phone Call                                         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  CONTENT                                                                │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Description                                                    │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  The County Emergency Manager contacts all municipal            │   │
│  │  coordinators to issue a mandatory evacuation order for         │   │
│  │  Zones A and B, effective immediately. All residents in         │   │
│  │  flood-prone areas must evacuate within 6 hours.                │   │
│  │                                                                 │   │
│  │  Transportation resources should be activated for residents     │   │
│  │  without personal vehicles. Special attention needed for        │   │
│  │  nursing homes and assisted living facilities.                  │   │
│  │                                                                 │   │
│  │  Expected Action                                                │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  • Acknowledge receipt of evacuation order                      │   │
│  │  • Activate municipal evacuation plan                           │   │
│  │  • Begin shelter notifications                                  │   │
│  │  • Request transportation resources if needed                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ORGANIZATION                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Phase:       1. Initial Response                               │   │
│  │                                                                 │   │
│  │  Objectives:                                                    │   │
│  │  [1. Demonstrate EOC activation] [2. Multi-agency communication]│   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  CONTROLLER NOTES                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  📝 Deliver with urgency. Have evacuation zone map ready.       │   │
│  │     If players ask about timeline, respond "ASAP, storm         │   │
│  │     making landfall in 12 hours."                               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### After Inject is Fired

```
│  STATUS                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  🟢 Fired                                                       │   │
│  │                                                                 │   │
│  │  Delivered:  09:32 AM EST (January 15, 2025)                   │   │
│  │  By:         Sarah Martinez (Controller)                       │   │
│  │                                                                 │   │
│  │  Variance:   +2 minutes from scheduled                         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
```

## Technical Notes

- Lazy load related data (objectives, phase) if not already cached
- Consider prefetching next/previous inject for quick navigation
- Preserve scroll position when returning to MSEL list
