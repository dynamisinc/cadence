# S13: Generate HSEEP EEG Document

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 8

## User Story

**As an** Exercise Director,
**I want** to generate a pre-filled Exercise Evaluation Guide document that matches the official HSEEP template format,
**So that** evaluators have a ready-to-use guide during conduct and I can produce completed EEG documents for After-Action Review.

## Context

The Exercise Evaluation Guide (EEG) is a standardized HSEEP document that serves two purposes:

1. **Before conduct (Planning):** Provides evaluators with a structured guide listing the Capability Targets, Critical Tasks, and sources they will assess. Evaluators bring this document (printed or digital) to the exercise.
2. **After conduct (AAR):** Captures completed observations, P/S/M/U ratings, and evaluator information. Serves as a primary input for the After-Action Report.

Currently, most organizations fill out the HSEEP EEG template (a Word document) by hand — copying capability targets and critical tasks from planning documents into the template, then printing copies for each evaluator. This is tedious and error-prone.

Cadence already captures all the data needed to auto-generate this document. This story produces a Word (.docx) file that matches the official HSEEP template layout, pre-populated with the exercise's capability targets, critical tasks, and (optionally) completed evaluation data.

### Two Generation Modes

| Mode | When | What's Included | Primary User |
|------|------|-----------------|--------------|
| **Blank EEG** | Before conduct | Targets, tasks, sources — observation columns empty | Exercise Director distributes to Evaluators |
| **Completed EEG** | After conduct | Targets, tasks, sources, observations, ratings, evaluator info | Exercise Director for AAR preparation |

### HSEEP Template Structure

The generated document follows the official HSEEP EEG template (2020 revision):

**Page 1 — Capability Overview:**
- Exercise metadata header (name, date, organization, venue)
- Mission area and exercise objective
- Core capability name and description
- Organizational Capability Targets (1–3 per page)
- Critical Tasks under each target
- Source references for each target

**Page 2 — Rating Chart:**
- Table: Capability Target | Critical Tasks | Observation Notes | Rating
- Final Core Capability Rating
- Evaluator Information (name, email, phone)
- Ratings Key (P/S/M/U definitions)
- Full Rating Definitions

**Note:** The template is organized *per Core Capability*. An exercise with 3 capabilities generates 3 EEG documents (or a combined document with sections per capability).

## Acceptance Criteria

### Access & Navigation

- [ ] **Given** I am on the EEG Setup tab (S03), **when** Capability Targets exist, **then** I see a "Generate EEG" button
- [ ] **Given** I am on the EEG Review page (S07/S09), **when** EEG entries exist, **then** I see a "Generate EEG" button with mode options
- [ ] **Given** I am a Director+ role, **when** I click Generate EEG, **then** I see the generation dialog
- [ ] **Given** I am an Evaluator, **when** I look for Generate EEG, **then** I do not see the button (Director+ only)
- [ ] **Given** no Capability Targets are defined, **when** I look for Generate EEG, **then** the button is disabled with tooltip "Define Capability Targets first"

### Generation Dialog

- [ ] **Given** the Generate EEG dialog, **when** displayed, **then** I see mode selection: "Blank (for evaluators)" and "Completed (with observations)"
- [ ] **Given** the dialog, **when** displayed, **then** I see the exercise name and capability target count
- [ ] **Given** the Completed mode, **when** EEG entries exist, **then** I see summary: "24 entries across 3 capabilities"
- [ ] **Given** the Completed mode, **when** no EEG entries exist, **then** the mode shows a warning: "No EEG entries recorded yet"
- [ ] **Given** the dialog, **when** displayed, **then** I can choose output format: "Single document (all capabilities)" or "Separate document per capability"
- [ ] **Given** separate documents selected, **when** generated, **then** a ZIP file downloads containing one .docx per capability

### Blank EEG Content

- [ ] **Given** I generate a Blank EEG, **when** the document opens, **then** the header shows Exercise Name, Date, Organization, and Venue
- [ ] **Given** the Blank EEG, **when** opened, **then** each Core Capability has its own section with Mission Area header
- [ ] **Given** each capability section, **when** displayed, **then** I see the linked Exercise Objective (if one is linked via E3-S09)
- [ ] **Given** each capability section, **when** displayed, **then** Capability Targets are listed with their descriptions
- [ ] **Given** each Capability Target, **when** displayed, **then** Critical Tasks are listed below it
- [ ] **Given** each Capability Target, **when** it has Sources (S11), **then** the Sources line appears below the tasks
- [ ] **Given** the Rating Chart page, **when** displayed, **then** the table has rows for each target with empty observation and rating columns
- [ ] **Given** the Rating Chart page, **when** displayed, **then** Evaluator Information section has blank lines for Name, Email, Phone
- [ ] **Given** the Rating Chart page, **when** displayed, **then** Ratings Key and full Rating Definitions are included

