# Feature: Core Domain Entities

**Phase:** Foundation
**Status:** In Progress

## Overview

Core domain entities define the fundamental data structures and business rules that underpin the entire Cadence platform. These are not user-facing features but rather the architectural foundation upon which all features are built.

## Problem Statement

Before implementing user-facing features, developers and AI agents need a shared understanding of the domain model. Entity definitions, relationships, validation rules, and naming conventions must be documented to ensure consistency across the codebase. Without this foundation, features will be built on inconsistent assumptions, leading to technical debt and rework.

## Entity Documentation

| Entity | Description | Status |
|--------|-------------|--------|
| [Exercise](./exercise-entity.md) | Top-level container for MSEL, participants, and settings | 📋 Ready |
| [Inject](./inject-entity.md) | Individual events that drive exercise scenarios | 📋 Ready |
| [User Roles](./user-roles.md) | Role definitions and permission matrices | 📋 Ready |

## User Stories

| Story                                            | Description                                           | Status      |
|--------------------------------------------------|-------------------------------------------------------|-------------|
| [S01-error-reporting](./S01-error-reporting.md)  | Send error reports from ErrorBoundary to support team | 📋 Backlog  |

## Key Concepts

### Entity Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                        EXERCISE                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │ Objectives  │  │   Phases    │  │      Participants       │ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
│                          │                                      │
│                    ┌─────┴─────┐                                │
│                    │   MSEL    │                                │
│                    │ (Version) │                                │
│                    └─────┬─────┘                                │
│                          │                                      │
│              ┌───────────┼───────────┐                          │
│              │           │           │                          │
│         ┌────┴────┐ ┌────┴────┐ ┌────┴────┐                    │
│         │ Inject  │ │ Inject  │ │ Inject  │                    │
│         │   #1    │ │   #2    │ │   #3    │                    │
│         └─────────┘ └─────────┘ └─────────┘                    │
└─────────────────────────────────────────────────────────────────┘
```

### Domain Rules

**Exercise Rules:**
1. An exercise must have at least one MSEL version
2. Only one MSEL version can be "Active" at a time
3. Archived exercises are read-only
4. Practice mode exercises are excluded from production reports

**Inject Rules:**
1. Inject numbers are unique within a MSEL
2. Scheduled Time is required; Scenario Time is optional
3. Deleted injects are soft-deleted (archived, not removed)
4. Child injects (branching) are orphaned when parent is deleted

**Role Rules:**
1. Users have exactly one role per exercise
2. Role permissions are fixed in MVP (not configurable)
3. Administrator and Exercise Director can modify role assignments

### Naming Conventions

| Convention | Example | Usage |
|------------|---------|-------|
| PascalCase | `ExerciseId` | Entity properties, class names |
| camelCase | `exerciseId` | JSON properties, local variables |
| kebab-case | `exercise-id` | URL paths, file names |
| SCREAMING_SNAKE | `MAX_INJECT_COUNT` | Constants |

### Audit Fields

All entities include standard audit fields:

| Field | Type | Description |
|-------|------|-------------|
| `CreatedAt` | DateTime | UTC timestamp of creation |
| `UpdatedAt` | DateTime | UTC timestamp of last modification |
| `IsDeleted` | Boolean | Soft delete flag |
| `DeletedAt` | DateTime? | UTC timestamp of deletion (if deleted) |
| `DeletedBy` | string? | User ID who deleted the record |

## Dependencies

None (foundation layer)

## Acceptance Criteria (Feature-Level)

- [ ] All core entities are documented with properties, relationships, and validation rules
- [ ] Naming conventions are defined and consistently applied
- [ ] Audit fields are standardized across all entities
- [ ] Domain rules are explicitly stated
- [ ] Entity diagrams illustrate key relationships

## Notes

- Entity definitions inform database schema design
- All times stored in UTC, displayed in exercise time zone
- Soft delete pattern used throughout for audit trail integrity
- This feature folder serves as reference documentation, not user stories
