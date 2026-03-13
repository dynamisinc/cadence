# Feature: MSEL Objective Coverage Intelligence

**Parent Epic:** Collaborative MSEL Review
**Priority:** P0 — Primary standalone differentiator
**Phase:** J-1 (first deliverable in the Collaborative Review epic)

---

## Description

Cadence automatically analyzes the relationship between a MSEL's injects and the exercise's stated objectives and core capabilities, surfacing coverage gaps, density imbalances, and phase distribution problems that would otherwise require hours of manual cross-referencing. The result is a living dashboard that answers the question every Exercise Director dreads before an MPC: *"Does my MSEL actually test what I promised it would?"*

This feature operates entirely on data Cadence already owns — injects, objectives, phases, player organizations — and requires no AI or external service. The analysis engine is deterministic and explainable, which is essential for practitioners who need to defend their design decisions to program managers and accreditors.

---

## Domain Context

In HSEEP doctrine, every exercise is designed around a set of **Exercise Objectives** — specific, measurable statements of what the exercise is intended to test. Each objective is linked to one or more **Core Capabilities** from FEMA's National Preparedness Goal (or equivalent framework for non-FEMA contexts — NATO, NIST, ISO, etc.). Injects are the mechanism by which objectives are tested: a well-designed MSEL ensures every objective has enough injects to generate observable, evaluable player actions.

In practice, this traceability is almost never maintained rigorously. Injects are written by multiple people over weeks, objectives drift from the original design intent, and no tool currently enforces or measures the relationship. The result is exercises that inadvertently under-test some objectives and over-test others — a finding that shows up in the AAR, too late to fix.

---

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Objective | A specific, measurable statement of what the exercise tests; linked to one or more core capabilities |
| Core Capability | A FEMA (or equivalent framework) capability area, e.g., "Mass Care Services", "Operational Coordination" |
| Coverage | The number and distribution of injects that trace to a given objective or capability |
| Coverage Gap | An objective or capability with insufficient inject coverage relative to its stated importance |
| Inject Density | The concentration of injects within a time window or phase; used to detect overloaded or empty periods |
| Traceability | The explicit linkage between an inject and one or more objectives — the foundation of the coverage model |
| Coverage Target | A configurable minimum inject count per objective, used to calculate gap thresholds |

---

## User Stories

---

### Story CI-1: Tag Injects to Objectives During MSEL Authoring

**As a** Planner,
**I want** to tag each inject to one or more exercise objectives when I create or edit it,
**So that** Cadence can track which objectives are being tested by which injects across the MSEL.

#### Context
Traceability is the foundation of the entire coverage model. Without explicit inject-to-objective linkage, no analysis is possible. This story establishes the data discipline. The UI must make tagging fast and frictionless — if it feels like overhead, planners will skip it, and the model breaks down.

The objectives themselves are created at the exercise level (see Story CI-2). Injects can trace to multiple objectives; one inject can simultaneously test Operational Coordination and Mass Care if it is designed that way.

#### Acceptance Criteria

- [ ] **Given** I am creating or editing an inject, **when** I view the inject form, **then** I see an "Objectives" multi-select field listing all objectives defined for this exercise
- [ ] **Given** I select one or more objectives, **when** I save the inject, **then** the inject-objective linkages are stored and immediately reflected in the coverage analysis
- [ ] **Given** an inject has no objectives tagged, **when** I view the MSEL, **then** the inject displays an "Untagged" indicator to prompt the planner to complete the linkage
- [ ] **Given** I am on the MSEL view, **when** I look at the inject list, **then** I can see a compact objective tag chip on each inject row without opening the detail panel
- [ ] **Given** an exercise has no objectives defined yet, **when** I create an inject, **then** the objective field displays a prompt to define objectives first, with a direct link to the objective management screen

#### UI/UX Notes

