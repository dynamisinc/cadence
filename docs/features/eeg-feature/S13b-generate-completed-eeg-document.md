# S13b: Generate Completed EEG Document

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to generate a completed Exercise Evaluation Guide document with all observations and ratings,
**So that** I can produce HSEEP-compliant documentation for the After-Action Review.

## Context

After exercise conduct, Directors need to compile evaluator observations into formal documentation. The completed EEG document includes:
- All Capability Targets and Critical Tasks (same as blank EEG)
- Evaluator observations filled in
- P/S/M/U ratings for each target
- Aggregated capability ratings
- Evaluator contact information

This document serves as a primary input for the After-Action Report (AAR).

### When to Use

- **After conduct:** Generate completed EEG for AAR preparation
- **Stakeholder review:** Share evaluation results in official format
- **Documentation:** Archive HSEEP-compliant evaluation records

### Relationship to S13a

Story S13a generates the **blank** EEG for pre-conduct distribution.
This story (S13b) generates the **completed** EEG with observations and ratings.

## Acceptance Criteria

### Access & Navigation

- [ ] **Given** I am on the EEG Review page (S07/S09), **when** EEG entries exist, **then** I see a "Generate EEG" button
- [ ] **Given** I am a Director+ role, **when** I click Generate EEG, **then** I see the generation dialog with mode options
- [ ] **Given** I am an Evaluator, **when** I look for Generate EEG, **then** I do not see the button (Director+ only)
- [ ] **Given** no EEG entries exist, **when** I access the dialog, **then** I see warning: "No EEG entries recorded yet"

### Generation Dialog

- [ ] **Given** the Generate EEG dialog, **when** displayed, **then** I see mode selection: "Blank" and "Completed (with observations)"
- [ ] **Given** the Completed mode, **when** EEG entries exist, **then** I see summary: "24 entries across 3 capabilities"
- [ ] **Given** the Completed mode, **when** no EEG entries exist, **then** the mode shows warning
- [ ] **Given** the dialog, **when** displayed, **then** I can choose output format: "Single document" or "Separate per capability"

### Phone Number Prompt (S12 Integration)

- [ ] **Given** I am generating a Completed EEG, **when** my phone is not on file, **then** I see a prompt to add contact info before generation
- [ ] **Given** I skip the phone prompt, **when** the document generates, **then** phone line shows "[Not provided]"
- [ ] **Given** I add my phone, **when** the document generates, **then** my phone appears in Evaluator Information

### Document Content — Observations

- [ ] **Given** I generate a Completed EEG, **when** the document opens, **then** all Blank EEG content is present (S13a)
- [ ] **Given** the Rating Chart table, **when** a task has EEG entries, **then** the Observation Notes column contains the observation text
- [ ] **Given** a task with no EEG entries, **when** displayed, **then** the observation column shows "[Not Evaluated]"

### Multiple Evaluators per Task

- [ ] **Given** a task with entries from multiple evaluators, **when** displayed, **then** observations are combined with evaluator attribution
- [ ] **Given** multiple entries, **when** formatted, **then** each entry shows: "[Evaluator Name, Time] Observation text..."
- [ ] **Given** multiple evaluators, **when** space is limited, **then** observations are truncated with indicator

### Document Content — Ratings

- [ ] **Given** the Rating Chart table, **when** a task has ratings, **then** the Target Rating column shows the P/S/M/U rating
- [ ] **Given** a target with multiple task ratings, **when** displayed, **then** a representative rating is shown (worst-case aggregation)
- [ ] **Given** the Final Core Capability Rating, **when** entries exist, **then** an aggregate rating is computed and displayed
- [ ] **Given** a task with no rating, **when** displayed, **then** the rating column shows "[N/E]" (Not Evaluated)

### Rating Aggregation Logic

- [ ] **Given** a Critical Task has entries with mixed ratings (P, M), **when** aggregating, **then** worst-case rating is used (M)
- [ ] **Given** multiple tasks under a target, **when** calculating target rating, **then** worst-case across tasks is used
- [ ] **Given** all tasks under a target are not evaluated, **when** displayed, **then** target rating shows "[Insufficient Data]"

### Evaluator Information Section

- [ ] **Given** the Evaluator Information section, **when** entries exist, **then** evaluator name, email, and phone are populated
- [ ] **Given** multiple evaluators contributed, **when** generating a single document, **then** all evaluators are listed
- [ ] **Given** evaluator has phone number (S12), **when** displayed, **then** phone appears
- [ ] **Given** evaluator has no phone, **when** displayed, **then** phone shows "[Not provided]"

### Document Formatting

- [ ] **Given** the generated document, **when** opened in Microsoft Word, **then** the layout matches HSEEP template
- [ ] **Given** observation text is long, **when** formatted, **then** text wraps properly within table cell
- [ ] **Given** the document, **when** printed, **then** page breaks fall between logical sections

### Generation Process

- [ ] **Given** I click Generate, **when** processing, **then** I see a progress indicator
- [ ] **Given** generation completes, **when** ready, **then** the file downloads automatically
- [ ] **Given** generation fails, **when** an error occurs, **then** I see an error message with retry option
- [ ] **Given** a large exercise (10+ capabilities, 100+ entries), **when** generating, **then** process completes within 60 seconds

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|-------|----------|------------|-----------|----------|
| Generate Completed EEG | ✅ | ✅ | ❌ | ❌ | ❌ |

## Rating Aggregation Logic

### Task Rating (from multiple evaluator entries)

When multiple evaluators rate the same task, use **worst-case**:

