# Story: S01 - Create Inject

## User Story

**As a** Controller or Exercise Director,
**I want** to create new injects for the MSEL,
**So that** I can build out the scenario events that will drive the exercise.

## Context

Injects are the building blocks of a MSEL. Each inject represents an event, message, or action that occurs during the exercise. Controllers deliver injects to players according to the schedule. Creating injects is a core authoring activity typically done during exercise planning.

## Acceptance Criteria

- [ ] **Given** I am viewing the MSEL, **when** I click "+ New Inject", **then** I see a create inject form
- [ ] **Given** I am on the create form, **when** I view required fields, **then** I see: Title, Scheduled Time
- [ ] **Given** I am on the create form, **when** I view optional fields, **then** I see: Scenario Day, Scenario Time, Description, From, To, Method, Expected Action, Notes, Phase, Objectives
- [ ] **Given** I enter a Title (3-200 characters) and Scheduled Time, **when** I click Save, **then** the inject is created
- [ ] **Given** I save an inject, **when** creation completes, **then** it receives an auto-generated Inject Number
- [ ] **Given** existing injects have numbers 1, 2, 3, **when** I create a new inject, **then** it receives number 4
- [ ] **Given** I am entering Scheduled Time, **when** I use the time picker, **then** I can select date and time in the exercise time zone
- [ ] **Given** I am entering Scenario Time, **when** I fill in the fields, **then** I enter Scenario Day (integer 1-99) and Scenario Time (HH:MM format)
- [ ] **Given** I enter Scenario Time without Scenario Day, **when** I try to save, **then** I see a validation error (Day required if Time provided)
- [ ] **Given** I am selecting a Method, **when** I view options, **then** I see: Phone Call, Email, Radio, In-Person, Fax, Video, Document, Other
- [ ] **Given** I want to link objectives, **when** I use the Objectives field, **then** I can select multiple from the exercise's objectives
- [ ] **Given** I want to assign a phase, **when** I use the Phase dropdown, **then** I can select from defined phases or leave unassigned
- [ ] **Given** I save successfully, **when** the form closes, **then** I see the new inject in the MSEL list at its scheduled position
- [ ] **Given** I am an Evaluator or Observer, **when** I view the MSEL, **then** I do not see "+ New Inject" button

## Out of Scope

- Inject templates or presets
- Bulk inject creation
- AI-assisted inject content generation
- Inject dependencies/prerequisites
- File attachments

## Dependencies

- exercise-crud/S01: Create Exercise (exercise must exist)
- exercise-objectives/S01: Create Objective (for objective linking)
- exercise-phases/S01: Define Phases (for phase assignment)
- exercise-config/S03: Time Zone Configuration (scheduled time display)

## Open Questions

- [ ] Should there be inject templates for common scenarios?
- [ ] Should Description support rich text formatting?
- [ ] Should there be a "duplicate inject" option on create?
- [ ] Maximum length for Description field?

## Domain Terms

| Term | Definition |
|------|------------|
| Inject | A scenario event, message, or action delivered during exercise conduct |
| Inject Number | Auto-generated sequential identifier within a MSEL |
| Scheduled Time | Wall-clock time when the inject should be delivered |
| Scenario Day | The day number within the exercise story (Day 1, Day 2, etc.) |
| Scenario Time | The time of day within the story (e.g., 14:00) |
| Method | The simulated communication channel for deliver |

## UI/UX Notes

### Create Inject Form

```
┌─────────────────────────────────────────────────────────────────────────┐
│  New Inject                                                         ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Title *                                                                │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ County issues mandatory evacuation order                        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ─────────────── TIME ───────────────                                  │
│                                                                         │
│  Scheduled Time *                          Scenario Time (optional)     │
│  ┌───────────────────────────────┐        ┌────────┐ ┌────────────┐   │
│  │ Jan 15, 2025  09:30 AM    📅 │        │ Day  1 │ │ 14:00      │   │
│  └───────────────────────────────┘        └────────┘ └────────────┘   │
│  Exercise time zone: EST                                               │
│                                                                         │
│  ─────────────── TARGETING ───────────────                             │
│                                                                         │
│  From                                      To                           │
│  ┌─────────────────────────────┐          ┌─────────────────────────┐ │
│  │ County Emergency Manager    │          │ All Municipalities      │ │
│  └─────────────────────────────┘          └─────────────────────────┘ │
│                                                                         │
│  Method                                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Phone Call                                                   ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ─────────────── CONTENT ───────────────                               │
│                                                                         │
│  Description                                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ The County Emergency Manager contacts all municipal            │   │
│  │ coordinators to issue a mandatory evacuation order for         │   │
│  │ Zones A and B, effective immediately...                        │   │
│  │                                                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Expected Action                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Players should acknowledge the order and begin activating      │   │
│  │ their evacuation plans.                                        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ─────────────── ORGANIZATION ───────────────                          │
│                                                                         │
│  Phase                                     Objectives                   │
│  ┌─────────────────────────────┐          ┌─────────────────────────┐ │
│  │ 1. Initial Response      ▼ │          │ [1. EOC ✕] [2. Comm ✕] │ │
│  └─────────────────────────────┘          └─────────────────────────┘ │
│                                                                         │
│  Controller Notes (internal)                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Deliver with urgency. Have map ready showing zones.            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                               [Cancel]  [Create Inject]                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Inject Number should be generated server-side to avoid conflicts
- Consider using GUID + sequential number pattern for ID
- Scheduled Time stored as UTC with time zone conversion on display
- Form should support keyboard navigation (Tab between fields)
- Auto-save draft after 30 seconds of inactivity (see _cross-cutting/S03)
