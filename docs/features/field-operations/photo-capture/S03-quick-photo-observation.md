# Story: Quick Photo (Auto-Create Draft Observation)

## S03-quick-photo-observation

**As an** Evaluator or Controller,
**I want** to quickly snap a photo that automatically creates a draft observation,
**So that** I can capture visual evidence immediately without stopping to fill out a full observation form in the middle of fast-moving exercise play.

### Context

During a fast-paced exercise, an Evaluator may witness multiple things happening simultaneously. They see the decon team set up incorrectly, but also notice the triage lead giving clear assignments. They don't have time to fill out a complete observation form for each — but they absolutely need to capture the evidence before it changes. The Quick Photo workflow lets them snap photos in rapid succession, each auto-creating a draft observation that they can return to and complete during a lull in exercise activity or during the hot wash. The principle is: **capture now, document later.** A photo with a timestamp and location is infinitely more valuable than a mental note that gets forgotten.

### Acceptance Criteria

- [ ] **Given** I am in an active exercise, **when** I tap the floating action button (FAB) camera icon, **then** my device camera opens immediately
- [ ] **Given** I take a photo via the FAB, **when** the photo is captured, **then** a new observation is automatically created in "Draft" status with the photo attached
- [ ] **Given** a draft observation is auto-created, **when** it is saved, **then** it includes: the photo, real-time timestamp, scenario timestamp, my participant ID, and my current location (if location sharing is enabled)
- [ ] **Given** a draft observation is auto-created, **when** I view it later, **then** the description field shows placeholder text: "Photo captured — add details" and the rating is unset
- [ ] **Given** I have taken a Quick Photo, **when** the photo is saved, **then** I see a brief toast confirmation ("Photo saved as draft") and I return to the previous screen within 2 seconds
- [ ] **Given** I want to add details immediately after a Quick Photo, **when** I see the toast confirmation, **then** I can tap the toast to open the draft observation for editing
- [ ] **Given** I have draft observations from Quick Photos, **when** I view my observation list, **then** draft observations are visually distinguished (e.g., "Draft" badge, dimmed appearance)
- [ ] **Given** I am viewing my draft observations, **when** I tap a draft, **then** I can edit it to add description, rating, inject linkage, and additional photos
- [ ] **Given** I take multiple Quick Photos in succession, **when** each is captured, **then** each creates its own separate draft observation (not grouped)
- [ ] **Given** I am offline, **when** I take a Quick Photo, **then** the draft observation and photo are saved locally and sync when connectivity returns

### Out of Scope

- Batch editing of multiple draft observations at once
- Auto-merging multiple Quick Photos into a single observation
- Video quick capture
- Timer or interval-based automatic photo capture

### Dependencies

- S01-capture-photo (camera access and compression)
- S02-attach-photo-to-observation (photo-observation linkage)
- Phase E: Observation entity with "Draft" status support
- Phase H: Offline sync architecture

### Open Questions

- [ ] Should the FAB camera icon be visible on all exercise screens, or only on the MSEL/conduct view? (Recommendation: all screens within an active exercise context)
- [ ] Should draft observations auto-delete after a configurable period if never completed? (Recommendation: no — drafts persist until the user explicitly deletes them or the exercise is archived)
- [ ] Should the Exercise Director see draft observations in the observation feed, or only completed ones? (Recommendation: Director sees all observations with draft status clearly indicated)
- [ ] Can a draft observation be submitted for the AAR without completing it? (Recommendation: yes, but show a warning that it's incomplete)

### Domain Terms

| Term | Definition |
|------|------------|
| Quick Photo | A streamlined capture workflow: one tap opens camera, photo auto-creates a draft observation with minimal metadata |
| Draft Observation | An observation in an incomplete state. Has photo and timestamps but may lack description, rating, or inject linkage. Can be completed later. |
| FAB (Floating Action Button) | A persistent, prominent button overlaid on exercise screens that provides quick access to the most common field action (photo capture) |
| Hot Wash | An immediate post-exercise discussion where participants share initial observations. Draft observations captured during the exercise can be reviewed and completed during this period. |

### UI/UX Notes

```
FAB position and behavior:

┌─────────────────────────────────────┐
│ Exercise Conduct View               │
│                                     │
│  [exercise content...]              │
│                                     │
│                                     │
│                                     │
│                                     │
│                            ┌──────┐ │
│                            │  📷  │ │
│                            └──────┘ │
└─────────────────────────────────────┘

After capture → toast notification:

┌─────────────────────────────────────┐
│ Exercise Conduct View               │
│                                     │
│  [exercise content...]              │
│                                     │
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ 📷 Photo saved as draft  [View]│ │
│ └─────────────────────────────────┘ │
│                            ┌──────┐ │
│                            │  📷  │ │
│                            └──────┘ │
└─────────────────────────────────────┘

Draft observations in list:

┌─────────────────────────────────────┐
│ My Observations                     │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ 🟡 DRAFT  10:32 AM             │ │
│ │ 📷 Photo captured — add details│ │
│ │ No rating · No inject linked    │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ ✅ COMPLETE  10:15 AM           │ │
│ │ ICP established with proper...  │ │
│ │ ⭐ Performed · INJ-003          │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

- FAB should be positioned at bottom-right, following Material Design conventions
- FAB should not obscure critical content — consider slight offset from screen edge
- Toast auto-dismisses after 4 seconds, tap to open the draft
- Draft observations should sort to the top of the user's observation list to encourage completion
- Consider a "Drafts" counter badge on the observations tab/section to remind users to complete them

### Technical Notes

- Draft observation entity needs a `status` field: `Draft | Complete` (or extend existing observation status if already present)
- Quick Photo flow should minimize taps: FAB tap → camera → capture → auto-save → return. Total interaction: 2 taps.
- Draft observations store the same fields as complete observations, with nullable description and rating
- Consider a `completedAt` timestamp that is null for drafts and set when the user saves with all required fields
