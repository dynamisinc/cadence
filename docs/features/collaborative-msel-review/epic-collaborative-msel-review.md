# Epic: Collaborative MSEL Review

## Vision

Exercise planners can conduct live, structured MSEL reviews during planning conferences (Initial Planning Conference, Mid-Planning Conference, Final Planning Conference) with all stakeholders participating — in person or remotely — using Cadence as the single authoritative source. Comments are captured, conflicts are surfaced, changes are made in real time, and a complete audit trail supports HSEEP documentation requirements without any post-meeting reconciliation of spreadsheets or notes.

---

## Business Value

- Eliminates the "screenshared Excel" problem that characterizes nearly every MPC today
- Produces a built-in HSEEP-compliant change history with no additional effort
- Enables async pre-review before planning conferences, making the meetings themselves more productive
- Surfaces inject conflicts automatically — timing overlaps, dependency violations, player overload
- Provides a clear "lock for conduct" handoff that prevents accidental edits once the MSEL is approved
- Differentiates Cadence from EXIS and every other known tool in the market — no competitor supports this workflow

## Success Metrics

| Metric | Current State | Target |
|--------|---------------|--------|
| Post-MPC reconciliation time | Manual (1–4 hrs) | Zero — Cadence is the record |
| Change traceability | None / email threads | Full audit trail per inject |
| Reviewer participation friction | Download/email/comment in email | Browser link, no install |
| Conflict detection | Manual / missed | Automated flags before the meeting |

---

## User Personas

| Persona | Description | Key Needs |
|---------|-------------|-----------|
| Exercise Director | Owns the MSEL; approves all changes | See all comments, approve/reject changes, lock the MSEL |
| Planner / Lead Controller | Manages the MSEL on behalf of the Director | Apply edits live during the conference, resolve comments |
| Subject Matter Expert (SME) | Partner agency representative; reviews injects in their lane | Comment on specific injects without editing |
| Controller | Owns specific injects during conduct; reviews for feasibility | Flag injects as problematic, suggest changes |
| Evaluator | Reviews inject-objective alignment | Comment on capability gaps, suggest objective linkages |
| Observer | Situational awareness only | Read-only view during conference |

---

## Features

1. **MSEL Presentation Mode** — Full-screen, phase-grouped, distraction-free view for projecting during a conference
2. **Inject Comment Threads** — Threaded comments on individual injects with type classification
3. **Live Collaborative Editing** — Real-time MSEL edits visible to all participants (SignalR)
4. **Review Workflow** — Comment resolution lifecycle: Open → In Review → Resolved / Deferred
5. **Async Pre-Review** — Share a review link before the conference for participants to pre-load comments
6. **Conflict Detection** — Automated flagging of timing, dependency, and player-overload conflicts
7. **MSEL Lock / Freeze** — Director explicitly locks the MSEL for conduct; prevents edits post-approval
8. **Review Audit Trail** — Complete history of who changed what, when, and in response to which comment

---

## Out of Scope

- Full document co-authoring (this is not Google Docs — Cadence is an exercise tool, not a word processor)
- Video/audio conferencing (Cadence does not replace Teams/Zoom; it sits alongside it)
- External guest accounts (reviewers must be Cadence users within the organization; guest/link-only access is a future consideration)
- Version branching (changes are linear; rollback is manual by the Director)
- Automated change acceptance from comments (a planner must explicitly apply any change)

---

## Risks & Assumptions

| Risk / Assumption | Mitigation / Validation |
|-------------------|------------------------|
| Users expect Google Docs-level co-authoring | Scope clearly to comment-and-apply model; set expectations in UX copy |
| Conflict detection false positives erode trust | Make conflict flags dismissible with a reason; never block — only warn |
| "Lock" is too rigid — Directors want to keep tweaking | Add a "Request unlock" workflow so changes after lock are intentional and logged |
| SignalR at scale in a large MPC (50+ participants) | Initial user base is small; architect for fan-out but don't over-engineer v1 |
| SMEs resist learning a new tool just for one meeting | Presentation mode and read/comment access must be frictionless — no training required |

---

---

# Feature 1: MSEL Presentation Mode

**Parent Epic:** Collaborative MSEL Review

## Description

