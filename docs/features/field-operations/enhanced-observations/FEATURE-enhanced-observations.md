# Feature: Enhanced Field Observations

**Parent Epic:** Field Operations

## Description

Field-optimized observation capture workflows that prioritize speed and minimal interaction for Evaluators, Controllers, and Observers working in the field during exercise conduct. Includes quick-add observation entry, voice-to-text input for hands-free capture, smart linking to recently-fired injects, safety flagging for real-world concerns, and a real-time observation feed that gives the Exercise Director live situational awareness of what the field team is documenting. Builds on and enhances the existing Phase E Evaluator Observations infrastructure.

## User Stories

| # | Story | File | Priority | Status |
|---|-------|------|----------|--------|
| 1 | Quick-Add Observation | `S01-quick-add-observation.md` | P0 | 📲 |
| 2 | Voice-to-Text Observation Input | `S02-voice-to-text.md` | P1 | 📲 |
| 3 | Link Observation to Active Inject | `S03-link-to-active-inject.md` | P0 | 📲 |
| 4 | Safety-Flagged Observation | `S04-safety-flag.md` | P1 | 📲 |
| 5 | Director Observation Feed | `S05-director-observation-feed.md` | P1 | 📲 |

## Acceptance Criteria (Feature-Level)

- [ ] An Evaluator can create a complete observation (text, rating, inject link) in under 30 seconds from any screen within an active exercise
- [ ] Voice input works without network connectivity (browser-native speech recognition)
- [ ] Recently-fired injects surface automatically when creating an observation, reducing manual search
- [ ] Safety concerns are visually distinct and immediately visible to the Exercise Director
- [ ] The Exercise Director can view a real-time stream of incoming observations without refreshing

## Wireframes/Mockups

### Quick-Add Observation (Minimal Form)
```
┌─────────────────────────────────────┐
│ Quick Observation               ✕   │
├─────────────────────────────────────┤
│                                     │
│ ┌─────────────────────────────────┐ │
│ │ What did you observe?       🎤  │ │
│ │                                 │ │
│ │ [text area - 3 lines visible]   │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Rating: [P] [S] [M] [U] [N/A]      │
│                                     │
│ Inject: [Hurricane Warning ▼]       │
│         Recently fired (3):         │
│         • INJ-012 Evacuation Order  │
│         • INJ-011 Road Closure      │
│         • INJ-010 Shelter Activation│
│                                     │
│ ⚠️ □ Safety Concern                 │
│                                     │
│ 📷 [Add Photo]                      │
│                                     │
│         [Cancel]  [Save]            │
└─────────────────────────────────────┘
```

### Director Observation Feed
```
┌─────────────────────────────────────┐
│ Live Observations       🔴 12 new   │
├─────────────────────────────────────┤
│ Filter: [All ▼] [All Ratings ▼]    │
├─────────────────────────────────────┤
│ ┌─────────────────────────────────┐ │
│ │ ⚠️ SAFETY · 11:32 AM · K.Lee    │ │
│ │ Wet floor near electrical panel │ │
│ │ in Building C west corridor     │ │
│ │ 📷 2 photos  · 📍 Bldg C West  │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🟡 PARTIALLY MET · 11:28 AM    │ │
│ │ J.Smith · INJ-012              │ │
│ │ Evacuation order communicated   │ │
│ │ but not all floors notified     │ │
│ │ 📷 1 photo                      │ │
│ └─────────────────────────────────┘ │
│ ┌─────────────────────────────────┐ │
│ │ 🟢 PERFORMED · 11:15 AM        │ │
│ │ M.Jones · INJ-011              │ │
│ │ Road closure barriers deployed  │ │
│ │ within 6 minutes of order       │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

## Dependencies

- Phase E: Evaluator Observations (observation entity, basic CRUD)
- Phase D: Exercise Conduct (exercise clock, inject firing — for inject linking)
- Phase H: Real-Time Sync (SignalR — for Director observation feed)
- Feature: Photo Capture & Attachment (photo integration in observation form)

## Domain Terms

| Term | Definition |
|------|------------|
| Quick-Add Observation | A streamlined observation form optimized for field speed: text, rating, and inject link on a single screen with minimal required fields |
| Safety-Flagged Observation | An observation marked as a real-world safety concern (not exercise play) requiring immediate attention from the Exercise Director and Safety Controller |
| Observation Feed | A real-time, reverse-chronological stream of incoming observations visible to the Exercise Director, updating automatically as field participants submit observations |
| Recently-Fired Inject | An inject whose status changed to "Fired" within the last 30 minutes of exercise time, surfaced as a quick-link option when creating observations |
| Voice-to-Text | Browser-native speech recognition (SpeechRecognition API) used to convert spoken observation notes into text without cloud dependency |
