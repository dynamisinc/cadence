# Feature: Location Tracking

**Parent Epic:** Field Operations

## Description

Exercise participants can opt in to sharing their GPS location during exercise conduct, giving the Exercise Director real-time situational awareness of where Controllers and Evaluators are positioned. Location data is automatically stamped on observations and inject firings, providing geographic context for post-exercise review. A Director map view shows team positions overlaid on a venue map, enabling coverage gap identification and resource reallocation. All location sharing is explicitly opt-in per exercise, with clear visual indicators when sharing is active, and location data collection stops automatically when the exercise clock stops.

## User Stories

| # | Story | File | Priority | Status |
|---|-------|------|----------|--------|
| 1 | Opt-In Location Sharing | `S01-opt-in-location-sharing.md` | P1 | 📲 |
| 2 | Location Stamp on Observations | `S02-location-stamp-observations.md` | P1 | 📲 |
| 3 | Location Stamp on Inject Firing | `S03-location-stamp-inject-firing.md` | P2 | 📲 |
| 4 | Director Location Map View | `S04-director-location-map.md` | P2 | 📲 |
| 5 | Post-Exercise Position History | `S05-position-history.md` | P3 | 📲 |

## Acceptance Criteria (Feature-Level)

- [ ] Location sharing is explicitly opt-in per exercise — never enabled by default or assumed
- [ ] Participants see a persistent visual indicator when their location is being shared
- [ ] Location sharing automatically stops when the exercise clock stops
- [ ] Location data is used to enrich observations and provide Director situational awareness, not for surveillance or performance monitoring
- [ ] The feature degrades gracefully when GPS is unavailable (indoors, permissions denied) — core observation and inject workflows are unaffected

## Wireframes/Mockups

### Opt-In Prompt
```
┌─────────────────────────────────────┐
│         📍 Share Your Location?     │
│                                     │
│ Sharing your location helps the     │
│ Exercise Director track team        │
│ coverage and automatically adds     │
│ location context to your            │
│ observations.                       │
│                                     │
│ • Your location is shared only      │
│   during active exercise conduct    │
│ • Only the Exercise Director can    │
│   see participant positions         │
│ • You can stop sharing at any time  │
│                                     │
│  [Not Now]  [Share Location ✓]      │
└─────────────────────────────────────┘
```

### Sharing Active Indicator
```
┌─────────────────────────────────────┐
│ 📍 Location sharing active     [■]  │  ← persistent header bar
├─────────────────────────────────────┤
│ Exercise Conduct View               │
│  ...                                │
```

### Director Map View
```
┌─────────────────────────────────────┐
│ Team Positions           [List] [Map]│
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │         🔵 J.Smith (E)         │ │
│ │                                 │ │
│ │    🟢 K.Lee (C)                │ │
│ │              🔴 INJ-012        │ │
│ │                   🔵 M.Jones(E)│ │
│ │                                 │ │
│ │  [map tiles]                    │ │
│ │                                 │ │
│ │         🟢 R.Davis (C)        │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
│                                     │
│ 🔵 Evaluator  🟢 Controller        │
│ 🟠 Observer   🔴 Active Inject     │
│                                     │
│ 4 of 6 participants sharing         │
│ Last updated: 11:32:15 AM          │
└─────────────────────────────────────┘
```

## Dependencies

- Phase D: Exercise Conduct (exercise clock state controls location sharing lifecycle)
- Phase H: Real-Time Sync (SignalR for location update broadcasting)
- Feature: Enhanced Field Observations (observations entity for location stamping)
- Browser Geolocation API

## Domain Terms

| Term | Definition |
|------|------------|
| Location Sharing | The opt-in feature that allows a participant's GPS position to be tracked and shared with the Exercise Director during active exercise conduct |
| Location Stamp | GPS coordinates (latitude, longitude) and accuracy radius automatically attached to an observation or inject firing event |
| Position Update | A periodic GPS reading sent from a participant's device to the server, used to update the Director's map view |
| Coverage Gap | An area of the exercise venue where no Evaluator or Controller is positioned, potentially meaning response activity in that area goes unobserved |
| Geofence | A virtual geographic boundary that could trigger notifications when a participant enters or exits (future enhancement, not in current scope) |

## Privacy & Consent Framework

This feature handles personal location data and requires careful attention to privacy:

| Principle | Implementation |
|-----------|---------------|
| **Explicit Consent** | Opt-in prompt shown once per exercise. No tracking until accepted. |
| **Transparency** | Persistent visual indicator when sharing. Clear statement of who can see the data. |
| **User Control** | Participant can disable sharing at any time with one tap. |
| **Purpose Limitation** | Location used for exercise situational awareness and observation context only. |
| **Data Minimization** | Position updates every 30-60 seconds, not continuous. Accuracy sufficient for venue-level positioning (not room-level). |
| **Temporal Boundaries** | Sharing auto-stops when exercise clock stops. No tracking outside exercise hours. |
| **Access Control** | Only Exercise Director and Safety Controller see the map. Participants cannot see each other's positions. |
| **Data Retention** | Location history retained with exercise data. Purged when exercise is archived or per organizational data retention policy. |
