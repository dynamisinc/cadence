# Architecture Code Review

> **Review Type:** Architecture
> **Pass:** 1
> **Date:** 2026-03-06
> **Reviewer:** Holistic Architecture Reviewer — Software architecture, system design, cross-cutting concerns
> **Scope:** Entire codebase at structural level — cross-layer consistency, feature patterns, error handling, test architecture, configuration, dependencies

---

## Reviewer Summary

- **Files reviewed:** 42 (sampled across all layers: 9 architecture docs, 6 backend feature modules, 5 controllers, 4 service implementations, 5 frontend feature modules, 6 test files, 3 configuration files, 4 dependency manifests)
- **Issues found:** 8 total — Critical: 1, Major: 5, Minor: 2
- **Positive patterns identified:** 6
- **Estimated remediation effort:** Medium
- **Top 3 priorities for this domain:**
  1. CORS policy uses wildcard origin (`_ => true`) unconditionally — no environment check separating dev from production
  2. Validation logic is duplicated in InjectsController as a private method instead of using the registered FluentValidation infrastructure
  3. Global exception handler is placed after `UseAuthorization()` in the pipeline, after several middleware components, meaning it cannot catch exceptions thrown by those middlewares

---

## Critical Issues

### AR-C01: CORS Policy Allows Any Origin With No Environment Guard

- **File(s):** `src/Cadence.WebApi/Program.cs:200-210`
- **Category:** Stability
- **Impact:** The comment on line 205 reads "Allow any origin for development" but no environment check guards this. The same `SetIsOriginAllowed(_ => true)` runs in production. This bypasses the same-origin protection for cookie-based refresh tokens (`withCredentials: true`) and nullifies CORS as a defence layer in production. Combined with `AllowCredentials()`, this is a misconfigured CORS policy that browsers permit in this specific combination only because `SetIsOriginAllowed` returns true — any attacker site can make credentialed requests.
- **Recommendation:** Gate the wildcard policy behind `builder.Environment.IsDevelopment()`. For all other environments, read a `Cors:AllowedOrigins` config array (e.g., the Static Web Apps URL) and use `.WithOrigins(allowedOrigins)`. The `appsettings.json` does not currently contain a `Cors` section, so that section needs to be added.
- **Affected files:**
  - `src/Cadence.WebApi/Program.cs`
  - `src/Cadence.WebApi/appsettings.json` (add `Cors:AllowedOrigins` section)

---

## Major Issues

### AR-M01: Frontend InjectDto Missing `modifiedBy` Field Present in Backend

- **File(s):** `src/frontend/src/features/injects/types/index.ts:13-77` vs `src/Cadence.Core/Features/Injects/Models/DTOs/InjectDtos.cs:8-74`
- **Category:** Convention
- **Impact:** The backend `InjectDto` record includes `string? ModifiedBy` as its second-to-last positional parameter. The frontend `InjectDto` interface ends at `linkedCriticalTaskCount` with no `modifiedBy` field. Any frontend code attempting to display or use the last-modified-by user for audit trail purposes will receive `undefined` for this field while the API is already transmitting it. This is a silent type mismatch — TypeScript will not complain because the field is simply absent from the interface (extra JSON properties are ignored), but it means the field cannot be used by any component or hook.
- **Recommendation:** Add `modifiedBy: string | null` to the frontend `InjectDto` interface immediately after `revertReason` to match the backend record's parameter ordering.
- **Affected files:**
  - `src/frontend/src/features/injects/types/index.ts`

---

### AR-M02: Validation Logic Duplicated in Controller Instead of Using FluentValidation Infrastructure

- **File(s):** `src/Cadence.WebApi/Controllers/InjectsController.cs:1252-1330` (private `ValidateInjectRequest` method, called at lines 343 and 460)
- **Category:** DRY
- **Impact:** The project explicitly includes `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` in `Cadence.Core.csproj`, and `ServiceCollectionExtensions` registers validators via `AddValidatorsFromAssemblyContaining<AppDbContext>()`. However, searching the codebase finds zero `AbstractValidator<T>` implementations anywhere in `Cadence.Core/Features/`. All input validation for injects is duplicated as a long private static method in the controller, returning a single `string?` message. This means: (1) validators are registered but unused; (2) all validation lives in the web layer rather than the Core domain; (3) validation cannot be unit-tested independently; (4) create and update paths share the same single-error-message model, losing the ability to return field-level errors.
- **Recommendation:** Create `src/Cadence.Core/Features/Injects/Validators/CreateInjectRequestValidator.cs` and `UpdateInjectRequestValidator.cs` inheriting from `AbstractValidator<T>`. These should contain the same rules as the private method. Inject `IValidator<CreateInjectRequest>` and `IValidator<UpdateInjectRequest>` into the controller and call `await validator.ValidateAsync(request)`, returning a structured `400` with field-level errors when validation fails. Delete the private `ValidateInjectRequest` method. Do the same audit across other controllers that use similar ad-hoc validation patterns.
- **Affected files:**
  - `src/Cadence.WebApi/Controllers/InjectsController.cs`
  - `src/Cadence.Core/Features/Injects/Validators/` (new files to create)

