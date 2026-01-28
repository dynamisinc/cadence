# Story: Organization Default Exercise Template

**Feature**: Settings  
**Story ID**: S10  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Administrator,  
**I want** to configure default settings for new exercises,  
**So that** Directors don't have to reconfigure common settings for every exercise my organization runs.

---

## Context

Organizations often have standard exercise configurations: preferred clock mode for TTX vs full-scale, standard confirmation settings, consistent observation requirements. Instead of configuring each exercise from scratch, Administrators can set organizational defaults that apply to all new exercises.

This reduces setup time and ensures organizational consistency.

---

## Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I access organization settings, **then** I see "Default Exercise Settings" section
- [ ] **Given** I configure default clock mode as "Real-time", **when** a Director creates a new exercise, **then** clock mode defaults to Real-time
- [ ] **Given** I configure default auto-fire as "Disabled", **when** a Director creates a new exercise, **then** auto-fire defaults to disabled
- [ ] **Given** I configure confirmation defaults, **when** a Director creates a new exercise, **then** those confirmation settings are pre-selected
- [ ] **Given** I configure observation required fields, **when** a Director creates a new exercise, **then** those requirements are pre-selected
- [ ] **Given** organization defaults exist, **when** a Director edits exercise settings, **then** they can override any default
- [ ] **Given** I update organization defaults, **when** I save changes, **then** existing exercises are NOT affected (only new exercises)
- [ ] **Given** I am a Director (not Admin), **when** I view organization settings, **then** I cannot access or modify defaults

---

## Out of Scope

- Exercise templates with pre-populated phases/injects
- Multiple templates per organization (single default only)
- Template inheritance (org → department → team)
- Locked settings that Directors cannot override

---

## Dependencies

- S03: Exercise Clock Mode Setting
- S04: Exercise Auto-Fire Setting
- S05: Exercise Confirmation Dialogs Setting
- S09: Exercise Observation Required Fields
- Organization entity in data model

---

## Open Questions

- [ ] Should we support multiple named templates?
- [ ] Can some defaults be "locked" (Director cannot change)?
- [ ] Should there be a "Reset to organization defaults" button on exercise settings?
- [ ] What about default exercise type (TTX, Functional, Full-Scale)?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Organization Defaults | Standard settings applied to all new exercises |
| Template | Pre-configured settings that can be applied to exercises |

---

## UI/UX Notes

### Organization Settings - Default Exercise Settings

```
┌─────────────────────────────────────────────────────────────┐
│  Organization Settings                        [Admin Only]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Default Exercise Settings                                  │
│  ─────────────────────────────────────────────              │
│                                                             │
│  These settings will be applied to all new exercises.       │
│  Directors can override any setting for specific exercises. │
│                                                             │
│  Clock Mode Default                                         │
│  [ Real-time (1x) ▼ ]                                      │
│                                                             │
│  Auto-Fire Injects Default                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                          [  OFF ]   │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Default Confirmation Settings                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [✓]  Confirm fire inject                           │   │
│  │  [✓]  Confirm skip inject                           │   │
│  │  [✓]  Confirm clock control                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Default Required Observation Fields                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [✓]  Description                                   │   │
│  │  [✓]  P/S/M/U Rating                                │   │
│  │  [ ]  Linked Inject                                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│                                        [Save Defaults]      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Exercise Creation - Defaults Applied

When Director creates exercise:

```
┌─────────────────────────────────────────────────────────────┐
│  Create New Exercise                                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Name: [                                    ]               │
│  Type: [ Full-Scale ▼ ]                                    │
│  ...                                                        │
│                                                             │
│  ℹ Settings pre-configured from organization defaults.     │
│    You can change these in Exercise Settings after         │
│    creation.                                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Create `OrganizationSettings` entity or add columns to `Organization`
- Fields mirror exercise settings:
  - `DefaultClockMode`
  - `DefaultAutoFireEnabled`
  - `DefaultConfirmFire`
  - `DefaultConfirmSkip`
  - `DefaultConfirmClock`
  - `DefaultRequireRating`
  - etc.
- Exercise creation copies org defaults to new Exercise record
- Consider: store as JSON blob for flexibility

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
