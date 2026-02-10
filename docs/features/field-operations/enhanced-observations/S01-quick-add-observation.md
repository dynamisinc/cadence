# Story: Quick-Add Observation

## S01-quick-add-observation

**As an** Evaluator in the field,
**I want** a streamlined observation form that I can open, fill in, and save in under 30 seconds,
**So that** I can capture what I'm seeing without missing the next thing happening in front of me.

### Context

The existing Phase E observation form supports complete data entry, but during fast-paced exercise conduct, Evaluators need a minimal-friction path. They're often standing, holding a tablet, watching multiple things happen at once. Every extra tap or screen transition is a moment they're looking at the device instead of the exercise. The Quick-Add form puts the essential fields on one screen — what happened, how it rated, what inject it relates to — and makes everything else optional. This is the primary observation workflow during conduct; the full form remains available for detailed post-exercise documentation.

### Acceptance Criteria

- [ ] **Given** I am a participant in an active exercise, **when** I tap the "New Observation" action (available from MSEL view, exercise conduct view, or observation list), **then** the Quick-Add observation form opens as a bottom sheet or modal
- [ ] **Given** the Quick-Add form is open, **when** I see the form, **then** it contains: observation text (required), rating selector (optional), inject link (optional), photo attachment (optional), and safety flag checkbox
- [ ] **Given** the Quick-Add form is open, **when** the text field has focus, **then** the keyboard opens immediately — no extra tap required
- [ ] **Given** I type observation text and tap Save, **when** the observation is saved, **then** it is created with the current real-time timestamp and scenario timestamp automatically applied
- [ ] **Given** I save an observation without selecting a rating, **when** the observation is saved, **then** it is saved successfully with rating as null — rating is not required for quick capture
- [ ] **Given** I save an observation without linking an inject, **when** the observation is saved, **then** it is saved successfully without an inject reference — inject link is not required
- [ ] **Given** the Quick-Add form is open, **when** I tap the rating selector, **then** I see touch-friendly buttons for P, S, M, U, and N/A — single tap to select, tap again to deselect
- [ ] **Given** I save a Quick-Add observation, **when** it is saved successfully, **then** I see a brief toast confirmation and the form closes, returning me to the previous screen
- [ ] **Given** I save a Quick-Add observation, **when** it is saved, **then** it appears immediately in the observation list and the Director's observation feed (via real-time sync)
- [ ] **Given** I am offline, **when** I save a Quick-Add observation, **then** it is saved locally and queued for sync — behavior is identical to the online experience from the user's perspective

### Out of Scope

- EEG (Exercise Evaluation Guide) task selection within Quick-Add (use the full observation form for detailed EEG mapping)
- Core Capability tagging within Quick-Add (available in full form or post-exercise editing)
- Observation templates or pre-filled text
- Batch observation creation

### Dependencies

- Phase E: Observation entity and CRUD endpoints
- Phase D: Exercise clock (for scenario timestamp)
- Phase H: Offline sync and real-time broadcast

### Open Questions

- [ ] Should the Quick-Add form remember the last-used rating for faster entry on consecutive similar observations? (Recommendation: no — each observation is independent, avoid accidental carry-over)
- [ ] Should there be a "Save & New" button for rapid sequential entry? (Recommendation: yes — allows creating multiple observations without navigating back and forth)
- [ ] Minimum observation text length? (Recommendation: 10 characters to prevent accidental empty submissions while allowing brief notes like "ICP set up")
- [ ] Should the Quick-Add form auto-close after save, or stay open for sequential entry? (Recommendation: close by default, "Save & New" keeps it open)

### Domain Terms

| Term | Definition |
|------|------------|
| Quick-Add Observation | A streamlined observation form optimized for field speed — single screen, minimal required fields, touch-optimized controls |
| Rating (P/S/M/U) | HSEEP performance rating scale: Performed (met the target), Satisfactory (met with minor issues), Marginal (partially met), Unsatisfactory (not met). N/A for observations that don't map to a rated capability. |
| Scenario Timestamp | The time within the exercise's fictional narrative, derived from the exercise clock, which may differ from wall clock time |

### UI/UX Notes

```
Quick-Add as bottom sheet (tablet):

┌─────────────────────────────────────┐
│ [exercise content visible above]    │
│                                     │
├─────────────────────────────────────┤
│ New Observation                 ✕   │
│ ┌─────────────────────────────────┐ │
│ │ What did you observe?       🎤  │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
│ [P] [S] [M] [U] [N/A]             │
│ Inject: [Select or search...  ▼]   │
│ 📷 Add Photo    ⚠️ □ Safety        │
│ [Cancel]  [Save & New]  [Save ✓]   │
└─────────────────────────────────────┘
```

- Bottom sheet pattern allows the evaluator to see exercise content above while entering the observation
- Rating buttons should be large enough for thumb taps (minimum 44x44px each)
- "Save & New" button clears the form and keeps it open for the next observation
- Voice input microphone icon (🎤) in the text field — tapping it activates speech recognition
- Form should be dismissible by swiping down or tapping outside (with unsaved data warning)

### Technical Notes

- Quick-Add form shares the same observation entity and API endpoint as the full form — it's a UI optimization, not a separate data model
- Rating stored as enum: `Performed | Satisfactory | Marginal | Unsatisfactory | NotApplicable | null`
- Bottom sheet component: consider MUI `SwipeableDrawer` with `anchor="bottom"`
- Auto-focus text field on form open: `autoFocus={true}` on the textarea
- "Save & New" implementation: on successful save, reset form state but keep the form open
