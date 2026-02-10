# Story: Annotate Photo

## S05-annotate-photo

**As an** Evaluator,
**I want** to add simple annotations (circles, arrows, text labels) to a photo I've captured,
**So that** I can highlight the specific detail the photo is documenting, making it immediately clear during the AAR what the audience should focus on.

### Context

A photo of an Incident Command Post may contain dozens of details, but the Evaluator specifically wants to highlight that the organizational chart is missing from the command board. Without annotation, the AAR reviewer has to guess what the photo is supposed to show. A simple red circle around the empty spot on the command board, with a text label "Org chart missing," transforms the photo from ambiguous evidence into a clear finding. Annotations should be fast and simple — this is field markup, not graphic design.

### Acceptance Criteria

- [ ] **Given** I have captured or attached a photo, **when** I tap the annotate button on the photo preview, **then** I enter an annotation mode overlaid on the photo
- [ ] **Given** I am in annotation mode, **when** I select the circle tool and draw on the photo, **then** a circle (outline, not filled) appears at the drawn location in a high-contrast color (red by default)
- [ ] **Given** I am in annotation mode, **when** I select the arrow tool and draw on the photo, **then** a directional arrow appears pointing from start to end of my draw gesture
- [ ] **Given** I am in annotation mode, **when** I select the text tool and tap on the photo, **then** a text input appears at that location where I can type a short label (max 100 characters)
- [ ] **Given** I have placed annotations, **when** I tap "Done", **then** the annotations are saved as a layer on top of the original photo — the original photo is preserved unmodified
- [ ] **Given** I have annotated a photo, **when** I view the photo in the gallery or observation detail, **then** annotations are visible overlaid on the photo
- [ ] **Given** I have annotated a photo, **when** I tap "Edit Annotations", **then** I can modify or remove existing annotations
- [ ] **Given** I want to undo an annotation, **when** I tap "Undo" in annotation mode, **then** the most recent annotation action is removed
- [ ] **Given** I am in annotation mode, **when** I tap "Cancel", **then** all unsaved annotations are discarded and I return to the photo preview

### Out of Scope

- Freehand drawing or pen tool
- Color selection for annotations (use default high-contrast red)
- Line thickness adjustment
- Image cropping, rotation, or filters
- Annotation on photos captured by other participants
- Collaborative annotation (multiple users annotating the same photo)

### Dependencies

- S01-capture-photo (photo must be captured and accessible)
- S02-attach-photo-to-observation (photo must be viewable in observation context)

### Open Questions

- [ ] Should annotations be stored as a separate SVG/Canvas overlay or burned into a copy of the image? (Recommendation: store as a separate overlay layer to preserve the original and enable editing)
- [ ] Should annotation be available in offline mode? (Recommendation: yes — annotations are client-side only, no server dependency)
- [ ] Should the AAR export include annotated versions, originals, or both? (Recommendation: both — annotated for the report, originals for the record)

### Domain Terms

| Term | Definition |
|------|------------|
| Annotation | A visual markup (circle, arrow, or text label) added to a photo to highlight a specific detail for evaluation purposes |
| Annotation Layer | A separate visual overlay stored independently from the original photo, enabling non-destructive editing |

### UI/UX Notes

```
Annotation mode:

┌─────────────────────────────────────┐
│ Annotate Photo         [Undo] [Done]│
├─────────────────────────────────────┤
│                                     │
│                                     │
│        [photo with annotations]     │
│              ⭕ ← circle            │
│                   "Missing" ← text  │
│                  ➡️ ← arrow          │
│                                     │
│                                     │
├─────────────────────────────────────┤
│  ⭕ Circle  │  ➡️ Arrow  │  T Text   │
│  [Cancel]                           │
└─────────────────────────────────────┘
```

- Tool selection bar at the bottom for thumb reach on tablets
- Active tool highlighted with selection indicator
- Annotations render at high contrast against any background (red outline with white shadow/border)
- Touch targets for annotation handles should be at least 44x44px for easy selection
- Text labels should use a legible sans-serif font with a semi-transparent background for readability

### Technical Notes

- Implement annotation as an HTML5 Canvas or SVG overlay on top of the photo `<img>` element
- Store annotations as JSON: `[{ type: 'circle', x, y, radius }, { type: 'arrow', x1, y1, x2, y2 }, { type: 'text', x, y, content }]`
- For export/sharing, render the annotated version by compositing the overlay onto the image using Canvas `drawImage` + overlay rendering
- Annotation JSON stored alongside photo metadata (not in blob storage — in the database)
- Consider fabric.js or Konva.js for canvas-based annotation, but evaluate bundle size impact