```
┌─────────────────────────────────────────────────────────────┐
│ Edit Inject                                                  │
│ ─────────────────────────────────────────────────────────── │
│ Description:  [EOC Activation ordered by County EM        ] │
│ Phase:        [Initial Response          ▼]                  │
│ Scheduled:    [H+0:30                    ]                   │
│ Controller:   [Smith, J.                 ▼]                  │
│                                                              │
│ Objectives:   [× Operational Coordination] [× Mass Care ▼] │
│               ⚠ This inject has no capability framework tag │
│                                                              │
│ [Cancel]                              [Save Inject]          │
└─────────────────────────────────────────────────────────────┘
```

#### Out of Scope
- Auto-tagging via AI/NLP (future capability — do not pre-architect for it)
- Mandatory tagging enforcement that blocks saving (warn, never block)

---

### Story CI-2: Define Exercise Objectives and Capability Linkages

**As an** Exercise Director or Planner,
**I want** to define the exercise's objectives and link each one to a core capability framework,
**So that** the coverage analysis has a structured foundation to measure against.

#### Context
Objectives are created during the Exercise Design phase, before MSEL authoring begins. Cadence should support the common frameworks out of the box (FEMA 32 Core Capabilities, NATO 7 Baseline Requirements, NIST CSF, ISO 22301) as pre-populated picklists, with a custom/free-text option for organizations that use their own capability framework. This is consistent with the "HSEEP is not universal" principle already established in Cadence's design philosophy.

#### Acceptance Criteria

- [ ] **Given** I am on the exercise detail page, **when** I navigate to the "Objectives" tab, **then** I can add, edit, reorder, and delete exercise objectives
- [ ] **Given** I am creating an objective, **when** I fill in the form, **then** I provide: objective text (free text), a priority/weight (High / Medium / Low), a coverage target (minimum inject count, default 3), and one or more capability tags
- [ ] **Given** I select a capability framework (e.g., FEMA Core Capabilities), **when** I open the capability tag picker, **then** I see the full list of capabilities for that framework as checkboxes
- [ ] **Given** the exercise uses a non-standard framework, **when** I select "Custom", **then** I can type free-text capability labels that function identically to pre-defined ones
- [ ] **Given** objectives exist and injects are tagged to them, **when** I edit an objective's coverage target, **then** the coverage dashboard recalculates immediately
- [ ] **Given** I delete an objective, **when** injects are tagged to it, **then** I receive a warning showing how many injects will lose that tag, and I must confirm before proceeding

#### UI/UX Notes

```
┌─────────────────────────────────────────────────────────────┐
│ Exercise Objectives                            [+ Add]       │
│ ─────────────────────────────────────────────────────────── │
│ Framework: [FEMA Core Capabilities ▼]                        │
│                                                              │
│ #  Objective                          Priority  Target  Caps │
│ 1  Demonstrate EOC activation...      High      4       OC   │
│ 2  Coordinate mass care resources...  High      3       MC   │
│ 3  Test public messaging protocols... Medium    2       PW   │
│ 4  Evaluate logistics coordination... Low       2       LS   │
│                                                              │
│ Coverage Framework: FEMA 32 Core Capabilities                │
└─────────────────────────────────────────────────────────────┘
```

---

### Story CI-3: View the Coverage Dashboard

**As an** Exercise Director or Planner,
**I want** to see a visual dashboard showing inject coverage across all objectives and phases,
**So that** I can instantly identify gaps and imbalances in the MSEL design without manual analysis.

#### Context
This is the headline feature. The dashboard is a heatmap matrix: objectives on the Y-axis, phases on the X-axis, inject count in each cell. Colors indicate coverage health relative to the target. A planner opening this view for the first time should be able to identify design problems within 30 seconds. During an MPC, this view projected on screen is a powerful discussion driver.

#### Acceptance Criteria

