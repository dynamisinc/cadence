# Feature: Photo Capture & Attachment

**Parent Epic:** Field Operations

## Description

Exercise participants can capture photos during exercise conduct using their device's camera (or select from their gallery), attach photos to observations, and optionally annotate them to highlight specific details. Photos compress automatically for bandwidth efficiency and queue for upload when offline, syncing when connectivity returns. A photo gallery view allows browsing all captured images for an exercise.

## User Stories

| # | Story | File | Priority | Status |
|---|-------|------|----------|--------|
| 1 | Capture Photo During Exercise | `S01-capture-photo.md` | P0 | 📲 |
| 2 | Attach Photo to Observation | `S02-attach-photo-to-observation.md` | P0 | 📲 |
| 3 | Quick Photo (Auto-Create Draft Observation) | `S03-quick-photo-observation.md` | P0 | 📲 |
| 4 | View Photo Gallery for Exercise | `S04-photo-gallery.md` | P1 | 📲 |
| 5 | Annotate Photo | `S05-annotate-photo.md` | P2 | 📲 |
| 6 | Offline Photo Queue | `S06-offline-photo-queue.md` | P0 | 📲 |

## Acceptance Criteria (Feature-Level)

- [ ] Participants can capture photos using device camera or select from device gallery during active exercise conduct
- [ ] Photos are automatically compressed client-side before storage to reduce bandwidth consumption
- [ ] Photos attach to observations with no more than 2 taps from the observation form
- [ ] Photos captured offline are queued locally and sync when connectivity returns without data loss
- [ ] All photos are associated with an exercise and the capturing participant
- [ ] Photos display as thumbnails in list views and full-size on tap/click
- [ ] Photo storage does not degrade application performance for exercises with 100+ captured images

## Wireframes/Mockups

### Observation Form with Photo Attachment
```
┌─────────────────────────────────────┐
│ New Observation                   ✕ │
├─────────────────────────────────────┤
│                                     │
│ What did you observe?               │
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │  [text area]                    │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Rating:  ○ P  ○ S  ○ M  ○ U  ○ N/A │
│                                     │
│ Photos:                             │
│ ┌──────┐ ┌──────┐ ┌──────────────┐ │
│ │ img1 │ │ img2 │ │  📷 + Add    │ │
│ │ thumb│ │ thumb│ │   Photo      │ │
│ └──────┘ └──────┘ └──────────────┘ │
│                                     │
│ Linked Inject: [Hurricane Warning…▼]│
│                                     │
│        [ Cancel ]  [ Save ]         │
└─────────────────────────────────────┘
```

### Quick Photo Capture (FAB)
```
┌─────────────────────────────────────┐
│ MSEL View / Exercise Conduct        │
│                                     │
│  ... exercise content ...           │
│                                     │
│                                     │
│                                     │
│                            ┌──────┐ │
│                            │  📷  │ │
│                            │ FAB  │ │
│                            └──────┘ │
└─────────────────────────────────────┘

Tap FAB → Camera opens → Photo captured →
Draft observation created with photo attached →
User can add details now or later
```

### Photo Gallery
```
┌─────────────────────────────────────┐
│ Exercise Photos          Filter ▼   │
├─────────────────────────────────────┤
│ Showing 47 photos  │ By: All       │
├─────────────────────────────────────┤
│ ┌────────┐ ┌────────┐ ┌────────┐   │
│ │        │ │        │ │        │   │
│ │  img1  │ │  img2  │ │  img3  │   │
│ │        │ │        │ │        │   │
│ ├────────┤ ├────────┤ ├────────┤   │
│ │10:32 AM│ │10:45 AM│ │11:02 AM│   │
│ │J.Smith │ │M.Jones │ │J.Smith │   │
│ └────────┘ └────────┘ └────────┘   │
│ ┌────────┐ ┌────────┐ ┌────────┐   │
│ │        │ │        │ │        │   │
│ │  img4  │ │  img5  │ │  img6  │   │
│ │        │ │        │ │        │   │
│ ├────────┤ ├────────┤ ├────────┤   │
│ │11:15 AM│ │11:22 AM│ │11:30 AM│   │
│ │K.Lee   │ │J.Smith │ │M.Jones │   │
│ └────────┘ └────────┘ └────────┘   │
└─────────────────────────────────────┘
```

## Dependencies

- Phase E: Evaluator Observations (observation entity and form must exist)
- Phase H: Real-Time & Offline (sync architecture for offline queue)
- Phase I: PWA (service worker context for camera access)
- Azure Blob Storage account (media file storage)

## Domain Terms

| Term | Definition |
|------|------------|
| Photo Evidence | A photograph captured during exercise conduct that documents an observed condition, response action, or safety concern |
| Quick Photo | A photo captured via the floating action button that auto-creates a draft observation, allowing the participant to add details later |
| Draft Observation | An observation in an incomplete state — has photo and timestamp but may lack description, rating, or inject linkage. Participant can complete it later. |
| Photo Annotation | Simple markup (circles, arrows, text labels) added to a photo to highlight what the photo is documenting |
| Thumbnail | A reduced-size version of a photo used in list views and galleries to improve loading performance |
