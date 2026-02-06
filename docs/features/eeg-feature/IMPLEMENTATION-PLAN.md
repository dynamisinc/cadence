# EEG Feature: Implementation Plan

**Created:** 2026-02-05
**Status:** Ready for Implementation

## Overview

This document provides the phased implementation plan for EEG stories S07–S13, including the new stories identified from the HSEEP template gap analysis.

## Story Summary

| Story | Title | Points | Status | Notes |
|-------|-------|--------|--------|-------|
| S07 | View EEG Entries | 5 | Revised | Added API spec, pagination, real-time |
| S08 | Edit and Delete EEG Entry | 3 | Revised | Added API spec, concurrency |
| S09 | EEG Coverage Dashboard | 5 | Revised | Added compact mode, status variations |
| S10 | EEG-Based AAR Export | 8 | Revised | Added S11/S12 integration |
| S11 | Capability Target Sources | 2 | New | HSEEP template gap |
| S12 | Evaluator Contact Prompt | 3 | New | HSEEP template gap |
| S13a | Generate Blank EEG Document | 5 | New (split) | Pre-conduct guide |
| S13b | Generate Completed EEG Document | 5 | New (split) | Post-conduct AAR |

**Total Points (S07–S13):** 36 points

---

## Dependency Graph

```
                        ┌─────────────────────────────────────────────────┐
                        │         EXTERNAL DEPENDENCIES                    │
                        │  ┌───────────────────────────────────────────┐  │
                        │  │ S01-S05: Capability Targets, Tasks, Links  │  │
                        │  │ E3-S07/S08/S09: Exercise Objectives        │  │
                        │  │ User Authentication (existing)             │  │
                        │  │ Offline Sync Service (existing)            │  │
                        │  └───────────────────────────────────────────┘  │
                        └──────────────────────┬──────────────────────────┘
                                               │
┌──────────────────────────────────────────────▼──────────────────────────────────────────────┐
│                              PHASE 1: FIELD ADDITIONS                                        │
│                                                                                              │
│  ┌─────────────────┐                        ┌─────────────────┐                             │
│  │ S11: Sources    │                        │ S12: Evaluator  │                             │
│  │ Field (2 pts)   │                        │ Phone (3 pts)   │                             │
│  │                 │                        │                 │                             │
│  │ • DB migration  │                        │ • DB migration  │                             │
│  │ • DTO update    │                        │ • PATCH API     │                             │
│  │ • UI field      │                        │ • Prompt UI     │                             │
│  └────────┬────────┘                        └────────┬────────┘                             │
│           │                                          │                                       │
└───────────┼──────────────────────────────────────────┼───────────────────────────────────────┘
            │                                          │
            │      Can run in parallel                 │
            │                                          │
┌───────────▼──────────────────────────────────────────▼───────────────────────────────────────┐
│                              PHASE 2: CONDUCT (EEG ENTRY) — S06 assumed complete             │
└──────────────────────────────────────────────────────┬───────────────────────────────────────┘
                                                       │
┌──────────────────────────────────────────────────────▼───────────────────────────────────────┐
│                              PHASE 3: REVIEW & EDIT                                          │
│                                                                                              │
│  ┌─────────────────────────────┐    ┌─────────────────────────────┐                         │
│  │ S07: View EEG Entries       │───▶│ S08: Edit/Delete Entry      │                         │
│  │ (5 pts)                     │    │ (3 pts)                     │                         │
│  │                             │    │                             │                         │
│  │ • List view API             │    │ • PUT/DELETE APIs           │                         │
│  │ • Filtering & pagination    │    │ • Edit form                 │                         │
│  │ • Grouped views             │    │ • Delete confirmation       │                         │
│  │ • Entry detail              │    │ • Audit trail               │                         │
│  │ • Real-time updates         │    │                             │                         │
│  └─────────────────────────────┘    └─────────────────────────────┘                         │
│                       │                           │                                          │
└───────────────────────┼───────────────────────────┼──────────────────────────────────────────┘
                        │                           │
┌───────────────────────▼───────────────────────────▼──────────────────────────────────────────┐
│                              PHASE 4: DASHBOARD & PRE-CONDUCT DOC                            │
│                                                                                              │
│  ┌─────────────────────────────┐    ┌─────────────────────────────┐                         │
│  │ S09: Coverage Dashboard     │    │ S13a: Blank EEG Document    │                         │
│  │ (5 pts)                     │    │ (5 pts)                     │                         │
│  │                             │    │                             │                         │
│  │ • Coverage API              │    │ • Document generation       │                         │
│  │ • Dashboard UI              │    │ • HSEEP template format     │                         │
│  │ • Real-time updates         │    │ • Blank rating chart        │                         │
│  │ • Compact mode              │    │                             │                         │
│  └─────────────────────────────┘    └─────────────────────────────┘                         │
│                                                                                              │
│              Can run in parallel (different output formats)                                  │
│                                                                                              │
└──────────────────────────────────────────────────────┬───────────────────────────────────────┘
                                                       │
┌──────────────────────────────────────────────────────▼───────────────────────────────────────┐
│                              PHASE 5: POST-CONDUCT & EXPORT                                  │
│                                                                                              │
│  ┌─────────────────────────────┐    ┌─────────────────────────────┐                         │
│  │ S10: AAR Export             │    │ S13b: Completed EEG Doc     │                         │
│  │ (8 pts)                     │    │ (5 pts)                     │                         │
│  │                             │    │                             │                         │
│  │ • Excel export              │    │ • Completed doc generation  │                         │
│  │ • JSON export               │    │ • Rating aggregation        │                         │
│  │ • Include S11 Sources       │    │ • Multiple evaluators       │                         │
│  │ • Include S12 Phone         │    │ • Evaluator info (S12)      │                         │
│  └─────────────────────────────┘    └─────────────────────────────┘                         │
│                                                                                              │
│              Can run in parallel (Excel vs Word output)                                      │
│                                                                                              │
└──────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Phases

### Phase 1: Field Additions (Can Start Immediately)

**Points:** 5 (S11: 2, S12: 3)
**Parallelizable:** Yes — S11 and S12 modify different entities

| Story | Agent | Backend Files | Frontend Files |
|-------|-------|---------------|----------------|
| S11 | database → backend → frontend | `CapabilityTarget.cs`, DTOs, Migration | `CapabilityTargetForm.tsx` |
| S12 | database → backend → frontend | `ApplicationUser.cs`, `UsersController.cs`, Migration | `EvaluatorContactPrompt.tsx` |

**Key Deliverables:**
- S11: New `Sources` column on CapabilityTarget
- S12: New `PhoneNumber` column on ApplicationUser, PATCH endpoint

---

### Phase 2: Conduct (Assumed S06 Complete)

S06 (EEG Entry Form) is assumed to be implemented or in progress. S12 integrates with S06 by adding the phone prompt before the entry form.

---

### Phase 3: Review & Edit

**Points:** 8 (S07: 5, S08: 3)
**Blocked by:** S06 (entries must exist)

| Story | Agent | Backend Files | Frontend Files |
|-------|-------|---------------|----------------|
| S07 | backend → frontend | `EegEntryService.cs`, list endpoint | `EegReviewPage.tsx`, `EegEntryList.tsx` |
| S08 | backend → frontend | PUT/DELETE endpoints | `EegEntryEditForm.tsx`, `DeleteConfirmDialog.tsx` |

**Key Deliverables:**
- S07: List, filter, group, search EEG entries
- S08: Edit/delete with permissions and audit trail

---

### Phase 4: Dashboard & Pre-Conduct Document

**Points:** 10 (S09: 5, S13a: 5)
**Blocked by:** S06-S07, S11
**Parallelizable:** Yes — UI dashboard vs document generation

| Story | Agent | Backend Files | Frontend Files |
|-------|-------|---------------|----------------|
| S09 | backend → frontend | `EegCoverageService.cs`, coverage endpoint | `EegDashboard.tsx` |
| S13a | backend | `EegDocumentService.cs`, document endpoint | `GenerateEegDialog.tsx` |

**Key Deliverables:**
- S09: Real-time coverage dashboard with charts
- S13a: Blank EEG Word document generation

---

### Phase 5: Post-Conduct & Export

**Points:** 13 (S10: 8, S13b: 5)
**Blocked by:** S07-S09, S11, S12
**Parallelizable:** Yes — Excel export vs Word document

| Story | Agent | Backend Files | Frontend Files |
|-------|-------|---------------|----------------|
| S10 | backend | `EegExportService.cs`, export endpoint | `EegExportDialog.tsx` |
| S13b | backend | `EegDocumentService.cs` (extends S13a) | `GenerateEegDialog.tsx` |

**Key Deliverables:**
- S10: Excel/JSON export with all data
- S13b: Completed EEG with observations and ratings

---

## Sprint Allocation (Suggested)

```
Sprint N: Foundation + Fields (5 pts)
├── S11 (Sources field) ─────────────────────── 2 pts
└── S12 (Evaluator phone) ───────────────────── 3 pts

