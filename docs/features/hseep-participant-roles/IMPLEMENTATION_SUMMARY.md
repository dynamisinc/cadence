# HSEEP Participant Roles - Implementation Summary

**Created:** 2026-02-09
**Feature Status:** 📋 Not Started

## Overview

This document provides a high-level implementation roadmap for adding complete HSEEP participant role support to Cadence.

## New Roles Being Added

| Role | Primary Purpose | Key Differentiator | Complexity |
|------|----------------|-------------------|-----------|
| **Player** | Exercise participants being tested/trained | Most restricted access | Medium |
| **Simulator** | Role-play external entities (SimCell) | Limited write for simulations | Medium |
| **Facilitator** | Guide discussions (TTX focus) | Clock control, discussion pacing | Low |
| **Safety Officer** | Safety oversight | Emergency stop authority | High |
| **Trusted Agent** | Embedded SME observers | Observation + coaching | Low |

## Story Dependencies

```
S01: Extend ExerciseRole Enum
  ↓
S02: Update Role Assignment UI
  ↓
├─ S03: Player Permissions
├─ S04: Simulator Permissions
├─ S05: Facilitator Permissions
├─ S06: Safety Officer Permissions
└─ S07: Trusted Agent Permissions
  ↓
S08: Role-Specific UI Adaptations
  ↓
S09: Bulk Import Support
```

## Implementation Phases

### Phase 1: Foundation (S01-S02)
**Estimated Effort:** 2-3 days

**Tasks:**
1. Update `ExerciseRole` enum in backend (5 new values)
2. Update `ExerciseRole` enum in frontend
3. Add role display names, descriptions, icons, colors
4. Update role assignment UI to show all 10 roles
5. Write enum and UI tests

**Deliverables:**
- All 10 roles selectable in participant assignment
- Role badges display correctly with new icons/colors
- No permissions implemented yet (all roles behave like Observer)

### Phase 2: Core Permissions (S03-S07)
**Estimated Effort:** 5-7 days

**Tasks per role:**
1. Define permission rules in `RolePermissions.cs`
2. Update service authorization checks
3. Filter API responses based on role
4. Add backend unit tests
5. Update frontend permission hooks
6. Add frontend component tests

**Priority Order:**
1. **S03: Player** (most restrictive, clearest scope)
2. **S04: Simulator** (moderate complexity)
3. **S05: Facilitator** (low complexity, clock control)
4. **S06: Safety Officer** (high complexity, new features needed)
5. **S07: Trusted Agent** (low complexity, observation extensions)

**Deliverables:**
- Each role has correct view/write permissions
- Backend enforces all authorization rules
- Frontend permission hooks return role-appropriate flags

### Phase 3: UI Adaptations (S08)
**Estimated Effort:** 3-4 days

**Tasks:**
1. Create `useExercisePermissions` centralized hook
2. Adapt `ExercisePage` based on role
3. Create role-specific views (PlayerTimelineView, etc.)
4. Update all feature components to check permissions
5. Add empty states and guidance for each role
6. Comprehensive UI testing

**Deliverables:**
- Each role sees tailored UI
- No features visible that user cannot access
- Clear guidance for each role's purpose

### Phase 4: Bulk Import (S09)
**Estimated Effort:** 2 days

**Tasks:**
1. Add role synonyms to parser
2. Update CSV/XLSX templates
3. Add role-specific validation warnings
4. Update preview grouping
5. Update results summary

**Deliverables:**
- Bulk import supports all 10 roles
- Templates include new roles
- Smart validation warnings (e.g., FSE needs Safety Officer)

## Database Considerations

### No Schema Changes Required

The `ExerciseUser` table already stores role as an enum/string. No migration needed unless:
- Using a Roles lookup table (add 5 new rows)
- Adding new fields (e.g., `Observation.CoachingProvided`)

### Recommended Enhancements (Optional)

```sql
-- Observation table enhancements
ALTER TABLE Observations
ADD CoachingProvided BIT NOT NULL DEFAULT 0,
ADD CoachingDetails NVARCHAR(MAX) NULL,
ADD IsSafetyObservation BIT NOT NULL DEFAULT 0,
ADD SafetySeverity INT NULL;

-- ExerciseClock enhancements for emergency stop
ALTER TABLE ExerciseClocks
ADD EmergencyStopReason NVARCHAR(500) NULL,
ADD EmergencyStoppedAt DATETIME2 NULL,
ADD EmergencyStoppedBy UNIQUEIDENTIFIER NULL;
```

## Testing Strategy

### Test Coverage Targets

| Layer | Target | Priority Tests |
|-------|--------|----------------|
| Backend Unit | 90%+ | Permission checks, role parsing |
| Backend Integration | 80%+ | End-to-end authorization flows |
| Frontend Unit | 85%+ | Permission hooks, role utilities |
| Frontend Component | 80%+ | Role-specific UI rendering |
| E2E | Key flows | Player view, Safety stop, Bulk import |

### Test Data Requirements

Create test exercises with:
- 1 exercise with all 10 roles assigned
- 1 FSE with Safety Officer
- 1 TTX with Facilitator
- 1 exercise with 50+ Players (bulk import testing)

## Documentation Updates Required

1. **_core/user-roles.md**: Complete permission matrix update
2. **DOMAIN_GLOSSARY.md**: Add new role definitions
3. **README.md**: Update role descriptions
4. **.claude/agents/cadence-domain-agent.md**: Add new roles to HSEEP section
5. **bulk-participant-import/FEATURE.md**: Note new role support

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Permission rule conflicts | Medium | High | Thorough test coverage, permission matrix review |
| UI becomes too complex | Medium | Medium | Role-specific views, progressive disclosure |
| Safety Officer authority misuse | Low | High | Audit logging, confirmation dialogs |
| Player role confusion | Medium | Medium | Clear documentation, empty state guidance |
| Bulk import validation gaps | Medium | Medium | Comprehensive synonym testing |

## Future Enhancements (Out of Scope)

1. **Inject Assignment**: Track which Simulator is assigned to each inject
2. **Player Targeting**: Send injects to specific players
3. **Safety Incident System**: Full safety reporting workflow
4. **Facilitator Tools**: TTX-specific pacing and discussion features
5. **Trusted Agent Notes**: Private coaching log separate from observations
6. **Role Analytics**: Dashboard showing role distribution and activity

## Questions for SME Review

- [ ] Is "Player" the correct term for exercise participants who are Cadence users?
- [ ] Should Simulators have any permissions beyond marking simulations sent?
- [ ] Should Facilitators be able to resume the clock, or only pause?
- [ ] Should Safety Officers see all observations, or only safety observations?
- [ ] Should Trusted Agents see controller notes, or only expected actions?

## Success Criteria

- [ ] All 10 HSEEP roles can be assigned to exercises
- [ ] Each role has appropriate permissions per HSEEP standards
- [ ] UI adapts correctly based on user's role
- [ ] Bulk import recognizes all new roles
- [ ] No regression in existing 5-role functionality
- [ ] Permission matrix documentation is comprehensive and accurate
- [ ] SME validation confirms HSEEP compliance

---

**Next Steps:**
1. Review this summary with product owner
2. Prioritize stories (suggest S01 → S02 → S03 → S05 → S04 → S07 → S06 → S08 → S09)
3. Create feature branch: `feature/hseep-participant-roles`
4. Begin with S01 (Extend ExerciseRole Enum)
