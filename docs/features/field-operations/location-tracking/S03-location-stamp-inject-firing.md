# Story: Location Stamp on Inject Firing

## S03-location-stamp-inject-firing

**As a** Controller,
**I want** my location to be automatically recorded when I fire an inject,
**So that** the exercise record shows where injects were physically delivered, supporting AAR analysis of inject delivery logistics.

### Context

In a distributed exercise, Controllers deliver injects at specific physical locations — handing a message to the Incident Commander at the ICP, delivering a phone call simulation at the dispatch center, or posting a notice at a staging area. Recording where the Controller was when they fired the inject provides logistical context: "Was the Controller at the right location when this inject was delivered?" For exercises spanning multiple buildings or sites, this data helps AAR reviewers understand the physical flow of exercise activity and whether inject delivery matched the intended locations. This is a lightweight enhancement — the location is captured silently using the same opt-in sharing mechanism as observations.

### Acceptance Criteria

- [ ] **Given** I am a Controller who has opted in to location sharing, **when** I fire an inject, **then** my current GPS coordinates are automatically recorded on the inject firing event
- [ ] **Given** I have NOT opted in to location sharing, **when** I fire an inject, **then** the inject fires normally without location data
- [ ] **Given** an inject was fired with location data, **when** the Exercise Director views the inject detail, **then** they see a location indicator showing where the inject was fired
- [ ] **Given** an inject was fired with location data, **when** viewing the inject on the Director's map view, **then** the inject's firing location is visible as a distinct marker (different from participant position markers)
- [ ] **Given** GPS is unavailable when I fire an inject, **when** the inject fires, **then** it fires successfully without location data — location is never a blocking requirement

### Out of Scope

- Comparing inject firing location to intended delivery location (requires inject location planning, which doesn't exist yet)
- Location-based inject delivery (auto-firing when a Controller enters a geofence)
- Location tracking of inject recipients (players)

### Dependencies

- S01-opt-in-location-sharing (Controller must have opted in)
- Phase D: Exercise Conduct (inject firing workflow)

### Open Questions

- [ ] Should the intended delivery location be a field on the inject, allowing comparison with actual firing location? (Recommendation: good future enhancement but out of scope for this story — adds planning complexity)
- [ ] Is this data useful enough to justify the implementation effort? (Recommendation: yes — it's low effort since the location sharing infrastructure already exists. Just read the current position when firing.)

### Domain Terms

| Term | Definition |
|------|------------|
| Inject Firing Location | The GPS coordinates of the Controller at the moment they triggered the inject status change to "Fired" |

### UI/UX Notes

- No additional UI during the firing workflow — location is captured silently
- Inject detail view shows a small location indicator similar to observation location stamps
- On the Director's map view, fired inject locations display as a different marker shape/color than participant positions (e.g., red burst icon vs. blue/green dots)

### Technical Notes

- Add to Inject (or InjectFiringEvent) entity: `firedLatitude: double?`, `firedLongitude: double?`, `firedLocationAccuracy: double?`
- Read from the same Geolocation watchPosition callback used for observations
- Low implementation effort if location sharing infrastructure from S01 is in place
