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

### Multi-Device Behavior

- [ ] **Given** I dismissed the prompt on one device, **when** I open the EEG form on a different device, **then** I see the prompt again (client-specific dismissal)
- [ ] **Given** I save my phone number, **when** I open the EEG form on any device, **then** I do NOT see the prompt (phone stored server-side)

### Prompt UI

- [ ] **Given** the contact info prompt, **when** displayed, **then** I see my name and email (read-only, from auth) for confirmation
- [ ] **Given** the contact info prompt, **when** displayed, **then** I see a phone number field (optional)
- [ ] **Given** the contact info prompt, **when** displayed, **then** I see brief context: "Your contact info appears on EEG documents. Phone is optional."
- [ ] **Given** the contact info prompt, **when** I enter a phone number and click "Save & Continue", **then** my phone is saved and the EEG form opens
- [ ] **Given** the contact info prompt, **when** I click "Skip" without entering a phone, **then** the EEG form opens and I am not prompted again in this exercise
- [ ] **Given** the contact info prompt, **when** displayed on mobile/tablet, **then** layout is responsive and usable

### Document Generation Prompt (S13 Integration)

- [ ] **Given** I am generating a Completed EEG document (S13b), **when** my phone is not on file, **then** I see a prompt to add contact info before generation (optional skip)
- [ ] **Given** I skip the document generation prompt, **when** the document generates, **then** phone line shows "[Not provided]"

### Validation

- [ ] **Given** the phone field, **when** I enter a valid phone number, **then** it is accepted (flexible format — digits, dashes, parentheses, spaces, plus sign)
- [ ] **Given** the phone field, **when** I enter fewer than 7 characters, **then** I see a validation hint (but can still save)
- [ ] **Given** the phone field, **when** I enter more than 25 characters, **then** I see a validation error
- [ ] **Given** the phone field, **when** displayed, **then** placeholder shows format hint: "e.g., (555) 123-4567 or +1-555-123-4567"

### Clear/Remove Phone

- [ ] **Given** I have a phone number saved, **when** I access user profile (future), **then** I can clear/remove my phone number
- [ ] **Given** I clear my phone number, **when** I next open EEG Entry form, **then** I see the prompt again

### API

- [ ] **Given** an authenticated user, **when** calling `PATCH /api/users/me/contact`, **then** PhoneNumber is updated
- [ ] **Given** the user profile endpoint, **when** called, **then** PhoneNumber is included in the response
- [ ] **Given** a non-authenticated request, **when** calling the contact endpoint, **then** returns 401

### Offline Behavior

- [ ] **Given** I am offline, **when** the prompt appears, **then** I can enter my phone number
- [ ] **Given** I save my phone number offline, **when** connectivity returns, **then** the phone number syncs to the server
- [ ] **Given** I skip the prompt offline, **when** connectivity returns, **then** the skip preference syncs (client-only)

### EEG Document Integration

- [ ] **Given** an EEG document is generated (S13), **when** the evaluator has a phone number, **then** it appears in the Evaluator Information section
- [ ] **Given** an EEG document is generated, **when** the evaluator has no phone number, **then** the phone line shows "[Not provided]"

## API Specification

### PATCH /api/users/me/contact

**Request Body:**
```json
{
  "phoneNumber": "string|null"  // null to clear
}
```

**Response 200:**
```json
{
  "id": "guid",
  "name": "Sarah Kim",
  "email": "sarah.kim@metrocounty.gov",
  "phoneNumber": "(555) 123-4567",
  "updatedAt": "2026-02-05T14:30:00Z"
}
```

**Response 400:** Validation error (phone exceeds 25 chars)
```json
{
  "errors": {
    "phoneNumber": ["Phone number cannot exceed 25 characters"]
  }
}
```

**Response 401:** Unauthorized

### GET /api/users/me

**Response 200:** (existing endpoint, add phoneNumber)
```json
{
  "id": "guid",
  "name": "Sarah Kim",
  "email": "sarah.kim@metrocounty.gov",
  "phoneNumber": "(555) 123-4567",
  "role": "User",
  "organizationMemberships": [...]
}
```

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
│  │          e.g., (555) 123-4567 or +1-555-123-4567                  │ │
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

### Document Generation Prompt

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Generate EEG Document                                          [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ℹ️ Your phone number is not on file.                                  │
│                                                                         │
│  EEG documents include evaluator contact information.                   │
│  Would you like to add your phone number now?                          │
│                                                                         │
│  Phone:  ┌──────────────────────────────────┐                          │
│          │                                   │                          │
│          └──────────────────────────────────┘                          │
│                                                                         │
│  [Skip — Generate without phone]    [Save & Generate]                  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Full user profile management page (future — authentication epic)
- Evaluator assignment to specific Capability Targets (future enhancement)
- Organization-level evaluator directory (future enhancement)
- Making phone number a required field (always optional)
- Phone number format normalization (store as entered)

## Dependencies

- S06: EEG Entry Form (prompt appears before this form)
- User entity exists with authentication
- S13b: Generate Completed EEG Document (consumes phone number)

## Related Stories

- S10: AAR Export should include phone number in evaluator section
- S13a/S13b: EEG Document generation includes evaluator contact info

## Technical Notes

- Store the "prompt dismissed for this exercise" state in the client (localStorage or IndexedDB), not the server — it's a UI preference, not business data
- Phone number format should be stored as entered (no normalization) since evaluators may use extensions, international formats, or satellite phone numbers
- Use `PATCH /api/users/me/contact` endpoint rather than a full user update to keep it lightweight
- The prompt should be a collapsible banner at the top of the EEG form panel, not a blocking modal — evaluators in time-pressured situations should be able to skip instantly
- When user profile page is implemented, phone number should be editable there

## Test Scenarios

### Component Tests
- Prompt appears when PhoneNumber is null
- Prompt does not appear when PhoneNumber exists
- Prompt does not reappear after dismissal (same exercise, same device)
- Phone validation accepts common formats
- Skip button closes prompt without saving
- Character counter shows correctly

### Integration Tests
- Save phone number → persists to User entity
- Phone number appears in subsequent EEG document generation
- Offline phone save → syncs on reconnect
- Prompt dismissal persists across page navigation within exercise
- Clear phone number → prompt reappears
- Multi-device: dismissal is device-specific, save is global

---

*Story created: 2026-02-05*
*Origin: EEG Template Gap Analysis — Gap #2*
