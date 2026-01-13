# Phase D+E Implementation Plan: Exercise Conduct & Observations

## Overview

This document outlines the implementation plan for two parallel work streams:
- **Stream 1 (Phase D):** Exercise Conduct - clock controls, fire/skip injects, status workflow
- **Stream 2 (Phase E):** Evaluator Observations - observation entity, CRUD, P/S/M/U ratings

## Sprint Breakdown

### Sprint 1: Database Schema (Sequential)

| Order | Task | Agent | Stream |
|-------|------|-------|--------|
| 1.1 | Create Observation entity & migration | database-agent | E |
| 1.2 | Add Exercise clock fields & migration | database-agent | D |
| 1.3 | Apply migrations | database-agent | Both |

### Sprint 2: Backend Services (Parallel)

| Order | Task | Agent | Stream | Dependencies |
|-------|------|-------|--------|--------------|
| 2.1 | Observation service + controller | backend-agent | E | 1.3 |
| 2.2 | Inject service + controller | backend-agent | D | 1.3 |
| 2.3 | Clock service + endpoints | backend-agent | D | 1.3 |
| 2.4 | IExerciseHubContext interface | realtime-agent | Both | None |
| 2.5 | ExerciseHub implementation | realtime-agent | Both | 2.4 |

### Sprint 3: Frontend (Parallel)

| Order | Task | Agent | Stream | Dependencies |
|-------|------|-------|--------|--------------|
| 3.1 | Observation types, service, hooks | frontend-agent | E | 2.1 |
| 3.2 | Observation components | frontend-agent | E | 3.1 |
| 3.3 | Inject types, service, hooks | frontend-agent | D | 2.2 |
| 3.4 | Inject/Fire/Skip components | frontend-agent | D | 3.3 |
| 3.5 | Clock components | frontend-agent | D | 2.3 |
| 3.6 | SignalR subscriptions | frontend-agent | Both | 2.5 |

### Sprint 4: Integration

| Order | Task | Agent | Dependencies |
|-------|------|-------|--------------|
| 4.1 | Update ExerciseDetailPage | frontend-agent | 3.4, 3.5 |
| 4.2 | Add routes (MSEL, Observations) | frontend-agent | 3.2, 3.4 |
| 4.3 | Navigation updates | frontend-agent | 4.2 |

### Sprint 5: Polish & Verification

| Order | Task | Agent | Dependencies |
|-------|------|-------|--------------|
| 5.1 | Integration tests | testing-agent | All |
| 5.2 | Feature documentation | story-agent | All |
| 5.3 | Code review | code-review | All |

---

## Entity Designs

### Observation Entity

```csharp
public class Observation : BaseEntity
{
    public Guid ExerciseId { get; set; }
    public Guid? InjectId { get; set; }
    public Guid? ObjectiveId { get; set; }

    public ObservationRating? Rating { get; set; }  // P/S/M/U

    public string Content { get; set; } = string.Empty;
    public string? Recommendation { get; set; }

    public DateTime ObservedAt { get; set; }
    public string? Location { get; set; }

    // Navigation
    public Exercise Exercise { get; set; } = null!;
    public Inject? Inject { get; set; }
}

public enum ObservationRating
{
    Performed,      // P - Completed as expected
    Satisfactory,   // S - With minor issues
    Marginal,       // M - Needs improvement
    Unsatisfactory  // U - Failed to meet objective
}
```

### Exercise Clock Fields (add to Exercise entity)

```csharp
public ExerciseClockState ClockState { get; set; } = ExerciseClockState.Stopped;
public DateTime? ClockStartedAt { get; set; }
public TimeSpan? ClockElapsedBeforePause { get; set; }
public Guid? ClockStartedBy { get; set; }

public enum ExerciseClockState
{
    Stopped,
    Running,
    Paused
}
```

---

## API Endpoints

### Observations API

- `GET /api/exercises/{exerciseId}/observations` - List observations
- `GET /api/observations/{id}` - Get single observation
- `POST /api/exercises/{exerciseId}/observations` - Create observation
- `PUT /api/observations/{id}` - Update observation
- `DELETE /api/observations/{id}` - Soft delete

### Injects API

