# Story: S11 Audit EF Core DeleteBehavior Cascades

> **Status**: Proposed
> **Priority**: P2 (Medium - Data Integrity)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 3
> **Deferred From**: Code hardening review (CD-P03)

## User Story

**As a** developer,
**I want** every EF Core foreign key relationship to have an explicitly documented and intentionally set `DeleteBehavior`,
**So that** deleting an exercise, MSEL, or organization never causes unintended silent cascade deletion of injects, observations, or other exercise artifacts.

## Context

EF Core's default `DeleteBehavior` for required relationships is `Cascade` — meaning if a parent entity is deleted, all child entities are deleted automatically at the database level. For an MSEL management platform, this has significant implications:

- Deleting an **Exercise** could cascade-delete its **MSEL**, then its **Injects**, then **Observations** — silently wiping the entire exercise history
- Deleting an **Organization** could cascade-delete all its **Exercises** and everything beneath them
- After-Action Report (AAR) data depends on the integrity of observations and inject firing records

Cadence uses soft delete (`IsDeleted = true`) for user-created data precisely to avoid permanent data loss. However, if a hard-delete code path reaches a parent entity with `Cascade` delete behavior configured, EF Core will hard-delete the children regardless of the soft-delete pattern.

This story audits every FK relationship, documents the intended behavior, and corrects any misconfigured cascades.

### Why Deferred

This is an analysis-first task. The work is careful and deliberate rather than large in volume. It was deferred because it requires a developer to reason about each relationship in context — it cannot be mechanically applied. It is low-urgency because the soft-delete guards already prevent most accidental hard-deletes in production code, but it is medium-priority as a correctness guarantee.

### Relationships to Audit

| Parent | Child | Expected Behavior | Risk if Wrong |
|--------|-------|-------------------|---------------|
| Organization | Exercise | Restrict (or soft-delete only) | Entire org's exercise history wiped |
| Exercise | MSEL | Restrict (or soft-delete only) | All injects lost |
| Exercise | ExerciseUser | Cascade (role assignments follow exercise) | Acceptable |
| Exercise | ExercisePhase | Cascade (phases follow exercise) | Acceptable |
| MSEL | Inject | Restrict (or soft-delete only) | MSEL history lost |
| Inject | (children, if any) | TBD per analysis | TBD |
| Organization | OrganizationMembership | Cascade (memberships follow org) | Acceptable |
| ApplicationUser | OrganizationMembership | Cascade (memberships follow user) | Acceptable |

## Acceptance Criteria

- [ ] **AC-01**: Given the EF Core entity configurations, when each FK relationship is reviewed, then every `DeleteBehavior` is explicitly set in the Fluent API (no implicit defaults left in place on domain entity relationships)
  - Test: Code review verification; grep for relationships lacking explicit `OnDelete(...)` calls

- [ ] **AC-02**: Given the Exercise → MSEL relationship, when an Exercise is soft-deleted (IsDeleted = true), then its MSELs are not cascade-hard-deleted at the database level
  - Test: `DeleteBehaviorAuditTests.cs::SoftDeleteExercise_DoesNotCascadeHardDeleteMsel`

- [ ] **AC-03**: Given the MSEL → Inject relationship, when a MSEL is soft-deleted, then its Injects are not cascade-hard-deleted at the database level
  - Test: `DeleteBehaviorAuditTests.cs::SoftDeleteMsel_DoesNotCascadeHardDeleteInjects`

- [ ] **AC-04**: Given the Organization → Exercise relationship, when an Organization record is deleted (any path), then the delete is blocked by a `Restrict` behavior unless an explicit bulk soft-delete has been performed first
  - Test: `DeleteBehaviorAuditTests.cs::DeleteOrganization_WithExistingExercises_IsRestricted`

- [ ] **AC-05**: Given the audit is complete, when a decision record table is added to this story's Implementation Notes, then each FK relationship has a documented intended `DeleteBehavior` and the rationale

- [ ] **AC-06**: Given any relationship where `Cascade` is intentionally correct (e.g., ExerciseUser follows Exercise), when the entity configuration is reviewed, then a comment in the Fluent API explains why cascade is appropriate

## Out of Scope

