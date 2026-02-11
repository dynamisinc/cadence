# Story: Director Location Map View

## S04-director-location-map

**As an** Exercise Director,
**I want** to see a real-time map showing where my Controllers and Evaluators are positioned,
**So that** I can identify coverage gaps, confirm evaluators are in position before firing injects, and make informed resource allocation decisions without relying on radio check-ins.

### Context

The Exercise Director sits in the Exercise Control room, managing the pace of the exercise: when to fire injects, whether to speed up or slow down scenario progression, and whether the exercise objectives are being achieved. A critical input to these decisions is knowing where the field team is. "Is anyone near Building C to observe the hazmat response?" Currently, the answer requires a radio call and wait for response — disrupting both the Director's workflow and the field team's observation. A map view showing real-time participant positions answers this question at a glance. Combined with inject firing locations, the map paints a live operational picture of the exercise. This is the feature that most directly addresses the SME feedback about "tracking C&E's" — giving the Director the situational awareness they need to run an effective exercise.

### Acceptance Criteria

- [ ] **Given** I am the Exercise Director (or Safety Controller), **when** I navigate to the location map, **then** I see a map centered on the exercise venue area showing position markers for all participants who have opted in to location sharing
- [ ] **Given** participants are sharing their locations, **when** I view the map, **then** each participant appears as a colored marker with their initials or name label, color-coded by role: blue for Evaluators, green for Controllers, orange for Observers
- [ ] **Given** participants are sending position updates, **when** a new position update arrives (every 30 seconds), **then** the marker smoothly transitions to the new position without page refresh
- [ ] **Given** some participants have not opted in to location sharing, **when** I view the map, **then** I see a count of non-sharing participants (e.g., "4 of 6 sharing") but no indication of which specific participants declined
- [ ] **Given** I tap a participant's marker on the map, **when** the info popup appears, **then** I see: participant name, role, last update time, and a count of their observations for this exercise
- [ ] **Given** injects have been fired with location data, **when** I toggle "Show Inject Locations", **then** recently-fired inject locations appear on the map as distinct markers (red burst icon) with inject number labels
- [ ] **Given** I am viewing the map, **when** I use the zoom controls, **then** I can zoom in to building level or zoom out to see the full venue area
- [ ] **Given** a participant's last update is older than 5 minutes, **when** I view their marker, **then** it appears dimmed/faded with a "Last seen: X min ago" label to indicate stale data
- [ ] **Given** the exercise clock is stopped, **when** I view the map, **then** I see the last known positions (frozen state) with a "Clock stopped" indicator
- [ ] **Given** I am on a tablet, **when** I view the map, **then** it is responsive and usable with touch gestures (pinch to zoom, drag to pan)

### Out of Scope

- Satellite or aerial imagery overlays (use standard map tiles)
- Custom venue floor plans or building maps (OpenStreetMap or standard map tiles only)
- Participant-to-participant messaging from the map
- Drawing tools for zones or boundaries on the map
- Routing or navigation between participants
- Offline map caching for the Director (Director is typically in the control room with connectivity)

### Dependencies

- S01-opt-in-location-sharing (participants must be sharing positions)
- S03-location-stamp-inject-firing (for inject location markers)
- Phase H: SignalR (real-time position update streaming)
- Map rendering library (Leaflet.js recommended)

### Open Questions

- [ ] Should the map support custom venue overlays (uploaded floor plans or satellite views)? (Recommendation: defer — adds significant complexity. Standard OpenStreetMap tiles work for most venues. Revisit if users report inadequate map detail.)
- [ ] Should the Director be able to send a message to a specific participant from the map? (Recommendation: defer to Communication Channels epic.)
- [ ] Should the map show a heatmap of observation density? (Recommendation: interesting for AAR but overkill for real-time conduct. Defer to Post-Exercise Position History.)
- [ ] Should the map be a dedicated page or embeddable as a panel alongside the observation feed? (Recommendation: both — a dedicated full-screen view and an embeddable mini-map panel. Start with dedicated page.)

### Domain Terms

| Term | Definition |
|------|------------|
| Director Map View | A real-time map interface showing exercise participant positions, available to the Exercise Director and Safety Controller |
| Position Marker | A colored, labeled icon on the map representing a participant's current or last-known GPS position |
| Stale Position | A participant position that has not been updated in more than 5 minutes, indicated visually as potentially outdated |
| Coverage Gap | An area of the venue where no participant markers are present, suggesting that activity there may go unobserved |
| Inject Location Marker | A distinct map marker showing where a Controller physically fired an inject |

### UI/UX Notes

```
Full-screen Director Map View:

┌─────────────────────────────────────┐
│ Team Positions     🔴 Live  [⚙️]    │
│ 4 of 6 sharing · Updated 11:32 AM  │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │      🔵JS                      │ │
│ │               🟢KL             │ │
│ │                    💥12        │ │
│ │           🔵MJ                 │ │
│ │                                 │ │
│ │     🟢RD                       │ │
│ │                                 │ │
│ │                   🟠AT         │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
├─────────────────────────────────────┤
│ Legend:                              │
│ 🔵 Evaluator  🟢 Controller        │
│ 🟠 Observer   💥 Inject Fired      │
│                                     │
│ [☐ Show Inject Locations]           │
│ [☐ Show Observation Counts]         │
└─────────────────────────────────────┘

Tap participant marker → info popup:

┌───────────────────────┐
│ J. Smith              │
│ Evaluator             │
│ Updated: 11:31:45 AM  │
│ 📝 7 observations     │
│ [View Observations]   │
└───────────────────────┘
```

- Map should fill available screen space with minimal chrome
- Legend should be collapsible on smaller screens
- Participant labels should be concise (initials or first name) to avoid overlapping
- At high zoom levels, show full names
- Stale markers (>5 min) should be visually distinct: semi-transparent, gray tint, or dashed border
- Consider a "Center on All" button to auto-fit all markers in view
- "Live" indicator pulses to show real-time updates are active

### Technical Notes

- Use Leaflet.js with OpenStreetMap tiles: `L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png')`
- Lazy-load Leaflet only when the map view is opened (code-split the map component)
- Participant markers as custom `L.divIcon` with role-colored backgrounds and initials
- Smooth marker movement: use `marker.setLatLng()` with CSS transitions or Leaflet.MovingMarker plugin
- Subscribe to SignalR `PositionUpdated` events: `{ participantId, lat, lng, accuracy, timestamp }`
- Initial map center: use the exercise venue coordinates if available, otherwise center on the first participant's position
- Map state (center, zoom) should persist in component state so switching tabs doesn't reset the view
- For exercises with many participants (20+), consider marker clustering at low zoom levels (Leaflet.markercluster)
- Performance: update markers individually as position events arrive, don't re-render the full participant list
- Consider Azure Maps as an alternative if staying fully within the Azure ecosystem is preferred (requires API key and has usage costs)
