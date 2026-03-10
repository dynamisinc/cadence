# Story: S08 XML Doc Comments on Public Service Interfaces

> **Status**: Proposed
> **Priority**: P3 (Low - Technical Chore)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 3
> **Deferred From**: Code hardening review (CD-N03)

## User Story

**As a** developer working on the Cadence backend,
**I want** all public service interface methods to have XML documentation comments,
**So that** IntelliSense surfaces meaningful descriptions, parameter explanations, and exception information when consuming services — reducing the time needed to understand contracts during active feature development.

## Context

Cadence's service layer is the primary seam between controllers and the domain. Developers (and AI coding agents) reading `IInjectService`, `IExerciseService`, and similar interfaces must currently read the implementation or the story files to understand:

- What a method returns when no record is found (null vs. exception?)
- Which exceptions can be thrown (e.g., `UnauthorizedException`, `NotFoundException`)
- What the parameters mean (especially `Guid` parameters where the type alone is not self-documenting)

XML doc comments fix this at the source — they appear in IntelliSense, are picked up by any future OpenAPI/NSwag tooling, and are preserved in generated documentation.

### Scope

All `I{Feature}Service` interfaces in `Cadence.Core/Features/**/Services/`. Concrete implementations do not require doc comments if the interface is fully documented (IntelliSense inherits from the interface via `<inheritdoc />`).

| File Pattern | Action |
|-------------|--------|
| `Core/Features/**/Services/I*Service.cs` | Add full XML doc comments |
| `Core/Features/**/Services/*Service.cs` | Add `<inheritdoc />` on overridden members |
| Controllers | Out of scope (Swagger attributes serve this purpose) |

## Acceptance Criteria

- [ ] **AC-01**: Given every public method on every `I{Feature}Service` interface, when a developer hovers over a call site in their IDE, then IntelliSense shows a summary description for the method
  - Test: Code review verification; no automated test applicable

- [ ] **AC-02**: Given a method with one or more parameters, when doc comments are added, then each parameter has a `<param>` element with a meaningful description (not just the parameter name repeated)

- [ ] **AC-03**: Given a method that can throw a documented exception (e.g., `NotFoundException`, `UnauthorizedException`), when doc comments are added, then an `<exception>` element lists that exception type and the condition under which it is thrown

- [ ] **AC-04**: Given a method with a non-void return type, when doc comments are added, then a `<returns>` element describes what is returned — including the null/empty behavior (e.g., "Returns null if no record is found" vs. "Throws NotFoundException if no record is found")

- [ ] **AC-05**: Given concrete service implementations, when doc comments are added, then `<inheritdoc />` is used on implementing members rather than duplicating the interface docs

- [ ] **AC-06**: Given the project is built with `<GenerateDocumentationFile>true</GenerateDocumentationFile>` enabled (or this is added as part of this story), when the build runs, then zero CS1591 (missing XML comment) warnings are emitted for service interface public members

## Out of Scope

- XML doc comments on DTOs, entities, or mappers (separate decision)
- XML doc comments on controllers (Swagger `[SwaggerOperation]` attributes serve this purpose)
- Generating an external documentation website
- Enforcing via compiler warning-as-error for the whole solution (too broad; apply to interface files only)

## Dependencies

- No blockers; purely additive documentation

## Implementation Notes

### Standard Template

```csharp
/// <summary>
/// Retrieves all injects belonging to the specified MSEL, scoped to the current organization.
/// </summary>
/// <param name="mselId">The unique identifier of the MSEL whose injects are returned.</param>
/// <param name="cancellationToken">Token to cancel the async operation.</param>
/// <returns>
/// A collection of <see cref="InjectDto"/> instances ordered by scenario time.
/// Returns an empty collection if the MSEL has no injects.
/// </returns>
/// <exception cref="UnauthorizedException">
/// Thrown when the caller does not have access to the organization that owns this MSEL.
/// </exception>
Task<IEnumerable<InjectDto>> GetInjectsAsync(Guid mselId, CancellationToken cancellationToken = default);
```

### Implementing Member Convention

```csharp
// In InjectService.cs
/// <inheritdoc />
public async Task<IEnumerable<InjectDto>> GetInjectsAsync(Guid mselId, CancellationToken cancellationToken = default)
{
    // implementation
}
```

### Enabling Documentation File

Add to `Cadence.Core.csproj` if not already present:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>  <!-- Remove this line once all comments are added -->
</PropertyGroup>
```

Remove the `NoWarn` suppression once all interface methods are documented.

## Test Scenarios

### Code Review Verification
- Reviewer confirms each method has `<summary>`, at least one `<param>` per non-trivial parameter, `<returns>` for non-void methods, and `<exception>` for thrown exceptions
- Build output shows zero CS1591 warnings after `NoWarn` suppression is removed

---

## INVEST Checklist

- [x] **I**ndependent - No dependencies on other stories
- [x] **N**egotiable - Scope can be limited to highest-traffic interfaces first (InjectService, ExerciseService)
- [x] **V**aluable - Reduces ramp-up time for contributors and AI coding agents
- [x] **E**stimable - ~3 points for all service interfaces
- [x] **S**mall - Documentation-only; no behavior or schema changes
- [x] **T**estable - Build warning count is a measurable metric

---

*Related Stories*: [S07 Structured Logging](./S07-structured-logging-templates.md)

*Last updated: 2026-03-09*
