# Phase M: Exercise Configuration - Gap Analysis Audit

> **Audit Date:** January 2026
> **Auditor:** AI Assistant
> **Status:** Complete

---

## Executive Summary

Phase M audit reveals that **Exercise Phases** are fully implemented, but **Exercise Objectives** and several other features have significant gaps. The InjectObjective many-to-many junction table is missing, and the Objectives CRUD endpoints don't exist.

### Implementation Status Overview

| Feature Area | Backend | Frontend | Status |
|--------------|---------|----------|--------|
| Exercise Phases | ✅ Complete | ✅ Complete | **DONE** |
| Exercise Objectives | ⚠️ Entity only | ❌ Missing | **GAPS** |
| Objective-Inject Linking | ❌ Missing | ❌ Missing | **MISSING** |
| Time Zone Configuration | ✅ Entity field | ⚠️ Partial | **GAPS** |
| Exercise Duplication | ❌ Missing | ❌ Missing | **MISSING** |
| Progress Dashboard | ❌ Missing | ❌ Missing | **P2 - DEFER** |

---

## Detailed Findings

### 1. Exercise Phases ✅ COMPLETE

**Documentation:** `docs/features/exercise-phases/`

#### Backend Status: ✅ Complete
- [x] `Phase` entity exists with all required fields (`src/Cadence.Core/Models/Entities/Phase.cs`)
  - Id, ExerciseId, Name, Description, Sequence, StartTime, EndTime
- [x] `PhasesController` with full CRUD (`src/Cadence.WebApi/Controllers/PhasesController.cs`)
  - GET all phases, GET single phase, POST create, PUT update, DELETE, PUT reorder
- [x] Phase DTOs exist (`src/Cadence.Core/Features/Phases/Models/DTOs/PhaseDtos.cs`)
- [x] DbContext configured with Phase entity
- [x] `Inject.PhaseId` relationship exists

#### Frontend Status: ✅ Complete
- [x] Phase types (`src/frontend/src/features/phases/types/index.ts`)
- [x] Phase service (`src/frontend/src/features/phases/services/phaseService.ts`)
- [x] Phase hooks (`src/frontend/src/features/phases/hooks/usePhases.ts`)
- [x] PhaseFormDialog component (`src/frontend/src/features/phases/components/PhaseFormDialog.tsx`)
- [x] PhaseHeader component (`src/frontend/src/features/phases/components/PhaseHeader.tsx`)
- [x] InjectForm includes Phase selector dropdown
- [x] MSEL grouping by phase implemented

**No action required.**

---

### 2. Exercise Objectives ⚠️ GAPS

**Documentation:** `docs/features/exercise-objectives/`

#### Backend Status: ⚠️ Entity Only
- [x] `Objective` entity exists (`src/Cadence.Core/Models/Entities/Objective.cs`)
  - ObjectiveNumber, Name, Description, ExerciseId
- [x] DbContext includes Objectives DbSet
- [x] DbContext configures Objective entity (indexes, constraints)
- [x] Exercise has `ICollection<Objective>` navigation property
- [ ] **MISSING: ObjectivesController** (no CRUD endpoints)
- [ ] **MISSING: IObjectiveService / ObjectiveService** (no service layer)
- [ ] **MISSING: ObjectiveDtos** (no DTOs for API)
- [ ] **MISSING: InjectObjective junction table** (many-to-many not implemented)

#### Frontend Status: ❌ Missing
- [ ] **MISSING: objectives feature folder** (`src/frontend/src/features/objectives/`)
- [ ] **MISSING: ObjectiveService** (no API calls)
- [ ] **MISSING: useObjectives hook**
- [ ] **MISSING: Objectives list/form components**
- [ ] **MISSING: Multi-select on InjectForm** for objectives
- [x] Observation types include `objectiveId` field (ready for linking)

#### Gaps to Implement (S01-S03):
1. Create `ObjectivesController` with CRUD endpoints
2. Create `ObjectiveService` and `IObjectiveService`
3. Create `ObjectiveDtos` (CreateObjectiveRequest, UpdateObjectiveRequest, ObjectiveDto)
4. Create `InjectObjective` junction entity for many-to-many
5. Update DbContext with InjectObjective configuration
6. Create frontend objectives feature module
7. Add objective multi-select to InjectForm
8. Add objectives column to MSEL list view
9. Add objective filter to MSEL filtering

---

### 3. Objective-Inject Linking ❌ MISSING

**Documentation:** `docs/features/exercise-objectives/S03-link-objective-inject.md`

#### Backend Status: ❌ Not Implemented
- [ ] **MISSING: InjectObjective entity** (junction table)
- [ ] **MISSING: Inject.Objectives navigation property**
- [ ] **MISSING: Objective.Injects navigation property**

#### Required Implementation:
```csharp
// src/Cadence.Core/Models/Entities/InjectObjective.cs
public class InjectObjective
{
    public Guid InjectId { get; set; }
    public Inject Inject { get; set; } = null!;

    public Guid ObjectiveId { get; set; }
    public Objective Objective { get; set; } = null!;
}
```

---

### 4. Time Zone Configuration ⚠️ PARTIAL

**Documentation:** `docs/features/exercise-config/S03-timezone-configuration.md`

#### Backend Status: ✅ Complete
- [x] `Exercise.TimeZoneId` field exists (default "UTC")
- [x] Field configured in DbContext with max length
- [x] CreateExerciseRequest and UpdateExerciseRequest include timeZoneId