- [ ] **Given** I navigate to the Coverage Dashboard, **when** the page loads, **then** I see a matrix with exercise objectives as rows, exercise phases as columns, and inject counts as cell values
- [ ] **Given** a cell's inject count meets or exceeds the objective's coverage target, **when** I view that cell, **then** it is colored green
- [ ] **Given** a cell's inject count is between 1 and the coverage target, **when** I view that cell, **then** it is colored amber
- [ ] **Given** a cell has zero injects, **when** I view that cell, **then** it is colored red with a "Gap" label
- [ ] **Given** I click any cell in the matrix, **when** the cell has injects, **then** a flyout panel shows the list of injects in that objective/phase intersection, with links to each inject
- [ ] **Given** I click a red (gap) cell, **when** the flyout opens, **then** I see a prompt to add an inject for this objective in this phase, with a pre-filled "Add Inject" shortcut
- [ ] **Given** the MSEL changes (inject added, removed, or re-tagged), **when** I am viewing the dashboard, **then** the matrix updates within 2 seconds (real-time via SignalR if the MSEL is in an active review session)
- [ ] **Given** the exercise has objectives with different priority weights, **when** I view the dashboard, **then** High-priority objectives are visually prominent (e.g., larger row height, bold label, or priority badge)

#### UI/UX Notes

```
┌──────────────────────────────────────────────────────────────────────┐
│ Coverage Dashboard — Hurricane Response FSE 2026                      │
│ Framework: FEMA Core Capabilities   ○ By Objective  ● By Capability  │
├──────────────────────┬──────────────┬──────────────┬─────────────────┤
│ Objective            │ Phase 1      │ Phase 2      │ Phase 3         │
│                      │ Initial Resp │ Stabilize    │ Recovery        │
├──────────────────────┼──────────────┼──────────────┼─────────────────┤
│ ★ EOC Activation     │  ████ 4/4    │  ██░░ 2/4    │  ░░░░ 0/4 GAP  │
│ ★ Mass Care Coord.   │  ███░ 3/4    │  ████ 4/4    │  ██░░ 2/4      │
│   Public Messaging   │  ██░░ 2/3    │  ░░░░ 0/3 GAP│  ██░ 2/3       │
│   Logistics Coord.   │  ██░░ 2/2    │  ██░░ 2/2    │  ██░░ 1/2      │
├──────────────────────┼──────────────┼──────────────┼─────────────────┤
│ Untagged Injects     │      2       │      0       │       1         │
│                      │  ⚠ Review   │              │  ⚠ Review       │
└──────────────────────┴──────────────┴──────────────┴─────────────────┘
  ■ Met target   ■ Partial   ■ Gap    ★ High priority
```

#### Out of Scope
- Trend analysis across multiple exercises (inject library feature)
- Automatic inject generation to fill gaps

---

### Story CI-4: View Coverage by Capability Framework

**As an** Exercise Director,
**I want** to toggle the coverage view to show coverage by capability area rather than by objective,
**So that** I can confirm that the exercise tests a balanced range of capabilities as required by the exercise design document.

#### Context
Objectives are exercise-specific, but capability frameworks are standardized. Program managers and accreditors often ask "Did this exercise test Mass Care?" rather than "Did it test Objective 2?" The capability view answers that question directly.

#### Acceptance Criteria

- [ ] **Given** I am on the Coverage Dashboard, **when** I toggle from "By Objective" to "By Capability", **then** the matrix rows change to capability areas from the selected framework
- [ ] **Given** I view the capability matrix, **when** a capability has no objectives mapped to it, **then** it is shown as "Not Targeted" with a neutral indicator (not red — absence is intentional if the capability is out of scope)
- [ ] **Given** I hover over a capability row label, **when** the tooltip appears, **then** I see which objectives are contributing injects to that capability
- [ ] **Given** the exercise uses multiple frameworks (e.g., both FEMA and NIST), **when** I switch frameworks, **then** the matrix remaps to the selected framework's capability taxonomy

---

### Story CI-5: Coverage Gap Alerts and Recommendations

**As a** Planner,
**I want** to receive proactive alerts when the MSEL has coverage gaps relative to the stated objectives,
**So that** I discover design problems during planning rather than in the after-action report.

#### Acceptance Criteria

