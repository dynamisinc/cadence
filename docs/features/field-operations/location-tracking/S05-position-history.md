# Story: Post-Exercise Position History

## S05-position-history

**As an** Exercise Director reviewing exercise performance,
**I want** to replay participant movements on a timeline,
**So that** I can assess evaluator coverage, identify periods where areas were unmonitored, and include geographic analysis in the After-Action Report.

### Context

During the AAR, a key question is: "Did we have adequate evaluator coverage for all exercise objectives?" A position history replay answers this definitively. The Director can scrub through the exercise timeline and see where each evaluator was at any point. Combined with inject firing times and observation timestamps, this creates a powerful visualization: "When INJ-012 (Evacuation Order) fired at 10:45, Evaluators J. Smith and M. Jones were at Building C and the staging area respectively — but nobody was monitoring the evacuation route." This kind of geographic gap analysis is nearly impossible with manual methods and represents a significant differentiator for Cadence. The position history uses the same location data captured during conduct (S01) — no additional data collection required.

### Acceptance Criteria

- [ ] **Given** the exercise has ended and location data was captured, **when** I navigate to the position history view, **then** I see a map with a timeline scrubber at the bottom
- [ ] **Given** I am viewing the position history, **when** I drag the timeline scrubber, **then** participant markers move to their positions at the selected time
- [ ] **Given** I am viewing the position history, **when** I press "Play", **then** the timeline advances automatically (at configurable speed: 1x, 2x, 5x, 10x real-time) and markers animate along their paths
- [ ] **Given** I am playing back the timeline, **when** inject firing events pass on the timeline, **then** inject markers appear on the map at their firing location with a brief animation (pulse or flash)
- [ ] **Given** I am playing back the timeline, **when** observation events pass on the timeline, **then** observation indicators appear briefly at their location
- [ ] **Given** I am viewing the position history, **when** I select a specific participant, **then** their full movement path is drawn on the map as a line with directional indicators
- [ ] **Given** the exercise had periods with no evaluator near a specific area, **when** I review the timeline, **then** I can visually identify time gaps where coverage was absent (no marker in an area for an extended period)
- [ ] **Given** I want to include position data in the AAR, **when** I view the position history, **then** I can capture a screenshot or export a summary of participant coverage statistics (total distance moved, time spent in each area)

### Out of Scope

- Automated coverage gap detection or alerting (the Director reviews visually)
- Integration with venue floor plans or indoor positioning
- Movement speed or distance analytics
- Comparison between planned evaluator assignments and actual positions
- Video export of the timeline replay

### Dependencies

- S01-opt-in-location-sharing (position data must have been captured during conduct)
- S04-director-location-map (map rendering foundation)
- Phase D: Exercise Conduct (exercise clock timeline for the scrubber)
- Phase E: Observations with location stamps

### Open Questions

- [ ] What is the minimum position data density needed for useful replay? (Recommendation: 30-second intervals are sufficient for smooth interpolation. Below 60 seconds, gaps become noticeable.)
- [ ] Should the replay show all participants simultaneously or support selecting individual participants? (Recommendation: both — all by default, with ability to select/deselect individuals)
- [ ] Is coverage statistics export (time in area, distance moved) needed for the AAR, or is visual replay sufficient? (Recommendation: visual replay first, stats as a future enhancement)
- [ ] What playback speeds are useful? (Recommendation: 1x for detailed review, 5x and 10x for overview. Pause and scrub for specific moments.)

### Domain Terms

| Term | Definition |
|------|------------|
| Position History | The complete record of all position updates captured during an exercise, enabling post-exercise timeline replay |
| Timeline Scrubber | A UI control that allows scrubbing through the exercise timeline to view participant positions at any point in time |
| Movement Path | A line drawn on the map showing a participant's full movement history during the exercise |
| Coverage Analysis | The process of reviewing position history to assess whether evaluators were adequately positioned throughout the exercise |

### UI/UX Notes

```
Position History Replay View:

┌─────────────────────────────────────┐
│ Position History          [Export]   │
│ Exercise: Hurricane Response 2026   │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │                                 │ │
│ │      🔵JS ·····→               │ │  ← movement path
│ │               🟢KL             │ │
│ │                    💥12        │ │  ← inject fired here
│ │           ·····→ 🔵MJ         │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
├─────────────────────────────────────┤
│ [◀◀] [▶ Play] [▶▶]  Speed: [5x ▼] │
│ ├──────────●──────────────────────┤ │
│ 09:00    10:45                15:00 │
│           ↑ INJ-012 fired          │
│                                     │
│ Participants:                       │
│ ☑ J.Smith  ☑ M.Jones  ☑ K.Lee     │
│ ☑ R.Davis  ☐ A.Torres             │
└─────────────────────────────────────┘
```

- Timeline scrubber should show inject firing times as tick marks for reference
- Movement paths use the participant's role color with directional arrows
- Path should use subtle transparency to avoid obscuring the map
- Participant checkboxes allow showing/hiding individual paths
- Current time indicator on the scrubber with tooltip showing exact time
- Inject and observation events pulse briefly on the map as the timeline passes them

### Technical Notes

- Position history is a read of the `ParticipantLocation` table filtered by exerciseId, ordered by capturedAt
- Interpolate positions between data points for smooth animation (linear interpolation between 30-second snapshots)
- Timeline scrubber: use a range input or custom draggable component synced to exercise clock range (start time → end time)
- Playback: use `requestAnimationFrame` loop advancing a virtual clock at the selected speed multiplier
- Movement paths: use Leaflet `L.polyline` with the participant's position history as coordinates
- For exercises with many position records (e.g., 6 participants × 4 hours × 2 updates/min = ~2,880 records), fetch in bulk and process client-side
- Consider chunked fetching for very long exercises: load position data for 30-minute windows as the user scrubs
- This feature is post-exercise only — no real-time performance concerns, can be more computation-heavy
