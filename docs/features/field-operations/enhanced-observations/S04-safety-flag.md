# Story: Safety-Flagged Observation

## S04-safety-flag

**As an** Evaluator, Controller, or Observer,
**I want** to flag an observation as a real-world safety concern,
**So that** the Exercise Director and Safety Controller are immediately alerted to a genuine hazard that may require exercise pause or real-world response.

### Context

During an operations-based exercise, real-world safety concerns can and do arise. A simulated building collapse scenario might reveal an actual structural concern. An exercise involving vehicle movement might create a genuine traffic hazard. Wet floors, exposed wiring, heat-related illness among participants, or an actual medical emergency during exercise play — these all require immediate attention from the Safety Controller, not documentation that surfaces hours later in the AAR. HSEEP requires that exercises have a Safety Controller who can pause or halt the exercise using a pre-established "real-world emergency" code word (often "No Duff" in military exercises or the exercise's specific safety phrase). A safety-flagged observation creates a digital parallel to the radio safety call — it ensures the concern is documented, timestamped, photographed if possible, and immediately visible to decision-makers.

### Acceptance Criteria

- [ ] **Given** I am creating or editing an observation, **when** I check the "Safety Concern" checkbox, **then** the observation is visually marked as a safety item with a distinct color (red) and icon (warning triangle)
- [ ] **Given** I save a safety-flagged observation, **when** it syncs to the server, **then** it triggers an immediate notification to the Exercise Director and Safety Controller (via real-time push, not just feed appearance)
- [ ] **Given** I save a safety-flagged observation, **when** it appears in the Director's observation feed, **then** it is pinned to the top of the feed regardless of chronological order and is visually prominent (red background/border)
- [ ] **Given** I save a safety-flagged observation, **when** the Director views it, **then** it includes: the safety description, photos (if attached), location (if available), the reporter's name, and timestamps
- [ ] **Given** a safety-flagged observation has been raised, **when** the Exercise Director reviews it, **then** they can mark it as "Acknowledged" to indicate they have seen it
- [ ] **Given** a safety-flagged observation has been raised, **when** the Exercise Director resolves it, **then** they can mark it as "Resolved" with a resolution note, and it moves from the pinned section to the chronological feed
- [ ] **Given** I am offline when I flag a safety concern, **when** the observation syncs, **then** the safety flag triggers the notification upon server receipt — but the local UI immediately shows the safety visual treatment
- [ ] **Given** I accidentally flag an observation as a safety concern, **when** I edit the observation, **then** I can uncheck the safety flag before saving (but cannot unflag after it has been acknowledged by the Director)
- [ ] **Given** multiple safety-flagged observations exist, **when** the Director views the feed, **then** all unresolved safety items appear in a pinned "Active Safety Concerns" section at the top
- [ ] **Given** the exercise is being reviewed post-conduct, **when** viewing safety-flagged observations, **then** all safety items (including resolved ones) are filterable as a group for safety review documentation

### Out of Scope

- Automatic exercise pause triggered by a safety flag (the Director makes that decision and uses the exercise clock controls)
- SMS or email notification of safety flags (push via SignalR only within the application)
- Safety concern categories or severity levels (free-text description only for now)
- Safety plan documentation or pre-exercise safety briefing features
- Integration with 911 or actual emergency services

### Dependencies

- S01-quick-add-observation (safety checkbox in the observation form)
- S05-director-observation-feed (feed where safety items surface)
- Phase H: Real-Time Sync (SignalR push notification to Director)
- Phase D: Exercise Conduct (exercise clock for potential pause)

### Open Questions

- [ ] Should safety-flagged observations require a photo? (Recommendation: strongly encouraged but not required — the concern might be a smell, sound, or reported symptom that can't be photographed)
- [ ] Should there be an audible alert on the Director's device when a safety flag is received? (Recommendation: yes — a distinct notification sound ensures the Director notices even if not looking at the screen)
- [ ] Can non-Director roles see safety-flagged observations? (Recommendation: yes, visible to all participants but only Director/Safety Controller can acknowledge/resolve)
- [ ] Should there be a dedicated "Safety" button separate from the observation form for truly urgent concerns? (Recommendation: defer — the safety checkbox in the observation form is sufficient for initial implementation. A dedicated SOS-style button adds complexity.)
- [ ] What is the correct HSEEP terminology for safety events during exercises? (Need SME input — "real-world emergency," "safety incident," "no-duff" are all used in different contexts)

### Domain Terms

| Term | Definition |
|------|------------|
| Safety-Flagged Observation | An observation marked as a genuine, real-world safety concern (not exercise play) requiring immediate attention from exercise leadership |
| Safety Controller | The designated individual responsible for monitoring real-world safety during exercise conduct. Has authority to recommend exercise pause or halt. |
| Acknowledged | A safety concern that the Exercise Director has seen and is aware of, but has not yet been resolved |
| Resolved | A safety concern that has been addressed, with a resolution note documenting what action was taken |
| No-Duff (or equivalent) | A code word used in exercises to indicate that a communication is about a real-world event, not exercise play |

### UI/UX Notes

```
Safety checkbox in observation form:

┌─────────────────────────────────────┐
│ ⚠️ ☑ This is a real-world safety   │
│      concern (not exercise play)    │
└─────────────────────────────────────┘

When checked, form gets visual treatment:

┌─────────────────────────────────────┐
│ ⚠️ SAFETY CONCERN             ✕    │
│ ─────────────────────────────────── │
│ ┌─────────────────────────────────┐ │
│ │ Describe the safety concern 🎤  │ │ ← red-tinted border
│ │                                 │ │
│ └─────────────────────────────────┘ │
│                                     │
│ 📷 [Add Photo — Recommended]       │
│                                     │
│       [Cancel]  [Report Safety ⚠️]  │
└─────────────────────────────────────┘

Director notification:

┌─────────────────────────────────────┐
│ ⚠️ SAFETY CONCERN REPORTED          │
│ From: K. Lee · 11:32 AM            │
│ Wet floor near electrical panel     │
│ in Building C west corridor         │
│ 📷 2 photos attached               │
│ 📍 Building C, West Wing           │
│                                     │
│ [View Details]  [Acknowledge ✓]     │
└─────────────────────────────────────┘

Active Safety Concerns (pinned in feed):

┌─────────────────────────────────────┐
│ ⚠️ ACTIVE SAFETY CONCERNS (2)      │
├─────────────────────────────────────┤
│ 🔴 Wet floor near electrical panel  │
│    K.Lee · 11:32 AM · Acknowledged  │
│    [View] [Resolve]                 │
├─────────────────────────────────────┤
│ 🔴 Participant reporting dizziness  │
│    M.Jones · 11:45 AM · NEW        │
│    [View] [Acknowledge] [Resolve]   │
└─────────────────────────────────────┘
```

- Safety checkbox should be clearly distinguished from other form elements — use red color and warning icon
- When safety is checked, the entire form takes on a red-tinted visual treatment to reinforce the significance
- Save button text changes to "Report Safety" to confirm intent
- Director notification should be modal or prominent banner — not just a feed item that could be missed
- Consider browser Notification API for push notification if the Director's tab is not focused
- Resolved safety items show a green "Resolved" badge with the resolution note visible on expansion

### Technical Notes

- Safety flag stored as boolean on observation entity: `isSafetyFlag: boolean`
- Safety status enum on observation: `null | Reported | Acknowledged | Resolved`
- Resolution note stored as: `safetyResolution: string | null`, `safetyResolvedAt: DateTime | null`, `safetyResolvedBy: participantId | null`
- SignalR notification: broadcast a `SafetyConcernReported` event to Exercise Director and Safety Controller roles specifically
- Consider a browser notification via `Notification.requestPermission()` + `new Notification(...)` for when the Director's tab is in the background
- Audible alert: use Web Audio API or a simple `<audio>` element with a distinct alert sound file
- Safety observations should be included in a dedicated section of the AAR export