- `GET /api/msels/{mselId}/injects` - List injects for MSEL
- `GET /api/injects/{id}` - Get single inject
- `POST /api/msels/{mselId}/injects` - Create inject
- `PUT /api/injects/{id}` - Update inject
- `DELETE /api/injects/{id}` - Soft delete
- `POST /api/injects/{id}/fire` - Fire inject
- `POST /api/injects/{id}/skip` - Skip inject with reason

### Clock API

- `GET /api/exercises/{id}/clock` - Get clock state
- `POST /api/exercises/{id}/clock/start` - Start clock
- `POST /api/exercises/{id}/clock/pause` - Pause clock
- `POST /api/exercises/{id}/clock/stop` - Stop clock

---

## SignalR Events

| Event | Payload | When |
|-------|---------|------|
| `InjectFired` | InjectDto | Controller fires inject |
| `InjectSkipped` | InjectDto | Controller skips inject |
| `ClockStarted` | ClockStateDto | Clock starts |
| `ClockPaused` | ClockStateDto | Clock pauses |
| `ClockStopped` | ClockStateDto | Clock stops |
| `ObservationAdded` | ObservationDto | New observation created |

---

## File Structure

### Backend Files to Create

```
src/Cadence.Core/
├── Models/Entities/
│   └── Observation.cs
├── Features/
│   ├── Observations/
│   │   ├── Models/DTOs/ObservationDtos.cs
│   │   ├── Services/IObservationService.cs
│   │   ├── Services/ObservationService.cs
│   │   ├── Mappers/ObservationMapper.cs
│   │   └── Validators/ObservationValidator.cs
│   ├── Injects/
│   │   ├── Models/DTOs/InjectDtos.cs
│   │   ├── Services/IInjectService.cs
│   │   ├── Services/InjectService.cs
│   │   ├── Mappers/InjectMapper.cs
│   │   └── Validators/InjectValidator.cs
│   └── ExerciseClock/
│       ├── Models/DTOs/ClockDtos.cs
│       ├── Services/IExerciseClockService.cs
│       └── Services/ExerciseClockService.cs
└── Hubs/
    └── IExerciseHubContext.cs

src/Cadence.WebApi/
├── Controllers/
│   ├── ObservationsController.cs
│   └── InjectsController.cs
└── Hubs/
    ├── ExerciseHub.cs
    └── ExerciseHubContext.cs
```

### Frontend Files to Create

```
src/frontend/src/features/
├── observations/
│   ├── components/
│   │   ├── ObservationCard.tsx
│   │   ├── ObservationForm.tsx
│   │   ├── ObservationList.tsx
│   │   └── RatingBadge.tsx
│   ├── hooks/useObservations.ts
│   ├── services/observationService.ts
│   └── types/index.ts
├── injects/
│   ├── components/
│   │   ├── InjectRow.tsx
│   │   ├── InjectStatusBadge.tsx
│   │   ├── FireInjectButton.tsx
│   │   └── SkipInjectDialog.tsx
│   ├── pages/
│   │   ├── MselPage.tsx
│   │   └── InjectDetailPage.tsx
│   ├── hooks/useInjects.ts
│   ├── services/injectService.ts
│   └── types/index.ts
└── exercise-clock/
    ├── components/
    │   ├── ClockDisplay.tsx
    │   └── ClockControls.tsx
    ├── hooks/useExerciseClock.ts
    ├── services/clockService.ts
    └── types/index.ts
```

---

## Verification Checklist

### Stream 1: Exercise Conduct
- [ ] Clock controls visible on exercise page (Active exercises only)
- [ ] Clock state persists across refresh
- [ ] Fire button fires inject with timestamp
- [ ] Skip button requires reason
- [ ] Status changes reflected in inject list
- [ ] Real-time updates via SignalR

### Stream 2: Observations
- [ ] "Add Observation" available during exercise
- [ ] Observation form captures rating + notes
- [ ] Observations linkable to specific inject
- [ ] Observation list visible on exercise
- [ ] P/S/M/U ratings display correctly
- [ ] Real-time updates via SignalR

### Integration
- [ ] Fire inject → add observation → observation shows inject link
- [ ] MSEL view shows both fire controls and observation counts
- [ ] No UI conflicts or overlapping elements
- [ ] Both features work on tablet viewport