Sprint N+1: Review & Dashboard (13 pts)
├── S07 (View EEG Entries) ──────────────────── 5 pts
├── S08 (Edit/Delete Entry) ─────────────────── 3 pts
└── S09 (Coverage Dashboard) ────────────────── 5 pts

Sprint N+2: Documents & Export (18 pts)
├── S13a (Blank EEG Document) ───────────────── 5 pts
├── S13b (Completed EEG Document) ───────────── 5 pts
└── S10 (AAR Export) ────────────────────────── 8 pts
```

---

## Claude Code Agent Orchestration

### Parallel Work Streams

**Stream A: Backend EEG Core (backend-agent)**
- Owner: `Core/Features/Eeg/`
- Stories: S11 API, S12 API, S07 API, S08 API, S09 API

**Stream B: Frontend EEG UI (frontend-agent)**
- Owner: `frontend/src/features/eeg/`
- Stories: S11 UI, S12 UI, S07 UI, S08 UI, S09 UI
- Note: Blocked by corresponding backend APIs

**Stream C: Document Generation (backend-agent, separate session)**
- Owner: `Core/Features/Eeg/Services/EegDocumentService.cs`, `EegExportService.cs`
- Stories: S13a, S13b, S10
- Note: Can work independently once data models exist

**Stream D: Database Migrations (database-agent)**
- Owner: `Core/Migrations/`, entity files
- Stories: S11 migration, S12 migration
- Note: Should run first before backend/frontend work

### File Ownership Boundaries

| Feature Area | Backend Files | Frontend Files |
|--------------|--------------|----------------|
| S11 Sources | `CapabilityTarget.cs`, `*Dto.cs` | `CapabilityTargetForm.tsx` |
| S12 Phone | `ApplicationUser.cs`, `UsersController.cs` | `EvaluatorContactPrompt.tsx` |
| S07-S08 Entries | `EegEntry.cs`, `EegEntryService.cs`, `EegController.cs` | `features/eeg/` |
| S09 Dashboard | `EegCoverageService.cs` | `EegDashboard.tsx` |
| S10 Export | `EegExportService.cs` | `EegExportDialog.tsx` |
| S13a/b Docs | `EegDocumentService.cs` | `GenerateEegDialog.tsx` |

---

## Risk Callouts

| Risk | Story | Severity | Mitigation |
|------|-------|----------|------------|
| **Document formatting accuracy** | S13a/S13b | High | Need official HSEEP template for reference |
| **S10 Excel complexity** | S10 | Medium | Reuse existing MSEL export patterns |
| **Real-time dashboard performance** | S09 | Medium | Cache coverage data, throttle SignalR updates |
| **Rating aggregation edge cases** | S13b | Medium | Define clear rules, add unit tests |
| **Offline sync for EEG entries** | S07, S08 | Medium | Existing sync service, needs conflict resolution |
| **E3 Objectives dependency** | S13a/S13b | Low | Can generate without objectives; graceful fallback |

---

## Definition of Done

Each story is complete when:

1. ✅ All acceptance criteria pass
2. ✅ Unit tests cover business logic
3. ✅ Integration tests cover API endpoints
4. ✅ Permission matrix enforced
5. ✅ Offline behavior tested (where applicable)
6. ✅ Accessibility criteria met (keyboard nav, screen reader)
7. ✅ Code reviewed by code-review agent
8. ✅ Story status updated in docs

---

## References

- [S07-view-eeg-entries.md](./S07-view-eeg-entries.md)
- [S08-edit-delete-eeg-entry.md](./S08-edit-delete-eeg-entry.md)
- [S09-eeg-coverage-dashboard.md](./S09-eeg-coverage-dashboard.md)
- [S10-eeg-aar-export.md](./S10-eeg-aar-export.md)
- [S11-capability-target-sources.md](./S11-capability-target-sources.md)
- [S12-evaluator-contact-prompt.md](./S12-evaluator-contact-prompt.md)
- [S13a-generate-blank-eeg-document.md](./S13a-generate-blank-eeg-document.md)
- [S13b-generate-completed-eeg-document.md](./S13b-generate-completed-eeg-document.md)
- [FEATURE.md](./FEATURE.md)
