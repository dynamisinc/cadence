# S13a: Generate Blank EEG Document

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to generate a blank Exercise Evaluation Guide document that matches the official HSEEP template format,
**So that** evaluators have a ready-to-use guide to bring to exercise conduct.

## Context

The Exercise Evaluation Guide (EEG) is a standardized HSEEP document. Before exercise conduct, Directors generate and distribute blank EEG forms to evaluators. This document lists all Capability Targets and Critical Tasks the evaluators will assess, with empty columns for observations and ratings.

Currently, most organizations fill out the HSEEP EEG template (a Word document) by hand — copying capability targets and critical tasks from planning documents into the template, then printing copies for each evaluator. This is tedious and error-prone.

Cadence captures all the data needed to auto-generate this document. This story produces a Word (.docx) file that matches the official HSEEP template layout, pre-populated with the exercise's capability targets and critical tasks.

### When to Use

- **Before conduct:** Director generates and prints blank EEGs for evaluators
- **Digital evaluators:** Can view the blank EEG on tablets during conduct
- **Exercise planning:** Verify all targets/tasks are properly documented

### Relationship to S13b

This story (S13a) generates the **blank** EEG for pre-conduct distribution.
Story S13b generates the **completed** EEG with observations and ratings for post-conduct AAR.

## Acceptance Criteria

### Access & Navigation

- [ ] **Given** I am on the EEG Setup tab (S03), **when** Capability Targets exist, **then** I see a "Generate EEG" button
- [ ] **Given** I am a Director+ role, **when** I click Generate EEG, **then** I see the generation dialog
- [ ] **Given** I am an Evaluator, **when** I look for Generate EEG, **then** I do not see the button (Director+ only)
- [ ] **Given** no Capability Targets are defined, **when** I look for Generate EEG, **then** the button is disabled with tooltip "Define Capability Targets first"

### Generation Dialog

- [ ] **Given** the Generate EEG dialog, **when** displayed, **then** I see the exercise name and capability target count
- [ ] **Given** the dialog, **when** displayed, **then** "Blank (for evaluators)" mode is selected by default
- [ ] **Given** the dialog, **when** displayed, **then** I can choose output format: "Single document (all capabilities)" or "Separate document per capability"
- [ ] **Given** separate documents selected, **when** generated, **then** a ZIP file downloads containing one .docx per capability

### Document Structure — Page 1 (Capability Overview)

- [ ] **Given** I generate a Blank EEG, **when** the document opens, **then** the header shows Exercise Name, Date, Organization, and Venue
- [ ] **Given** the Blank EEG, **when** opened, **then** each Core Capability has its own section with Mission Area header
- [ ] **Given** each capability section, **when** displayed, **then** I see the linked Exercise Objective (if one is linked via E3-S09)
- [ ] **Given** each capability section, **when** no objective is linked, **then** the objective line is omitted gracefully
- [ ] **Given** each capability section, **when** displayed, **then** Capability Targets are listed with their descriptions
- [ ] **Given** each Capability Target, **when** displayed, **then** Critical Tasks are listed below it
- [ ] **Given** each Capability Target, **when** it has Sources (S11), **then** the Sources line appears below the tasks
- [ ] **Given** each Capability Target, **when** it has no Sources, **then** the Sources line is omitted

### Document Structure — Page 2 (Rating Chart)

- [ ] **Given** the Rating Chart page, **when** displayed, **then** the table has columns: Capability Target, Critical Tasks, Observation Notes, Rating
- [ ] **Given** the Rating Chart table, **when** displayed, **then** rows exist for each target with empty observation and rating columns
- [ ] **Given** the Rating Chart page, **when** displayed, **then** Final Core Capability Rating row appears at bottom (empty)
- [ ] **Given** the Rating Chart page, **when** displayed, **then** Evaluator Information section has blank lines for Name, Email, Phone
- [ ] **Given** the Rating Chart page, **when** displayed, **then** Ratings Key (P/S/M/U one-line definitions) is included
- [ ] **Given** the Rating Chart page, **when** displayed, **then** full Rating Definitions are included

