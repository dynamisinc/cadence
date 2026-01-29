# Cadence Feature Backlog & Ideas

> **Purpose**: Capture feature ideas for future consideration. Not committed - just preserved for later evaluation.
> **Last Updated**: 2026-01-24

---

## Post-MVP Candidates

### Control Room - Participant Status
**Priority**: Post-MVP
**Menu Location**: CONDUCT section (currently stubbed)

Real-time dashboard showing connected participants and their activity during exercise conduct.

**Core Concept**:
- Show all assigned participants grouped by role
- Online/offline status indicators
- Last activity timestamp and description
- Useful for Directors ensuring full participation

**Mockup**:
```
┌─────────────────────────────────────────────────────────────┐
│  Control Room - Participants                                 │
├─────────────────────────────────────────────────────────────┤
│  Hurricane Response FSE                                      │
│                                                              │
│  CONTROLLERS (3/3 online)                                    │
│  🟢 Sarah Kim         Last action: Fired INJ-14 (2m ago)    │
│  🟢 Mike Chen         Last action: Viewing Queue            │
│  🟢 Tom Wilson        Last action: Fired INJ-13 (5m ago)    │
│                                                              │
│  EVALUATORS (2/3 online)                                     │
│  🟢 Robert Chen       Last action: Added observation (1m)   │
│  🟢 Lisa Park         Last action: Added observation (3m)   │
│  🔴 James Taylor      Offline since 10:15 AM                │
│                                                              │
│  OBSERVERS (4/4 online)                                      │
│  🟢 All connected                                            │
└─────────────────────────────────────────────────────────────┘
```

**Technical Considerations**:
- SignalR presence tracking
- Activity event logging
- Privacy: How much activity detail to show?

---

## Future Capabilities

### Exercise Communications / Chat
**Priority**: Future
**Complexity**: High

Full communications capability with channels, including role-based restrictions.

**Core Concept**:
- Real-time chat during exercise conduct
- Multiple channels per exercise
- Role-based channel access (e.g., "Controllers Only", "All Staff")
- Backchannel coordination between exercise staff
- Separate from player-facing inject delivery

**Use Cases**:
- Controller asks Director for guidance on stuck players
- Evaluators share emerging themes
- Director broadcasts announcements to all staff
- Private channel for sensitive coordination

**Channel Types**:
| Channel | Access | Purpose |
|---------|--------|---------|
| #general | All staff | Announcements, general coordination |
| #controllers | Controllers, Directors, Admins | Inject coordination |
| #evaluators | Evaluators, Directors, Admins | Observation themes |
| #directors | Directors, Admins | Exercise leadership |

**Technical Considerations**:
- SignalR for real-time messaging
- Message persistence (for AAR review?)
- Offline message queue
- Message threading or flat?
- File/image sharing?
- Integration with notifications

**V2 Requirements Reference**: "Exercise control backchannel communication"

---

### SimCell (Simulation Cell)
**Priority**: Future
**Complexity**: High

Dedicated capability for role-playing external entities during exercises.

**Core Concept**:
In HSEEP exercises, the Simulation Cell (SimCell) consists of staff who role-play external organizations and individuals that players interact with (hospitals, media, elected officials, public, etc.).

**Features**:
- Define SimCell roles per exercise (e.g., "Regional Hospital", "Local Media")
- Assign users to SimCell roles
- Provide scripts/talking points for each role
- Track inbound "calls" from players
- Log SimCell interactions for AAR

**Mockup - SimCell Dashboard**:
```
┌─────────────────────────────────────────────────────────────┐
│  SimCell - My Roles                                          │
├─────────────────────────────────────────────────────────────┤
│  Hurricane Response FSE                                      │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 🏥 Regional Hospital                                    │ │
│  │    Contact: (555) 123-4567                              │ │
│  │    ────────────────────────────────────────────────    │ │
│  │    SCRIPT:                                              │ │
│  │    • Current capacity: 85% (can accept 20 more)        │ │
│  │    • Trauma center: OPEN                                │ │
│  │    • Estimated wait: 2 hours for non-critical          │ │
│  │    ────────────────────────────────────────────────    │ │
│  │    Pending calls: 2                     [View Calls]    │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ 📺 Local Media (WXYZ News)                             │ │
│  │    Contact: media@exercise.local                        │ │
│  │    ────────────────────────────────────────────────    │ │
│  │    SCRIPT:                                              │ │
│  │    • Aggressive but professional                        │ │
│  │    • Push for PIO interview                             │ │
│  │    • Ask about evacuation timeline                      │ │
│  │    ────────────────────────────────────────────────    │ │
│  │    Pending calls: 0                     [View Calls]    │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**Relationship to Injects**:
- Some injects may be "SimCell-delivered" (phone call, email)
- SimCell can trigger ad-hoc interactions based on player actions
- SimCell logs become part of exercise record

**Technical Considerations**:
- SimCell role entity
- Script/talking points management
- Call/interaction logging
- Integration with inject delivery?
- Separate from or part of Comms feature?

---

## Parking Lot

*Ideas mentioned but not yet fleshed out*

| Idea | Source | Notes |
|------|--------|-------|
| Gaming/simulation runs | V2 Requirements | Integration with external simulation systems |
| Multi-channel inject delivery | V2 Requirements | Deliver injects via email, SMS, radio, etc. |
| Inject release approval | V2 Requirements | Workflow for approving inject fires |
| Timeline visualization | V2 Requirements | Visual timeline of exercise events |
| AI-powered inject suggestions | Original brainstorm | ML-based inject recommendations |
| Video analysis integration | Original brainstorm | Link video clips to observations |
| Multi-location coordination | Original brainstorm | Distributed exercise support |
| AAR generation automation | Original brainstorm | Auto-generate After-Action Report |

---

## How to Promote an Idea

When ready to implement a backlog item:

1. Create feature folder: `docs/features/{feature-name}/`
2. Write FEATURE.md with full specification
3. Break into user stories (S01, S02, etc.)
4. Create implementation prompt
5. Remove from this backlog

---

*Document created: 2026-01-24*
