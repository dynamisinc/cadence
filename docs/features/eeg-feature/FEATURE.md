# Feature: Exercise Evaluation Guide (EEG)

**Phase:** MVP
**Status:** Not Started

## Overview

Exercise planners define Capability Targets and Critical Tasks aligned with HSEEP methodology, link MSEL injects to the tasks they're designed to test, and evaluators record structured assessments using the Exercise Evaluation Guide format. This enables capability-based performance tracking, complete evaluation traceability from objectives through tasks to observations, and HSEEP-compliant After-Action Reports.

## Problem Statement

HSEEP-compliant exercises require structured evaluation against defined capability targets and critical tasks—not just free-form observations. Without this structure:

- **Planners** cannot verify that MSEL injects adequately test all capability targets
- **Evaluators** lack guidance on what specific tasks to observe and assess
- **Directors** cannot produce AAR/IP documents organized by capability performance
- **Organizations** cannot track improvement in specific task performance across exercises

Current observation capture in Cadence is valuable for ad-hoc notes but doesn't provide the structured evaluation chain required by HSEEP: Objective → Capability → Capability Target → Critical Task → MSEL Inject → EEG Entry.

## HSEEP Alignment

Per HSEEP 2020 Doctrine, the Exercise Evaluation Guide (EEG) is a standardized document that:

> "Streamlines data collection, enables thorough assessments of participant organizations' capability targets, and supports development of the After-Action Report."

The EEG organizes evaluation by:
1. **Capability Targets** - Performance thresholds defining what "success" looks like
2. **Critical Tasks** - Specific actions required to achieve the capability target
3. **P/S/M/U Ratings** - Standardized performance assessment scale

This feature implements the EEG methodology within Cadence.

## User Stories

| Story | Title | Priority | Status | Points |
|-------|-------|----------|--------|--------|
| [S01](./S01-capability-target-entity.md) | Capability Target Entity and API | P0 | Not Started | 5 |
| [S02](./S02-critical-task-entity.md) | Critical Task Entity and API | P0 | Not Started | 5 |
| [S03](./S03-define-capability-targets-ui.md) | Define Capability Targets UI | P0 | Not Started | 8 |
| [S04](./S04-define-critical-tasks-ui.md) | Define Critical Tasks UI | P0 | Not Started | 5 |
| [S05](./S05-link-inject-critical-task.md) | Link Injects to Critical Tasks | P0 | Not Started | 5 |
| [S06](./S06-eeg-entry-form.md) | EEG Entry Form | P0 | Not Started | 8 |
| [S07](./S07-view-eeg-entries.md) | View EEG Entries | P0 | Not Started | 5 |
| [S08](./S08-edit-delete-eeg-entry.md) | Edit and Delete EEG Entry | P1 | Not Started | 3 |
| [S09](./S09-eeg-coverage-dashboard.md) | EEG Coverage Dashboard | P1 | Not Started | 5 |
| [S10](./S10-eeg-aar-export.md) | EEG-Based AAR Export | P1 | Not Started | 8 |

**Total Estimated Points:** 57

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Administrator** | Full access to all EEG data across organization |
| **Exercise Director** | Defines Capability Targets and Critical Tasks during exercise planning; reviews EEG entries for AAR |
| **Controller** | Views Critical Tasks linked to their assigned injects for delivery context |
| **Evaluator** | Primary user - enters EEG observations against Critical Tasks during conduct |
| **Observer** | View-only access to EEG entries (if permitted) |

## Key Concepts

| Term | Definition |
|------|------------|
| **Capability Target** | A measurable performance threshold for a capability within a specific exercise (e.g., "Activate EOC within 60 minutes of notification") |
| **Critical Task** | A specific action required to achieve a capability target (e.g., "Issue EOC activation notification to all stakeholders") |
| **EEG Entry** | A structured observation recorded against a Critical Task with P/S/M/U rating, timestamp, and notes |
| **Task Coverage** | Percentage of Critical Tasks that have at least one EEG entry |
| **Inject-Task Link** | Association between a MSEL inject and the Critical Task(s) it is designed to test |
| **P/S/M/U Rating** | HSEEP performance scale: Performed, Some challenges, Major challenges, Unable to perform |

## HSEEP Evaluation Hierarchy

```
Exercise
├── Objectives (SMART format - what we aim to achieve)
│     └── linked to → Capability
│
├── Capability Targets (exercise-scoped performance thresholds)
│     ├── Capability (from org library)
│     ├── Target Description ("Activate EOC within 60 minutes")
│     └── Critical Tasks
│           ├── Task Description ("Issue activation notification")
│           ├── Standard/Condition (optional - how we measure success)
│           └── linked to → Injects (which MSEL events test this task)
│
├── EEG Entries (evaluator observations)
│     ├── Critical Task (what was being evaluated)
│     ├── Observation text
│     ├── P/S/M/U Rating
│     ├── Timestamp
│     └── Evaluator
│
└── Injects (MSEL events)
      └── linked to → Critical Tasks (what tasks this inject tests)
```

