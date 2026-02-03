# Feature: Inject CRUD Operations

**Phase:** MVP
**Status:** In Progress

## Overview

Injects are the core content of a MSEL - they are the events, messages, and scenarios delivered during exercise conduct. This feature covers the basic create, read, update, and delete operations for injects, including Cadence's dual-time tracking capability.

## Problem Statement

Exercise planners need to build Master Scenario Events Lists (MSELs) containing dozens or hundreds of injects - the phone calls, emails, news reports, and resource requests that drive the exercise scenario. They need to create injects with scheduling details, edit inject content as planning evolves, view inject details for review and delivery, and remove or reorganize injects as scenarios change. Cadence's dual-time tracking (scheduled delivery time vs. scenario story time) allows multi-day scenarios to be compressed into shorter exercise windows.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-inject.md) | Create Inject | P0 | ✅ Complete |
| [S02](./S02-edit-inject.md) | Edit Inject | P0 | ✅ Complete |
| [S03](./S03-view-inject-detail.md) | View Inject Detail | P0 | ✅ Complete |
| [S04](./S04-delete-inject.md) | Delete Inject | P1 | 📋 Ready |
| [S05](./S05-dual-time-tracking.md) | Dual Time Tracking | P0 | ✅ Complete |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Administrator** | Full CRUD access to all injects |
| **Exercise Director** | Full CRUD access to injects in their exercises |
| **Controller** | Create, edit, view injects; limited delete |
| **Evaluator** | View injects only |
| **Observer** | View injects only |

## Key Concepts

### Dual Time Tracking

Cadence supports two time concepts for each inject:

| Time Type | Purpose | Example |
|-----------|---------|---------|
| **Scheduled Time** | When to deliver the inject (wall clock) | "10:30 AM EST" |
| **Scenario Time** | When it happens in the story | "Day 2, 14:00" |

This allows exercises to compress multi-day scenarios into shorter conduct periods. A "Day 3" scenario event might be delivered at 11:00 AM on the actual exercise day.

### Inject Fields

| Field | Required | Description |
|-------|----------|-------------|
| Inject Number | Yes (auto) | Unique identifier within MSEL |
| Title | Yes | Brief description |
| Scheduled Time | Yes | Wall-clock delivery time |
| Scenario Day | No | Story day (1, 2, 3...) |
| Scenario Time | No | Story time (HH:MM) |
| Description | No | Full inject content |
| From | No | Simulated sender |
| To | No | Target recipient(s) |
| Method | No | Delivery channel |
| Expected Action | No | What players should do |
| Notes | No | Controller notes |

## Dependencies

- exercise-crud/S01: Create Exercise (injects belong to exercises)
- exercise-objectives (if linking objectives to injects)
- exercise-phases (if assigning injects to phases)

## Acceptance Criteria (Feature-Level)

- [ ] Users can create injects with required and optional fields
- [ ] Users can view inject details including all time information
- [ ] Users can edit inject content before and during conduct
- [ ] Users can delete injects with confirmation
- [ ] Dual time (Scheduled + Scenario) is supported on all injects
- [ ] Inject numbering is automatic and sequential within MSEL

## Notes

- Inject numbering is automatic and sequential within the MSEL
- Soft delete is used to allow recovery of accidentally deleted injects
- During conduct, some fields may become read-only to preserve audit trail
- Scenario time is optional - not all exercises use compressed timelines
