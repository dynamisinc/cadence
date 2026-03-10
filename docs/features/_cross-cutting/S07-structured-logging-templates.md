# Story: S07 Structured Logging Templates

> **Status**: Proposed
> **Priority**: P3 (Low - Technical Chore)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 2
> **Deferred From**: Code hardening review (CD-N02)

## User Story

**As a** developer or operator,
**I want** all log calls to use structured message templates rather than string interpolation,
**So that** log events in Application Insights (and any future log sink) can be queried by property name, enabling faster diagnosis of issues during or after exercise conduct.

## Context

Serilog and the .NET `ILogger<T>` API support two syntactically similar but semantically different approaches to logging:

```csharp
// String interpolation — value is baked into the message string, NOT queryable as a property
_logger.LogInformation($"Exercise {exerciseId} started by {userId}");

// Structured template — values are stored as named properties alongside the message
_logger.LogInformation("Exercise {ExerciseId} started by {UserId}", exerciseId, userId);
```

When exercises run live, operators may need to query "show me all log events for exercise X in the last hour". With interpolated strings, this requires full-text search. With structured templates, it is a simple property filter in Application Insights (`customDimensions.ExerciseId == "..."` ).

### Why Deferred

This is a cosmetic/observability improvement with no functional impact. No behavior changes, no schema changes, no API contract changes. It is safe to do incrementally file-by-file.

### Affected Areas

- All `_logger.Log*` calls using `$"..."` interpolated strings or `+` concatenation in `Cadence.Core` and `Cadence.WebApi`
- Exception log calls should continue to pass the `Exception` as the first argument (before the message template), per .NET conventions

## Acceptance Criteria

- [ ] **AC-01**: Given the `Cadence.Core` project, when a grep is run for `LogInformation\(.*\$"` or `LogWarning\(.*\$"` or `LogError\(.*\$"`, then zero matches are found
  - Test: CI linting step or manual verification

- [ ] **AC-02**: Given the `Cadence.WebApi` project, when the same grep patterns are run, then zero matches are found

- [ ] **AC-03**: Given a structured log call, when the message template uses a placeholder like `{ExerciseId}`, then the placeholder name uses PascalCase to match Application Insights property naming conventions

- [ ] **AC-04**: Given a log call at `LogError` that includes an exception, when the template is reviewed, then the exception is passed as the first overload argument (not embedded in the string)
  - Example: `_logger.LogError(ex, "Failed to fire inject {InjectId}", injectId)`

- [ ] **AC-05**: Given the change, when existing unit tests and integration tests run, then all tests pass (no behavior has changed)

## Out of Scope

- Adding new log statements (this story is about reformatting existing ones only)
- Changing log levels or adding/removing log calls
- Configuring Application Insights sinks (infrastructure concern)
- Frontend logging (separate concern)
- Enforcing via a Roslyn analyzer (future enhancement; out of scope for this story)

## Dependencies

- No blockers

## Implementation Notes

### Search Patterns

Run the following to find all interpolated log calls requiring conversion:

```bash
# Find interpolated log calls
grep -rn "Log(Information|Warning|Error|Debug|Trace|Critical)(\$\"" src/Cadence.Core src/Cadence.WebApi

# Also catch string concatenation patterns
grep -rn "Log(Information|Warning|Error|Debug|Trace|Critical)(\".*\" \+" src/Cadence.Core src/Cadence.WebApi
```

### Property Naming Convention

Use PascalCase for placeholder names to match Application Insights `customDimensions` naming:

| Bad | Good |
|-----|------|
| `{exerciseId}` | `{ExerciseId}` |
| `{id}` | `{EntityId}` or the specific type (e.g., `{InjectId}`) |
| `{ex.Message}` | Pass exception as first arg; do not embed message in template |

### Before/After Examples

```csharp
// Before
_logger.LogInformation($"Creating exercise '{name}' for org {organizationId}");
_logger.LogError($"Failed to fire inject {injectId}: {ex.Message}");

// After
_logger.LogInformation("Creating exercise {ExerciseName} for org {OrganizationId}", name, organizationId);
_logger.LogError(ex, "Failed to fire inject {InjectId}", injectId);
```

## Test Scenarios

### Verification (Non-Automated)
- Grep sweep before and after to confirm zero interpolated log calls remain
- Spot-check that PascalCase convention is applied consistently

### Automated
- All existing tests pass unchanged (no behavior difference)

---

## INVEST Checklist

- [x] **I**ndependent - No dependencies on other stories
- [x] **N**egotiable - Can be done one file at a time
- [x] **V**aluable - Enables structured querying of logs in Application Insights during incident response
- [x] **E**stimable - Well-scoped, ~2 points
- [x] **S**mall - Purely cosmetic; low risk
- [x] **T**estable - Grep verification is deterministic

---

*Related Stories*: [S06 CancellationToken](./S06-cancellation-tokens.md), [S08 XML Doc Comments](./S08-xml-doc-comments.md)

*Last updated: 2026-03-09*