### Document Formatting

- [ ] **Given** the generated document, **when** opened in Microsoft Word, **then** the layout matches the HSEEP template structure
- [ ] **Given** the generated document, **when** opened, **then** body text uses 11pt Calibri font
- [ ] **Given** table headers, **when** displayed, **then** they use bold text with light gray (15%) shading
- [ ] **Given** tables, **when** displayed, **then** they have 1pt black borders
- [ ] **Given** page setup, **when** printed, **then** margins are 1" on all sides with portrait orientation
- [ ] **Given** multi-page documents, **when** printed, **then** page numbers appear in footer
- [ ] **Given** the document, **when** printed, **then** page breaks fall between logical sections (not mid-table)
- [ ] **Given** the document, **when** opened, **then** the filename follows pattern: `EEG_{ExerciseName}_{CapabilityName}_{Date}.docx`

### Ratings Key Text

- [ ] **Given** the Ratings Key, **when** displayed, **then** it shows the HSEEP definitions:
  - P: Performed without Challenges
  - S: Performed with Some Challenges
  - M: Performed with Major Challenges
  - U: Unable to be Performed

### Full Rating Definitions

- [ ] **Given** the Rating Definitions section, **when** displayed, **then** it includes the complete HSEEP text for each rating

### Generation Process

- [ ] **Given** I click Generate, **when** processing, **then** I see a progress indicator
- [ ] **Given** generation completes, **when** ready, **then** the file downloads automatically
- [ ] **Given** generation fails, **when** an error occurs, **then** I see an error message with retry option
- [ ] **Given** a large exercise (10+ capabilities, 50+ tasks), **when** generating, **then** the process completes within 30 seconds

### Word Compatibility

- [ ] **Given** the generated .docx, **when** opened in Microsoft Word 2016+, **then** formatting renders correctly
- [ ] **Given** the generated .docx, **when** opened in Google Docs, **then** formatting renders acceptably (some variance allowed)
- [ ] **Given** the generated .docx, **when** opened in LibreOffice Writer, **then** content is readable (formatting may vary)

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|-------|----------|------------|-----------|----------|
| Generate Blank EEG | ✅ | ✅ | ❌ | ❌ | ❌ |

## API Specification

### POST /api/exercises/{exerciseId}/eeg-document

**Request Body:**
```json
{
  "mode": "blank",
  "outputFormat": "single"  // or "perCapability"
}
```

**Response 200:** File download
- Content-Type: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- Content-Disposition: `attachment; filename="EEG_Hurricane_Response_TTX_OperationalComms_2026-02-15.docx"`

**Response 200 (ZIP):** For perCapability mode
- Content-Type: `application/zip`
- Content-Disposition: `attachment; filename="EEG_Hurricane_Response_TTX_2026-02-15.zip"`

**Response 400:** Validation error (no capability targets defined)
```json
{
  "error": "NoCapabilityTargets",
  "message": "Define at least one Capability Target before generating an EEG document"
}
```

