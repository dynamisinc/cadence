# Story: Opt-In Location Sharing

## S01-opt-in-location-sharing

**As an** Evaluator, Controller, or Observer,
**I want** to choose whether to share my location during an exercise,
**So that** I maintain control over my personal data while optionally helping the Exercise Director track team coverage.

### Context

Location tracking is inherently sensitive. Exercise participants — especially those from external agencies, volunteer organizations, or elected office — may be uncomfortable with position tracking. The opt-in model ensures nobody is tracked without explicit consent. The prompt appears when a participant first enters an active exercise, clearly explains what location sharing does and who can see it, and respects whatever choice the participant makes. Participants who decline can still use every other feature of Cadence normally. Those who opt in see a persistent indicator reminding them that sharing is active, and can disable it at any time with a single tap. Location sharing automatically stops when the exercise clock stops, ensuring no accidental tracking outside exercise hours.

### Acceptance Criteria

- [ ] **Given** I am a participant joining an active exercise (clock running), **when** I enter the exercise conduct view for the first time, **then** I see a location sharing opt-in prompt
- [ ] **Given** I see the opt-in prompt, **when** I read it, **then** it clearly explains: what is shared (your approximate location), who can see it (Exercise Director only), when it's active (only during exercise conduct), and how to stop (one tap at any time)
- [ ] **Given** I see the opt-in prompt, **when** I tap "Share Location", **then** the browser's native geolocation permission dialog appears (if not already granted)
- [ ] **Given** I grant browser geolocation permission and confirm opt-in, **when** sharing begins, **then** a persistent indicator appears in the app header showing "📍 Location sharing active" with a toggle or stop button
- [ ] **Given** I see the opt-in prompt, **when** I tap "Not Now", **then** the prompt dismisses and location sharing is not enabled — I can use all other app features normally
- [ ] **Given** I declined location sharing, **when** I change my mind later, **then** I can enable sharing from my exercise profile or settings (a "Share Location" toggle accessible from the exercise menu)
- [ ] **Given** location sharing is active, **when** I tap the persistent indicator or toggle, **then** sharing stops immediately and the indicator disappears
- [ ] **Given** location sharing is active, **when** the exercise clock stops (paused or ended), **then** location sharing stops automatically and the indicator disappears
- [ ] **Given** the exercise clock resumes after a pause, **when** I was sharing before the pause, **then** sharing resumes automatically without re-prompting (my preference is preserved for this exercise)
- [ ] **Given** location sharing is active, **when** my device sends position updates, **then** updates are sent every 30 seconds while the exercise is running
- [ ] **Given** I have opted in to sharing, **when** the browser cannot obtain my location (GPS unavailable, indoors, permissions revoked at OS level), **then** I see a subtle indicator that location is unavailable but the app continues functioning normally
- [ ] **Given** I start a different exercise, **when** I enter the conduct view, **then** I am prompted for location sharing again (opt-in is per-exercise, not global)

### Out of Scope

- Admin or Director ability to require location sharing (always participant-controlled)
- Location sharing outside of active exercise conduct
- Location accuracy configuration (use browser defaults)
- Location sharing history or audit log for participants
- Push notification to participants reminding them to enable sharing

### Dependencies

- Phase D: Exercise Conduct (exercise clock state for auto-stop)
- Browser Geolocation API (`navigator.geolocation.watchPosition`)
- Phase H: SignalR (for transmitting position updates to server)

### Open Questions

- [ ] Should the opt-in prompt re-appear on each exercise session, or remember the choice for the duration of the exercise? (Recommendation: remember for the exercise duration — prompt once, preference persists until exercise ends or participant explicitly changes it)
- [ ] Should the position update interval (30 seconds) be configurable by the Director? (Recommendation: not initially — 30 seconds balances battery life and situational awareness. Revisit if users request more/less frequent updates.)
- [ ] Should participants who are sharing see their own position on a mini-map? (Recommendation: defer — focus on Director map view first. Self-positioning adds UI complexity without clear exercise value.)
- [ ] What happens when a participant's device goes to sleep? (Recommendation: the Geolocation API may stop in the background depending on browser/OS. Document this limitation. The PWA service worker may help maintain wakefulness.)

### Domain Terms

| Term | Definition |
|------|------------|
| Location Sharing Opt-In | A per-exercise consent decision by a participant to share their GPS position with the Exercise Director |
| Persistent Sharing Indicator | A visible UI element (header bar or icon) that remains on screen whenever location sharing is active, providing constant transparency |
| Position Update | A GPS coordinate reading sent from the participant's device to the server at regular intervals (every 30 seconds) |
| Sharing Preference | The participant's opt-in/opt-out choice for a specific exercise, preserved across clock pauses but reset for new exercises |

### UI/UX Notes

```
Opt-in prompt (modal):

┌─────────────────────────────────────┐
│         📍 Share Your Location?     │
│                                     │
│ Help your Exercise Director track   │
│ team coverage during the exercise.  │
│                                     │
│ ✓ Shared only during active conduct │
│ ✓ Only the Director can see your    │
│   position                          │
│ ✓ Stop sharing any time with one    │
│   tap                               │
│ ✓ Automatically stops when the      │
│   exercise ends                     │
│                                     │
│  [Not Now]  [Share Location 📍]     │
└─────────────────────────────────────┘

Persistent indicator (header bar):

Active:
┌─────────────────────────────────────┐
│ 📍 Sharing location          [Stop] │
├─────────────────────────────────────┤

Unavailable (GPS issues):
┌─────────────────────────────────────┐
│ 📍 Location unavailable        [ℹ] │
├─────────────────────────────────────┤

Settings toggle (exercise menu):
┌─────────────────────────────────────┐
│ Exercise Settings                   │
│                                     │
│ Location Sharing     [━━━━━━●] ON   │
│ Sharing with Exercise Director      │
└─────────────────────────────────────┘
```

- Opt-in prompt should use a non-dismissable modal (must choose "Not Now" or "Share Location" — no background tap to dismiss) to ensure explicit choice
- Persistent indicator should be compact but always visible — a thin bar below the app header
- "Stop" button on the indicator should be a single tap with no confirmation dialog (friction-free to disable)
- When GPS is unavailable, show a muted indicator rather than an error dialog
- The opt-in prompt should feel inviting, not alarming — use positive language and clear bullet points

### Technical Notes

- Use `navigator.geolocation.watchPosition()` with options: `{ enableHighAccuracy: false, maximumAge: 30000, timeout: 10000 }`
- `enableHighAccuracy: false` uses network-based positioning which is faster and more battery-friendly than GPS-only
- Position update throttle: ignore updates more frequent than every 30 seconds client-side
- Send position updates via SignalR: `hubConnection.invoke('UpdatePosition', { lat, lng, accuracy, timestamp })`
- Server stores in `ParticipantLocation` table: `{ id, exerciseId, participantId, latitude, longitude, accuracy, capturedAt, scenarioTime }`
- Opt-in preference stored client-side per exercise (localStorage or IndexedDB): `{ exerciseId, sharingEnabled: boolean }`
- Auto-stop: subscribe to exercise clock state changes — when clock stops, call `navigator.geolocation.clearWatch(watchId)`
- Handle permission denial: catch `GeolocationPositionError.PERMISSION_DENIED` and update UI to "unavailable" state
- Battery consideration: 30-second interval with `enableHighAccuracy: false` has minimal battery impact (< 5% per hour based on web app benchmarks)
