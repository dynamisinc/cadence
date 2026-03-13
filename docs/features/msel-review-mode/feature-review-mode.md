# Feature: Review Mode

**Parent Epic:** Collaborative MSEL Review
**Priority:** P0 — UX foundation for all review workflows
**Phase:** J-1 (first deliverable; enables all other review features)

---

## Description

Review Mode is a purpose-built context within Cadence that de-emphasizes or hides all features unrelated to reading, navigating, and commenting on the MSEL. It is distinct from Presentation Mode (which is a projector-focused display) — Review Mode is an active reading and annotation experience for individuals. When a user enters Review Mode, the interface reorients around comprehension and feedback, not exercise management.

Think of it as the difference between a word processor and a document reader with a red pen. The content is the same; the context is completely different.

---

## Domain Context

Cadence users have different jobs at different moments in the exercise lifecycle. The same Exercise Director who manages inject CRUD during planning needs to shift into a focused review posture during an MPC. Forcing them to navigate a full management interface while trying to read and comment on a 60-inject MSEL is friction that erodes the review experience. Review Mode removes that friction by making the interface match the task.

Review Mode is also the host context for Guest Portal access — guests always land in Review Mode because they have no other Cadence workflow.

---

## User Stories

---

### Story RM-1: Enter and Exit Review Mode

**As any** Cadence user with MSEL access,
**I want** to switch the MSEL into Review Mode,
**So that** I can focus on reading and commenting without the distraction of management controls.

#### Acceptance Criteria

- [ ] **Given** I am on the MSEL view, **when** I click "Review Mode", **then** the interface transitions to the Review Mode layout
- [ ] **Given** I am in Review Mode, **when** I view the interface, **then** the following elements are hidden: global navigation sidebar, exercise management tabs (Settings, Participants, etc.), inject create/delete buttons, phase management controls, exercise status controls
- [ ] **Given** I am in Review Mode, **when** I view the interface, **then** the following elements remain visible: all inject content (read-only), comment thread controls, coverage indicator per inject, phase navigation, search/filter bar (read-only filters only), Coverage Dashboard (read-only)
- [ ] **Given** I am in Review Mode and have edit rights, **when** I need to make a change, **then** I can click "Exit Review Mode" or a contextual "Edit this inject" affordance to return to the management view for that specific inject
- [ ] **Given** I am a Guest Reviewer (no Cadence account), **when** I access Cadence via a guest link, **then** I always land in Review Mode and the "Exit Review Mode" control is not available

#### UI/UX Notes

```
NORMAL MODE:
┌──────────────────────────────────────────────────────────┐
│ [≡] Cadence  Exercises > Hurricane FSE > MSEL            │
│ ────────────────────────────────────────────────────────│
│ [Dashboard] [Exercises] [Reports] [Settings]             │
│                                                          │
│  Hurricane FSE MSEL                [+ Add Inject] [⚙]   │
│  [Phases ▼] [Filter ▼] [Sort ▼]   [Import] [Export]     │
│  ...inject list...                                       │
└──────────────────────────────────────────────────────────┘

REVIEW MODE:
┌──────────────────────────────────────────────────────────┐
│  📋 MSEL Review — Hurricane Response FSE 2026            │
│  Reviewing as: Tom B.    Review closes: Mar 22           │
│  [Coverage ▼] [Filter ▼] [Search]    [Exit Review Mode] │
│ ─────────────────────────────────────────────────────── │
│  Phase 1 — Initial Response                              │
│  ┌───────────────────────────────────────────────────┐  │
│  │ H+0:15  Initial 911 call received to County EOC   │  │
│  │         Owner: Smith  │ 🎯 OC, MC  │ 💬 2 comments│  │
│  │                              [Add Comment]        │  │
│  └───────────────────────────────────────────────────┘  │
│  ...                                                     │
└──────────────────────────────────────────────────────────┘
```

---

### Story RM-2: Navigate the MSEL in Review Mode

**As a** Reviewer,
**I want** to navigate the MSEL efficiently by phase, objective, or search term while in Review Mode,
**So that** I can find the injects most relevant to my lane or focus area quickly.

#### Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I use the phase navigation bar, **then** clicking a phase scrolls the inject list to that phase's first inject
- [ ] **Given** I am in Review Mode, **when** I use the search bar, **then** I can search inject descriptions and the results highlight in place (no navigation away from the MSEL)
- [ ] **Given** I am in Review Mode, **when** I use the filter control, **then** I can filter by: my comments only, unreviewed injects, injects with open conflicts, injects by objective
- [ ] **Given** I am in Review Mode, **when** I click an inject row, **then** it expands in place to show full detail and the comment thread — I do not navigate to a separate inject detail page
- [ ] **Given** I have an objective filter active, **when** I view the MSEL, **then** a mini Coverage Dashboard summary appears at the top showing coverage health for the filtered objective

---

### Story RM-3: Inject Review Status (Personal Progress Tracking)

**As a** Reviewer,
**I want** to mark injects as personally reviewed as I work through them,
**So that** I can track my own progress through the MSEL and know where I left off.

#### Context
This is personal progress tracking, not a workflow state visible to the Exercise Director. It is equivalent to checking off items in a reading list. This is particularly valuable when a reviewer has 40+ injects and needs to review in multiple sessions over a pre-review window.

#### Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I expand an inject and view it, **then** a "Mark as Reviewed" checkbox is available
- [ ] **Given** I check "Mark as Reviewed", **when** I view the inject list, **then** that inject displays a personal checkmark indicator and appears visually de-emphasized
- [ ] **Given** I have marked some injects as reviewed, **when** I view the phase navigation bar, **then** each phase shows my review progress (e.g., "Phase 1 — 4/6 reviewed")
- [ ] **Given** I close the browser and return later via the same link or session, **when** I view the MSEL, **then** my previously-reviewed injects are still marked (persisted per user/guest session)
- [ ] **Given** I use the filter control, **when** I select "Show unreviewed only", **then** only injects I have not yet marked are shown

---

### Story RM-4: Coverage Indicators in Review Mode

**As a** Reviewer,
**I want** to see at-a-glance coverage health for each inject and objective while I review,
**So that** I can provide informed feedback about whether injects are serving their intended purpose.

#### Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I view an inject row, **then** I see compact objective tags showing which objectives this inject contributes to
- [ ] **Given** I am in Review Mode, **when** I view the Coverage Summary bar at the top, **then** I see a compact traffic-light summary of overall coverage health (e.g., "3 gaps, 2 objectives at risk")
- [ ] **Given** I click the Coverage Summary bar, **when** the panel expands, **then** I see the full Coverage Dashboard in read-only mode within the Review Mode layout
- [ ] **Given** an inject has no objective tags, **when** I view it in Review Mode, **then** it displays an "Untagged" badge prompting a comment or, if I have edit access, a quick-tag affordance