**Response 401:** Unauthorized
**Response 403:** Not Director+ role

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
│  [○ Completed EEG (with observations)]  → See S13b                     │
│      Includes all EEG entries, ratings, and evaluator info.             │
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
│  │ Target 1:    │ • Issue EOC │                      │            │   │
│  │ Establish    │   activation│                      │            │   │
│  │ comms within │ • Activate  │                      │            │   │
│  │ 30 min       │   systems   │                      │            │   │
│  │              │ • Establish │                      │            │   │
│  │              │   radio net │                      │            │   │
│  ├──────────────┼─────────────┼──────────────────────┼────────────┤   │
│  │ Target 2:    │ • Activate  │                      │            │   │
│  │ Test backup  │   sat phone │                      │            │   │
│  │ comms within │ • Test ham  │                      │            │   │
│  │ 45 min       │   radio     │                      │            │   │
│  └──────────────┴─────────────┴──────────────────────┴────────────┘   │
│                                                                         │
│  Final Core Capability Rating: ________                                │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Evaluator Information                                          │   │
│  │  Name:   _______________________________                        │   │
│  │  Email:  _______________________________                        │   │
│  │  Phone:  _______________________________                        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Ratings Key                                                    │   │
│  │  P: Performed without Challenges                                │   │
│  │  S: Performed with Some Challenges                              │   │
│  │  M: Performed with Major Challenges                             │   │
│  │  U: Unable to be Performed                                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Document Field Sources

| Document Field | Source in Cadence |
|----------------|-------------------|
| Exercise Name | Exercise.Name |
| Exercise Date | Exercise.ScheduledDate |
| Organization | Exercise.Organization.Name |
| Venue | Exercise.Location |
| Mission Area | Capability.MissionArea |
| Exercise Objective | Objective linked to Capability (E3-S09) |
| Core Capability Name | Capability.Name |
| Capability Description | Capability.Description |
| Capability Target | CapabilityTarget.TargetDescription |
| Critical Task | CriticalTask.TaskDescription |
| Sources | CapabilityTarget.Sources (S11) |

## Out of Scope

- PDF export (future enhancement)
- EEG template customization (logo, colors, custom sections)
- Batch generation across multiple exercises
- EEG document editing within Cadence (download-and-use artifact)
- Auto-email EEG documents to evaluators (future enhancement)

## Dependencies

- S01-S04: Capability Targets and Critical Tasks exist
- S11: Sources field on CapabilityTarget
- Exercise Objectives (E3-S07/S08/S09) for objective display (optional)
- Exercise entity (name, date, location, organization)

## Technical Notes

- Use server-side document generation (.NET library: DocumentFormat.OpenXml or similar)
- Consider using the official HSEEP template as a base and filling in fields (template-driven approach)
- HSEEP terminology mapping: Cadence "Capability" → HSEEP "Core Capability"
- For "Separate document per capability" mode, generate individual files and package in a ZIP
- Sanitize filename: replace invalid characters with underscores

## Ratings Text (HSEEP Official)

Include these exact definitions in the generated document:

**Ratings Key (short):**
- P: Performed without Challenges
- S: Performed with Some Challenges
- M: Performed with Major Challenges
- U: Unable to be Performed

**Full Definitions:**
```
Performed without Challenges (P): The targets and critical tasks associated
with the core capability were completed in a manner that achieved the
objective(s) and met the performance measure(s).

Performed with Some Challenges (S): The targets and critical tasks associated
with the core capability were completed in a manner that achieved the
objective(s) and met the performance measure(s); however, some challenges
were noted.

Performed with Major Challenges (M): The targets and critical tasks associated
with the core capability were completed in a manner that achieved the
objective(s) and met the performance measure(s); however, significant
challenges were noted.

Unable to be Performed (U): The targets and critical tasks associated with
the core capability were not performed in a manner that achieved the
objective(s) or met the performance measure(s).
```

## Test Scenarios

### Unit Tests
- Document field mapping from Cadence entities
- Filename generation with special character handling
- Multiple capability section ordering
- Empty sources handling

### Integration Tests
- Generate Blank EEG → valid .docx opens in Word
- Generate with missing Sources → Sources line omitted gracefully
- Generate with no Exercise Objective linked → objective line omitted
- Generate per-capability ZIP → contains correct number of documents
- Large exercise generation completes within time limit
- Generated document prints cleanly (no mid-table page breaks)
- Permission check: Director can generate, Evaluator cannot

---

*Story created: 2026-02-05*
*Split from S13 for INVEST compliance*
*Origin: EEG Template Gap Analysis — Recommendation*