## Dependencies

- Exercise Capabilities feature (Capability entity and library)
- Exercise CRUD (exercises to attach targets to)
- Inject CRUD (injects to link to tasks)
- User authentication (evaluator identity)
- Offline sync service (EEG entries must work offline)

## Acceptance Criteria (Feature-Level)

- [ ] Exercise Directors can define Capability Targets with measurable descriptions
- [ ] Exercise Directors can define Critical Tasks under each Capability Target
- [ ] Planners can link MSEL injects to one or more Critical Tasks
- [ ] Evaluators can view their assigned Critical Tasks during exercise conduct
- [ ] Evaluators can enter EEG observations against specific Critical Tasks
- [ ] EEG entries capture: observation text, P/S/M/U rating, timestamp, evaluator
- [ ] EEG entries can link to the triggering inject (optional)
- [ ] Dashboard shows Critical Task coverage (evaluated vs. not evaluated)
- [ ] Dashboard shows performance distribution by Capability Target
- [ ] AAR export organizes findings by Capability → Target → Task → Observations
- [ ] All EEG data syncs offline and reconciles on reconnect
- [ ] EEG entries are read-only after exercise is Completed (except by Admin)

## Data Model

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           HSEEP EVALUATION CHAIN                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Organization                                                           │
│       │                                                                 │
│       └── Capability (library)                                          │
│                 │                                                       │
│                 ▼                                                       │
│  Exercise ─────────────────────────────────────────────────────────┐    │
│       │                                                            │    │
│       ├── Objective ──────────────────────────────────────────┐    │    │
│       │        └── linked to Capability (optional)            │    │    │
│       │                                                       │    │    │
│       ├── CapabilityTarget ◄──────────────────────────────────┘    │    │
│       │        ├── CapabilityId (FK to org capability)             │    │
│       │        ├── TargetDescription                               │    │
│       │        │                                                   │    │
│       │        └── CriticalTask                                    │    │
│       │                 ├── TaskDescription                        │    │
│       │                 ├── Standard (optional)                    │    │
│       │                 │                                          │    │
│       │                 ├── InjectCriticalTask (junction) ◄────────┼────┤
│       │                 │        └── InjectId                      │    │
│       │                 │                                          │    │
│       │                 └── EegEntry                               │    │
│       │                          ├── ObservationText               │    │
│       │                          ├── Rating (P/S/M/U)              │    │
│       │                          ├── ObservedAt                    │    │
│       │                          ├── EvaluatorId                   │    │
│       │                          └── InjectId (optional trigger)   │    │
│       │                                                            │    │
│       └── Inject ◄─────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Entity Definitions