- Changing the soft-delete mechanism itself (already implemented via `ISoftDeletable` and `BaseEntity`)
- Adding cascade soft-delete (where deleting a parent automatically soft-deletes children) — this is a separate feature decision
- Auditing relationships in third-party Identity tables (`AspNetRoles`, `AspNetUserRoles`, etc.) — managed by ASP.NET Core Identity

## Dependencies

- _cross-cutting/S05 (Deprecate Legacy User Table) — the audit should be performed after the user table migration to avoid auditing relationships that will be removed

## Implementation Notes

### Fluent API Convention

All explicit `DeleteBehavior` settings belong in the entity's `EntityTypeConfiguration` class or in `AppDbContext.OnModelCreating`:

```csharp
// Restricting cascade (preferred for data-bearing relationships)
builder.HasOne(e => e.Msel)
    .WithMany(m => m.Exercises)
    .HasForeignKey(e => e.MselId)
    .OnDelete(DeleteBehavior.Restrict); // Intentional: soft-delete required before hard-delete

// Allowing cascade (for structural/role relationships)
builder.HasMany(e => e.ExerciseUsers)
    .WithOne(eu => eu.Exercise)
    .HasForeignKey(eu => eu.ExerciseId)
    .OnDelete(DeleteBehavior.Cascade); // Intentional: role assignments have no value without their exercise
```

### Decision Record (To Be Completed During Implementation)

The following table must be filled in during the audit. Each relationship must have an entry:

| Relationship | Current Behavior | Intended Behavior | Changed? | Rationale |
|-------------|-----------------|-------------------|----------|-----------|
| Organization -> Exercise | TBD | Restrict | TBD | Exercises are primary data; accidental hard-delete must be blocked |
| Exercise -> Msel | TBD | Restrict | TBD | MSEL is the exercise artifact |
| Msel -> Inject | TBD | Restrict | TBD | Injects and firing records are AAR source data |
| Exercise -> ExerciseUser | TBD | Cascade | TBD | Role assignments are not independent data |
| Exercise -> ExercisePhase | TBD | Cascade | TBD | Phase definitions follow the exercise |
| Organization -> OrganizationMembership | TBD | Cascade | TBD | Membership records follow the organization |
| ApplicationUser -> OrganizationMembership | TBD | Cascade | TBD | Membership records follow the user |

### Migration Implications

Changing `DeleteBehavior` from `Cascade` to `Restrict` (or `NoAction`) on an existing relationship requires a migration that alters the database constraint. Verify migration does not break existing foreign key data before applying.

### Grep for Unset Behaviors

```bash
# Find HasOne/HasMany chains without an explicit OnDelete call
grep -rn "HasForeignKey" src/Cadence.Core/Data/ | grep -v "OnDelete"
```

Any result from this grep on a non-Identity entity is a candidate for review.

## Domain Terms

| Term | Definition |
|------|------------|
| DeleteBehavior.Cascade | EF Core deletes child rows when parent row is deleted |
| DeleteBehavior.Restrict | EF Core throws if parent row is deleted while children exist |
| DeleteBehavior.SetNull | EF Core sets child FK to null when parent is deleted |
| Soft Delete | Marking `IsDeleted = true` rather than removing the database row |
| AAR | After-Action Report — post-exercise documentation that depends on intact inject and observation history |

## Test Scenarios

### Integration Tests
- Soft-delete exercise — verify MSEL rows remain with `IsDeleted = false`
- Attempt hard-delete of organization with exercises — verify exception is thrown (Restrict)
- Soft-delete MSEL — verify inject rows remain with `IsDeleted = false`

### Schema Verification
- Run `dotnet ef migrations script` and review generated SQL for any unintended `ON DELETE CASCADE` on domain entity FK constraints

---

## INVEST Checklist

- [x] **I**ndependent - Analysis can be done without other stories (though best after S05)
- [x] **N**egotiable - Scope is the domain entities only; Identity tables excluded
- [x] **V**aluable - Prevents silent data loss that would corrupt After-Action Reports
- [x] **E**stimable - ~3 points (analysis + corrections + decision table + migration)
- [x] **S**mall - Focused on FK configuration only
- [x] **T**estable - Integration tests verify correct restrict/cascade behavior; schema review is deterministic

---

*Related Stories*: [S05 Deprecate Legacy User Table](./S05-deprecate-user-table.md), [S09 Multi-Tenant Integration Tests](./S09-multi-tenant-integration-tests.md)

*Last updated: 2026-03-09*
