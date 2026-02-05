# S12: Evaluator Contact Information Prompt

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 3

## User Story

**As an** Evaluator,
**I want** to be prompted for my phone number when I first use the EEG Entry form (if not already on file),
**So that** generated EEG documents include my complete contact information per HSEEP standards.

## Context

The HSEEP EEG template includes an "Evaluator Information" footer section with three fields: Name, Email, and Phone. Cadence captures evaluator name and email through authentication, but phone number is not part of the standard user profile.

Rather than requiring phone number at registration (which adds friction to onboarding), this story introduces a contextual prompt — the first time an evaluator opens the EEG Entry form, they're asked to provide their phone number if one isn't already on file. This keeps the onboarding lightweight while ensuring EEG documents are complete.

### HSEEP Template Reference

```
┌───────────────────────────────────────────────┐
│ Evaluator Information                         │
│───────────────────────────────────────────────│
│ Evaluator Name:  [Insert]                     │
│ Evaluator Email: [Insert]                     │
│ Evaluator Phone: [Insert]    ← THIS FIELD     │
└───────────────────────────────────────────────┘
```

### Design Philosophy

- **Don't block the workflow.** The prompt should be dismissible — evaluators can skip it and still enter EEG data. Phone number is optional.
- **Ask once.** Once provided (or explicitly dismissed), don't prompt again in this exercise.
- **Store per user, not per exercise.** Phone number is a user-level attribute that carries across exercises.
- **Future-proof.** When user profile management is built, phone number will be editable there. This prompt is the initial collection mechanism.

## Acceptance Criteria

### Data Model

- [ ] **Given** the User entity, **when** migrations run, **then** a `PhoneNumber` column exists (nvarchar(25), nullable)
- [ ] **Given** an existing user, **when** the migration runs, **then** PhoneNumber defaults to null

### Prompt Trigger

- [ ] **Given** I am an Evaluator opening the EEG Entry form (S06), **when** my PhoneNumber is null, **then** I see a one-time contact info prompt before the form
- [ ] **Given** I am an Evaluator opening the EEG Entry form, **when** my PhoneNumber is already set, **then** I go directly to the form (no prompt)
- [ ] **Given** I dismissed the prompt previously in this exercise, **when** I open the EEG Entry form again, **then** I do not see the prompt again
- [ ] **Given** I am a Director or Admin, **when** I open the EEG Entry form, **then** the same prompt behavior applies (all EEG-entering roles)

### Prompt UI

- [ ] **Given** the contact info prompt, **when** displayed, **then** I see my name and email (read-only, from auth) for confirmation
- [ ] **Given** the contact info prompt, **when** displayed, **then** I see a phone number field (optional)
- [ ] **Given** the contact info prompt, **when** displayed, **then** I see brief context: "Your contact info appears on EEG documents. Phone is optional."
- [ ] **Given** the contact info prompt, **when** I enter a phone number and click "Save & Continue", **then** my phone is saved and the EEG form opens
- [ ] **Given** the contact info prompt, **when** I click "Skip" without entering a phone, **then** the EEG form opens and I am not prompted again in this exercise
- [ ] **Given** the contact info prompt, **when** displayed on mobile/tablet, **then** layout is responsive and usable

### Validation

- [ ] **Given** the phone field, **when** I enter a valid phone number, **then** it is accepted (flexible format — digits, dashes, parentheses, spaces, plus sign)
- [ ] **Given** the phone field, **when** I enter fewer than 7 characters, **then** I see a validation hint (but can still save)
- [ ] **Given** the phone field, **when** I enter more than 25 characters, **then** I see a validation error

### API

- [ ] **Given** an authenticated user, **when** calling `PATCH /api/users/me/contact`, **then** PhoneNumber is updated
- [ ] **Given** the user profile endpoint, **when** called, **then** PhoneNumber is included in the response
- [ ] **Given** a non-authenticated request, **when** calling the contact endpoint, **then** returns 401

### Offline Behavior

- [ ] **Given** I am offline, **when** the prompt appears, **then** I can enter my phone number
- [ ] **Given** I save my phone number offline, **when** connectivity returns, **then** the phone number syncs to the server
- [ ] **Given** I skip the prompt offline, **when** connectivity returns, **then** the skip preference syncs

### EEG Document Integration

- [ ] **Given** an EEG document is generated (S13), **when** the evaluator has a phone number, **then** it appears in the Evaluator Information section
- [ ] **Given** an EEG document is generated, **when** the evaluator has no phone number, **then** the phone line shows "[Not provided]"

## Wireframes

### Contact Info Prompt (Inline Banner)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  + EEG Entry                                     Exercise Time: 10:45   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  📋 Complete Your Evaluator Contact Info                          │ │
│  │                                                                   │ │
│  │  Your contact information appears on EEG documents.              │ │
│  │  Phone number is optional.                                        │ │
│  │                                                                   │ │
│  │  Name:   Sarah Kim                              (from account)    │ │
│  │  Email:  sarah.kim@metrocounty.gov              (from account)    │ │
│  │                                                                   │ │
│  │  Phone:  ┌──────────────────────────────────┐                     │ │
│  │          │ (555) 123-4567                    │                     │ │
│  │          └──────────────────────────────────┘                     │ │
│  │                                                                   │ │
│  │                           [Skip for Now]   [Save & Continue]      │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌─── EEG Entry Form (below, dimmed until prompt resolved) ─────────┐ │
│  │  Capability Target *                                              │ │
│  │  ...                                                              │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Full user profile management page (future — authentication epic)
- Evaluator assignment to specific Capability Targets (future enhancement)
- Organization-level evaluator directory (future enhancement)
- Making phone number a required field (always optional)

## Dependencies

- S06: EEG Entry Form (prompt appears before this form)
- User entity exists with authentication
- S13: Generate EEG Document (consumes phone number)

## Technical Notes

- Store the "prompt dismissed for this exercise" state in the client (localStorage or IndexedDB), not the server — it's a UI preference, not business data
- Phone number format should be stored as entered (no normalization) since evaluators may use extensions, international formats, or satellite phone numbers
- Consider a `PATCH /api/users/me/contact` endpoint rather than a full user update to keep it lightweight
- The prompt should be a collapsible banner at the top of the EEG form panel, not a blocking modal — evaluators in time-pressured situations should be able to skip instantly

## Test Scenarios

### Component Tests
- Prompt appears when PhoneNumber is null
- Prompt does not appear when PhoneNumber exists
- Prompt does not reappear after dismissal
- Phone validation accepts common formats
- Skip button closes prompt without saving

### Integration Tests
- Save phone number → persists to User entity
- Phone number appears in subsequent EEG document generation
- Offline phone save → syncs on reconnect
- Prompt dismissal persists across page navigation within exercise

---

*Story created: 2026-02-05*
*Origin: EEG Template Gap Analysis — Gap #2*