```csharp
public class CapabilityTarget : BaseEntity
{
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    
    public Guid CapabilityId { get; set; }
    public Capability Capability { get; set; } = null!;
    
    /// <summary>
    /// Measurable performance threshold (e.g., "Activate EOC within 60 minutes")
    /// </summary>
    public string TargetDescription { get; set; } = string.Empty;
    
    public int SortOrder { get; set; }
    
    public ICollection<CriticalTask> CriticalTasks { get; set; } = new List<CriticalTask>();
}

public class CriticalTask : BaseEntity
{
    public Guid CapabilityTargetId { get; set; }
    public CapabilityTarget CapabilityTarget { get; set; } = null!;
    
    /// <summary>
    /// Specific action required to achieve the capability target
    /// </summary>
    public string TaskDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional: Conditions and standards for task performance
    /// </summary>
    public string? Standard { get; set; }
    
    public int SortOrder { get; set; }
    
    public ICollection<InjectCriticalTask> LinkedInjects { get; set; } = new List<InjectCriticalTask>();
    public ICollection<EegEntry> EegEntries { get; set; } = new List<EegEntry>();
}

public class InjectCriticalTask
{
    public Guid InjectId { get; set; }
    public Inject Inject { get; set; } = null!;
    
    public Guid CriticalTaskId { get; set; }
    public CriticalTask CriticalTask { get; set; } = null!;
}

public class EegEntry : BaseEntity
{
    public Guid CriticalTaskId { get; set; }
    public CriticalTask CriticalTask { get; set; } = null!;
    
    /// <summary>
    /// The observation/assessment text
    /// </summary>
    public string ObservationText { get; set; } = string.Empty;
    
    /// <summary>
    /// P/S/M/U performance rating
    /// </summary>
    public PerformanceRating Rating { get; set; }
    
    /// <summary>
    /// When the task performance was observed (exercise time)
    /// </summary>
    public DateTime ObservedAt { get; set; }
    
    /// <summary>
    /// Wall clock time when entry was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }
    
    /// <summary>
    /// Evaluator who made the observation
    /// </summary>
    public Guid EvaluatorId { get; set; }
    public User Evaluator { get; set; } = null!;
    
    /// <summary>
    /// Optional: The inject that triggered this observation
    /// </summary>
    public Guid? TriggeringInjectId { get; set; }
    public Inject? TriggeringInject { get; set; }
}
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/exercises/{exerciseId}/capability-targets` | List capability targets with their critical tasks |
| POST | `/api/exercises/{exerciseId}/capability-targets` | Create capability target |
| PUT | `/api/exercises/{exerciseId}/capability-targets/{id}` | Update capability target |
| DELETE | `/api/exercises/{exerciseId}/capability-targets/{id}` | Delete capability target (cascades to tasks) |
| GET | `/api/capability-targets/{targetId}/critical-tasks` | List critical tasks for a target |
| POST | `/api/capability-targets/{targetId}/critical-tasks` | Create critical task |
| PUT | `/api/critical-tasks/{id}` | Update critical task |
| DELETE | `/api/critical-tasks/{id}` | Delete critical task |
| PUT | `/api/critical-tasks/{id}/injects` | Set linked injects for a task |
| PUT | `/api/injects/{injectId}/critical-tasks` | Set linked tasks for an inject |
| GET | `/api/exercises/{exerciseId}/eeg-entries` | List all EEG entries |
| GET | `/api/critical-tasks/{taskId}/eeg-entries` | List EEG entries for a task |
| POST | `/api/critical-tasks/{taskId}/eeg-entries` | Create EEG entry |
| PUT | `/api/eeg-entries/{id}` | Update EEG entry |
| DELETE | `/api/eeg-entries/{id}` | Delete EEG entry |
| GET | `/api/exercises/{exerciseId}/eeg-coverage` | Get coverage metrics |
| GET | `/api/exercises/{exerciseId}/eeg-export` | Export AAR-formatted data |

## Relationship to Existing Features

### Exercise Observations (Existing)
The existing Observations feature captures **ad-hoc observations** during exercise conduct. These remain valuable for:
- Quick notes that don't fit a specific Critical Task
- General strengths and areas for improvement
- Observations about exercise logistics vs. player performance

**EEG Entries are different:** They are **structured assessments** tied to specific Critical Tasks with mandatory P/S/M/U ratings.

**Recommendation:** Keep both. Evaluators use:
- **EEG Entries** for structured capability assessment (feeds AAR)
- **Observations** for ad-hoc notes and general findings

### Exercise Capabilities (Existing)
The existing Capabilities feature provides the **organization-level capability library** (FEMA, NATO, NIST, ISO, custom). 

**EEG extends this:** Capability Targets are exercise-scoped instances that reference capabilities from the library but add measurable thresholds specific to each exercise.

### Exercise Objectives (Existing)
Objectives define **what the exercise aims to achieve**. 

**EEG complements this:** Capability Targets define **how we measure achievement**. An objective like "Test EOC activation procedures" becomes measurable through a Capability Target like "Activate EOC within 60 minutes" with Critical Tasks like "Issue notification," "Staff arrives," "Systems operational."

## Wireframes

### Capability Targets & Critical Tasks Setup

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response TTX                                       │
│  ══════════════════════════════════════════════════════════════════════ │
│  Details │ Objectives │ Participants │ MSEL │ [EEG Setup] │ Conduct    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability Targets                              [+ Add Target]         │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ 📋 Operational Communications                              [Edit] │ │
│  │    Target: Establish interoperable communications within 30 min   │ │
│  │                                                                   │ │
│  │    Critical Tasks:                                   [+ Add Task] │ │
│  │    ┌─────────────────────────────────────────────────────────────┐│ │
│  │    │ □ 1. Activate emergency communication plan                  ││ │
│  │    │      Standard: Per SOP 5.2                                  ││ │
│  │    │      Linked Injects: INJ-003, INJ-007           [2 injects] ││ │
│  │    ├─────────────────────────────────────────────────────────────┤│ │
│  │    │ □ 2. Establish radio net with field units                   ││ │
│  │    │      Linked Injects: INJ-012                    [1 inject]  ││ │
│  │    ├─────────────────────────────────────────────────────────────┤│ │
│  │    │ □ 3. Test backup communication systems                      ││ │
│  │    │      Linked Injects: None                       [0 injects] ││ │
│  │    │      ⚠️ No injects test this task                           ││ │
│  │    └─────────────────────────────────────────────────────────────┘│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ 📋 Mass Care Services                                      [Edit] │ │
│  │    Target: Open and staff shelter within 2 hours of activation    │ │
│  │                                                                   │ │
│  │    Critical Tasks:                                   [+ Add Task] │ │
│  │    ┌─────────────────────────────────────────────────────────────┐│ │
│  │    │ □ 1. Activate shelter team                                  ││ │
│  │    │      Linked Injects: INJ-015, INJ-018           [2 injects] ││ │
│  │    └─────────────────────────────────────────────────────────────┘│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### EEG Entry During Conduct

