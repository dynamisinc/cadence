# Story: Capture Photo During Exercise

## S01-capture-photo

**As an** Evaluator, Controller, or Observer,
**I want** to take a photo using my device's camera during an active exercise,
**So that** I can visually document what I'm observing in the moment rather than trying to describe it from memory later.

### Context

During an operations-based exercise, field participants witness response actions that are difficult to capture in text alone — the layout of a triage area, the condition of PPE being worn, signage posted at an ICP, or the physical setup of a decontamination corridor. Currently, participants take photos on personal phones that are disconnected from the exercise record. These photos rarely make it into the AAR because they aren't linked to observations, timestamps, or specific injects. Cadence needs to make photo capture a first-class part of the exercise workflow so that visual evidence is captured, organized, and available for post-exercise review.

### Acceptance Criteria

- [ ] **Given** I am a participant in an active exercise (clock is running or exercise status is "In Progress"), **when** I tap the camera button, **then** my device's camera opens for photo capture
- [ ] **Given** my device camera is open, **when** I take a photo, **then** the photo is saved and associated with the current exercise and my participant record
- [ ] **Given** my device camera is open, **when** I cancel without taking a photo, **then** I return to the previous screen with no photo saved
- [ ] **Given** I have taken a photo, **when** it is saved, **then** it is automatically compressed to a maximum of 1920px on the longest edge and JPEG quality of 80% to reduce file size
- [ ] **Given** I have taken a photo, **when** it is saved, **then** a thumbnail (300px) is generated client-side for use in list views
- [ ] **Given** I have taken a photo, **when** it is saved, **then** it is timestamped with both real-time (wall clock) and scenario time (exercise clock)
- [ ] **Given** I have not granted camera permissions to the application, **when** I tap the camera button, **then** I see a clear prompt explaining why camera access is needed with a link to device settings
- [ ] **Given** I want to attach an existing photo from my device gallery, **when** I tap the photo attachment area, **then** I can choose between "Take Photo" and "Choose from Gallery"
- [ ] **Given** I select a photo from my gallery, **when** it is attached, **then** it is compressed using the same rules as a camera-captured photo
- [ ] **Given** the exercise is not active (status is Draft, Planned, or Completed), **when** I view the exercise, **then** the camera capture button is not available

### Out of Scope

- Video capture (file size and bandwidth constraints)
- Editing the photo beyond annotation (no filters, cropping, rotation)
- Bulk photo import from device gallery
- Photo capture outside of an active exercise context
- Automatic object or text recognition in photos

### Dependencies

- Phase E: Observation entity exists in the data model
- Browser MediaDevices API (getUserMedia) for camera access
- Client-side image compression library (e.g., browser-image-compression)
- Azure Blob Storage configured for the environment

### Open Questions

- [ ] Should there be a maximum number of photos per observation? Per exercise? (Recommendation: no hard limit per observation, monitor storage per exercise)
- [ ] Should photos be capturable by all participant roles, or only Evaluators and Controllers? (SME feedback suggests all roles — Observers may capture evidence too)
- [ ] Is JPEG acceptable for all cases, or should PNG be preserved for screenshots/documents? (Recommendation: JPEG for camera, preserve original format for gallery selections)

### Domain Terms

| Term | Definition |
|------|------------|
| Photo Evidence | A photograph captured during exercise conduct that documents a condition, action, or concern |
| Active Exercise | An exercise whose clock is running or whose status is "In Progress" — the period during which field capture is enabled |
| Scenario Time | The fictional time within the exercise narrative, which may differ from wall clock time |
| Participant | A user who has been assigned an exercise role (Controller, Evaluator, Observer) for a specific exercise |

### UI/UX Notes

- Camera button should be accessible from multiple contexts: observation form, MSEL view, and as a floating action button (FAB)
- After photo capture, show a brief preview (1-2 seconds) with options: "Use Photo" / "Retake"
- Compression should happen silently in the background — no progress indicator needed unless the photo is very large
- Gallery selection should use the native device file picker (input type="file" accept="image/*") for familiarity
- Consider haptic feedback on photo capture if the device supports it

### Technical Notes

- Use `navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } })` to default to rear camera
- Fallback to `<input type="file" accept="image/*" capture="environment">` for browsers with limited MediaDevices support
- Client-side compression via browser-image-compression: `{ maxWidthOrHeight: 1920, maxSizeMB: 2, useWebWorker: true }`
- Store compressed photo + thumbnail in IndexedDB immediately (offline-first)
- Sync to Azure Blob Storage via existing pending action queue when online
- Blob metadata should include: exerciseId, participantId, observationId (if linked), capturedAt, scenarioTime