### Completed EEG Content

- [ ] **Given** I generate a Completed EEG, **when** the document opens, **then** all Blank EEG content is present
- [ ] **Given** the Rating Chart table, **when** a task has EEG entries, **then** the Observation Notes column contains the observation text
- [ ] **Given** a task with multiple EEG entries, **when** displayed, **then** observations are combined with evaluator attribution and timestamps
- [ ] **Given** the Rating Chart table, **when** a task has ratings, **then** the Target Rating column shows the P/S/M/U rating
- [ ] **Given** a target with multiple task ratings, **when** displayed, **then** a representative rating is shown (worst-case or most common — configurable)
- [ ] **Given** the Final Core Capability Rating, **when** entries exist, **then** an aggregate rating is computed and displayed
- [ ] **Given** the Evaluator Information section, **when** entries exist, **then** evaluator name, email, and phone (S12) are populated
- [ ] **Given** multiple evaluators contributed, **when** generating a single document, **then** all evaluators are listed

### Document Formatting

- [ ] **Given** the generated document, **when** opened in Microsoft Word, **then** the layout matches the HSEEP template structure
- [ ] **Given** the generated document, **when** opened, **then** it uses professional formatting (consistent fonts, borders, spacing)
- [ ] **Given** the generated document, **when** opened, **then** tables render correctly without broken layouts
- [ ] **Given** the generated document, **when** printed, **then** page breaks fall between logical sections (not mid-table)
- [ ] **Given** the document, **when** opened, **then** the filename follows pattern: `EEG_{ExerciseName}_{CapabilityName}_{Date}.docx`

### Generation Process

- [ ] **Given** I click Generate, **when** processing, **then** I see a progress indicator
- [ ] **Given** generation completes, **when** ready, **then** the file downloads automatically
- [ ] **Given** generation fails, **when** an error occurs, **then** I see an error message with retry option
- [ ] **Given** a large exercise (10+ capabilities, 50+ tasks), **when** generating, **then** the process completes within 30 seconds

## Wireframes

### Generate EEG Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Generate Exercise Evaluation Guide                              [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Exercise: Hurricane Response TTX                                       │
│  Capabilities: 3 targets defined, 12 critical tasks                     │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Document Mode                                                          │
│                                                                         │
│  [● Blank EEG (for evaluators)]                                        │
│      Capability targets and tasks with empty observation columns.       │
│      Print or share with evaluators before exercise conduct.            │
│                                                                         │
│  [○ Completed EEG (with observations)]                                  │
│      Includes all EEG entries, ratings, and evaluator info.             │
│      24 entries across 3 capabilities.                                  │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Output Format                                                          │
│                                                                         │
│  [● Single document (all capabilities)]                                │
│  [○ Separate document per capability (ZIP download)]                   │
│                                                                         │
│                                          [Cancel]  [Generate]           │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Generated Document — Page 1 (Capability Overview)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│                       Exercise Evaluation Guide                         │
│                                                                         │
│  Exercise Name:  Hurricane Response TTX                                 │
│  Exercise Date:  February 15, 2026                                      │
│  Organization:   Metro County Emergency Management Agency               │
│  Venue:          Metro County EOC                                       │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  RESPONSE                                                       │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │  Exercise Objective: Evaluate the county's ability to           │   │
│  │  activate and coordinate emergency response operations          │   │
│  │  within 60 minutes of a hurricane warning.                      │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │  Core Capability: Operational Communications                    │   │
│  │                                                                 │   │
│  │  Conduct a systematic process to establish interoperable        │   │
│  │  communications across agencies and jurisdictions.              │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │  Organizational Capability Target 1:                            │   │
│  │  Establish interoperable communications within 30 minutes       │   │
│  │  of EOC activation.                                             │   │
│  │                                                                 │   │
│  │  Critical Task: Issue EOC activation notification to all        │   │
│  │                 stakeholders                                    │   │
│  │  Critical Task: Activate emergency communication systems        │   │
│  │  Critical Task: Establish radio net with field units            │   │
│  │                                                                 │   │
│  │  Source(s): Metro County EOP, Annex F; SOP 5.2                  │   │
│  ├─────────────────────────────────────────────────────────────────┤   │
│  │  Organizational Capability Target 2:                            │   │
│  │  Test backup communication systems within 45 minutes.           │   │
│  │                                                                 │   │
│  │  Critical Task: Activate satellite phone system                 │   │
│  │  Critical Task: Test amateur radio backup                       │   │
│  │                                                                 │   │
│  │  Source(s): Metro County COOP Plan; FCC Part 97                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Generated Document — Page 2 (Rating Chart)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ┌──────────────┬─────────────┬──────────────────────┬────────────┐   │
│  │ Capability   │ Associated  │ Observation Notes    │ Target     │   │
│  │ Target       │ Critical    │ and Explanation      │ Rating     │   │
│  │              │ Tasks       │ of Rating            │            │   │
│  ├──────────────┼─────────────┼──────────────────────┼────────────┤   │
│  │ Target 1:    │ • Issue EOC │ [Blank or completed  │ [P/S/M/U]  │   │
│  │ Establish    │   activation│  observation text]   │            │   │
│  │ comms within │ • Activate  │                      │            │   │
│  │ 30 min       │   systems   │                      │            │   │
│  │              │ • Establish │                      │            │   │
│  │              │   radio net │                      │            │   │
│  ├──────────────┼─────────────┼──────────────────────┼────────────┤   │
│  │ Target 2:    │ • Activate  │ [Blank or completed  │ [P/S/M/U]  │   │
│  │ Test backup  │   sat phone │  observation text]   │            │   │
│  │ comms within │ • Test ham  │                      │            │   │
│  │ 45 min       │   radio     │                      │            │   │
│  └──────────────┴─────────────┴──────────────────────┴────────────┘   │
│                                                                         │
│  Final Core Capability Rating: [P/S/M/U or blank]                      │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Evaluator Information                                          │   │
│  │  Name:   [Sarah Kim or blank line]                              │   │
│  │  Email:  [sarah.kim@metrocounty.gov or blank line]              │   │
│  │  Phone:  [(555) 123-4567 or blank line]                         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Ratings Key                                                    │   │
│  │  P: Performed without challenges                                │   │
│  │  S: Performed with some challenges                              │   │
│  │  M: Performed with major challenges                             │   │
│  │  U: Unable to be performed                                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Ratings Definitions                                                    │
│                                                                         │
│  Performed without Challenges (P): The targets and critical tasks...   │
│  Performed with Some Challenges (S): The targets and critical tasks... │
│  Performed with Major Challenges (M): The targets and critical tasks.. │
│  Unable to be Performed (U): The targets and critical tasks...         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Rating Aggregation Logic