A full-screen, read-only view of the MSEL optimized for projection onto a conference room screen or sharing via screen share. Injects are displayed in a clean, phase-grouped layout with status indicators, scheduled times, and owning controller visible at a glance. The facilitator can navigate by phase, zoom to a specific inject, and highlight the currently-discussed inject for the room.

## User Stories

1. [ ] Enter/exit Presentation Mode from the MSEL view — *Ready*
2. [ ] Navigate MSEL by phase in Presentation Mode — *Ready*
3. [ ] Highlight active inject for room focus — *Ready*

---

## Story 1.1: Enter and Exit Presentation Mode

**As an** Exercise Director or Planner,
**I want** to switch the MSEL into a full-screen presentation view,
**So that** I can project it during a planning conference without UI chrome distracting participants.

### Context

During MPCs the facilitator typically screenshares. The standard Cadence MSEL view has navigation, toolbars, and edit controls that are noise during a read-only review session. Presentation Mode removes all of that and optimizes for readability at a distance.

### Acceptance Criteria

- [ ] **Given** I am on the MSEL view with Director or Planner role, **when** I click "Present", **then** the view enters full-screen and hides all navigation chrome, toolbars, and sidebars
- [ ] **Given** I am in Presentation Mode, **when** I press Escape or click "Exit Presentation", **then** the normal MSEL view is restored
- [ ] **Given** I am in Presentation Mode, **when** another user makes an edit, **then** my view updates in real time (SignalR)
- [ ] **Given** I am an Observer role, **when** I navigate to the MSEL, **then** I only see a read-only view functionally equivalent to Presentation Mode (no edit controls)

### Out of Scope
- Presenter notes or speaker view (not an exercise tool need)
- Automatic slideshow / timed progression

### UI/UX Notes

