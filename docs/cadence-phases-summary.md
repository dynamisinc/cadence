# Cadence Development Phases

## Overview

This document tracks the phased development approach for Cadence MVP. The strategy is **hybrid foundation-first**: establish frontend patterns in Phase A, then build vertical slices (B-D), refine cross-cutting concerns (E), and finally add real-time complexity (F).

> ⚠️ **Data Model Backport Alert — Read Before Starting Phase D**
> Phase J design work has identified two data model additions that must be seeded now to avoid costly retrofits later. See [Data Model Backports](#phase-c-data-model-backports--do-before-phase-d) section below.

---

## Phase Summary

| Phase | Name | Focus | Status | Features |
|-------|------|-------|--------|----------|
| **0** | Infrastructure | Backend, DB, CI/CD, Deployment Docs | ✅ Complete | MVP-A, B, C |
| **A** | Frontend Foundation | Vite, MUI, Shell, Auth, Routes | ✅ Complete | MVP-R |
| **B** | Exercise CRUD | List, Create, Edit, Detail | ✅ Complete | MVP-D |
| **C** | Inject CRUD | MSEL View, Create, Edit, Status | ✅ Complete | MVP-E (partial M) |
| **C+** | ⚠️ Data Model Backports | Objective + Library FK stubs | 🔲 **Do before D** | — |
| **D** | Exercise Conduct | Clock, Fire Inject, Status Workflow | ✅ Complete | MVP-J, K, M |
| **E** | Evaluator Observations | Observation capture, P/S/M/U ratings | ✅ Complete | MVP-N |
| **F** | Inject Organization | Sort, Filter, Group, Search | 🔲 Blocked by D+E | MVP-H, I |
| **G** | Excel Import/Export | Import, Validation, Export | ⏸️ Deferred | MVP-F, G |
| **H** | Real-Time & Offline | SignalR sync, Offline cache | ✅ Complete | MVP-O, P, Q |
| **I** | PWA | Service Worker, Install, App Shell | ✅ Complete | — |
| **J-1** | Review Mode + Comments | MSEL Review UX, Comment Threads, Objective UI | 🔲 After I | MVP-S1–S3 |
| **J-2** | Live Collaboration | Presence, Live Editing, Coverage Dashboard | 🔲 After J-1 | MVP-S4–S6 |
| **J-3** | Review Workflow + Lock | Comment resolution, MSEL Lock | 🔲 After J-2 | MVP-S7–S8 |
| **J-4** | Guest Portal | External stakeholder review, Formal approval | 🔲 After J-3 | MVP-S9–S10 |
| **J-5** | Conflict Detection | Timing, player overload, dismiss | 🔲 After J-3 | MVP-S11 |
| **J-6** | Audit Trail + Export | Review history, coverage export | 🔲 After J-3 + G | MVP-S12 |
| **K** | Inject Library | Org library, search, starter packs | 🔲 After J | MVP-T1–T4 |

> **Note:** Phases D and E can run in parallel. See `phase-d-e-parallel-prompt.md`.
> **Note:** Phase G deferred — seed data sufficient for initial testing.
> **Note:** Phase H centralized offline sync; Phase I adds PWA shell caching.
> **Note:** Phase J is the Collaborative MSEL Review epic — the primary standalone planning differentiator.
> **Note:** J-1 minimum viable slice (Review Mode + Comments + MSEL Lock) is demonstrable as its own beta milestone and is the first point at which Cadence has value independent of conduct.

---

## ⚠️ Data Model Backports Required Before Phase D

These additions were identified during Phase J design. They are **backward-compatible** (nullable FKs, new tables that existing code ignores), but retrofitting them after Phase D accumulates real exercise data is significantly more expensive. Complete these in a focused migration sprint immediately before starting Phase D.

### Backport 1: Exercise Objective Model

**Why now:** The Coverage Intelligence dashboard (Phase J-2) is built entirely on inject-to-objective linkages. Every inject created from Phase D forward should be tag-able to objectives. Without the schema, exercises running during the beta period will have no traceability data, and the coverage dashboard will be empty when it ships — defeating its purpose.

**Entities to add:**

```
ExerciseObjective
  Id                  GUID
  ExerciseId          GUID (FK → Exercise)
  Text                string
  Priority            enum: High | Medium | Low  (default: Medium)
  CoverageTarget      int (default: 3)
  DisplayOrder        int
  CreatedAt / UpdatedAt

ObjectiveCapabilityTag
  Id                  GUID
  ObjectiveId         GUID (FK → ExerciseObjective)
  Framework           enum: FEMA | NATO | NIST | ISO | Custom
  CapabilityCode      string
  CapabilityLabel     string

InjectObjective  (join table)
  InjectId            GUID (FK → Inject)
  ObjectiveId         GUID (FK → ExerciseObjective)
  CreatedAt           datetime
  CreatedBy           GUID (FK → User)
```

**UI work in this backport:** One optional multi-select "Objectives" field on the inject create/edit form. Shows "No objectives defined yet — add them under Exercise Settings" if the exercise has no objectives. No dashboard, no gap logic — just the field and the data.

---

### Backport 2: Library Source FK on Inject

**Why now:** Once Phase K ships, we need to know which MSEL injects were created from library entries. This FK is impossible to reconstruct retroactively. It is a single nullable column — zero behavioral impact until Phase K.

**Entities to add:**

```
LibraryInject  (stub — no UI, no endpoints until Phase K)
  Id                  GUID
  OrganizationId      GUID? (null = Cadence built-in)
  Title               string
  Description         string
  Source              enum: BuiltIn | Organization | Shared  (default: Organization)
  IsArchived          bool  (default: false)
  CreatedAt           datetime
  CreatedBy           GUID?

Inject  (alter existing)
  + LibrarySourceId   GUID? (FK → LibraryInject — null if not from library)
```

**UI work in this backport:** None. The `LibraryInject` table is an empty stub. No API endpoints, no UI. The FK on `Inject` is nullable and ignored by all Phase D–J code.

---

## Phase 0: Infrastructure ✅ COMPLETE

**Completed Items:**
- [x] .NET 10 backend with EF Core
- [x] Core entities: Organization, User, Exercise, Msel, Phase, Inject, ExerciseParticipant
- [x] SQLite for local development
- [x] Database migrations
- [x] GitHub Actions CI workflow
- [x] Deployment workflows (backend + frontend)
- [x] Deployment documentation (`docs/deployment/`)
- [x] Namespace refactoring to `Cadence.Core`

**Artifacts:**
- `src/Cadence.Core/` — Backend project
- `src/Cadence.Core.Tests/` — Unit tests
- `.github/workflows/` — CI/CD pipelines
- `docs/deployment/` — Azure setup guides

---

## Phase A: Frontend Foundation ✅ COMPLETE

**Completed Items:**
- [x] Vite + React 19 + TypeScript setup
- [x] MUI theme configuration
- [x] Responsive navigation shell (drawer/sidebar)
- [x] Axios client with interceptors
- [x] Auth context with role-based access
- [x] Route structure with React Router
- [x] Error boundary
- [x] Loading/spinner components

**Minor Gaps Fixed:**
- [x] Custom breakpoints (768, 1024, 1440)
- [x] Mobile blocker component (<768px)

---

## Phase B: Exercise CRUD ✅ COMPLETE

**Goal:** First vertical slice — users can view, create, and edit exercises.

**Stories:** `docs/features/exercise-crud/`
| Story | File | Priority | Status |
|-------|------|----------|--------|
| Exercise List View | S03-exercise-list-view.md | P0 | ✅ |
| Create New Exercise | S01-create-exercise.md | P0 | ✅ |
| Edit Exercise Details | S02-edit-exercise.md | P0 | ✅ |

**UX Patterns Established:**
- List view with status/type chips
- Create/Edit modal or page flow
- Form validation with inline errors
- Loading states (skeletons)
- Error handling (toast notifications)
- Empty states

---

## Phase C: Inject CRUD ✅ COMPLETE

**Goal:** MSEL management — view and manage injects within an exercise.

**Stories:** `docs/features/inject-crud/`
| Story | File | Priority | Status |
|-------|------|----------|--------|
| View Inject List (MSEL) | S03-*.md | P0 | ✅ |
| Create New Inject | S01-*.md | P0 | ✅ |
| Edit Inject Details | S02-*.md | P0 | ✅ |
| Minimal Phase Management | (addendum) | P0 | ✅ |

---

## Phase C+: Data Model Backports ⚠️ DO BEFORE PHASE D

**Goal:** Add forward-compatible data model stubs identified by Phase J design. No UI required beyond the inject objectives multi-select stub. Migration and minimal form change only.

**Stories:** `docs/features/data-model-backports/`
| Story | Description | Priority | Status |
|-------|-------------|----------|--------|
| Exercise Objectives Schema | ExerciseObjective, ObjectiveCapabilityTag, InjectObjective tables | P0 | 🔲 |
| Inject Objectives Field Stub | Optional objectives multi-select on inject form | P0 | 🔲 |
| Library Source FK Stub | LibraryInject stub table + nullable FK on Inject | P0 | 🔲 |

**What this is NOT:**
- Not the Coverage Dashboard (Phase J-2)
- Not the Objective management UI (Phase J-1)
- Not the Inject Library (Phase K)
- Not required for Phase D conduct to function correctly

**Agents:** `backend-agent.md` (migration), `testing-agent.md` (migration validation)

---

## Phase D: Exercise Conduct ✅ COMPLETE

**Goal:** Enable Controllers to run an exercise — start the clock, fire injects, track status.

**Features:** `docs/features/exercise-clock/`, `docs/features/inject-crud/`
| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Unified Exercise Clock | Start/Pause/Resume/Stop, time display | P0 | ✅ |
| Manual Inject Firing | Fire button, confirmation, timestamp | P0 | ✅ |
| Inject Status Workflow | Pending → Fired/Skipped with audit | P0 | ✅ |

**Dependencies:**
- Phase C complete ✅
- Phase C+ data model backports still needed for J-phase features
- **Ran parallel with Phase E**

**Prompts:**
- `phase-d-exercise-conduct-prompt.md` (standalone)
- `phase-d-e-parallel-prompt.md` (combined with E)

---

## Phase E: Evaluator Observations ✅ COMPLETE

**Goal:** Enable Evaluators to capture observations during exercise conduct.

**Features:** `docs/features/exercise-observations/`
| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Observation Entry | Quick-capture form for observations | P0 | ✅ |
| Inject/Objective Linking | Associate observations with injects | P0 | ✅ |
| P/S/M/U Rating | HSEEP performance rating system | P0 | ✅ |
| Core Capability Tagging | Tag to FEMA Core Capabilities | P1 | ✅ |

**Dependencies:**
- Phase C complete ✅
- **Ran parallel with Phase D**

---

## Phase F: Inject Organization 🔲 BLOCKED BY D+E

**Goal:** Help users find and organize injects in large MSELs.

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Sort Inject List | Column sorting, persist preference | P0 | 🔲 |
| Filter Injects | By status, phase, controller, time | P0 | 🔲 |
| Group Injects | Collapsible groups by phase/status | P1 | 🔲 |
| Search Injects | Full-text search, highlight matches | P0 | 🔲 |

> **Note:** Phase J-1 Review Mode adds objective-based filtering. Coordinate with J-1 to avoid duplicate filter component work — shared components are likely.

**Dependencies:** Phase D+E complete

---

## Phase G: Excel Import/Export ⏸️ DEFERRED

**Status:** Deferred — seed data sufficient for initial testing.

> **Note:** Phase J-6 (Audit Trail Export) and J-4 (Guest Portal approval PDF) both depend on export infrastructure. Consider unblocking Phase G before J-6 rather than after to avoid building export utilities twice.

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Import MSEL from Excel | Upload, parse, map fields | P0 | ⏸️ |
| Import Validation | Preview, error reporting | P0 | ⏸️ |
| Export MSEL to Excel | Download formatted spreadsheet | P0 | ⏸️ |

---

## Phase H: Real-Time & Offline ✅ COMPLETE

| Feature | MVP ID | Priority | Status |
|---------|--------|----------|--------|
| Real-Time Data Sync | MVP-O | P0 | ✅ |
| Offline Detection | MVP-P | P0 | ✅ |
| Local Data Cache | MVP-P | P0 | ✅ |
| Offline Action Queue | MVP-P | P0 | ✅ |
| Sync on Reconnect | MVP-Q | P0 | ✅ |
| Conflict Resolution | MVP-Q | P1 | ✅ |

**Architecture:** Centralized app-level sync service, IndexedDB (Dexie.js), SignalR, optimistic UI queue.

---

## Phase I: PWA ✅ COMPLETE

| Feature | Priority | Status |
|---------|----------|--------|
| Web App Manifest | P0 | ✅ |
| Service Worker (vite-plugin-pwa) | P0 | ✅ |
| App Shell Caching | P0 | ✅ |
| Static Asset Precaching | P0 | ✅ |
| Install Prompt UI | P1 | ✅ |
| Update Notification | P1 | ✅ |

**Architecture Note:** Service Worker handles asset caching only. Data caching stays in Phase H's IndexedDB sync service. API calls use `NetworkOnly`.

**Dependencies:** Phase H complete ✅

---

## Phase J: Collaborative MSEL Review 🔲 AFTER I

**Epic File:** `docs/features/collaborative-msel-review/epic-collaborative-msel-review.md`

**Vision:** Exercise planners conduct structured, live MSEL reviews during planning conferences with all stakeholders — in person or remote — using Cadence as the single authoritative source. This is the primary standalone differentiator: a planning team gets immediate value without ever using the conduct module.

**Standalone Value Proposition:**
- Organizations in regulated industries (healthcare coalitions, utilities, nuclear) need documented multi-agency approval of exercise plans. No existing tool provides this.
- The Coverage Dashboard answers the question "Does my MSEL actually test what I promised?" — a question that currently requires 2–3 hours of manual cross-referencing.
- The Guest Portal removes all adoption friction for partner organizations — no account, no training, no IT involvement.

---

### Phase J-1: Review Mode + Comment Threads + Exercise Objectives UI 🔲

**Goal:** Establish the core review experience — purpose-built annotation context that all other J-phase features build on. First phase where Cadence has standalone planning value.

**Feature Files:**
- `docs/features/msel-review-mode/feature-review-mode.md`
- `docs/features/inject-comments/` (stories 2.1–2.5 from epic)
- `docs/features/exercise-objectives/` (story CI-2)

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Review Mode | De-cluttered MSEL reading/annotation context | P0 | 🔲 |
| Inject Comment Threads | Add, classify (Question/Change/Conflict/Approved), reply | P0 | 🔲 |
| Exercise Objective Management UI | Create/edit/reorder objectives, link to capability frameworks | P0 | 🔲 |
| Inject Review Progress | Personal mark-as-reviewed, phase progress tracking | P1 | 🔲 |
| Presentation Mode | Full-screen projector view, phase navigation, inject highlight | P1 | 🔲 |

**Key Stories:** RM-1 through RM-4, Story 2.1–2.5, CI-2

**Architecture Notes:**
- Review Mode is a layout/context switch, not a separate route — same data, different chrome
- Guest Portal users always land in Review Mode automatically
- Comments stored server-side; real-time delivery via SignalR

**Dependencies:**
- Phase I complete
- Phase C+ objectives schema already exists ✅

**UAT Checkpoint:** ✅ Yes — first standalone planning milestone

---

### Phase J-2: Live Collaboration + Coverage Dashboard 🔲

**Goal:** Real-time multi-user MSEL review with presence awareness and the Coverage Intelligence heatmap dashboard.

**Feature Files:**
- `docs/features/coverage-intelligence/feature-coverage-intelligence.md`
- `docs/features/live-collaboration/`

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Live Collaborative Editing | Real-time inject edits broadcast to all viewers | P0 | 🔲 |
| Presence Indicators | See who is viewing the MSEL | P0 | 🔲 |
| Inject-to-Objective Tagging (full) | Multi-select on inject form, untagged warnings | P0 | 🔲 |
| Coverage Dashboard | Objective × Phase heatmap, gap detection, drill-down | P0 | 🔲 |
| Coverage Gap Alerts | Proactive warnings when objectives under-injected | P0 | 🔲 |
| Coverage by Capability Toggle | Remap matrix to capability framework view | P1 | 🔲 |

**Key Stories:** Story 3.1–3.2, CI-1, CI-3, CI-4, CI-5

**Architecture Notes:**
- Extend existing SignalR hub to MSEL review sessions (new group per exercise review session)
- Coverage matrix computed on-demand from InjectObjectives — never stored/denormalized
- Optimistic locking on Inject entity prevents concurrent edit corruption

**Dependencies:** Phase J-1 complete, Phase H SignalR ✅

---

### Phase J-3: Review Workflow + MSEL Lock 🔲

**Goal:** Comment lifecycle management and the formal planning-to-conduct handoff gate.

**Feature Files:** `docs/features/review-workflow/`

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Comment Resolution | Resolve, defer, link to inject change | P0 | 🔲 |
| All Open Comments View | Aggregated cross-MSEL comment backlog with filters | P0 | 🔲 |
| Async Pre-Review Window | Director opens MSEL for pre-conference review | P0 | 🔲 |
| MSEL Lock for Conduct | Director locks; unlock-request workflow with reason + approval | P0 | 🔲 |

**Key Stories:** Story 4.1–4.3, Story 5.1, Story 5.3, Story 7.1–7.2

**Dependencies:** Phase J-2 complete

---

### Phase J-4: Guest Portal 🔲

**Goal:** External partner organizations review and formally approve their lane with no Cadence account required.

**Feature File:** `docs/features/guest-portal/feature-guest-portal.md`

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Generate Guest Review Link | Org-scoped, time-limited, signed JWT token | P0 | 🔲 |
| Guest Portal (no login) | Lane-filtered inject view, mobile-responsive | P0 | 🔲 |
| Guest Commenting | Post comments attributed to name + org | P0 | 🔲 |
| Formal Lane Approval | Name/title confirmation, reference number, compliance record | P0 | 🔲 |
| Guest Tracker Dashboard | Per-org status: Pending/Opened/Commented/Approved | P0 | 🔲 |
| Approval Record Export PDF | Printable record for accreditation/exercise file | P0 | 🔲 |
| Review Notifications (ACS) | Invitation and reminder emails | P1 | 🔲 |

**Key Stories:** GP-1 through GP-5

**Security Notes:**
- Guest tokens are signed JWTs entirely separate from ASP.NET Core Identity user sessions
- Org scope enforced server-side on every API request — not UI-only filtering
- Rate limiting per token on all guest endpoints

**Dependencies:**
- Phase J-3 complete
- ACS email integration (P1 — portal functions without it; notifications degraded)
- PDF export utility

**UAT Checkpoint:** ✅ Yes — regulated industry demo milestone (healthcare coalitions, utilities, NERC CIP)

---

### Phase J-5: Conflict Detection 🔲

**Goal:** Automated MSEL structural analysis that flags timing, sequencing, and workload problems before exercise day.

**Feature File:** `docs/features/conflict-detection/`

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Timing Conflict Detection | Injects too close together for same player | P0 | 🔲 |
| Player/Controller Overload | Too many injects in a rolling window | P1 | 🔲 |
| Dependency Sequencing Validation | Inject requires condition not yet established | P1 | 🔲 |
| Dismiss with Reason | Acknowledge intentional design decisions | P0 | 🔲 |

**Architecture Notes:**
- Rules engine pattern — configurable thresholds stored as org-level admin settings
- Analysis runs as a background pass on MSEL save events (not blocking)
- Flags are advisory only — never block saving or locking

**Dependencies:** Phase J-3 complete (MSEL Lock references conflict state)

---

### Phase J-6: Audit Trail + Coverage Export 🔲

**Goal:** Complete review history and exportable documentation for the HSEEP Exercise Record.

**Feature Files:** `docs/features/review-audit-trail/`

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Inject Change History | Field-level diff, comment linkage, creation event | P0 | 🔲 |
| MSEL Review Timeline | Cross-exercise event feed grouped by planning conference | P0 | 🔲 |
| Audit Trail Export | Excel + PDF for exercise record | P0 | 🔲 |
| Coverage Report Export | Objective × phase matrix + gap narrative PDF | P0 | 🔲 |

**Key Stories:** Story 8.1–8.3, CI-6

**Dependencies:** Phase J-3 complete; Phase G (Excel) recommended before this phase

---

## Phase K: Inject Library 🔲 AFTER J

**Feature File:** `docs/features/inject-library/feature-inject-library.md`

**Goal:** Organizational inject library with curated starter packs — turns every completed exercise into a reusable asset and dramatically accelerates future exercise design. Primary retention feature.

| Feature | Description | Priority | Status |
|---------|-------------|----------|--------|
| Save Inject to Library | Save from any completed exercise with tags | P0 | 🔲 |
| Browse and Search Library | Filter by framework, exercise type, keyword, usage | P0 | 🔲 |
| Add Library Inject to MSEL | Instantiate copy (not reference) into active exercise | P0 | 🔲 |
| Cadence Starter Packs | 5 built-in packs (Hurricane FSE, Active Shooter FSE, Cyber TTX, EOC TTX, MAC TTX) | P0 | 🔲 |
| Library Management | Edit, archive, tag org, usage analytics | P1 | 🔲 |

**Key Stories:** IL-1 through IL-5

**Architecture Notes:**
- Copy-on-instantiation is non-negotiable — library entry and MSEL inject are fully independent after creation
- `LibrarySourceId` FK already seeded in Phase C+ backport ✅
- Starter packs are migration seed data maintained by the Cadence team — not user-editable

**Dependencies:** Phase J complete; Phase C+ data model backport ✅

**UAT Checkpoint:** ✅ Yes — retention milestone; organizations with library data have switching costs

---

## UAT Deployment Checkpoints

| After Phase | Deploy to UAT? | Purpose |
|-------------|----------------|---------|
| A | Optional | Verify infrastructure |
| B | ✅ Yes | Exercise list/create/edit |
| C | ✅ Yes | MSEL/Inject management |
| C+ | ✅ Yes | Validate data model migration |
| **D+E** | ✅ Complete | **Controllers + Evaluators can run exercises — key conduct milestone** |
| F | Optional | UX polish (sort/filter/search) |
| G | ⏸️ Deferred | Excel import/export — revisit before J-6 |
| H | ✅ Deployed | Multi-user sync + offline capability |
| **I** | ✅ Complete | **PWA — installable, reliable offline loading** |
| **J-1** | **✅ Yes** | **First standalone planning value — review mode + comments** |
| **J-4** | **✅ Yes** | **Guest Portal — regulated industry demo milestone** |
| **K** | **✅ Yes** | **Inject Library — retention milestone** |

---

## MVP Features by Phase

| Phase | MVP ID | Feature | Status |
|-------|--------|---------|--------|
| 0 | MVP-A | Project Foundation & CI/CD | ✅ |
| 0 | MVP-B | Authentication & User Roles | ✅ |
| 0 | MVP-C | Exercise & MSEL Data Model | ✅ |
| A | MVP-R | Responsive UI | ✅ |
| B | MVP-D | Exercise Management (CRUD) | ✅ |
| C | MVP-E | Inject Management (CRUD) | ✅ |
| **C+** | **—** | **Exercise Objectives Schema (backport)** | **🔲** |
| **C+** | **—** | **Library Source FK (backport)** | **🔲** |
| **D** | **MVP-J** | **Unified Exercise Clock** | **✅** |
| **D** | **MVP-K** | **Manual Inject Firing** | **✅** |
| **D** | **MVP-M** | **Inject Status Workflow** | **✅** |
| **E** | **MVP-N** | **Evaluator Observations** | **✅** |
| F | MVP-H | Sort, Filter & Group Injects | 🔲 |
| F | MVP-I | Global Search | 🔲 |
| G | MVP-F | Excel Import | ⏸️ Deferred |
| G | MVP-G | Excel Export | ⏸️ Deferred |
| H | MVP-O | Real-Time Data Sync | ✅ |
| H | MVP-P | Offline Capability | ✅ |
| H | MVP-Q | Sync on Reconnect | ✅ |
| I | — | PWA (App Shell, Install) | ✅ |
| J-1 | MVP-S1 | Review Mode | 🔲 |
| J-1 | MVP-S2 | Inject Comment Threads | 🔲 |
| J-1 | MVP-S3 | Exercise Objective Management UI | 🔲 |
| J-2 | MVP-S4 | Live Collaborative Editing + Presence | 🔲 |
| J-2 | MVP-S5 | Coverage Dashboard (heatmap) | 🔲 |
| J-2 | MVP-S6 | Coverage Gap Alerts | 🔲 |
| J-3 | MVP-S7 | Comment Resolution Workflow | 🔲 |
| J-3 | MVP-S8 | MSEL Lock for Conduct | 🔲 |
| J-4 | MVP-S9 | Guest Portal (no-login review) | 🔲 |
| J-4 | MVP-S10 | Formal Lane Approval | 🔲 |
| J-5 | MVP-S11 | Conflict Detection | 🔲 |
| J-6 | MVP-S12 | Audit Trail + Export | 🔲 |
| K | MVP-T1 | Inject Library (save + browse) | 🔲 |
| K | MVP-T2 | Add Library Inject to MSEL | 🔲 |
| K | MVP-T3 | Cadence Starter Packs | 🔲 |
| K | MVP-T4 | Library Management + Analytics | 🔲 |

**Progress:** 15/36 features complete (42%)
**Immediate next:** C+ backports → F (Inject Organization)
**First standalone planning value:** J-1 — 16/36 (44%)
**Planning phase complete:** J-4 — 26/36 (72%)
**Platform complete:** K — 36/36 (100%)

---

## Feature File Index

| Feature | Location | Phase |
|---------|----------|-------|
| Exercise CRUD | `docs/features/exercise-crud/` | B |
| Inject CRUD | `docs/features/inject-crud/` | C |
| Data Model Backports | `docs/features/data-model-backports/` | C+ |
| Exercise Clock | `docs/features/exercise-clock/` (directory pending) | D |
| Evaluator Observations | `docs/features/exercise-observations/` | E |
| Inject Organization | `docs/features/inject-filtering/` | F |
| Excel Import | `docs/features/excel-import/` | G |
| Excel Export | `docs/features/excel-export/` | G |
| **Exercise Objectives** | **`docs/features/exercise-objectives/`** | **C+ / J-1** |
| **Review Mode** | **`docs/features/msel-review-mode/`** | **J-1** |
| **Inject Comments** | **`docs/features/inject-comments/`** | **J-1** |
| **Coverage Intelligence** | **`docs/features/coverage-intelligence/`** | **J-2** |
| **Live Collaboration** | **`docs/features/live-collaboration/`** | **J-2** |
| **Review Workflow** | **`docs/features/review-workflow/`** | **J-3** |
| **Guest Portal** | **`docs/features/guest-portal/`** | **J-4** |
| **Conflict Detection** | **`docs/features/conflict-detection/`** | **J-5** |
| **Audit Trail** | **`docs/features/review-audit-trail/`** | **J-6** |
| **Collaborative MSEL Review** | **`docs/features/collaborative-msel-review/`** | **J (epic)** |
| **Inject Library** | **`docs/features/inject-library/`** | **K** |

---

## Quick Reference

| Item | Location |
|------|----------|
| AI Instructions | `CLAUDE.md` |
| Styling Guide | `docs/COBRA_STYLING.md` |
| Coding Standards | `docs/CODING_STANDARDS.md` |
| Feature Stories | `docs/features/{feature-name}/` |
| Agent Definitions | `.claude/agents/` |
| Domain Terminology | `.claude/agents/cadence-domain-agent.md` |
| Deployment Docs | `docs/deployment/` |
| Collaborative Review Epic | `docs/features/collaborative-msel-review/epic-collaborative-msel-review.md` |
| Coverage Intelligence Feature | `docs/features/coverage-intelligence/feature-coverage-intelligence.md` |
| Guest Portal Feature | `docs/features/guest-portal/feature-guest-portal.md` |
| Review Mode Feature | `docs/features/msel-review-mode/feature-review-mode.md` |
| Inject Library Feature | `docs/features/inject-library/feature-inject-library.md` |

---

## Changelog

| Date | Phase | Update |
|------|-------|--------|
| 2026-01-12 | 0 | Infrastructure complete |
| 2026-01-12 | A | Frontend foundation complete |
| 2026-01-12 | B | Exercise CRUD complete, UX patterns established |
| 2026-01-13 | C | Inject CRUD complete, Phase management added |
| 2026-01-13 | — | **Phases reorganized** to prioritize conduct over Excel import |
| 2026-01-13 | D | Phase D prompt created (Exercise Conduct) |
| 2026-01-13 | D+E | Parallel prompt created — orchestrator coordinates both streams |
| 2026-01-13 | F | Phase F prompt created (Inject Organization) |
| 2026-01-13 | D | Addendum created — clock-based inject workflow |
| 2026-01-13 | D+E | Conduct UX improvements prompt — progress bar, observation links, previews, narrative view |
| 2026-01-13 | — | Review/AAR Mode feature stories created (6 stories, P1-P2) |
| 2026-01-14 | G | Phase G deferred — Excel import/export postponed for initial testing |
| 2026-01-14 | H | Phase H prompt created (Real-Time & Offline) |
| 2026-01-14 | H | **Phase H complete** — centralized app-level sync service, IndexedDB caching |
| 2026-01-14 | I | Phase I prompt created (PWA) |
| 2026-03-10 | J | **Phase J designed** — Collaborative MSEL Review epic (6 sub-phases, 12 MVP features) |
| 2026-03-10 | J | Coverage Intelligence feature drafted — CI-1 through CI-6, heatmap dashboard |
| 2026-03-10 | J | Guest Portal feature drafted — GP-1 through GP-5, no-login external review |
| 2026-03-10 | J | Review Mode feature drafted — RM-1 through RM-4, de-cluttered annotation context |
| 2026-03-10 | K | Inject Library feature drafted — IL-1 through IL-5, starter packs |
| 2026-03-10 | C+ | **Data model backports identified** — ExerciseObjectives + LibraryInjects stubs must precede Phase D |
| 2026-03-13 | — | **Status corrections** — Phases D, E, I verified complete; phases summary filed to docs/ |
