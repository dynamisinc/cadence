# Story: Director Observation Feed

## S05-director-observation-feed

**As an** Exercise Director,
**I want** a real-time feed showing all observations as they come in from the field,
**So that** I have continuous situational awareness of what my evaluation team is finding and can make informed decisions about exercise pacing and resource allocation.

### Context

The Exercise Director typically operates from the Exercise Control room (SimCell) and depends on radio communication to understand what's happening in the field. Radio provides brief, sporadic updates: "Evaluator 3 reporting — triage area set up at Building C." The Director gets fragments, not the full picture. A real-time observation feed transforms the Director's situational awareness from radio snapshots to a continuous, documented stream. They can see not just what's being observed, but how it's being rated, what photos support it, and which injects it relates to. This enables better exercise pacing decisions: "Evaluators are reporting good coverage at Building C, but nothing from the staging area — let me check if anyone is positioned there before firing the next inject." The feed becomes the Director's primary tool for monitoring exercise quality in real-time.

### Acceptance Criteria

- [ ] **Given** I am the Exercise Director (or have the Director role), **when** I navigate to the observation feed, **then** I see a reverse-chronological list of all observations submitted by all participants in the exercise
- [ ] **Given** I am viewing the observation feed, **when** a new observation is submitted by any participant, **then** it appears at the top of the feed within 5 seconds (via SignalR real-time push) with a subtle highlight animation
- [ ] **Given** I am viewing the observation feed, **when** a new observation arrives, **then** I see a count badge (e.g., "3 new") if I have scrolled down, with a "Jump to top" action to see new items
- [ ] **Given** I am viewing the observation feed, **when** I see an observation, **then** each entry shows: participant name, timestamp (scenario time), rating badge (color-coded P/S/M/U), observation text (truncated to 2 lines), linked inject number (if any), photo count indicator, safety flag indicator, and location (if available)
- [ ] **Given** I am viewing the observation feed, **when** I tap an observation entry, **then** I see the full observation detail including complete text, all attached photos, and linked inject details
- [ ] **Given** I am viewing the observation feed, **when** I use the filter controls, **then** I can filter by: participant (evaluator), rating (P/S/M/U/N/A), safety flag only, has photos, linked inject, and time range
- [ ] **Given** the observation feed has active safety-flagged observations, **when** I view the feed, **then** unresolved safety items are pinned in an "Active Safety Concerns" section at the top, above the chronological feed
- [ ] **Given** no observations have been submitted yet, **when** I view the feed, **then** I see an empty state: "No observations yet. Observations will appear here in real-time as your field team submits them."
- [ ] **Given** the exercise is not active (clock not running), **when** I view the observation feed, **then** I see all previously submitted observations in read-only mode (feed does not show "live" indicators)
- [ ] **Given** I am viewing the observation feed on a tablet, **when** I view the layout, **then** it is optimized for a side-panel or full-screen view that I can monitor while managing other exercise functions

### Out of Scope

- Director commenting on or replying to observations (communication feature, separate epic)
- Reassigning observations between evaluators
- Observation approval workflow (observations are submitted directly, not reviewed before appearing)
- Analytics or summary statistics within the feed (separate metrics/reporting story)
- Observation feed for non-Director roles (Evaluators see their own observations list; Controllers see their own)

### Dependencies

- Phase E: Observation entity and list view
- Phase H: SignalR real-time sync (observation broadcast events)
- S01-quick-add-observation (observations exist to populate the feed)
- S04-safety-flag (safety items pinned in feed)

### Open Questions

