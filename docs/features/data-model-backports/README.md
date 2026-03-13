# Feature: Data Model Backports (Phase C+)

**Phase:** C+ — Do Before Phase D
**Status:** 🔲 Ready to Implement
**Priority:** P0 — Blocking for Phase J correctness

## Purpose

Forward-compatible database schema additions identified during Phase J design.
Backward-compatible (nullable FKs, stub tables ignored by existing code), but
must be added before Phase D accumulates real exercise data.

## Stories

| Story | Description | Status |
|-------|-------------|--------|
| C+-1 | ExerciseObjective, ObjectiveCapabilityTag, InjectObjective — EF entities + migration | 🔲 |
| C+-2 | Optional Objectives multi-select stub on inject create/edit form | 🔲 |
| C+-3 | LibraryInject stub table + nullable LibrarySourceId FK on Inject — EF entity + migration | 🔲 |

## Why Now

See the Data Model Backports section in `cadence-phases-summary.md` for full
rationale. Short version:

- Every inject created from Phase D forward should be tag-able to objectives.
  Without this schema, beta exercises accumulate no traceability data and the
  Phase J Coverage Dashboard will be empty when it ships.
- The `LibrarySourceId` FK on `Inject` cannot be reconstructed retroactively
  once production exercises exist.

## Implementation

When ready, start a new Claude Code session with:

> "Generate the Phase C+ implementation plan based on
> docs/features/data-model-backports/README.md and the Data Model Backports
> section of cadence-phases-summary.md."