```
┌─────────────────────────────────────────────────────────────┐
│  HURRICANE RESPONSE EXERCISE 2026  │  MSEL REVIEW  │ [Exit] │
├─────────────────────────────────────────────────────────────┤
│  Phase: Initial Response                          [◀] [▶]   │
│                                                             │
│  #   Time     Description               Owner    Status    │
│  ─── ──────── ──────────────────────── ──────── ──────── │
│  01  H+0:15   Initial 911 call received  Smith   PENDING  │
│  02  H+0:30   EOC activation ordered     Jones   PENDING  │
│  ...                                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Story 1.2: Navigate MSEL by Phase in Presentation Mode

**As a** Meeting Facilitator,
**I want** to move between phases of the MSEL during a review conference,
**So that** I can guide the group through the MSEL in a structured, phase-by-phase sequence.

### Acceptance Criteria

- [ ] **Given** I am in Presentation Mode, **when** I click the forward arrow or press the right arrow key, **then** the view advances to the next phase
- [ ] **Given** I am in Presentation Mode, **when** I click the back arrow or press the left arrow key, **then** the view returns to the previous phase
- [ ] **Given** I am in Presentation Mode, **when** I click a phase name in the phase navigation bar, **then** the view jumps directly to that phase
- [ ] **Given** I am on the last phase, **when** I click the forward arrow, **then** the forward arrow is disabled

---

## Story 1.3: Highlight Active Inject

**As a** Meeting Facilitator,
**I want** to highlight a specific inject so the room's attention is focused on it,
**So that** discussion is clearly anchored to a single item at a time.

### Acceptance Criteria

- [ ] **Given** I am in Presentation Mode, **when** I click on an inject row, **then** it is visually highlighted (distinct background, border, or overlay) and all other rows dim
- [ ] **Given** an inject is highlighted, **when** I click it again or click elsewhere, **then** the highlight is cleared
- [ ] **Given** an inject is highlighted, **when** a comment is added to that inject by any participant, **then** a comment indicator appears on the row in real time

---

---

# Feature 2: Inject Comment Threads

**Parent Epic:** Collaborative MSEL Review

## Description

Any authorized participant can attach a threaded comment to a specific inject. Comments are typed, and the author classifies each comment to help planners prioritize their response. The comment panel is visible alongside the inject detail without navigating away. Comments persist as part of the MSEL review record.

## User Stories

1. [ ] Add a comment to an inject — *Ready*
2. [ ] Classify a comment by type — *Ready*
3. [ ] Reply to an existing comment — *Ready*
4. [ ] View all comments for an inject — *Ready*
5. [ ] View all open comments across the MSEL — *Ready*

---

## Story 2.1: Add a Comment to an Inject

**As a** Reviewer (any role with MSEL read access),
**I want** to add a comment to a specific inject,
**So that** my feedback is captured against the exact inject it refers to without interrupting the facilitator.

### Context

During an MPC, not everyone should shout comments out loud — remote participants especially need a parallel channel. Comments also capture feedback from pre-review sessions before the meeting starts.

### Acceptance Criteria

- [ ] **Given** I have MSEL read access, **when** I click on an inject and then click "Add Comment", **then** a comment input panel opens
- [ ] **Given** the comment input is open, **when** I type text and click "Post", **then** the comment is saved and appears in the inject's comment thread
- [ ] **Given** I post a comment, **when** any participant views that inject, **then** they see a comment indicator badge on the inject row
- [ ] **Given** I post a comment, **when** it is saved, **then** it records my name, role, and timestamp automatically
- [ ] **Given** I am in read-only / Observer role, **when** I view an inject, **then** I can see existing comments but the "Add Comment" action is not available

### Domain Terms

| Term | Definition |
|------|------------|
| Comment | A participant-authored note attached to a specific inject during a review session |
| Review Session | A designated planning conference event (IPC, MPC, FPC) during which the MSEL is formally reviewed |

---

## Story 2.2: Classify a Comment by Type

**As a** Reviewer,
**I want** to classify my comment as a Question, Suggested Change, Conflict Flag, or Approval,
**So that** planners can triage and prioritize responses efficiently.

### Acceptance Criteria

- [ ] **Given** I am composing a comment, **when** I view the comment form, **then** I see a required Type selector with options: **Question**, **Suggested Change**, **Conflict**, **Approved**
- [ ] **Given** I select "Conflict", **when** I post the comment, **then** the inject row displays a conflict indicator icon in all views
- [ ] **Given** I select "Approved", **when** I post the comment, **then** the inject row displays a green approval badge
- [ ] **Given** an inject has comments of multiple types, **when** I view the inject row, **then** the most severe indicator is shown (Conflict > Suggested Change > Question > Approved)

### Comment Type Definitions

| Type | When to Use |
|------|-------------|
| Question | Seeking clarification — no action required until answered |
| Suggested Change | Proposing a specific edit to inject content, timing, or ownership |
| Conflict | Flagging a logical conflict with another inject, resource, or assumption |
| Approved | Explicitly endorsing the inject as written — no changes needed |

---

## Story 2.3: Reply to a Comment

**As a** Planner or Exercise Director,
**I want** to reply directly to a comment in its thread,
**So that** the conversation stays attached to the inject and is visible to all participants.

### Acceptance Criteria

- [ ] **Given** I view a comment thread, **when** I click "Reply", **then** an inline reply input appears nested under that comment
- [ ] **Given** I submit a reply, **when** it is saved, **then** it appears indented beneath the parent comment with my name and timestamp
- [ ] **Given** a reply is posted, **when** the original commenter views the thread, **then** they see the reply with an unread indicator

---

## Story 2.4: View All Comments for an Inject

**As any** MSEL participant,
**I want** to see the complete comment thread for an inject in a side panel,
**So that** I can understand the full review history without leaving the MSEL view.

### Acceptance Criteria

- [ ] **Given** I click on an inject, **when** the inject detail panel opens, **then** the comment thread is displayed below the inject fields
- [ ] **Given** the comment panel is open, **when** new comments are posted by others, **then** they appear in real time without page refresh
- [ ] **Given** an inject has no comments, **when** I view its detail, **then** I see an empty state with a prompt to add the first comment

---

## Story 2.5: View All Open Comments Across the MSEL

**As an** Exercise Director or Planner,
**I want** to see a consolidated list of all unresolved comments across the entire MSEL,
**So that** I can work through the review backlog systematically and ensure nothing is missed.

### Acceptance Criteria

- [ ] **Given** I am on the MSEL view, **when** I click "Review Comments", **then** a panel or page displays all comments grouped by inject
- [ ] **Given** I view the comment list, **when** I click a comment, **then** the MSEL scrolls to and highlights the relevant inject
- [ ] **Given** a comment is resolved, **when** I view the comment list, **then** I can toggle to show or hide resolved comments
- [ ] **Given** I filter by comment type (e.g., "Conflicts only"), **when** the filter is applied, **then** only comments of that type are shown

---

---

# Feature 3: Live Collaborative Editing

**Parent Epic:** Collaborative MSEL Review

## Description

A designated Planner can edit inject details in real time during a planning conference while all other participants watch the changes appear live. This replaces the "one person types while everyone watches" screenshare pattern with a native, structured, conflict-safe editing experience.

## User Stories

1. [ ] Edit inject fields in real time with live broadcast — *Ready*
2. [ ] Presence indicators — see who is viewing the MSEL — *Ready*

---

## Story 3.1: Edit Inject Fields with Live Broadcast

**As a** Planner,
**I want** to edit an inject's fields and have all other participants see my changes immediately,
**So that** the MSEL in front of the group is always the current version.

### Context

SignalR is already in the Cadence stack for exercise conduct. This story extends that pattern to the planning phase. Only users with edit access (Director, Planner) can make changes; all others see updates in real time.

### Acceptance Criteria

- [ ] **Given** I have edit access and the MSEL is not locked, **when** I modify a field on an inject and save, **then** all other participants viewing the MSEL see the updated value within 2 seconds
- [ ] **Given** I am editing an inject, **when** another user with edit access attempts to edit the same inject simultaneously, **then** they receive a notification that the inject is currently being edited and are asked to wait
- [ ] **Given** I save a change, **when** it is applied, **then** the inject row briefly highlights to draw attention to the update in all participant views
- [ ] **Given** I am a read-only participant (SME, Observer), **when** the MSEL is updated by a Planner, **then** I see the update automatically — I cannot trigger edits

### Technical Notes
- Optimistic locking on the inject entity prevents simultaneous edit conflicts
- SignalR group per MSEL exercise session

---

## Story 3.2: Presence Indicators

**As a** Meeting Facilitator,
**I want** to see who is currently viewing the MSEL,
**So that** I can confirm remote participants are connected and following along.

### Acceptance Criteria

- [ ] **Given** multiple users are on the MSEL view, **when** I look at the presence bar, **then** I see avatar initials or names for all active participants
- [ ] **Given** a participant navigates away or loses connection, **when** their presence times out (>30 seconds inactive), **then** they are removed from the presence bar
- [ ] **Given** there are more than 8 active participants, **when** I view the presence bar, **then** I see the first 7 avatars and a "+N more" overflow indicator

---

---

# Feature 4: Review Workflow

**Parent Epic:** Collaborative MSEL Review

## Description

Comments have a lifecycle. Once posted, a Planner or Director can acknowledge, act on, and close each comment. The workflow is lightweight — this is not a formal approval chain — but it provides enough structure that nothing slips through and the review record is complete.

## User Stories

1. [ ] Resolve a comment — *Ready*
2. [ ] Defer a comment to a future planning event — *Ready*
3. [ ] Link a resolved comment to the change that addressed it — *Ready*

---

## Story 4.1: Resolve a Comment

**As a** Planner or Exercise Director,
**I want** to mark a comment as resolved,
**So that** the review backlog reflects what has been addressed and what still needs attention.

### Acceptance Criteria

- [ ] **Given** I am viewing a comment thread, **when** I click "Resolve" on a comment, **then** the comment status changes to Resolved and it is visually distinguished (grayed, struck, or collapsed)
- [ ] **Given** a comment is resolved, **when** the original commenter views the thread, **then** they see a "Resolved by [name] at [time]" attribution
- [ ] **Given** all comments on an inject are resolved, **when** I view the inject row, **then** no unresolved comment indicators are shown
- [ ] **Given** I resolve a comment, **when** I view the audit trail, **then** the resolution is recorded with my name and timestamp

---

## Story 4.2: Defer a Comment

**As a** Planner or Exercise Director,
**I want** to defer a comment to the next planning event rather than resolving it now,
**So that** the current meeting can stay on schedule while nothing is lost.

### Acceptance Criteria

- [ ] **Given** I am viewing an open comment, **when** I click "Defer", **then** I am prompted to optionally enter a reason and confirm
- [ ] **Given** a comment is deferred, **when** I view the comment list, **then** it appears in a "Deferred" section separate from Open and Resolved
- [ ] **Given** a comment is deferred, **when** a new review session is started, **then** deferred comments from the prior session carry forward as Open

---

## Story 4.3: Link a Resolved Comment to a Change

**As a** Planner,
**I want** to link a resolved comment to the inject change that addressed it,
**So that** the review record clearly shows which feedback drove which edit.

### Acceptance Criteria

- [ ] **Given** I resolve a comment, **when** the resolution dialog opens, **then** I can optionally select "This was addressed by a change to this inject"
- [ ] **Given** I select that option, **when** the comment is resolved, **then** the inject change history shows a reference to the originating comment
- [ ] **Given** I view the audit trail, **when** I look at an inject with linked comment-changes, **then** I can navigate from the change to the comment and vice versa

---

---

# Feature 5: Async Pre-Review

**Parent Epic:** Collaborative MSEL Review

## Description

Before a planning conference, the Exercise Director or Planner distributes a review link to participants. Reviewers can read the MSEL, add comments, and flag concerns on their own time — so the meeting itself can focus on discussion and resolution rather than first reads.

## User Stories

1. [ ] Open a MSEL for pre-review — *Ready*
2. [ ] Notify reviewers that the MSEL is ready for pre-review — *Ready*
3. [ ] View pre-review activity summary before the meeting — *Ready*

---

## Story 5.1: Open a MSEL for Pre-Review

**As an** Exercise Director or Planner,
**I want** to designate a time window during which the MSEL is open for pre-review,
**So that** participants can add comments before the planning conference.

### Acceptance Criteria

- [ ] **Given** I am on the MSEL view, **when** I click "Open for Review", **then** I can set a review window (start date/time, end date/time) and an optional note to reviewers
- [ ] **Given** the review window is active, **when** any authorized user views the MSEL, **then** they see a banner indicating the MSEL is in pre-review and comments are welcome
- [ ] **Given** the review window closes, **when** a user attempts to add a comment, **then** they see a message that the pre-review window has closed and are directed to raise items during the conference

---

## Story 5.2: Notify Reviewers

**As a** Planner,
**I want** to send a notification to designated reviewers when the MSEL is ready for pre-review,
**So that** participants know to review before the meeting without needing a separate email.

### Context
Cadence has Azure Communication Services (ACS) in the stack for email/SMS. This story uses the email channel.

### Acceptance Criteria

- [ ] **Given** I open the MSEL for pre-review, **when** I select participants to notify, **then** each selected participant receives an email with a direct link to the MSEL and the review window dates
- [ ] **Given** the notification is sent, **when** I view the notification log, **then** I see delivery status for each recipient
- [ ] **Given** a participant has not commented within 24 hours of the review window closing, **when** the reminder threshold is reached, **then** they receive one automated reminder (configurable: on/off per exercise)

### Dependencies
- Email communications epic must be complete or this story scoped to in-app notification only as MVP

---

## Story 5.3: Pre-Review Activity Summary

**As an** Exercise Director,
**I want** to see a summary of pre-review activity before the planning conference starts,
**So that** I can plan the meeting agenda around the injects that need the most discussion.

### Acceptance Criteria

- [ ] **Given** I open the MSEL before a planning conference, **when** pre-review comments exist, **then** I see a summary panel showing: total injects commented, comment breakdown by type, top 5 most-commented injects
- [ ] **Given** I view the summary, **when** I click an inject in the top-commented list, **then** I navigate directly to that inject and its thread
- [ ] **Given** no pre-review comments exist, **when** I view the summary, **then** I see an empty state with a prompt to open the MSEL for review

---

---

# Feature 6: Conflict Detection

**Parent Epic:** Collaborative MSEL Review

## Description

Cadence automatically analyzes the MSEL for common structural problems and flags them before or during the review conference. Flags are advisory — they never block an action — but they surface issues that would otherwise require manual review by an experienced planner.

## User Stories

1. [ ] Detect timing conflicts between injects — *Ready*
2. [ ] Detect player/controller overload — *Ready*
3. [ ] Dismiss a conflict flag with a reason — *Ready*

---

## Story 6.1: Detect Timing Conflicts

**As an** Exercise Director,
**I want** Cadence to flag injects that are scheduled too close together or that reference conditions not yet established,
**So that** I can catch sequencing problems before the exercise is conducted.

### Acceptance Criteria

- [ ] **Given** two injects targeting the same primary player are scheduled within a configurable minimum gap (default: 5 minutes), **when** the MSEL is analyzed, **then** both injects are flagged with a timing conflict warning
- [ ] **Given** an inject references a condition established by a prior inject (dependency link), **when** the prior inject is scheduled after the dependent inject, **then** a sequencing conflict is flagged
- [ ] **Given** conflicts are detected, **when** I view the MSEL, **then** flagged injects display a warning icon; a "Conflicts" filter in the MSEL view shows only flagged injects
- [ ] **Given** I fix a conflict (by adjusting timing or removing a dependency), **when** I save the change, **then** the conflict flag clears automatically

### Out of Scope
- AI-assisted conflict resolution suggestions (future capability)

---

## Story 6.2: Detect Player/Controller Overload

**As a** Lead Controller,
**I want** Cadence to flag when a single controller or player has too many injects within a short time window,
**So that** the workload is realistic and the exercise doesn't overwhelm key participants.

### Acceptance Criteria

- [ ] **Given** a controller is assigned more than N injects within a configurable rolling time window (default: 3 injects within 15 minutes), **when** the MSEL is analyzed, **then** the affected injects are flagged with an overload warning
- [ ] **Given** an overload is flagged, **when** I view the controller's inject list, **then** I see a visual indicator of the overloaded window
- [ ] **Given** I reassign one of the conflicting injects to another controller, **when** I save, **then** the overload flag re-evaluates and clears if the threshold is no longer exceeded

---

## Story 6.3: Dismiss a Conflict Flag with a Reason

**As an** Exercise Director,
**I want** to dismiss a conflict flag when it reflects an intentional design decision,
**So that** legitimate conflicts (e.g., intentional stress testing) are not treated as problems.

### Acceptance Criteria

- [ ] **Given** a conflict flag exists on an inject, **when** I click "Dismiss", **then** I am prompted to enter a brief reason (required)
- [ ] **Given** I dismiss a flag, **when** I view that inject, **then** the warning icon is replaced by a "Reviewed — [reason]" indicator
- [ ] **Given** a dismissed flag is re-triggered (e.g., a related inject is moved back into conflict), **when** the analysis re-runs, **then** the prior dismissal is cleared and the flag reappears

---

---

# Feature 7: MSEL Lock / Freeze

**Parent Epic:** Collaborative MSEL Review

## Description

When the MSEL has been reviewed and approved in the Final Planning Conference, the Exercise Director explicitly locks it for conduct. Once locked, no edits are possible without a deliberate unlock action. This is the formal handoff from planning to execution.

## User Stories

1. [ ] Lock the MSEL for conduct — *Ready*
2. [ ] Request and approve an unlock after the MSEL is locked — *Ready*

---

## Story 7.1: Lock the MSEL for Conduct

**As an** Exercise Director,
**I want** to lock the MSEL once it has been approved,
**So that** no accidental edits can occur between the Final Planning Conference and the exercise.

### Acceptance Criteria

- [ ] **Given** I am the Exercise Director, **when** I click "Lock for Conduct", **then** I am shown a confirmation dialog summarizing the current MSEL state (inject count, open comment count) and asked to confirm
- [ ] **Given** open (unresolved) comments exist, **when** I attempt to lock, **then** I receive a warning listing the open comments and must acknowledge them before proceeding
- [ ] **Given** the MSEL is locked, **when** any user attempts to edit an inject, **then** all edit controls are hidden and a "MSEL Locked" banner is displayed
- [ ] **Given** the MSEL is locked, **when** I view the exercise header, **then** a lock status badge is visible to all users
- [ ] **Given** the MSEL is locked, **when** a user views it, **then** read-only viewing, commenting, and conflict review are still accessible

---

## Story 7.2: Request and Approve Unlock

**As a** Planner,
**I want** to request that the MSEL be unlocked so a critical late change can be made,
**So that** last-minute corrections can be handled through a controlled, documented process rather than being skipped or made informally.

### Acceptance Criteria

- [ ] **Given** the MSEL is locked and I am a Planner, **when** I click "Request Unlock", **then** I enter a reason and the Exercise Director receives an in-app notification
- [ ] **Given** the Director approves the unlock request, **when** approval is granted, **then** the MSEL enters an "Unlocked for Edit" state and the Planner can make the specific change
- [ ] **Given** the change is complete, **when** the Planner or Director clicks "Re-lock", **then** the MSEL returns to locked state
- [ ] **Given** the unlock-edit-relock sequence completes, **when** I view the audit trail, **then** the reason, approver, editor, and timestamps are all recorded

---

---

# Feature 8: Review Audit Trail

**Parent Epic:** Collaborative MSEL Review

## Description

Every change, comment, resolution, and lock/unlock event during the MSEL planning lifecycle is recorded in a time-stamped audit trail. This supports HSEEP after-action documentation requirements and provides a clear record of how the MSEL evolved from first draft to approved conduct version.

## User Stories

1. [ ] View inject-level change history — *Ready*
2. [ ] View MSEL-level review timeline — *Ready*
3. [ ] Export the audit trail — *Ready*

---

## Story 8.1: View Inject-Level Change History

**As any** MSEL participant,
**I want** to see a history of all changes made to a specific inject,
**So that** I can understand why it looks the way it does and who made each decision.

### Acceptance Criteria

- [ ] **Given** I view an inject's detail panel, **when** I click "History", **then** I see a chronological list of all field changes, each showing: field name, old value, new value, changed by, timestamp
- [ ] **Given** a change was made in response to a comment, **when** I view that change in history, **then** a link to the originating comment is shown
- [ ] **Given** no changes have been made to an inject, **when** I view history, **then** I see the creation event only

---

## Story 8.2: View MSEL-Level Review Timeline

**As an** Exercise Director,
**I want** to see a high-level timeline of all review activity across the MSEL,
**So that** I can document the planning conference history for the HSEEP Exercise Record.

### Acceptance Criteria

- [ ] **Given** I am on the MSEL page, **when** I click "Review Timeline", **then** I see a chronological feed of all review events: comments added, changes made, comments resolved, MSEL locked/unlocked
- [ ] **Given** I view the timeline, **when** I click any event, **then** I navigate to the relevant inject and the event is highlighted
- [ ] **Given** review events span multiple planning conferences (IPC, MPC, FPC), **when** I view the timeline, **then** events are grouped by planning conference label

---

## Story 8.3: Export the Audit Trail

**As an** Exercise Director,
**I want** to export the audit trail to Excel or PDF,
**So that** it can be included in the Exercise Record and shared with stakeholders who do not have Cadence access.

### Acceptance Criteria

- [ ] **Given** I am on the Review Timeline, **when** I click "Export", **then** I can choose Excel (.xlsx) or PDF format
- [ ] **Given** I select Excel export, **when** the file is generated, **then** it contains: inject number, event type, description, actor, timestamp — one row per event
- [ ] **Given** I select PDF export, **when** the file is generated, **then** it is formatted as a printable summary document with the exercise name, date range, and event log

### Dependencies
- Excel export utility (Phase G) for .xlsx format
- PDF generation library for PDF format

---

---

# Implementation Phasing Recommendation

Given the current Cadence phase structure, this epic fits between the existing planning phases and Phase D (Exercise Conduct). It should be treated as **Phase J: Collaborative MSEL Review**.

| Sub-Phase | Features | Prerequisite |
|-----------|----------|--------------|
| J-1 | Presentation Mode, Comment Threads (basic) | Phase C complete ✅ |
| J-2 | Live Editing (SignalR), Presence Indicators | Phase H complete ✅ |
| J-3 | Review Workflow, MSEL Lock | J-1 + J-2 |
| J-4 | Async Pre-Review, ACS Notifications | Email comms epic |
| J-5 | Conflict Detection | J-3 |
| J-6 | Audit Trail + Export | J-3 + Phase G (Excel) |

**Minimum viable version (J-1 + J-2 + basic lock)** is demonstrable to beta testers as a distinct MPC workflow and is a strong differentiator story for VA and other enterprise prospects.

---

*Document generated for Cadence — HSEEP Exercise Management Platform*
*Follows business-analyst-agent.md story format*