- [ ] Should the feed auto-scroll to show new observations, or require manual scroll-to-top? (Recommendation: show "X new" badge, don't auto-scroll — the Director may be reading an older observation when a new one arrives)
- [ ] Should the Director be able to "star" or "flag for AAR" specific observations from the feed? (Recommendation: yes, good feature — but separate story. Keep feed read-only for this story.)
- [ ] Should the feed include draft observations (from Quick Photo), or only completed ones? (Recommendation: include all, with draft status clearly indicated — the photo evidence alone may be valuable for situational awareness)
- [ ] Should the feed be accessible to Safety Controllers as well? (Recommendation: yes — Safety Controllers need the same situational awareness, especially for safety-flagged items)
- [ ] What is the expected volume? (Estimate: 5-10 evaluators × 2-4 observations/hour = 10-40 observations per exercise hour. Feed should handle up to 200 observations without performance issues.)

### Domain Terms

| Term | Definition |
|------|------------|
| Observation Feed | A real-time, reverse-chronological stream of all observations submitted during the exercise, visible to the Exercise Director |
| Feed Entry | A single observation displayed in the feed, showing summary information with expandable detail |
| Pinned Safety Concern | A safety-flagged observation that remains at the top of the feed until resolved, regardless of when it was submitted |
| New Observation Indicator | A visual count badge showing how many new observations have arrived since the Director last scrolled to the top of the feed |

### UI/UX Notes

```
Director Observation Feed (full-screen view):

┌─────────────────────────────────────┐
│ 🔴 Live Observations    [Filter ▼]  │
│ 32 total · 3 new                    │
├─────────────────────────────────────┤
│ ⚠️ ACTIVE SAFETY CONCERNS (1)      │
│ ┌─────────────────────────────────┐ │
│ │ ⚠️ Wet floor, electrical panel   │ │
│ │ K.Lee · 11:32 · Acknowledged    │ │
│ │ [View Details]  [Resolve]       │ │
│ └─────────────────────────────────┘ │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ 🟡 PARTIALLY MET  11:28 AM     │ │
│ │ J.Smith                         │ │
│ │ INJ-012 · Evacuation Order      │ │
│ │ Evacuation communicated but     │ │
│ │ not all floors notified...      │ │
│ │ 📷 1  📍 Bldg C                 │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🟢 PERFORMED  11:15 AM         │ │
│ │ M.Jones                         │ │
│ │ INJ-011 · Road Closure          │ │
│ │ Road closure barriers deployed  │ │
│ │ within 6 minutes of order...    │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🟡 DRAFT  11:10 AM              │ │
│ │ R.Davis                         │ │
│ │ 📷 Quick photo — details pending│ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🔴 UNSATISFACTORY  11:05 AM    │ │
│ │ J.Smith                         │ │
│ │ INJ-010 · Shelter Activation    │ │
│ │ Shelter not opened within 30... │ │
│ │ 📷 3                            │ │
│ └─────────────────────────────────┘ │
│                                     │
│ ──── Earlier observations ────      │
│ [Load more]                         │
└─────────────────────────────────────┘

"New" badge when scrolled down:

┌─────────────────────────────────────┐
│ ┌─────────────────────────────────┐ │
│ │    ↑ 3 new observations         │ │ ← floating badge
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🟢 PERFORMED  10:45 AM         │ │
│ │ ...                             │ │
```

Rating color coding:
- 🟢 Performed = green
- 🔵 Satisfactory = blue
- 🟡 Marginal / Partially Met = yellow/amber
- 🔴 Unsatisfactory = red
- ⚪ N/A or unrated = gray
- 🟡 Draft = yellow outline (dimmed)

- Feed entries should be cards with left-border color matching the rating
- Photo indicator shows camera icon + count, tappable to preview
- Location indicator shows pin icon + brief location text
- Filter panel slides in from right or drops down from filter button
- "Load more" at bottom for pagination (initial load: 50 observations, load 25 more on demand)
- Consider a split-view layout on wider screens: feed on left, observation detail on right

### Technical Notes

- SignalR hub event: `ObservationCreated`, `ObservationUpdated` — broadcast to all connected clients with Director or SafetyController role
- Feed API endpoint: `GET /api/exercises/{id}/observations?sort=desc&page=1&pageSize=50&participant=&rating=&safetyOnly=&hasPhotos=`
- Client maintains a local feed list, prepending new observations received via SignalR without full refetch
- "New" badge count: track `lastSeenTimestamp` client-side, count observations newer than that timestamp
- Safety items query separately or flagged in the response for pinned section rendering
- Feed pagination: cursor-based using the oldest observation's timestamp, not offset-based (avoids issues with real-time inserts)
- Consider virtual scrolling (react-window or similar) if feed grows beyond 200 items for DOM performance
