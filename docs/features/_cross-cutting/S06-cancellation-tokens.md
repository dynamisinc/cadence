# Story: S06 Add CancellationToken to Async Methods

> **Status**: Proposed
> **Priority**: P3 (Low - Technical Chore)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 5
> **Deferred From**: Code hardening review (CD-N01)

## User Story

**As a** developer,
**I want** all async controller actions and service methods to accept and propagate a `CancellationToken`,
**So that** in-flight database queries and HTTP calls are cancelled gracefully when a client disconnects, freeing server resources during active exercise conduct.

## Context

During exercise conduct, Controllers and Exercise Directors may navigate away, close browser tabs, or experience network drops mid-request. Without cancellation token propagation, the server continues executing the full query chain — including potentially expensive MSEL or inject list queries — even after the client is gone. This wastes database connections and thread-pool slots that should be available for other exercise participants.

The fix is mechanical: add `CancellationToken cancellationToken = default` to every async method signature and pass it through to EF Core `.ToListAsync()`, `.FirstOrDefaultAsync()`, `.SaveChangesAsync()`, and any outbound HTTP calls.

### Why Deferred

The surface area spans all service interfaces, service implementations, and controllers across every feature. While each individual change is trivial, the volume means a single large PR with a high diff count and non-trivial merge risk if done all at once. There is no functional correctness risk at current usage patterns — this is a resource efficiency improvement.

### Scope

| Layer | Action |
|-------|--------|
| Controllers | Add `CancellationToken cancellationToken = default` as last parameter on all async actions |
| Service interfaces | Add `CancellationToken cancellationToken = default` as last parameter on all async methods |
| Service implementations | Accept token and pass to EF Core async calls |
| EF Core queries | Pass token to `ToListAsync`, `FirstOrDefaultAsync`, `SingleOrDefaultAsync`, `SaveChangesAsync`, `AnyAsync`, `CountAsync` |
| HTTP calls (if any) | Pass token to `HttpClient` send methods |

## Acceptance Criteria

- [ ] **AC-01**: Given any async controller action, when a client disconnects mid-request, then EF Core receives the cancellation signal and the query is aborted
  - Test: `CancellationTokenPropagationTests.cs::ControllerAction_ClientDisconnects_QueryIsCancelled`

- [ ] **AC-02**: Given service interface methods, when a `CancellationToken` is passed by the caller, then it flows through to the innermost EF Core call without being swallowed or ignored
  - Test: `CancellationTokenPropagationTests.cs::ServiceMethod_TokenPropagatedToEfCoreQuery`

- [ ] **AC-03**: Given a default parameter value of `default` on all signatures, when callers omit the token (existing unit tests), then existing tests continue to compile and pass without modification
  - Test: All existing service and controller unit tests remain green

- [ ] **AC-04**: Given the complete codebase, when a grep is run for `.ToListAsync()`, `.FirstOrDefaultAsync()`, `.SaveChangesAsync()` without a cancellation token argument, then zero results remain inside `Cadence.Core` and `Cadence.WebApi` (excluding migrations)

- [ ] **AC-05**: Given `SaveChangesAsync` calls in `AppDbContext`, when a token is provided, then it is forwarded to the base `SaveChangesAsync(cancellationToken)` override

## Out of Scope

- Changing timeout durations or adding per-request timeout policies (separate infrastructure concern)
- Azure Functions timer triggers (no client to cancel them)
- Adding cancellation to synchronous code paths
- Custom `OperationCanceledException` handling beyond ASP.NET Core defaults (already returns 499/connection reset)

## Dependencies

- No blockers; can be implemented feature-by-feature or in a single sweep PR

## Implementation Notes

### Recommended Approach

Work feature-by-feature (Exercises, Injects, Observations, etc.) to keep PRs reviewable. Update the interface first, then the implementation, then the controller — the compiler will flag every missed call site.

### Signature Convention

```csharp
// Service interface
Task<IEnumerable<InjectDto>> GetInjectsAsync(Guid exerciseId, CancellationToken cancellationToken = default);

// Service implementation
public async Task<IEnumerable<InjectDto>> GetInjectsAsync(Guid exerciseId, CancellationToken cancellationToken = default)
{
    return await _context.Injects
        .Where(i => i.Msel.ExerciseId == exerciseId)
        .ToListAsync(cancellationToken);
}

// Controller action
[HttpGet]
public async Task<IActionResult> GetInjects(Guid exerciseId, CancellationToken cancellationToken)
{
    // ASP.NET Core automatically binds HttpContext.RequestAborted
    var injects = await _injectService.GetInjectsAsync(exerciseId, cancellationToken);
    return Ok(injects);
}
```

ASP.NET Core automatically injects `HttpContext.RequestAborted` into action parameters typed as `CancellationToken` — no `[FromBody]` or `[FromQuery]` attribute needed.

## Test Scenarios

### Unit Tests
- Mock service with a token, assert the mock received the same token instance
- Verify `SaveChangesAsync(cancellationToken)` is called (not the parameterless overload)

### Integration Tests
- Use `CancellationTokenSource.CancelAfter(0)` to trigger immediate cancellation and assert `OperationCanceledException` propagates correctly

---

## INVEST Checklist

- [x] **I**ndependent - No dependencies on other stories
- [x] **N**egotiable - Can be done per-feature or all at once
- [x] **V**aluable - Prevents resource exhaustion during high-concurrency exercise conduct
- [x] **E**stimable - Mechanical change, ~5 points for full surface area
- [ ] **S**mall - Large surface area; consider splitting by feature folder
- [x] **T**estable - Compiler verification + focused unit tests for propagation

---

*Related Stories*: [S07 Structured Logging Templates](./S07-structured-logging-templates.md)

*Last updated: 2026-03-09*
