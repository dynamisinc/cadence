# Story: View Photo Gallery for Exercise

## S04-photo-gallery

**As an** Exercise Director,
**I want** to browse all photos captured during an exercise in a gallery view,
**So that** I can review visual evidence across all evaluators and use it for After-Action Review preparation.

### Context

After an exercise (and sometimes during), the Exercise Director and evaluation team need to review all captured photo evidence. Photos may have been taken by a dozen different evaluators across multiple locations. Without a consolidated view, the Director would need to open each observation individually to see its attached photos. A gallery view provides a visual overview of everything captured, filterable by who took it, when, and what it's linked to. This becomes a primary input for AAR slide decks and final reports — which are HSEEP deliverables.

### Acceptance Criteria

- [ ] **Given** I am a participant in the exercise (any role), **when** I navigate to the exercise photo gallery, **then** I see thumbnails of all photos captured during the exercise in reverse chronological order (newest first)
- [ ] **Given** I am viewing the photo gallery, **when** I see a thumbnail, **then** it displays the capture time, participant name, and a linked observation indicator (if the photo is attached to an observation)
- [ ] **Given** I am viewing the photo gallery, **when** I tap a thumbnail, **then** I see the full-size photo with metadata: capture time (real and scenario), participant name, location (if available), and linked observation summary
- [ ] **Given** I am viewing a full-size photo that is linked to an observation, **when** I tap the observation link, **then** I navigate to the full observation detail view
- [ ] **Given** I am viewing the photo gallery, **when** I use the filter controls, **then** I can filter by: participant (who captured it), time range, linked/unlinked status, and observation rating (if linked)
- [ ] **Given** the exercise has no captured photos, **when** I navigate to the photo gallery, **then** I see an empty state message: "No photos captured yet. Use the camera button during exercise conduct to capture photo evidence."
- [ ] **Given** the exercise has more than 50 photos, **when** I scroll the gallery, **then** thumbnails load progressively (lazy loading) without blocking the UI
- [ ] **Given** I am viewing a full-size photo, **when** I swipe left/right (or use arrow controls), **then** I navigate to the previous/next photo in the current filtered set

### Out of Scope

- Downloading photos individually or as a batch (separate story for export)
- Deleting photos from the gallery (photos are deleted by removing them from observations)
- Uploading photos after the exercise has ended
- Photo slideshow or presentation mode
- Printing photos directly from the gallery

### Dependencies

- S01-capture-photo (photos must exist to display)
- S02-attach-photo-to-observation (observation linkage metadata)
- Phase H: Real-Time Sync (gallery updates as new photos sync from field)

### Open Questions

- [ ] Should the gallery be accessible during exercise conduct, or only after? (Recommendation: accessible during — Director needs real-time visual situational awareness)
- [ ] Should unlinked photos (Quick Photos with draft observations) appear differently? (Recommendation: yes, show a "Draft" indicator)
- [ ] Should there be a map view option showing photos plotted on a map by capture location? (Recommendation: defer to Location Tracking feature, but design gallery to support this later)
- [ ] Who can access the gallery? All participants or only Director/Admin? (Recommendation: all participants can view, but filter defaults to "My Photos" for non-Director roles)

### Domain Terms

| Term | Definition |
|------|------------|
| Photo Gallery | A consolidated view of all photos captured during an exercise, organized chronologically with filtering capabilities |
| Unlinked Photo | A photo that exists in the exercise context but is not attached to a completed observation (typically from Quick Photo drafts) |
| Photo Metadata | Contextual information stored with a photo: timestamps, participant, location, observation linkage |

### UI/UX Notes

```
Gallery view with filters:

┌─────────────────────────────────────┐
│ Exercise Photos              ▼ Filter│
│ 47 photos · 6 evaluators           │
├─────────────────────────────────────┤
│ [All] [J.Smith] [M.Jones] [K.Lee]  │  ← Participant chips
├─────────────────────────────────────┤
│ ┌────────┐ ┌────────┐ ┌────────┐   │
│ │        │ │        │ │  DRAFT │   │
│ │  img1  │ │  img2  │ │  img3  │   │
│ │        │ │        │ │        │   │
│ ├────────┤ ├────────┤ ├────────┤   │
│ │10:32 AM│ │10:45 AM│ │11:02 AM│   │
│ │J.Smith │ │M.Jones │ │J.Smith │   │
│ │⭐ P     │ │⭐ S     │ │—       │   │
│ └────────┘ └────────┘ └────────┘   │
│ ┌────────┐ ┌────────┐ ┌────────┐   │
│ │ 🔴SAFE │ │        │ │        │   │
│ │  img4  │ │  img5  │ │  img6  │   │
│ │  TY    │ │        │ │        │   │
│ ├────────┤ ├────────┤ ├────────┤   │
│ │11:15 AM│ │11:22 AM│ │11:30 AM│   │
│ │K.Lee   │ │J.Smith │ │M.Jones │   │
│ │⚠️ Safety│ │⭐ M     │ │⭐ P     │   │
│ └────────┘ └────────┘ └────────┘   │
└─────────────────────────────────────┘
```

- 3-column grid on tablet, 2-column on narrow screens
- Thumbnails should maintain aspect ratio within fixed-height containers
- Safety-flagged observation photos should have a distinct visual indicator (red border or badge)
- Participant filter uses horizontally scrollable chips
- Gallery is accessible from exercise detail navigation (tab or sidebar item)

### Technical Notes

- Gallery queries photos by exerciseId, returns paginated results with thumbnail URLs
- Thumbnails should be served from Blob Storage with CDN if available, or directly from blob with SAS tokens
- Consider a dedicated API endpoint: `GET /api/exercises/{id}/photos?participant=&from=&to=&linked=&page=&pageSize=`
- Lazy loading: use Intersection Observer API to load thumbnails as they scroll into view
- Full-size images load on demand (not prefetched) to conserve bandwidth
