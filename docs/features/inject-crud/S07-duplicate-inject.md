# Story: S07 - Duplicate Inject

**Priority:** P1 (Standard)
**Feature:** Inject CRUD
**Created:** 2026-02-12

---

## User Story

**As a** Controller or Exercise Director authoring a MSEL,
**I want** to duplicate an existing inject as the starting point for a new one,
**So that** I can quickly create similar injects without re-entering shared field values.

---

## Context

Real-world MSELs frequently contain clusters of similar injects. For example:
- A series of injects targeting different agencies but with the same source and delivery method
- Sequential escalation injects with similar structure but increasing severity
- Multiple injects at different times with the same target and track

Currently, creating each inject requires filling in all fields from scratch. A "duplicate" action would copy all field values from an existing inject into a new create form, letting the user modify only what differs. This dramatically reduces data entry time for MSELs with 50-100+ injects.

This was identified as an open question in S01 ("Should there be a 'duplicate inject' option on create?") and is now being specified.

---

## Acceptance Criteria

### Duplicate Action

- [ ] **Given** I am viewing the MSEL list, **when** I open the actions menu for an inject, **then** I see a "Duplicate" option
- [ ] **Given** I am viewing inject detail, **when** I open the actions menu, **then** I see a "Duplicate" option
- [ ] **Given** I click "Duplicate", **when** the create form opens, **then** all fields are pre-filled from the source inject

### Copied Fields

- [ ] **Given** I duplicate an inject, **when** the form opens, **then** the Title is copied with " (Copy)" appended
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Description is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Source (From) is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Target (To) is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Delivery Method is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Track is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Expected Action is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Notes are copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Responsible Controller is copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Location Name and Location Type are copied exactly
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Phase assignment is copied
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Objective links are copied

### Fields NOT Copied

- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Inject Number is NOT copied (will be auto-generated on save)
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Scheduled Time is NOT copied (must be set for new inject)
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Scenario Day and Scenario Time are NOT copied
- [ ] **Given** I duplicate an inject, **when** the form opens, **then** Status is set to "Pending" (not copied from source)

### Form Behavior

- [ ] **Given** the form opens from a duplicate, **when** I view the form header, **then** it says "New Inject (from duplicate)"
- [ ] **Given** the form opens from a duplicate, **when** I edit any pre-filled field, **then** my changes are reflected normally
- [ ] **Given** I save the duplicated inject, **when** creation succeeds, **then** a new inject is created (the original is not modified)
- [ ] **Given** I cancel the duplicate form, **when** the form closes, **then** no inject is created and the original is not modified
- [ ] **Given** Scheduled Time is required, **when** the form opens, **then** Scheduled Time is empty and highlighted as required

### Multiple Duplicates

- [ ] **Given** I duplicate an inject, **when** I immediately duplicate the same inject again, **then** a second form opens with the same pre-filled values (not from the first duplicate)

---

## Out of Scope

- Bulk duplication (duplicating multiple injects at once)
- "Duplicate and increment time" (auto-spacing duplicates by interval)
- Template library (reusable inject templates across exercises)
- Copy inject to a different exercise/MSEL
- Keyboard shortcut for duplicate

---

## Dependencies

- inject-crud/S01: Create Inject (create form must exist)
- inject-crud/S02: Edit Inject (for detail view duplicate action)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Duplicate | Create a new inject pre-filled with values from an existing inject |
| Source Inject | The existing inject being duplicated (not modified) |

---

## UI/UX Notes

### MSEL Row Actions Menu

```
┌────────────────────────┐
│ ✏️ Edit               │
│ 📋 Duplicate           │  ← New action
│ ─────────────────────  │
│ 🗑️ Delete             │
└────────────────────────┘
```

### Inject Detail Actions

```
┌─────────────────────────────────────────────────────────────────┐
│ Inject #4: Multi-vehicle accident reported                       │
│                                                                   │
│ [Edit]  [Duplicate]  [Delete]                                    │
│                                                                   │
│ ...inject details...                                             │
└─────────────────────────────────────────────────────────────────┘
```

### Duplicate Form Header

```
┌─────────────────────────────────────────────────────────────────┐
│ New Inject (from duplicate)                                   ✕  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│ Title *                                                          │
│ ┌─────────────────────────────────────────────────────────┐     │
│ │ Multi-vehicle accident reported (Copy)                   │     │
│ └─────────────────────────────────────────────────────────┘     │
│                                                                   │
│ ─────────────── TIME ───────────────                             │
│                                                                   │
│ Scheduled Time *                    ⚠️ Required                  │
│ ┌─────────────────────────────────┐                              │
│ │                              📅 │ ← Empty, must be set         │
│ └─────────────────────────────────┘                              │
│                                                                   │
│ ─────────────── DELIVERY ───────────────                         │
│                                                                   │
│ From (Source)                       To (Target) *                 │
│ ┌───────────────────────┐          ┌───────────────────────┐    │
│ │ 911 Dispatch Center   │          │ Fire Department       │    │
│ └───────────────────────┘          └───────────────────────┘    │
│                    ↑ Pre-filled from source inject                │
│                                                                   │
│                               [Cancel]  [Create Inject]          │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### Frontend-Only Implementation

This feature is entirely frontend. No new API endpoint is needed:

1. Read the source inject's data (already available from the MSEL list or detail query)
2. Open the create form with pre-filled values
3. Submit via the existing create inject API

### Implementation Approach

```typescript
// In MSEL list or inject detail:
const handleDuplicate = (inject: InjectDto) => {
  // Navigate to create form with source inject data
  navigate('/exercises/{id}/injects/new', {
    state: {
      duplicateFrom: {
        ...inject,
        // Clear fields that shouldn't be copied
        id: undefined,
        injectNumber: undefined,
        scheduledTime: undefined,
        scenarioDay: undefined,
        scenarioTime: undefined,
        status: 'Pending',
        title: `${inject.title} (Copy)`,
      }
    }
  });
};

// In CreateInjectPage:
const location = useLocation();
const duplicateData = location.state?.duplicateFrom;
// Pass duplicateData as initialValues to InjectForm
```

### Form Initialization

The InjectForm component should accept optional `initialValues` prop. When provided (from duplicate), these values populate the form instead of defaults.

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Duplicate button visible in MSEL row actions | Component | P0 |
| Clicking Duplicate opens create form with pre-filled values | Component | P0 |
| Title has " (Copy)" suffix | Component | P0 |
| Scheduled Time is empty on duplicate | Component | P0 |
| Status is Pending on duplicate | Component | P0 |
| Inject Number is not copied | Component | P0 |
| All content fields are copied (Source, Target, Track, etc.) | Component | P0 |
| Phase and Objectives are copied | Component | P1 |
| Saving duplicate creates a new inject (original unchanged) | Integration | P0 |
| Canceling duplicate does not create anything | Component | P1 |

---

## Implementation Checklist

### Frontend
- [ ] Add `initialValues` prop to InjectForm component
- [ ] Add "Duplicate" action to MSEL row actions menu
- [ ] Add "Duplicate" button to inject detail view
- [ ] Implement `handleDuplicate` with navigation + state
- [ ] Handle `duplicateFrom` in CreateInjectPage
- [ ] Map source inject fields to form initial values
- [ ] Clear time-related fields and inject number
- [ ] Append " (Copy)" to title
- [ ] Update form header to show "New Inject (from duplicate)"
- [ ] Component tests for duplicate flow
- [ ] Test that original inject is not modified

---

## Changelog

| Date | Change |
|------|--------|
| 2026-02-12 | Initial story creation (resolves open question from S01) |