- [ ] **Given** an objective's inject count falls below its coverage target, **when** the gap is detected, **then** a coverage alert appears in the MSEL header and in the exercise notification area
- [ ] **Given** I view a coverage alert, **when** I click it, **then** I navigate directly to the Coverage Dashboard with the gap cell highlighted
- [ ] **Given** the exercise design document specifies a target inject count (e.g., 40 injects for a 4-hour FSE), **when** I view the dashboard summary, **then** I see: current inject count vs. target, recommended distribution by phase, percentage of injects currently untagged
- [ ] **Given** I add injects that fill a gap, **when** the coverage target is met, **then** the alert clears automatically
- [ ] **Given** an alert exists on a locked MSEL, **when** I view the lock confirmation, **then** unmet coverage targets are listed as warnings I must acknowledge before locking

#### Coverage Recommendation Logic
- Recommended injects per phase = (total target injects / number of phases), weighted by phase duration if durations are set
- Recommended injects per objective per phase = coverage target / number of phases (evenly distributed as default)
- These are suggestions displayed as guidance, never enforced

---

### Story CI-6: Coverage Summary in the Exercise Record

**As an** Exercise Director,
**I want** to include a coverage summary in the exercise documentation,
**So that** the design rationale is captured for the HSEEP Exercise Record and available for the AAR team.

#### Acceptance Criteria

- [ ] **Given** I am on the Coverage Dashboard, **when** I click "Export Coverage Report", **then** I can export as Excel or PDF
- [ ] **Given** I export to Excel, **when** the file is generated, **then** it contains: one sheet with the objective/phase matrix, one sheet listing all inject-objective linkages, one sheet summarizing gap counts by capability
- [ ] **Given** I export to PDF, **when** the file is generated, **then** it contains a printable summary with exercise name, date, framework used, the coverage matrix, and a gap analysis narrative paragraph
- [ ] **Given** I include the coverage report in the exercise package, **when** an evaluator reads it, **then** they can confirm which objectives were addressed by which injects without accessing Cadence

---

## Data Model Additions

### New Entities

```
ExerciseObjective
  Id                  GUID
  ExerciseId          GUID (FK → Exercise)
  Text                string (the objective statement)
  Priority            enum: High | Medium | Low
  CoverageTarget      int (minimum inject count, default 3)
  DisplayOrder        int
  CreatedAt           datetime
  UpdatedAt           datetime

ObjectiveCapabilityTag
  Id                  GUID
  ObjectiveId         GUID (FK → ExerciseObjective)
  Framework           enum: FEMA | NATO | NIST | ISO | Custom
  CapabilityCode      string (e.g., "MC" for Mass Care)
  CapabilityLabel     string (display name)

InjectObjective  (join table)
  InjectId            GUID (FK → Inject)
  ObjectiveId         GUID (FK → ExerciseObjective)
  CreatedAt           datetime
  CreatedBy           GUID (FK → User)
```

### Coverage View (computed, not stored)
Coverage metrics are computed on-demand from InjectObjective join table counts grouped by ObjectiveId and PhaseId. No denormalized coverage fields — always derived to stay accurate.

---

## API Endpoints Required

| Method | Route | Description |
|--------|-------|-------------|
| GET | /exercises/{id}/objectives | List all objectives for an exercise |
| POST | /exercises/{id}/objectives | Create an objective |
| PUT | /objectives/{id} | Update an objective |
| DELETE | /objectives/{id} | Delete an objective (with impact check) |
| GET | /exercises/{id}/coverage | Get full coverage matrix (objective × phase) |
| GET | /exercises/{id}/coverage/gaps | Get gap summary only |
| POST | /injects/{id}/objectives | Add objective tag to inject |
| DELETE | /injects/{id}/objectives/{objectiveId} | Remove objective tag |
| GET | /exercises/{id}/coverage/export | Export coverage report (query param: format=xlsx|pdf) |

---

## Dependencies

- Exercise Objectives data model (new — this story creates it)
- Inject CRUD complete ✅ (Phase C)
- Excel export utility (Phase G) for export story
- SignalR (Phase H ✅) for real-time dashboard updates during review sessions