| Evaluator Ratings | Task Rating |
|-------------------|-------------|
| P, P | P |
| P, S | S |
| S, M | M |
| P, U | U |

### Target Rating (from multiple task ratings)

Use **worst-case** across all tasks in the target:

| Task Ratings | Target Rating |
|--------------|---------------|
| P, P, P | P |
| P, S, P | S |
| P, S, M | M |
| P, S, U | U |
| [N/E], [N/E] | [Insufficient Data] |
| P, [N/E] | P (evaluated tasks only) |

### Core Capability Rating (from multiple target ratings)

Same worst-case aggregation across targets.

## API Specification

### POST /api/exercises/{exerciseId}/eeg-document

**Request Body:**
```json
{
  "mode": "completed",
  "outputFormat": "single",  // or "perCapability"
  "ratingAggregation": "worstCase"  // future: "mostCommon"
}
```

**Response 200:** File download
- Content-Type: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- Content-Disposition: `attachment; filename="EEG_Hurricane_Response_TTX_OperationalComms_Completed_2026-02-15.docx"`

**Response 200 (ZIP):** For perCapability mode
- Content-Type: `application/zip`

**Response 400:** Validation error
**Response 401:** Unauthorized
**Response 403:** Not Director+ role

## Wireframes

### Generate EEG Dialog (Completed Mode)

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
│  [○ Blank EEG (for evaluators)]                                        │
│                                                                         │
│  [● Completed EEG (with observations)]                                  │
│      Includes all EEG entries, ratings, and evaluator info.             │
│      ✓ 24 entries across 3 capabilities                                │
│      ✓ 8 of 12 tasks evaluated (67%)                                   │
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

### Generated Document — Completed Rating Chart

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ┌──────────────┬─────────────┬──────────────────────┬────────────┐   │
│  │ Capability   │ Associated  │ Observation Notes    │ Target     │   │
│  │ Target       │ Critical    │ and Explanation      │ Rating     │   │
│  │              │ Tasks       │ of Rating            │            │   │
│  ├──────────────┼─────────────┼──────────────────────┼────────────┤   │
│  │ Target 1:    │ • Issue EOC │ [R. Chen, 09:15]     │     S      │   │
│  │ Establish    │   activation│ EOC issued activa-   │            │   │
│  │ comms within │ • Activate  │ tion notification    │            │   │
│  │ 30 min       │   systems   │ promptly. All stake- │            │   │
│  │              │ • Establish │ holders confirmed.   │            │   │
│  │              │   radio net │                      │            │   │
│  │              │             │ [S. Kim, 09:22]      │            │   │
│  │              │             │ Confirmed all notif- │            │   │
│  │              │             │ ications received... │            │   │
│  ├──────────────┼─────────────┼──────────────────────┼────────────┤   │
│  │ Target 2:    │ • Activate  │ [R. Chen, 10:05]     │     M      │   │
│  │ Test backup  │   sat phone │ Satellite phone      │            │   │
│  │ comms within │ • Test ham  │ activated but had    │            │   │
│  │ 45 min       │   radio     │ major delays due to  │            │   │
│  │              │             │ equipment issues...  │            │   │
│  └──────────────┴─────────────┴──────────────────────┴────────────┘   │
│                                                                         │
│  Final Core Capability Rating:    S                                    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Evaluator Information                                          │   │
│  │                                                                 │   │
│  │  Robert Chen                                                    │   │
│  │  robert.chen@metrocounty.gov                                    │   │
│  │  (555) 123-4567                                                 │   │
│  │                                                                 │   │
│  │  Sarah Kim                                                      │   │
│  │  sarah.kim@metrocounty.gov                                      │   │
│  │  [Not provided]                                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Multiple Evaluator Observation Format

```
[R. Chen, 09:15] EOC issued activation notification at 09:15. All
stakeholders confirmed receipt within 10 minutes. Communication plan
followed correctly per SOP 5.2.

[S. Kim, 09:22] Confirmed all stakeholders received notification.
Minor delay in reaching Field Unit 3 due to radio interference -
switched to backup channel successfully.
```

## Out of Scope

- PDF export (future enhancement)
- EEG template customization
- Observation editing within the document (generate-and-use)
- Alternative aggregation strategies (only worst-case supported)
- Batch generation across multiple exercises

## Dependencies

- S13a: Blank EEG generation (shared document generation infrastructure)
- S06-S08: EEG Entries exist
- S11: Sources field on CapabilityTarget
- S12: Evaluator phone number
- Exercise Objectives (E3-S07/S08/S09) for objective display

## Technical Notes

- Reuse document generation infrastructure from S13a
- Rating aggregation should be a shared service (also used by S09 dashboard)
- For long observations, truncate at ~500 characters with "..." indicator
- Multiple evaluator format: `[Name, HH:MM] Observation text...`
- Consider caching generated documents for large exercises (invalidate on EEG data changes)

## Test Scenarios

### Unit Tests
- Rating aggregation logic (worst-case across tasks)
- Capability rating aggregation (worst-case across targets)
- Multiple evaluator observation formatting
- Empty state handling (no tasks, no entries)
- Truncation of long observations

### Integration Tests
- Generate Completed EEG → valid .docx opens in Word
- Observations and ratings populate correctly
- Multiple evaluator observations formatted correctly
- Evaluator info includes phone when available
- Generate with missing Sources → Sources line omitted gracefully
- Generate per-capability ZIP → correct documents included
- Large exercise generation completes within time limit
- Permission check: Director can generate, Evaluator cannot

---

*Story created: 2026-02-05*
*Split from S13 for INVEST compliance*
*Origin: EEG Template Gap Analysis — Recommendation*
