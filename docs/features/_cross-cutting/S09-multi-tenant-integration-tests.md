# Story: S09 Integration Tests for Multi-Tenant Query Filters

> **Status**: Proposed
> **Priority**: P2 (Medium - Quality)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 8
> **Deferred From**: Code hardening review (CD-P01)

## User Story

**As a** developer and security-conscious team member,
**I want** automated integration tests that verify organization-scoped query filters isolate data correctly across tenant boundaries,
**So that** regressions in multi-tenancy are caught before they reach production and before any exercise participant can inadvertently access another organization's exercises or injects.

## Context

Cadence is a multi-tenant platform. Every domain entity that implements `IOrganizationScoped` is protected by:

1. **Read-side**: EF Core query filters that append `WHERE OrganizationId = @orgId` to every query
2. **Write-side**: `OrganizationValidationInterceptor` that rejects writes where the entity's `OrganizationId` does not match the current context

These filters are critical for data isolation. A bug that strips a query filter — such as a missing `.IgnoreQueryFilters()` guard, an accidental `AsNoTracking()` placement, or a context mis-scoping — could expose one organization's MSEL to another organization's Controllers.

Currently, this protection is covered only by code review. This story adds a dedicated integration test suite that runs against a real (in-memory or test) database to verify isolation guarantees are maintained end-to-end.

### Why Deferred

Requires test infrastructure investment: setting up an integration test project with a real database (likely SQL Server LocalDB or SQLite via EF Core's in-memory provider), seeding multi-tenant fixture data, and building a pattern for scoping `ICurrentOrganizationContext` per test. This infrastructure is reusable but takes meaningful upfront effort.

### Scenarios to Cover

| Scenario | Description |
|----------|-------------|
| Cross-org read isolation | Org A cannot read Org B's exercises or injects |
| Org context required | Requests with no org context are rejected |
| Admin bypass | System Admin reads across orgs via explicit bypass (if supported) |
| Write isolation | Writing an entity with wrong `OrganizationId` is rejected by interceptor |
| Soft-delete filter | Soft-deleted records are excluded from all tenant-scoped queries |
| Combined filters | Soft-delete and org filter both apply simultaneously |

## Acceptance Criteria

- [ ] **AC-01**: Given two organizations (Org A and Org B) each with seeded exercises, when a service method is called under Org A's context, then only Org A's exercises are returned — none from Org B
  - Test: `MultiTenantQueryFilterTests.cs::GetExercises_OrgAContext_ReturnsOnlyOrgAExercises`

- [ ] **AC-02**: Given two organizations with injects on their respective MSELs, when inject queries run under Org A's context, then Org B's injects are not included in the result
  - Test: `MultiTenantQueryFilterTests.cs::GetInjects_OrgAContext_ExcludesOrgBInjects`

- [ ] **AC-03**: Given a service call where `ICurrentOrganizationContext.HasOrganization` is false, when the method is invoked, then an `UnauthorizedException` (or equivalent) is thrown before any database query executes
  - Test: `MultiTenantQueryFilterTests.cs::ServiceMethod_NoOrgContext_ThrowsUnauthorized`

- [ ] **AC-04**: Given an attempt to save an `IOrganizationScoped` entity with an `OrganizationId` that does not match the current organization context, when `SaveChangesAsync` is called, then `OrganizationValidationInterceptor` throws an exception and no record is persisted
  - Test: `MultiTenantQueryFilterTests.cs::SaveEntity_WrongOrganizationId_InterceptorRejects`

- [ ] **AC-05**: Given a soft-deleted exercise in Org A, when queries run under Org A's context, then the soft-deleted exercise is excluded from results (both filters apply)
  - Test: `MultiTenantQueryFilterTests.cs::GetExercises_SoftDeleted_ExcludedEvenWithinSameOrg`

- [ ] **AC-06**: Given the integration test infrastructure, when all tests in the suite run, then each test uses an isolated database state (no cross-test contamination from shared DbContext instances)

- [ ] **AC-07**: Given a direct `IgnoreQueryFilters()` call used intentionally (e.g., for admin restore operations), when the test exercises that code path, then the correct elevated data set is returned and the call is documented as an intentional bypass
  - Test: `MultiTenantQueryFilterTests.cs::AdminRestore_IgnoreQueryFilters_ReturnsDeletedRecord`

## Out of Scope

- End-to-end HTTP-level tests through the controller layer (that is a separate E2E suite)
- Testing the JWT claim extraction mechanism (unit-testable separately)
- Performance testing of query filter overhead
- Testing organization membership logic (belongs in organization feature tests)

## Dependencies

- _cross-cutting/S05 (Deprecate Legacy User Table) — integration tests should target the post-migration schema
- Requires a shared integration test project or a conventions decision (SQL Server LocalDB vs. EF Core InMemory vs. Respawn-based SQL reset)

## Implementation Notes

### Recommended Test Infrastructure

Use `Microsoft.EntityFrameworkCore.InMemory` for speed, or SQL Server LocalDB for fidelity. The choice should be documented in the test project's README. If LocalDB is used, consider `Respawn` for database reset between tests.

### Mock Organization Context

```csharp
// Helper to create a context scoped to a specific org
public static ICurrentOrganizationContext ForOrg(Guid orgId, string orgRole = "OrgUser")
{
    var mock = new Mock<ICurrentOrganizationContext>();
    mock.Setup(c => c.OrganizationId).Returns(orgId);
    mock.Setup(c => c.OrganizationRole).Returns(orgRole);
    mock.Setup(c => c.HasOrganization).Returns(true);
    return mock.Object;
}

public static ICurrentOrganizationContext NoOrg()
{
    var mock = new Mock<ICurrentOrganizationContext>();
    mock.Setup(c => c.HasOrganization).Returns(false);
    return mock.Object;
}
```

### Test Data Strategy

Seed two organizations with non-overlapping data sets before each test class. Use `IClassFixture<T>` to share setup across tests in a class, with per-test transaction rollback where possible.

### Test Project Location

```
src/Cadence.Core.IntegrationTests/
├── Helpers/
│   ├── IntegrationTestDbFactory.cs
│   └── OrganizationContextFactory.cs
└── Features/
    └── MultiTenancy/
        └── MultiTenantQueryFilterTests.cs
```

## Domain Terms

| Term | Definition |
|------|------------|
| Tenant | An organization in the Cadence multi-tenant model |
| Query Filter | EF Core global filter applied automatically to all queries for an entity type |
| Org Context | The `ICurrentOrganizationContext` scoped to the current HTTP request |
| Interceptor | `OrganizationValidationInterceptor` — validates org ID on write operations |

## Test Scenarios

### Integration Tests
- Org isolation on read (exercises, injects, observations)
- Org validation on write (interceptor rejection)
- No-context rejection (service guard)
- Soft-delete combined with org filter
- Intentional bypass with `IgnoreQueryFilters()`

---

## INVEST Checklist

- [x] **I**ndependent - Infrastructure investment is self-contained to a new test project
- [x] **N**egotiable - Can start with highest-risk entities (Exercise, Inject) and expand
- [x] **V**aluable - Prevents data leakage between exercise organizations — a critical security property
- [x] **E**stimable - ~8 points including test infrastructure setup
- [ ] **S**mall - Test infrastructure setup is the bulk of the work; consider splitting infrastructure from test cases
- [x] **T**estable - Tests are themselves the deliverable

---

*Related Stories*: [S05 Deprecate Legacy User Table](./S05-deprecate-user-table.md), [S11 Audit DeleteBehavior Cascades](./S11-audit-delete-behavior.md)

*Last updated: 2026-03-09*