When generating the Completed EEG, ratings must be aggregated from individual EEG entries to the Capability Target and Core Capability levels.

### Target Rating (from multiple task ratings)

Use **worst-case** by default: if any task received U, target is U; if any M, target is M; etc.

| Task Ratings | Target Rating |
|---|---|
| P, P, P | P |
| P, S, P | S |
| P, S, M | M |
| P, S, U | U |

### Core Capability Rating (from multiple target ratings)

Same worst-case aggregation across targets.

### Multiple Entries per Task

If multiple evaluators rated the same task, include all observations in the Notes column, attributed by evaluator:

```
[R. Chen, 09:15] EOC issued activation notification promptly...
[S. Kim, 09:22] Confirmed all stakeholders received notification...
```

Use the **lowest (worst) rating** among evaluators for the rating column.

## Out of Scope

- PDF export (future enhancement — S10 already plans this)
- EEG template customization (logo, colors, custom sections)
- Batch generation across multiple exercises
- EEG document editing within Cadence (it's a download-and-use artifact)
- Auto-email EEG documents to evaluators (future enhancement)
- Custom rating aggregation strategies (configurable per exercise)

## Dependencies

- S01-S04: Capability Targets and Critical Tasks exist
- S06-S08: EEG Entries exist (for Completed mode)
- S11: Sources field on CapabilityTarget (for Source(s) line)
- S12: Evaluator phone number (for Evaluator Information section)
- Exercise Objectives (E3-S07/S08/S09) for objective display
- Exercise entity (name, date, location, organization)

## Technical Notes

- Use server-side document generation (.NET library: DocumentFormat.OpenXml or similar)
- Consider using the uploaded HSEEP template as a base and filling in fields (template-driven approach) vs. building from scratch (code-driven approach). Template-driven is preferred for accuracy.
- The HSEEP template uses specific formatting (bordered tables, shaded headers, specific fonts) — match as closely as practical
- For "Separate document per capability" mode, generate individual files and package in a ZIP
- File generation should be an API endpoint: `POST /api/exercises/{exerciseId}/eeg-document` with body specifying mode and format options
- Consider caching generated documents for large exercises (invalidate on EEG data changes)

## Test Scenarios

### Unit Tests
- Rating aggregation logic (worst-case across tasks)
- Capability rating aggregation (worst-case across targets)
- Multiple evaluator observation formatting
- Filename generation pattern
- Empty state handling (no tasks, no entries)

### Integration Tests
- Generate Blank EEG → valid .docx opens in Word
- Generate Completed EEG → observations and ratings populate correctly
- Generate with missing Sources → Sources line omitted gracefully
- Generate with no Exercise Objective linked → objective line omitted
- Generate per-capability ZIP → contains correct number of documents
- Large exercise generation completes within time limit
- Generated document prints cleanly (no mid-table page breaks)
- Permission check: Director can generate, Evaluator cannot

---

*Story created: 2026-02-05*
*Origin: EEG Template Gap Analysis — Recommendation*