```
┌─────────────────────────────────────────────────────────────────────────┐
│  + EEG Entry                                               [Minimize]   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability Target:  [Operational Communications ▼]                     │
│                      "Establish interoperable communications..."        │
│                                                                         │
│  Critical Task:      [Activate emergency communication plan ▼]          │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ What did you observe?                                              │ │
│  │                                                                    │ │
│  │ EOC issued activation notification at 09:15. All stakeholders     │ │
│  │ confirmed receipt within 10 minutes. Communication plan           │ │
│  │ followed correctly per SOP 5.2.                                   │ │
│  │                                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  Performance Rating:                                                    │
│  [● P] [○ S] [○ M] [○ U]                                               │
│    ▲                                                                    │
│    Performed without Challenges                                         │
│                                                                         │
│  Triggered by Inject: [INJ-003: EOC Activation Notice ▼] (optional)    │
│                                                                         │
│  [Save Entry]                                     Observed at: 09:22    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### EEG Coverage Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Coverage                                          Exercise: Active │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Overall Coverage: 8 of 12 tasks evaluated (67%)                        │
│  ████████████████████░░░░░░░░░░                                         │
│                                                                         │
│  Rating Distribution:                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  P (Performed)        ████████████  5 (42%)                     │   │
│  │  S (Some Challenges)  ██████        3 (25%)                     │   │
│  │  M (Major Challenges) ████          2 (17%)                     │   │
│  │  U (Unable)           ██            1 (8%)                      │   │
│  │  Not Evaluated        ██            1 (8%)                      │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  By Capability Target:                                                  │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Operational Communications          3/3 tasks   [P] [S] [P]     │   │
│  │ Mass Care Services                  2/4 tasks   [M] [S] [?] [?] │   │
│  │ Emergency Operations Coordination   3/5 tasks   [P] [P] [U] [?] │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ⚠️ Tasks Not Yet Evaluated:                                           │
│  • Mass Care: "Coordinate with Red Cross"                              │
│  • Mass Care: "Track shelter population"                               │
│  • EOC: "Establish resource tracking"                                  │
│  • EOC: "Conduct shift change briefing"                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Notes

### Implementation Sequence

Stories should be implemented in order:
1. **S01-S02** - Backend entities and APIs (can be parallel)
2. **S03-S04** - Planning UI for targets and tasks
3. **S05** - Inject linking (bridges planning to conduct)
4. **S06-S08** - Conduct UI for EEG entry
5. **S09-S10** - Post-conduct metrics and export

### Offline Considerations

EEG entries are critical during conduct when connectivity may be poor. The sync service must:
- Queue EEG entries locally when offline
- Sync with conflict resolution when reconnected
- Show sync status on each entry
- Prevent duplicate entries on reconnect

### HSEEP Terminology

This feature uses official HSEEP terminology:
- "Capability Target" not "capability objective" or "performance goal"
- "Critical Task" not "MET" or "mission essential task" (civilian vs. military)
- "EEG Entry" not "evaluation" or "assessment"
- P/S/M/U ratings exactly as defined in HSEEP 2020

### Out of Scope

- Evaluator assignment to specific Capability Targets (future enhancement)
- EEG templates imported from FEMA PrepToolkit (future enhancement)
- Critical Task libraries at organization level (future enhancement)
- AI-suggested task-to-inject linking (future enhancement)
- Cross-exercise task performance trending (organization metrics - future)

## Success Metrics

| Metric | Target |
|--------|--------|
| Exercises with Capability Targets defined | >80% of exercises |
| Critical Tasks with linked injects | >90% of tasks |
| Task coverage during conduct | >75% of tasks evaluated |
| EEG entries per exercise | Average 15+ entries |
| Time from inject fire to EEG entry | <5 minutes average |

## References

- [HSEEP 2020 Doctrine](https://www.fema.gov/sites/default/files/2020-04/Homeland-Security-Exercise-and-Evaluation-Program-Doctrine-2020-Revision-2-2-25.pdf)
- [FEMA EEG Templates](https://preptoolkit.fema.gov/web/hseep-resources)
- [Exercise Capabilities Feature](../exercise-capabilities/FEATURE.md)
- [Exercise Observations Feature](../exercise-observations/FEATURE.md)

---

*Feature created: 2026-02-03*
