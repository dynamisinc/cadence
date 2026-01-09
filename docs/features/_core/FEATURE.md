# Feature: Core Domain Entities

**Parent Epic**: Foundation

## Description

Core domain entities define the fundamental data structures and business rules that underpin the entire Cadence platform. These are not user-facing features but rather the architectural foundation upon which all features are built.

This feature folder contains entity definitions rather than user stories, as these represent domain knowledge that developers and AI agents need to understand before implementing any feature.

## Entity Documentation

| Entity | Description | Status |
|--------|-------------|--------|
| [Exercise](./exercise-entity.md) | Top-level container for MSEL, participants, and settings | 📋 Ready |
| [Inject](./inject-entity.md) | Individual events that drive exercise scenarios | 📋 Ready |
| [User Roles](./user-roles.md) | Role definitions and permission matrices | 📋 Ready |

## Key Relationships

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

## Domain Rules

### Exercise Rules
1. An exercise must have at least one MSEL version
2. Only one MSEL version can be "Active" at a time
3. Archived exercises are read-only
4. Practice mode exercises are excluded from production reports

### Inject Rules
1. Inject numbers are unique within a MSEL
2. Scheduled Time is required; Scenario Time is optional
3. Deleted injects are soft-deleted (archived, not removed)
4. Child injects (branching) are orphaned when parent is deleted

### Role Rules
1. Users have exactly one role per exercise
2. Role permissions are fixed in MVP (not configurable)
3. Administrator and Exercise Director can modify role assignments

## Naming Conventions

| Convention | Example | Usage |
|------------|---------|-------|
| PascalCase | `ExerciseId` | Entity properties, class names |
| camelCase | `exerciseId` | JSON properties, local variables |
| kebab-case | `exercise-id` | URL paths, file names |
| SCREAMING_SNAKE | `MAX_INJECT_COUNT` | Constants |

## Validation Rules

All entities must validate:
- Required fields are present and non-empty
- String lengths are within defined limits
- Enums contain valid values
- Foreign keys reference existing entities
- Business rules are satisfied

## Audit Fields

All entities include standard audit fields:

| Field | Type | Description |
|-------|------|-------------|
| `CreatedAt` | DateTime | UTC timestamp of creation |
| `CreatedBy` | UserId | User who created the record |
| `ModifiedAt` | DateTime | UTC timestamp of last modification |
| `ModifiedBy` | UserId | User who last modified the record |
| `IsDeleted` | Boolean | Soft delete flag |
| `DeletedAt` | DateTime? | UTC timestamp of deletion (if deleted) |

## Dependencies

- None (foundation layer)

## Notes

- Entity definitions inform database schema design
- All times stored in UTC, displayed in exercise time zone
- Soft delete pattern used throughout for audit trail integrity