#### Frontend Status: ⚠️ Partial
- [x] ExerciseDto includes `timeZoneId`
- [x] Create/Update request types include `timeZoneId`
- [ ] **MISSING: TimeZone selector in ExerciseForm**
- [ ] **MISSING: Time zone display indicator**
- [ ] **MISSING: IANA timezone list component**
- [ ] **MISSING: Timezone change confirmation dialog**

#### Gaps to Implement:
1. Add timezone autocomplete/selector to ExerciseForm
2. Show timezone abbreviation in MSEL scheduled times
3. Add timezone info to exercise detail/header
4. (Optional) Implement timezone change warning dialog

---

### 5. Exercise Duplication ❌ MISSING

**Documentation:** `docs/features/msel-management/S02-duplicate-msel.md`

#### Backend Status: ❌ Not Implemented
- [ ] **MISSING: DuplicateExercise endpoint** on ExercisesController
- [ ] **MISSING: DuplicateExerciseService** (transaction handling)
- [ ] **MISSING: DuplicateExerciseRequest DTO**

#### Frontend Status: ❌ Not Implemented
- [ ] **MISSING: "Duplicate Exercise" menu item**
- [ ] **MISSING: DuplicateExerciseDialog component**
- [ ] **MISSING: Duplication progress UI**

#### Implementation Notes:
- Must duplicate in transaction: Exercise → MSEL → Phases → Objectives → Injects → InjectObjective links
- Generate new GUIDs for all entities
- Set new exercise status to Draft
- Preserve relationships (inject.PhaseId, inject.ObjectiveIds)

---

### 6. Progress Dashboard ❌ MISSING (P2 - DEFER)

**Documentation:** `docs/features/progress-dashboard/S01-setup-progress.md`

#### Status: Not Started
- [ ] Backend calculation endpoint
- [ ] Frontend progress component
- [ ] Integration with exercise detail page

**Recommendation:** Defer to post-MVP. Nice-to-have feature.

---

## Priority Ranking

### P0 - Blocks Other Features
*None identified*

### P1 - Required for Exercise Setup
1. **Objectives CRUD** (S01-S02)
   - Backend: Controller, Service, DTOs
   - Frontend: Feature module, forms, hooks

2. **InjectObjective Junction** (S03)
   - Backend: Entity, DbContext config, migration
   - Update Inject DTOs to include objective IDs
   - Frontend: Multi-select in InjectForm

3. **Time Zone UI** (S03)
   - Frontend: Timezone selector in ExerciseForm
   - Display timezone in MSEL views

### P2 - Nice to Have
4. **Exercise Duplication** (S02)
   - Backend: DuplicateExercise endpoint
   - Frontend: Dialog and menu integration

5. **Progress Dashboard** (S01)
   - Defer to post-MVP

---

## Recommended Implementation Order

### Sprint M.1: Objectives Backend (1 day)
1. Create `InjectObjective` entity
2. Create `ObjectiveDtos.cs`
3. Create `IObjectiveService` + `ObjectiveService`
4. Create `ObjectivesController`
5. Add migration for InjectObjective table
6. Write unit tests

### Sprint M.2: Objectives Frontend (1-2 days)
1. Create `src/frontend/src/features/objectives/` structure
2. Implement objective types and service
3. Implement useObjectives hook
4. Create ObjectiveFormDialog component
5. Create ObjectiveList component
6. Add Objectives management UI to Exercise detail
7. Write component tests

### Sprint M.3: Inject-Objective Linking (1 day)
1. Update InjectDto to include objectiveIds
2. Update Create/UpdateInjectRequest to accept objectiveIds
3. Add objective multi-select to InjectForm
4. Add objectives column to InjectRow/MSEL list
5. Add objective filter option
6. Write tests

### Sprint M.4: Time Zone UI (0.5 day)
1. Create TimeZoneSelector component
2. Add to ExerciseForm
3. Display timezone in MSEL header
4. Write tests

### Sprint M.5: Exercise Duplication (1 day) - P2
1. Create DuplicateExercise endpoint
2. Create DuplicateExerciseDialog frontend
3. Add menu option to exercise actions
4. Write tests

---

## File Structure for New Code

### Backend
```
src/Cadence.Core/
├── Models/Entities/
│   └── InjectObjective.cs          # NEW
├── Features/
│   └── Objectives/                  # NEW
│       ├── Models/DTOs/
│       │   └── ObjectiveDtos.cs
│       ├── Services/
│       │   ├── IObjectiveService.cs
│       │   └── ObjectiveService.cs
│       └── Validators/
│           └── ObjectiveValidators.cs

src/Cadence.WebApi/Controllers/
└── ObjectivesController.cs          # NEW
```

### Frontend
```
src/frontend/src/features/
└── objectives/                      # NEW
    ├── components/
    │   ├── ObjectiveFormDialog.tsx
    │   ├── ObjectiveList.tsx
    │   └── index.ts
    ├── hooks/
    │   ├── useObjectives.ts
    │   └── index.ts
    ├── services/
    │   ├── objectiveService.ts
    │   └── objectiveService.test.ts
    ├── types/
    │   └── index.ts
    └── index.ts
```

---

## Next Steps

1. Review this audit with stakeholder
2. Confirm P1 vs P2 priorities
3. Begin Sprint M.1: Objectives Backend
4. Follow TDD workflow per CLAUDE.md

---

## Change Log

| Date | Update |
|------|--------|
| Jan 2026 | Initial audit complete |
