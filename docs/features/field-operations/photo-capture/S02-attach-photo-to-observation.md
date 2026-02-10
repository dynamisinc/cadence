# Story: Attach Photo to Observation

## S02-attach-photo-to-observation

**As an** Evaluator,
**I want** to attach one or more photos to an observation I'm creating or editing,
**So that** my written assessment is supported by visual evidence that can be referenced during the After-Action Review.

### Context

An Evaluator observing a mass casualty triage setup needs to document both their assessment ("Triage area established within 8 minutes, but patient flow was disorganized") and the visual evidence that supports it. Currently, written observations and photos live in separate places — the observation in the exercise tool, the photos on a personal phone. Linking photos directly to observations creates a complete evidence package: what was assessed, how it was rated, and what it looked like. This linkage is what makes Cadence observations significantly more valuable than paper-based evaluation.

### Acceptance Criteria

- [ ] **Given** I am creating a new observation, **when** I view the observation form, **then** I see a "Photos" section with an "Add Photo" button
- [ ] **Given** I am on the observation form, **when** I tap "Add Photo", **then** I can choose between "Take Photo" (camera) and "Choose from Gallery" (device file picker)
- [ ] **Given** I take or select a photo, **when** it is added to the observation, **then** a thumbnail preview appears in the Photos section of the form
- [ ] **Given** I have attached one or more photos, **when** I tap a thumbnail, **then** I see a full-size preview with an option to remove the photo
- [ ] **Given** I have attached photos and I save the observation, **when** the observation is saved, **then** all attached photos are associated with that observation record
- [ ] **Given** I am editing an existing observation, **when** I view the edit form, **then** I see previously attached photos and can add more or remove existing ones
- [ ] **Given** I remove a photo from an observation, **when** I save the observation, **then** the photo is disassociated from the observation but remains in the exercise photo gallery (not permanently deleted)
- [ ] **Given** I am viewing a saved observation in read-only mode, **when** I see attached photos, **then** I can tap a thumbnail to view the full-size image
- [ ] **Given** I attach multiple photos to an observation, **when** viewing the observation, **then** photos display in the order they were attached (chronological)
- [ ] **Given** I am offline when I attach a photo to an observation, **when** connectivity returns, **then** both the observation and photo sync together — the observation references the correct photo after sync

### Out of Scope

- Drag-and-drop reordering of attached photos
- Photo editing or cropping within the attachment workflow
- Attaching photos from other observations or exercises
- Attaching non-image files (PDFs, documents) to observations

### Dependencies

- S01-capture-photo (camera and gallery access must work)
- Phase E: Observation CRUD (observation form must exist)
- S06-offline-photo-queue (offline attachment must sync correctly)

### Open Questions

- [ ] Should there be a maximum number of photos per observation? (Recommendation: soft limit of 10 with a warning, no hard block)
- [ ] When a photo is removed from an observation, should it remain in the exercise gallery or be deleted entirely? (Recommendation: remain in gallery — it was still captured during the exercise)
- [ ] Should photos attached to observations be visible to all roles, or only to Evaluators and Directors? (Recommendation: visible to all roles who can view the observation)

### Domain Terms

| Term | Definition |
|------|------------|
| Observation | A record of something an Evaluator witnessed during exercise conduct, including descriptive text, an optional HSEEP rating, and linked evidence |
| Photo Evidence | A photograph attached to an observation that visually supports the assessment |
| Exercise Photo Gallery | A collection of all photos captured during an exercise, regardless of observation linkage |

### UI/UX Notes

```
Photos section within observation form:

┌─────────────────────────────────────┐
│ Photos (3 attached)                 │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌─────┐│
│ │      │ │      │ │      │ │  +  ││
│ │ img1 │ │ img2 │ │ img3 │ │ Add ││
│ │      │ │      │ │      │ │     ││
│ └──────┘ └──────┘ └──────┘ └─────┘│
└─────────────────────────────────────┘

Tap thumbnail → full-size preview:

┌─────────────────────────────────────┐
│                              ✕  🗑  │
│                                     │
│                                     │
│          [full-size photo]          │
│                                     │
│                                     │
│  10:32 AM (Scenario: 14:32)        │
│  Captured by: J. Smith              │
│  ◀  2 of 3  ▶                      │
└─────────────────────────────────────┘
```

- Thumbnails should be 80x80px in the form, displayed in a horizontal scrollable row
- "Add Photo" button always visible as the last item in the row
- Full-size preview should include swipe navigation between attached photos
- Remove button (trash icon) only visible in edit mode, not read-only view
- Show timestamp and participant name on full-size preview

### Technical Notes

- Photo-observation relationship is many-to-many in concept but one-to-many in practice (a photo belongs to one observation, an observation has many photos)
- Consider a `PhotoAttachment` join entity: `{ id, observationId, photoId, displayOrder, attachedAt }`
- When saving offline, generate a temporary client-side ID for the photo that maps to the server-generated ID after sync
- Blob storage path convention: `{organizationId}/{exerciseId}/photos/{photoId}.jpg`