---

### AR-M03: Global Exception Handler Registered After Authorization — Does Not Cover Early Pipeline Exceptions

- **File(s):** `src/Cadence.WebApi/Program.cs:368-390`
- **Category:** Stability
- **Impact:** The global exception handler is an inline `app.Use()` middleware registered at line 368, after `UseRateLimiter`, `UseAuthentication`, `UseAuthorization`, and `UseSerilogRequestLogging`. This means exceptions thrown by the rate limiter, authentication validation, authorization handlers, or Serilog request logging fall outside the handler's `try/catch` and propagate as unhandled exceptions. The ASP.NET Core framework will catch them with a generic 500, but they will not be logged with the structured error info (user ID, org ID) that the Serilog context middleware provides. In production this means auth-related failures may produce unstructured 500 responses with no audit trail.
- **Recommendation:** Register the exception handler before all other middleware so it forms the outermost layer. Use ASP.NET Core's built-in `app.UseExceptionHandler()` with a properly configured error handler, or move the inline middleware to position 1 in the pipeline.
- **Affected files:**
  - `src/Cadence.WebApi/Program.cs`

---

### AR-M04: Production Debug Console Logging Committed to API Client

- **File(s):** `src/frontend/src/core/services/api.ts:76-239`
- **Category:** Readability
- **Impact:** The Axios API client contains 13 `console.log` statements that log every request URL, every response status, every token refresh attempt, and the access token length. This is not gated behind a development-only flag. In production, this leaks request paths and auth-flow details to anyone with browser DevTools open. Additionally, across the frontend codebase, 96 total `console.log` occurrences are distributed across 16 files including `AuthContext.tsx` (40 logs), `OrganizationContext.tsx` (13 logs), and `authService.ts` (5 logs).
- **Recommendation:** Create a conditional logging utility `src/frontend/src/core/utils/logger.ts` that only emits in development. Replace `console.log` with `devLog` in `api.ts`, `AuthContext.tsx`, and `OrganizationContext.tsx`. Reserve `console.error` for genuine unexpected errors that should surface even in production.
- **Affected files:**
  - `src/frontend/src/core/services/api.ts`
  - `src/frontend/src/contexts/AuthContext.tsx`
  - `src/frontend/src/contexts/OrganizationContext.tsx`
  - `src/frontend/src/features/auth/services/authService.ts`
  - 12 other frontend files with `console.log` calls

---

### AR-M05: InMemory EF Core Database Used in Backend Tests — Does Not Catch SQL-Specific Behaviour

- **File(s):** `src/Cadence.Core.Tests/Helpers/TestDbContextFactory.cs:18-32`
- **Category:** Stability
- **Impact:** All 47 backend test files use `UseInMemoryDatabase` via `TestDbContextFactory`. EF Core's InMemory provider has well-documented divergences from SQL Server: it does not enforce referential integrity, does not support transactions, does not enforce unique constraints, ignores `datetime2` column conventions, and does not execute raw SQL. The global query filters (`IsDeleted == false`, `OrganizationId == currentOrgId`) are the primary data isolation mechanism, and while they work in InMemory for basic `LINQ` queries, the `OrganizationValidationInterceptor` that validates writes may behave differently because InMemory transactions are silently ignored.
- **Recommendation:** Migrate the test infrastructure to use `Microsoft.EntityFrameworkCore.SqlServer` with a local SQL Server or `Testcontainers.MsSql` to run tests against a real SQL Server instance in CI. In the near term, add `context.Database.EnsureCreated()` assertions that verify global query filters are active and add at least one integration test per feature that exercises the `OrganizationValidationInterceptor` to prove cross-org data isolation.
- **Affected files:**
  - `src/Cadence.Core.Tests/Helpers/TestDbContextFactory.cs`
  - All 47 test files in `src/Cadence.Core.Tests/Features/`

---

## Minor Issues

### AR-N01: 16-Level Provider Stack in App.tsx Creates Testability and Navigation Friction

- **File(s):** `src/frontend/src/App.tsx`
- **Category:** Readability
- **Description:** The frontend context provider hierarchy has 16 levels. While ordering is intentional, the depth creates friction: every test needs manual provider wrapping, adding new cross-cutting concerns requires reasoning about 15+ existing providers, and leaf providers like `WhatsNewProvider`, `NotificationToastProvider` could be sibling components rather than providers.
- **Recommendation:** Extract inner leaf providers into an `<AppShell>` component. Create a `renderWithProviders` test utility in `src/frontend/src/test-utils/`.

---

### AR-N02: `Feedback` Feature Has No Backend Mapper — Uses Ad-hoc Service Queries

