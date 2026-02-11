# Story: Location Stamp on Observations

## S02-location-stamp-observations

**As an** Evaluator,
**I want** my GPS location to be automatically recorded when I create an observation,
**So that** the Exercise Director and AAR reviewers know exactly where I was when I documented the finding, adding geographic context without any extra effort on my part.

### Context

When an Evaluator writes "Triage area poorly organized, patients not tagged," the natural question during AAR is "Where was this? Which triage area?" If the observation includes a location stamp — coordinates that map to "Building C, west parking lot" — the context is immediately clear. Location stamping is automatic and invisible to the Evaluator: if they've opted in to location sharing, their current position is attached to each observation they create. No extra taps, no manual entry. For exercises spanning large venues or multiple sites, this geographic context transforms observations from ambiguous notes into precisely situated evidence. Combined with photos, a location-stamped observation gives the AAR team a complete picture: what was observed, how it was rated, what it looked like, and where it happened.

### Acceptance Criteria

- [ ] **Given** I have opted in to location sharing, **when** I save an observation (Quick-Add, full form, or Quick Photo), **then** my current GPS coordinates and accuracy radius are automatically attached to the observation
- [ ] **Given** I have opted in to location sharing, **when** the observation is saved, **then** the location stamp is applied silently — no additional UI interaction required from me
- [ ] **Given** I have NOT opted in to location sharing, **when** I save an observation, **then** no location data is attached — the observation saves normally without coordinates
- [ ] **Given** I am viewing a saved observation that has a location stamp, **when** I see the observation detail, **then** I see a location indicator (pin icon with coordinate-derived description or "lat, lng" if no reverse geocoding) 
- [ ] **Given** I am viewing a location-stamped observation, **when** I tap the location indicator, **then** I see the position on a mini-map centered on the observation's coordinates
- [ ] **Given** I am the Exercise Director viewing the observation feed, **when** I see a location-stamped observation, **then** the feed entry shows a brief location indicator (e.g., "📍 38.2527, -77.4928")
- [ ] **Given** I have opted in to location sharing but GPS is temporarily unavailable, **when** I save an observation, **then** the observation saves successfully without location data — location stamp is best-effort, never blocking
- [ ] **Given** I am offline and have opted in to location sharing, **when** I save an observation, **then** the most recent cached GPS position is attached (within the last 60 seconds), or no location if no recent position is available
- [ ] **Given** observations are exported for AAR, **when** the export includes location-stamped observations, **then** coordinates are included in the export data

### Out of Scope

- Reverse geocoding (converting coordinates to human-readable addresses) — display raw coordinates or a mini-map. Reverse geocoding adds API dependency and cost.
- Manual location entry or correction by the Evaluator
- Location history trail showing where the Evaluator moved before/after the observation
- Indoor positioning or floor-level identification
- Location-based filtering in the observation list (covered by Director map view)

### Dependencies

- S01-opt-in-location-sharing (location sharing must be active)
- Phase E: Observation entity (needs latitude, longitude, accuracy fields)
- S01-quick-add-observation / S03-quick-photo-observation (observation creation flows)

### Open Questions

- [ ] Should the observation display show raw coordinates, a mini-map, or both? (Recommendation: show a pin icon with coordinates; tap to see mini-map. Avoid reverse geocoding cost/complexity.)
- [ ] Should location accuracy be displayed to the user? (Recommendation: only if accuracy is poor (> 100m), show a note like "Approximate location (±150m)" so users understand the precision)
- [ ] If the last cached position is older than 60 seconds, should it still be attached? (Recommendation: no — stale location data could be misleading. Attach only if position is < 60 seconds old.)

### Domain Terms

| Term | Definition |
|------|------------|
| Location Stamp | GPS coordinates (latitude, longitude) and accuracy radius automatically attached to an observation at the moment of creation |
| Accuracy Radius | The estimated uncertainty of the GPS position in meters. Lower numbers indicate more precise positioning. |
| Cached Position | The most recently received GPS reading, stored client-side, used when the device cannot obtain a fresh position at the moment of observation creation |

### UI/UX Notes

```
Observation detail with location stamp:

┌─────────────────────────────────────┐
│ Observation Detail                  │
├─────────────────────────────────────┤
│ 🟡 PARTIALLY MET  · 11:28 AM       │
│ J. Smith · INJ-012 Evacuation Order │
│                                     │
│ Evacuation order communicated to    │
│ floors 1-3 but not to floors 4-5.  │
│ Building PA system not used.        │
│                                     │
│ 📷 Photos (2)                       │
│ ┌──────┐ ┌──────┐                  │
│ │ img1 │ │ img2 │                  │
│ └──────┘ └──────┘                  │
│                                     │
│ 📍 38.2527, -77.4928 (±25m)  [Map] │
│                                     │
└─────────────────────────────────────┘

Tap [Map] → mini-map popup:

┌─────────────────────────────────────┐
│ Observation Location           ✕    │
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │        [map tiles]              │ │
│ │              📍                 │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
│ 38.2527, -77.4928 · ±25m accuracy  │
│ Captured: 11:28 AM (Scenario 14:28)│
└─────────────────────────────────────┘
```

- Location indicator is subtle — a small pin icon with coordinates, not prominent
- Mini-map opens in a modal or bottom sheet, not a full navigation
- If accuracy > 100m, show the accuracy circle on the mini-map to indicate uncertainty
- Location is informational, not interactive — users can't edit or move the pin

### Technical Notes

- Add to Observation entity: `latitude: double?`, `longitude: double?`, `locationAccuracy: double?` (all nullable)
- When creating observation, read from the most recent Geolocation watchPosition callback if available and < 60 seconds old
- Store as decimal degrees with 6 decimal places (~0.1m precision, more than GPS provides)
- Mini-map rendering: use Leaflet.js with OpenStreetMap tiles — lightweight, free, no API key
- Map tile URL: `https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png`
- Lazy-load the map component (don't include Leaflet in the main bundle) — only load when user taps the map button
- For offline: cache a few zoom levels of map tiles in the service worker for the venue area (requires knowing the venue location in advance — consider making this configurable per exercise)
