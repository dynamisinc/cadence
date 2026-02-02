# Feature: Exercise Observations

**Phase:** MVP
**Status:** Complete

## Overview

Evaluators capture real-time observations during exercise conduct to document player performance, supporting HSEEP evaluation requirements and After-Action Review (AAR) preparation.

## Problem Statement

HSEEP evaluation requires structured documentation of player performance during exercise conduct. Without a real-time observation capture system, evaluators must rely on memory and paper notes, leading to incomplete or lost data. This feature enables evaluators to document observations as they happen, link them to objectives and injects, and apply standardized HSEEP performance ratings for consistent evaluation across exercises.

## User Personas

| Persona | Interaction |
|---------|-------------|
| Evaluator | Primary user - creates, edits observations during conduct |
| Exercise Director | Reviews observations, ensures coverage, prepares AAR |
| Controller | May view observations for situational awareness |
| Administrator | Full access to all observations |
| Observer | View-only access to observations (if permitted) |

## Key Concepts

| Term | Definition |
|------|------------|
| Observation | Documented note of player performance during exercise conduct |
| P/S/M/U Rating | HSEEP performance rating scale (Performed, Some challenges, Major challenges, Unable) |
| Observation Type | Classification as Strength, Area for Improvement, or Neutral |
| Observation Linking | Association of observations with injects and/or objectives |
| Exercise Time | Timestamp context (when observed vs when recorded) |

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-observation.md) | Create Observation | P0 | 📋 Ready |
| [S02](./S02-edit-observation.md) | Edit Observation | P0 | 📋 Ready |
| [S03](./S03-delete-observation.md) | Delete Observation | P1 | 📋 Ready |
| [S04](./S04-link-observation-inject.md) | Link Observation to Inject | P1 | 📋 Ready |
| [S05](./S05-link-observation-objective.md) | Link Observation to Objective | P1 | 📋 Ready |
| [S06](./S06-psmu-rating.md) | Apply P/S/M/U Rating | P1 | 📋 Ready |
| [S07](./S07-view-observations-list.md) | View Observations List | P0 | 📋 Ready |
| [S08](./S08-filter-observations.md) | Filter Observations | P2 | 📋 Ready |


## Dependencies

- exercise-crud/S01 - Create Exercise (observations belong to exercises)
- inject-crud/S01 - Create Inject (observations can link to injects)
- exercise-objectives/S01 - Create Objective (observations can link to objectives)
- Authentication - Role-based access (Evaluator+ can create)

## Acceptance Criteria (Feature-Level)

- [ ] Evaluators can create observations during exercise conduct
- [ ] Observations capture: text, type, timestamp, evaluator, optional P/S/M/U rating
- [ ] Observations can be linked to one or more injects (many-to-many)
- [ ] Observations can be linked to one or more objectives (many-to-many)
- [ ] Observations persist through offline periods and sync when connected
- [ ] Observations are read-only after exercise is Completed (except by Admin)
- [ ] Observations visible in Review Mode for AAR preparation

## Notes

HSEEP P/S/M/U Rating Scale:
- **P**: Performed without Challenges - Tasks achieved objective(s)
- **S**: Performed with Some Challenges - Tasks achieved objective(s) with challenges
- **M**: Performed with Major Challenges - Tasks did not achieve objective(s)
- **U**: Unable to be Performed - Tasks were not performed

Observation Types:
- **Strength**: Positive performance observation
- **Area for Improvement**: Performance gap identified
- **Neutral**: Factual observation without judgment

Additional considerations:
- Observations should be quick to enter during fast-paced conduct
- Consider floating/docked observation panel for continuous entry
- Offline support critical - evaluators may be in poor connectivity areas
- Observation timestamps use exercise time context (not wall clock)
- Review Mode provides different view optimized for AAR preparation

## Data Model

```csharp
public class Observation : BaseEntity
{
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// The observation text/notes
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of observation: Strength, AreaForImprovement, Neutral
    /// </summary>
    public ObservationType Type { get; set; } = ObservationType.Neutral;

    /// <summary>
    /// Optional P/S/M/U rating
    /// </summary>
    public PerformanceRating? Rating { get; set; }

    /// <summary>
    /// When the observation was recorded (exercise time context)
    /// </summary>
    public DateTime ObservedAt { get; set; }

    /// <summary>
    /// Wall clock time when observation was created
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// User who created the observation
    /// </summary>
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    // Many-to-many relationships
    public ICollection<ObservationInject> LinkedInjects { get; set; } = new List<ObservationInject>();
    public ICollection<ObservationObjective> LinkedObjectives { get; set; } = new List<ObservationObjective>();
}

public enum ObservationType
{
    Neutral,
    Strength,
    AreaForImprovement
}

public enum PerformanceRating
{
    P,  // Performed without Challenges
    S,  // Performed with Some Challenges
    M,  // Performed with Major Challenges
    U   // Unable to be Performed
}
```

## Wireframes/Mockups

### Observation Quick Entry (During Conduct)

```
┌─────────────────────────────────────────────────────────────────────┐
│  + Quick Observation                                    [Minimize]   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ What did you observe?                                          │ │
│  │ _____________________________________________________________  │ │
│  │ _____________________________________________________________  │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  Type: [◉ Neutral] [○ Strength] [○ Area for Improvement]          │
│                                                                     │
│  Rating (optional): [P] [S] [M] [U] [None]                         │
│                                                                     │
│  Link to: [Select Inject...▼]  [Select Objective...▼]             │
│                                                                     │
│  [Save Observation]                                                 │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Observations List View

```
┌─────────────────────────────────────────────────────────────────────┐
│  Observations (24)                               [+ New Observation] │
├─────────────────────────────────────────────────────────────────────┤
│  Filter: [All Types ▼] [All Ratings ▼] [All Objectives ▼]          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  09:15 │ ⬆ Strength │ P │ EOC activated within 30 minutes of       │
│        │            │   │ notification - excellent response time    │
│        │            │   │ 📎 INJ-003, OBJ-1                         │
│        │            │   │ 👤 Robert Chen                            │
│  ──────┼────────────┼───┼──────────────────────────────────────────│
│  09:42 │ ⬇ AFI      │ M │ Communication breakdown between EOC       │
│        │            │   │ and field units - radio protocol not     │
│        │            │   │ followed                                  │
│        │            │   │ 📎 INJ-007, OBJ-2                         │
│        │            │   │ 👤 Sarah Kim                              │
│  ──────┼────────────┼───┼──────────────────────────────────────────│
│  10:05 │ ─ Neutral  │   │ Shelter capacity reached at 10:00,       │
│        │            │   │ overflow procedures initiated             │
│        │            │   │ 📎 INJ-012                                │
│        │            │   │ 👤 Robert Chen                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create observation | ✅ | ✅* | ✅* | ✅* | ❌ |
| Edit own observation | ✅ | ✅* | ✅* | ✅* | ❌ |
| Edit others' observation | ✅ | ✅* | ❌ | ❌ | ❌ |
| Delete observation | ✅ | ✅* | ❌ | ❌ | ❌ |
| View observations | ✅ | ✅* | ✅* | ✅* | ✅* |

*Within exercises where user has this role

## Notes

- Observations should be quick to enter during fast-paced conduct
- Consider floating/docked observation panel for continuous entry
- Offline support critical - evaluators may be in poor connectivity areas
- Observation timestamps use exercise time context (not wall clock)
- Review Mode provides different view optimized for AAR preparation

---

*Feature created: 2026-01-21*
