# Story: S03 - Link Objective to Inject

## User Story

**As an** Administrator, Exercise Director, or Controller,
**I want** to link exercise objectives to injects,
**So that** Controllers understand the purpose of each inject and evaluators can track which objectives are being tested.

## Context

The relationship between objectives and injects is many-to-many: a single inject can exercise multiple objectives, and each objective typically has multiple injects that test it. This linking creates the foundation for tracking exercise progress against objectives and supports after-action analysis.

During conduct, seeing linked objectives helps Controllers provide context when delivering injects and helps Evaluators know which capabilities to observe.

## Acceptance Criteria

- [ ] **Given** I am creating or editing an inject, **when** I view the form, **then** I see an "Objectives" field with multi-select capability
- [ ] **Given** I am in the Objectives field, **when** I click, **then** I see a dropdown with all exercise objectives (number and name)
- [ ] **Given** I am selecting objectives, **when** I click an objective, **then** it is added to the inject's linked objectives (shown as chips/tags)
- [ ] **Given** an objective is already linked, **when** I click the X on its chip, **then** it is removed from the inject
- [ ] **Given** I have selected objectives, **when** I save the inject, **then** the objective links are persisted
- [ ] **Given** I am viewing an inject in the MSEL list, **when** I look at the Objectives column, **then** I see linked objective numbers (e.g., "1, 2")
- [ ] **Given** I am viewing inject detail, **when** I look at the Objectives section, **then** I see full objective names with links to view each
- [ ] **Given** I am viewing an objective, **when** I look at linked injects, **then** I see a list of all injects linked to this objective
- [ ] **Given** I am filtering the MSEL, **when** I filter by objective, **then** only injects linked to that objective are shown
- [ ] **Given** an inject has no objectives linked, **when** I view it, **then** the Objectives field shows "None" or is empty
- [ ] **Given** I am an Evaluator or Observer, **when** I view inject-objective links, **then** I can view but not modify them

## Out of Scope

- Automatic objective suggestion based on inject content
- Required minimum objectives per inject
- Objective weighting or priority within an inject
- Bulk objective assignment across multiple injects

## Dependencies

- exercise-objectives/S01: Create Objective (objectives must exist)
- inject-crud/S01: Create Inject (linking happens during inject creation/edit)
- inject-filtering/S01: Filter Injects (filtering by objective)

## Open Questions

- [ ] Should there be a visual indicator when an objective has no linked injects?
- [ ] Should objectives be suggested based on inject keywords?
- [ ] Should we show objective completion percentage during conduct?

## Domain Terms

| Term | Definition |
|------|------------|
| Objective Link | Association between an inject and an exercise objective |
| Objective Coverage | The set of injects that test a particular objective |

## UI/UX Notes

### Objective Selection in Inject Form

```
┌─────────────────────────────────────────────────────────────┐
│  Objectives                                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ [1. EOC Activation ✕] [2. Multi-agency Comm ✕]     │   │
│  │                                                     │   │
│  │ [Select objectives...                          ▼]  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Dropdown expanded:                                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ☑ 1. Demonstrate EOC activation procedures         │   │
│  │ ☑ 2. Test multi-agency communication               │   │
│  │ ☐ 3. Evaluate resource request process             │   │
│  │ ☐ 4. Assess public information coordination        │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### MSEL List View with Objectives

```
┌──────────────────────────────────────────────────────────────────────────┐
│  # │ Scheduled  │ Inject Title              │ Objectives │ Status │     │
│ ───┼────────────┼───────────────────────────┼────────────┼────────┼──── │
│  1 │ 09:00 AM   │ Hurricane warning issued  │ 1, 2       │ Pending│ ••• │
│  2 │ 09:15 AM   │ EOC activation ordered    │ 1          │ Pending│ ••• │
│  3 │ 09:30 AM   │ First responders dispatch │ 2, 3       │ Pending│ ••• │
│  4 │ 09:45 AM   │ Media inquiry received    │ 4          │ Pending│ ••• │
└──────────────────────────────────────────────────────────────────────────┘
```

### Inject Detail - Objectives Section

```
┌─────────────────────────────────────────────────────────────┐
│  Linked Objectives                                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. Demonstrate EOC activation procedures                   │
│     Test the ability of county emergency management to...   │
│                                                             │
│  2. Test multi-agency communication                         │
│     Evaluate the effectiveness of communication between...  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Use junction table for many-to-many relationship
- Consider indexing for efficient filtering by objective
- Include objective data in inject export for Excel roundtrip