- **File(s):** `src/Cadence.Core/Features/Feedback/` (no `Mappers/` directory present)
- **Category:** Convention
- **Description:** Every other feature with entities follows the pattern `Services/ + Models/DTOs/ + Mappers/`. The `Feedback` feature is the only meaningful deviation from the golden path directory layout.
- **Recommendation:** Add `src/Cadence.Core/Features/Feedback/Mappers/FeedbackReportMapper.cs` following the same static class pattern as `OrganizationMapper` and `InjectMapper`.

---

## Positive Patterns

### AR-P01: Core vs WebApi Separation Is Well-Enforced

- **Where:** `src/Cadence.Core/Hubs/IExerciseHubContext.cs` and `src/Cadence.WebApi/Hubs/ExerciseHubContext.cs`
- **What:** The `IExerciseHubContext` interface lives entirely in `Cadence.Core` with no SignalR dependency. Services in Core inject this interface and broadcast events without knowing SignalR exists. The concrete implementation in WebApi holds the actual `IHubContext<ExerciseHub>` dependency. This allows the 47 backend test files to mock `IExerciseHubContext` without pulling in SignalR infrastructure. This is the right application of the Dependency Inversion Principle.
- **Replicate in:** Any future event-driven features (email notifications, webhook callbacks) — define the interface in Core, implement in WebApi.

---

### AR-P02: React Query Cache Invalidation via SignalR (Not State Mutation)

- **Where:** `src/frontend/src/features/injects/hooks/useInjects.ts` and `src/frontend/src/shared/hooks/useExerciseSignalR.ts`
- **What:** SignalR events trigger `queryClient.invalidateQueries()` rather than directly mutating cached state. The single source of truth remains the API, and the cache simply marks data as stale. This avoids the class of bugs where local SignalR-driven state mutations diverge from server state.
- **Replicate in:** Any future real-time features — always invalidate, never patch the cache directly from event payloads.

---

### AR-P03: Organization Validation Interceptor Provides Write-Side Data Isolation

- **Where:** `src/Cadence.Core/Data/Interceptors/OrganizationValidationInterceptor.cs`
- **What:** The interceptor pattern cleanly separates the "validate organization ownership" concern from business logic. Every `SaveChangesAsync` call on entities implementing `IOrganizationScoped` is validated before the write commits, with documented bypasses for SysAdmins and seeders. Combined with the read-side global query filters, this creates a two-layer defence for multi-tenancy.
- **Replicate in:** If soft-delete is ever handled via an interceptor rather than explicit `IsDeleted = true` calls, follow the same singleton-with-scoped-resolution pattern.

---

### AR-P04: Feature Module Consistency Is Strong in Well-Developed Features

- **Where:** `src/Cadence.Core/Features/Exercises/`, `src/Cadence.Core/Features/Injects/`, `src/Cadence.Core/Features/Organizations/` and frontend counterparts
- **What:** The three mature features all follow the documented golden path: `Services/I{Feature}Service.cs + {Feature}Service.cs`, `Models/DTOs/{Feature}Dtos.cs`, `Mappers/{Entity}Mapper.cs`. Frontend mirrors this with `services/{feature}Service.ts`, `hooks/use{Feature}.ts`, `types/index.ts`. Simpler features like `delivery-methods` and `autocomplete` correctly follow the same structure despite their smaller scope.
- **Replicate in:** Any new feature should follow this exact layout. The `feedback` feature (AR-N02) is the only meaningful deviation.

---

### AR-P05: InjectDto Type Alignment Between Layers Is 99% Accurate

- **Where:** `src/Cadence.Core/Features/Injects/Models/DTOs/InjectDtos.cs` vs `src/frontend/src/features/injects/types/index.ts`
- **What:** The `InjectDto` has 40+ fields across approval workflow, Phase G extension fields, EEG linking, and conduct audit data. The frontend interface correctly mirrors all fields including optional nullable types, the dual delivery method, branching fields, and the full approval workflow. The only gap is the `modifiedBy` field (AR-M01). This level of type alignment without code generation is impressive.
- **Replicate in:** When new fields are added to any backend DTO, the corresponding frontend type file must be updated in the same PR.

---

### AR-P06: Authentication Token Strategy Is Secure and Well-Implemented

- **Where:** `src/frontend/src/core/services/api.ts`, `src/Cadence.WebApi/Program.cs:165-189`, `src/Cadence.Core/Features/Authentication/Services/`
- **What:** Access tokens are stored in memory (React state), not `localStorage`. Refresh tokens use `httpOnly` cookies. The single-flight pattern in the Axios interceptor prevents token refresh stampedes. Proactive token refresh fires 2 minutes before expiry. Cross-tab logout detection via the `storage` event is present. The `ClockSkew` is set to 5 seconds, which is appropriately tight.
- **Replicate in:** This pattern should not be changed. Document it explicitly in CODING_STANDARDS.md as the mandated auth pattern.
