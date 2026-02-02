# Feature: Exercise CRUD

**Phase:** MVP
**Status:** In Progress

## Overview

Core exercise lifecycle management allowing users to create, view, edit, and archive exercises. This feature provides the foundation for all exercise-related functionality in Cadence.

## Problem Statement

Emergency management professionals conduct multiple exercises throughout the year (tabletop, functional, full-scale). They need a centralized system to create new exercises, view upcoming and past exercises, update exercise details as planning evolves, and archive completed exercises for historical reference. Without proper exercise lifecycle management, teams struggle with scattered documentation, version confusion, and difficulty tracking exercise history.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-exercise.md) | Create Exercise | P0 | ✅ Complete |
| [S02](./S02-edit-exercise.md) | Edit Exercise | P0 | ✅ Complete |
| [S03](./S03-view-exercise-list.md) | View Exercise List | P0 | ✅ Complete |
| [S04](./S04-archive-exercise.md) | Archive Exercise | P1 | 📋 Ready |
| [S05](./S05-practice-mode.md) | Practice Mode | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Administrator** | Full CRUD access, manages all exercises across organization |
| **Exercise Director** | Creates/edits own exercises, archives when complete |
| **Controller** | Views exercises assigned to them, no create/edit access |
| **Evaluator** | Views exercises assigned to them, no create/edit access |
| **Observer** | Views assigned exercises only, read-only access |

## Key Concepts

| Term | Definition |
|------|------------|
| **Exercise** | Planned event to test emergency response capabilities |
| **MSEL** | Master Scenario Events List - automatically created with exercise |
| **Archive** | Soft-delete operation making exercise read-only but viewable |
| **Practice Mode** | Exercise mode for training without affecting production metrics |

## Dependencies

- User authentication and authorization
- Organization management (exercises belong to organizations)
- Core entity definitions (Exercise, MSEL)

## Acceptance Criteria (Feature-Level)

- [ ] Users can create new exercises with required fields
- [ ] Users can view a list of exercises they have access to
- [ ] Users can edit exercise details before conduct begins
- [ ] Users can archive completed exercises
- [ ] Practice exercises are clearly distinguished from production exercises

## Notes

- Exercise creation automatically creates a draft MSEL
- Archiving is soft-delete; exercises can be viewed but not modified
- Practice mode allows testing without affecting production metrics
- Exercise list supports filtering by status, type, and date range
