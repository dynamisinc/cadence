# Epic: Field Operations

## Vision

Controllers, Evaluators, and Observers operating in the field during an operations-based exercise have rich, fast tools to document what they see — capturing observations with photos and location context — while the Exercise Director maintains real-time situational awareness of where their team is and what they're finding. All field-captured data flows directly into After-Action Review materials, eliminating the gap between what happens during the exercise and what gets documented.

## Business Value

- **Richer AAR evidence** — Photo-documented, geo-located, timestamped observations replace handwritten notes reconstructed from memory hours later
- **Reduced radio traffic** — Exercise Director can see evaluator positions and incoming observations without asking "Where are you? What are you seeing?" over radio
- **Better exercise coverage** — Director can identify coverage gaps (no evaluator near a fired inject's location) and redirect before opportunities are missed
- **Faster field capture** — Evaluators spend less time fighting the tool and more time watching the exercise, increasing observation quality and quantity
- **Safety accountability** — Real-world safety concerns are flagged immediately with photo evidence and location, not buried in post-exercise notes

## Success Metrics

| Metric | Current (Paper/Spreadsheet) | Target (Cadence Field Ops) |
|--------|----------------------------|---------------------------|
| Avg observations per evaluator per exercise | 5-10 (reconstructed post-exercise) | 15-25 (captured in real-time) |
| Time from observation to Director visibility | 30-60 min (post-exercise debrief) | < 30 seconds (real-time sync) |
| Observations with photo evidence | ~5% (personal phone photos, unlinked) | > 40% |
| Observations with location data | 0% | > 80% (for opted-in participants) |
| Safety concerns surfaced during exercise | Inconsistent (verbal radio only) | 100% (flagged and tracked in system) |
| Director awareness of evaluator positions | Radio check-ins every 15-30 min | Continuous (opt-in location sharing) |

## User Personas

| Persona | Description | Key Needs |
|---------|-------------|-----------|
| **Evaluator** | Assigned to observe and assess specific capabilities at a venue location. Often moving between positions. May have limited connectivity. Not necessarily tech-savvy. | Fast capture with minimal taps. Photos that auto-link to observations. Works offline. Doesn't lose data. |
| **Observer** | Watches exercise play without assessment responsibilities. May be a VIP, elected official, or training participant. | Simple note-taking. Photo capture. Read-only view of exercise progress. |
| **Controller** | Delivers injects and manages exercise play in their assigned area. Needs to document what they delivered and how players responded. | Quick observation capture tied to injects they fired. Photo evidence of player response. |
| **Exercise Director** | Oversees entire exercise from Exercise Control (SimCell). Needs big-picture situational awareness. | Real-time observation feed. Evaluator location map. Coverage gap identification. Safety alert visibility. |
| **Safety Controller** | Monitors for real-world safety concerns during exercise play. Must be able to halt exercise if needed. | Immediate visibility of safety-flagged observations. Location of safety concerns. Photo evidence. |

## Features

1. **Photo Capture & Attachment** — Participants capture photos during exercise conduct, attach them to observations, and include optional annotations. Photos compress for bandwidth efficiency and queue for upload when offline.

2. **Enhanced Field Observations** — Fast, field-optimized observation capture with quick-add workflows, voice-to-text input, smart inject linking, and a real-time observation feed for the Exercise Director. Builds on existing Phase E observation infrastructure.

3. **Location Tracking** — Opt-in GPS location sharing for exercise participants. Exercise Director sees a real-time map of team positions. Observations and inject firings are automatically geo-stamped. Supports post-exercise movement analysis for AAR.

## Feature Dependency Map

```
Phase E: Evaluator Observations (existing)
    │
    ├── Feature 1: Photo Capture & Attachment
    │       │
    │       └── Photo Annotation (P2, optional enhancement)
    │
    ├── Feature 2: Enhanced Field Observations
    │       │
    │       ├── Quick-Add Observation
    │       ├── Voice-to-Text Capture
    │       ├── Smart Inject Linking
    │       ├── Safety-Flagged Observations
    │       └── Director Observation Feed
    │
    └── Feature 3: Location Tracking
            │
            ├── Opt-In Location Sharing
            ├── Location Stamp on Observations (depends on Feature 2)
            ├── Director Location Map View
            └── Post-Exercise Position History (P3)
```

## Out of Scope

- **Communication channels / chat** — SME-identified as valuable, but deferred to a separate epic. Field Operations focuses on capture and awareness, not conversation.
- **Video capture** — File sizes and bandwidth requirements make this impractical for field use in the near term. Photos provide sufficient evidence.
- **Automated evaluator assignment based on location** — The system shows where people are; the Director decides how to redirect. No auto-assignment logic.
- **Third-party device integration** — No body cameras, GPS trackers, or external hardware. Browser/PWA capabilities only.
- **Augmented reality overlays** — No AR features for field visualization.
- **Live streaming from field positions** — Bandwidth and complexity make this out of scope.

## Risks & Assumptions

| Risk/Assumption | Mitigation/Validation |
|-----------------|----------------------|
| **Evaluators may resist location tracking due to privacy concerns** | Explicit opt-in per exercise with clear visual indicator. Auto-disable when exercise clock stops. Location data purged on configurable retention schedule. |
| **Field connectivity may be too poor for photo upload** | Client-side photo compression. Offline queue with IndexedDB blob storage. Photos sync when connectivity returns (existing sync architecture). |
| **Camera access may be blocked by device/browser settings** | Graceful degradation — photo attachment is optional, not required. Clear permission prompts with explanation of why camera access is needed. |
| **GPS accuracy varies significantly indoors vs outdoors** | Location is informational, not precision-dependent. Display accuracy indicator. Indoor exercises may have limited location value — document this for users. |
| **Battery drain from GPS tracking may concern field users** | Use significant-change updates (not continuous polling). Document expected battery impact. Allow frequency configuration. |
| **Large photo volumes may create storage cost concerns** | Client-side compression before storage. Thumbnail generation for list views. Azure Blob Storage tiering for older exercise data. Monitor per-exercise storage consumption. |
| **Users may not understand when they are being tracked** | Persistent visual indicator (banner/icon) when location sharing is active. Exercise Director cannot enable tracking for others — each participant controls their own sharing. |
| **Voice-to-text quality varies by device and environment** | Use browser SpeechRecognition API (no cloud dependency). Position as convenience feature, not primary input method. Always allow text editing after voice capture. |

## Implementation Sequence

| Order | Feature | Priority | Rationale |
|-------|---------|----------|-----------|
| 1 | Photo Capture & Attachment | P0 | Net-new capability. High SME demand. Independent of other field features. |
| 2 | Enhanced Field Observations | P0 | Builds on existing Phase E. Improves core workflow for all field roles. |
| 3 | Location Tracking | P1-P2 | Most complex. Benefits from patterns established in Features 1-2. Privacy considerations need careful implementation. |

## Cross-Cutting Concerns

### Offline Support
All three features must work offline leveraging the existing Phase H sync architecture:
- Photos queue as IndexedDB blobs
- Observations (with photo references) queue as pending actions
- Location updates queue and batch-sync on reconnect

### HSEEP Alignment
- Observations follow HSEEP evaluation methodology (P/S/M/U ratings, capability-task alignment)
- Photo evidence supports HSEEP After-Action Report documentation requirements
- Location data supports HSEEP exercise documentation of evaluator coverage

### Responsive Design
- Primary field use is tablet form factor
- All capture workflows must be optimized for touch interaction
- Camera and location features leverage device-native capabilities through browser APIs

### Accessibility
- Voice-to-text provides an alternative input method for observation capture
- Photo attachment is always optional, never blocking
- Location sharing controls must be clearly discoverable and operable

---

## Related Documents

| Document | Location |
|----------|----------|
| Feature: Photo Capture & Attachment | `docs/features/field-operations/photo-capture/` |
| Feature: Enhanced Field Observations | `docs/features/field-operations/enhanced-observations/` |
| Feature: Location Tracking | `docs/features/field-operations/location-tracking/` |
| Phase E: Evaluator Observations (existing) | `docs/features/observations/` |
| Phase H: Real-Time & Offline (existing) | `docs/features/connectivity/` |
| Business Analyst Agent | `.claude/agents/business-analyst-agent.md` |
