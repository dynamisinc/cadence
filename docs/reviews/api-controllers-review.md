# API & Controllers Code Review

> **Review Type:** API & Controllers
> **Pass:** 1
> **Date:** 2026-03-06
> **Reviewer:** .NET API & Controller Specialist — ASP.NET Core 10, REST API design, authorization, middleware
> **Scope:** src/Cadence.WebApi/ — 36 controllers, 4 hubs, 2 middleware, 4 services, 6 authorization files, Program.cs

---

## Reviewer Summary

- **Files reviewed:** 54
- **Issues found:** 22 total — Critical: 6, Major: 10, Minor: 6
- **Positive patterns identified:** 6
- **Estimated remediation effort:** High
- **Top 3 priorities for this domain:**
  1. Hardcoded `SystemConstants.SystemUserIdString` used as the acting user in mutation endpoints — bypasses audit trail and records system user instead of actual user
  2. Missing exercise-scoped authorization on 15 endpoints across 7 controllers — any authenticated user can access exercise data without membership verification
  3. Business logic and direct `AppDbContext` queries in controllers instead of delegating to Core services — violates the Core/WebApi separation pattern

---

## Critical Issues

### AC-C01: Hardcoded `SystemConstants.SystemUserIdString` in mutation endpoints

- **File(s):** `src/Cadence.WebApi/Controllers/ExerciseStatusController.cs`, `ExercisesController.cs`, `ObjectivesController.cs`, `PhasesController.cs`, `ExpectedOutcomesController.cs`
- **Category:** Stability
- **Impact:** Mutation endpoints (create, update, delete) pass `SystemConstants.SystemUserIdString` as the acting user ID to service methods instead of extracting the authenticated user's ID from the JWT claims. This means the audit trail records "system" as the author of all changes, making it impossible to determine which user performed an action. In a HSEEP exercise context, this breaks accountability for inject modifications, exercise status changes, and phase management.
- **Description:** Multiple controllers have mutation endpoints that call service methods with a `userId` parameter but pass a hardcoded system constant instead of calling `GetCurrentUserId()` (or the equivalent claims extraction). This appears to be a placeholder pattern from initial development that was never replaced with actual user context.
- **Recommendation:** Replace all `SystemConstants.SystemUserIdString` usages in controller action methods with `GetCurrentUserId()` — the same pattern used correctly in `InjectsController` and `AuthController`. Audit every controller for this pattern. Add a Roslyn analyzer rule or code review checklist item to prevent future regression.
- **Affected files:**
  - `src/Cadence.WebApi/Controllers/ExerciseStatusController.cs`
  - `src/Cadence.WebApi/Controllers/ExercisesController.cs`
  - `src/Cadence.WebApi/Controllers/ObjectivesController.cs`
  - `src/Cadence.WebApi/Controllers/PhasesController.cs`
  - `src/Cadence.WebApi/Controllers/ExpectedOutcomesController.cs`

---

### AC-C02: Missing exercise-scoped authorization on 15 endpoints across 7 controllers

- **File(s):** `src/Cadence.WebApi/Controllers/ExerciseMetricsController.cs`, `ExerciseStatusController.cs`, `ObservationsController.cs`, `EegEntriesController.cs`, `CriticalTasksController.cs`, `ExcelExportController.cs`, `BulkParticipantImportController.cs`
- **Category:** Stability
- **Impact:** Any authenticated user in the same organization can access exercise data, metrics, observations, EEG entries, and critical tasks for exercises they are not assigned to. The authorization infrastructure (`[ExerciseAccess]`, `[ExerciseRole]` attributes) exists and is used correctly on some controllers (e.g., `InjectsController`), but these 7 controllers rely only on `[Authorize]` (authentication check) without exercise-scoped access control.
- **Description:** The backend has a well-designed exercise authorization system with `ExerciseAccessRequirement` and `ExerciseRoleRequirement` handlers. However, 15 endpoints across 7 controllers do not use these attributes. This means the exercise role hierarchy (ExerciseDirector > Controller > Evaluator > Observer) is not enforced on these endpoints.
- **Recommendation:** Add `[AuthorizeExerciseAccess]` to all read endpoints and `[AuthorizeExerciseRole("ExerciseDirector")]` (or appropriate role) to all write endpoints in the affected controllers. Verify the exercise ID is extracted from the route parameter consistently.
- **Affected files:**
  - `src/Cadence.WebApi/Controllers/ExerciseMetricsController.cs`
  - `src/Cadence.WebApi/Controllers/ExerciseStatusController.cs`
  - `src/Cadence.WebApi/Controllers/ObservationsController.cs`
  - `src/Cadence.WebApi/Controllers/EegEntriesController.cs`
  - `src/Cadence.WebApi/Controllers/CriticalTasksController.cs`
  - `src/Cadence.WebApi/Controllers/ExcelExportController.cs`
  - `src/Cadence.WebApi/Controllers/BulkParticipantImportController.cs`

---

### AC-C03: Business logic and direct `AppDbContext` in controllers

- **File(s):** `src/Cadence.WebApi/Controllers/ExercisesController.cs`, `PhasesController.cs`, `ExpectedOutcomesController.cs`, `AutocompleteController.cs`, `InjectsController.cs`, `ExerciseStatusController.cs`
- **Category:** Convention
- **Impact:** Controllers contain direct `AppDbContext` queries, LINQ expressions, entity manipulation, and business rule enforcement. This violates the Core/WebApi separation documented in CLAUDE.md: "Cadence.Core — Domain/business logic (testable without web dependencies)" and "Cadence.WebApi — Web infrastructure (ASP.NET Core specific)." Business logic in controllers cannot be unit-tested without spinning up the ASP.NET Core test server, and it bypasses the organization validation interceptor if queries are not properly scoped.
- **Description:** Six controllers inject `AppDbContext` directly and contain query logic that should live in service classes in `Cadence.Core/Features/`. Some controllers have hundreds of lines of LINQ queries with `.Include()`, `.Where()`, `.Select()` chains directly in action methods.
- **Recommendation:** For each affected controller, create or extend the corresponding service in `Cadence.Core/Features/{Feature}/Services/`. Move all `AppDbContext` queries from the controller into the service. The controller should only call the service method and return the result. Priority order: `ExercisesController` (most complex), `InjectsController` (already has a service but bypasses it in some actions), `PhasesController`, `ExpectedOutcomesController`, `AutocompleteController`, `ExerciseStatusController`.
- **Affected files:**
  - `src/Cadence.WebApi/Controllers/ExercisesController.cs`
  - `src/Cadence.WebApi/Controllers/PhasesController.cs`
  - `src/Cadence.WebApi/Controllers/ExpectedOutcomesController.cs`
  - `src/Cadence.WebApi/Controllers/AutocompleteController.cs`
  - `src/Cadence.WebApi/Controllers/InjectsController.cs`
  - `src/Cadence.WebApi/Controllers/ExerciseStatusController.cs`

---

### AC-C04: Hardcoded `isAdmin = true` in delete operations

- **File(s):** `src/Cadence.WebApi/Controllers/ExercisesController.cs:698,718`
- **Category:** Stability
- **Impact:** Delete operations hardcode `isAdmin = true` when calling service methods, bypassing the actual admin check. This means any user who can reach the delete endpoint (even if they only have the `OrgUser` role) will have their delete processed as an admin-level operation, potentially skipping soft-delete protections or ownership checks that the service layer enforces differently for admin vs non-admin callers.
- **Description:** Lines 698 and 718 in ExercisesController pass `isAdmin: true` as a literal parameter to service deletion methods instead of deriving the admin status from the authenticated user's claims or role.
- **Recommendation:** Replace `isAdmin: true` with the result of checking the user's actual role: `var isAdmin = User.IsInRole("Admin") || User.IsInRole("OrgAdmin")`. Apply this fix to any other controller that passes hardcoded admin flags.
- **Affected files:** `src/Cadence.WebApi/Controllers/ExercisesController.cs`

---

### AC-C05: CORS allows all origins without environment guard

- **File(s):** `src/Cadence.WebApi/Program.cs:205`
- **Category:** Stability
- **Impact:** `SetIsOriginAllowed(_ => true)` runs in all environments including production. Combined with `AllowCredentials()`, any attacker site can make credentialed cross-origin requests to the API and receive cookie-based refresh tokens. This is a critical security misconfiguration.
- **Description:** The CORS policy comment says "Allow any origin for development" but there is no `IsDevelopment()` guard. The same wildcard policy applies in production.
- **Recommendation:** Gate behind `builder.Environment.IsDevelopment()`. For non-dev environments, read allowed origins from configuration (`Cors:AllowedOrigins` array in `appsettings.json`).
- **Affected files:**
  - `src/Cadence.WebApi/Program.cs`
  - `src/Cadence.WebApi/appsettings.json`

---

### AC-C06: `ExerciseHub.JoinUserGroup` accepts client-supplied user ID

- **File(s):** `src/Cadence.WebApi/Hubs/ExerciseHub.cs:45`
- **Category:** Stability
- **Impact:** The `JoinUserGroup` method accepts a `userId` parameter from the client and uses it to add the connection to a user-specific SignalR group. A malicious client can supply any user ID and receive real-time notifications intended for that user, including inject status changes, observation updates, and exercise clock events. This is an impersonation vector.
- **Description:** SignalR hub methods should extract the user ID from the authenticated connection context (`Context.UserIdentifier`) rather than accepting it as a client parameter. The `JoinExerciseGroup` method correctly uses the `exerciseId` from the route, but `JoinUserGroup` trusts the client.
- **Recommendation:** Replace the `userId` parameter with `Context.UserIdentifier` or `Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value`. Remove the parameter from the hub method signature entirely.
- **Affected files:** `src/Cadence.WebApi/Hubs/ExerciseHub.cs`

---

## Major Issues

### AC-M01: `GetCurrentUserId()` duplicated across 12 controllers

- **File(s):** 12 controller files (with 2 copies in `ExerciseClockController` alone)
- **Category:** DRY
- **Impact:** The same 3-5 line method for extracting user ID from JWT claims is copy-pasted in 12 controllers. If the claim name changes or error handling needs to be updated, all 12 copies must be found and modified.
- **Description:** Each controller has its own `private Guid GetCurrentUserId()` or similar method that extracts the `sub` or `NameIdentifier` claim from `HttpContext.User`. Some implementations differ slightly (string vs Guid return, different claim names).
- **Recommendation:** Extract into a `ControllerBase` extension method or a shared base class. Create `src/Cadence.WebApi/Extensions/ClaimsPrincipalExtensions.cs` with `GetUserId()` and `GetOrganizationId()` methods. All controllers import and call `User.GetUserId()`.
- **Affected files:** 12 controller files across `src/Cadence.WebApi/Controllers/`

---

### AC-M02: `UsersController.GetUserMemberships` missing `[AuthorizeAdmin]`

- **File(s):** `src/Cadence.WebApi/Controllers/UsersController.cs`
- **Category:** Stability
- **Impact:** The `GetUserMemberships` endpoint allows any authenticated user to query another user's organization memberships by user ID. This exposes the multi-tenancy structure to unauthorized users.
- **Description:** Other admin-level user management endpoints in UsersController use `[AuthorizeAdmin]`, but `GetUserMemberships` only has `[Authorize]`.
- **Recommendation:** Add `[AuthorizeAdmin]` or add a check that the requesting user can only query their own memberships (or is an OrgAdmin for the relevant organization).
- **Affected files:** `src/Cadence.WebApi/Controllers/UsersController.cs`

---

### AC-M03: `SystemSettingsController` uses string literal `"Admin"` instead of `[AuthorizeAdmin]`

- **File(s):** `src/Cadence.WebApi/Controllers/SystemSettingsController.cs`
- **Category:** Convention
- **Impact:** Uses `[Authorize(Roles = "Admin")]` magic string instead of the project's `[AuthorizeAdmin]` attribute. If the admin role name ever changes, this controller will be missed.
- **Description:** The project has a well-designed `[AuthorizeAdmin]` attribute used consistently across other controllers. SystemSettingsController bypasses it with a raw string.
- **Recommendation:** Replace `[Authorize(Roles = "Admin")]` with `[AuthorizeAdmin]`.
- **Affected files:** `src/Cadence.WebApi/Controllers/SystemSettingsController.cs`

---

### AC-M04: `ExercisesController` at 957 lines with mixed concerns

- **File(s):** `src/Cadence.WebApi/Controllers/ExercisesController.cs`
- **Category:** FileSize
- **Impact:** At 957 lines, ExercisesController is the largest controller and handles CRUD, status management, cloning, archiving, participant management, and exercise settings — all in one file. Finding a specific endpoint requires significant scrolling, and the mixed concerns make it difficult to reason about authorization coverage.
- **Description:** Exceeds the 500-line guideline significantly. Contains business logic that should be in services (see AC-C03) which inflates the line count.
- **Recommendation:** After extracting business logic to services (AC-C03), the controller should shrink naturally. If it remains large, split into `ExercisesController` (CRUD), `ExerciseSettingsController` (settings/config), and `ExerciseParticipantController` (participant management).
- **Affected files:** `src/Cadence.WebApi/Controllers/ExercisesController.cs`

---

### AC-M05: `FeedbackController` at ~505 lines with service-layer orchestration

- **File(s):** `src/Cadence.WebApi/Controllers/FeedbackController.cs`
- **Category:** FileSize
- **Impact:** The feedback controller contains complex orchestration logic for feedback reports including multi-step workflows that belong in the service layer.
- **Description:** Slightly exceeds the 500-line guideline. Contains business logic that should delegate to `IFeedbackService`.
- **Recommendation:** Move orchestration logic to `FeedbackService`. The controller should only handle HTTP concerns.
- **Affected files:** `src/Cadence.WebApi/Controllers/FeedbackController.cs`

---

### AC-M06: `AutocompleteController` bypasses authorization infrastructure

- **File(s):** `src/Cadence.WebApi/Controllers/AutocompleteController.cs`
- **Category:** Convention
- **Impact:** Instead of using the project's `[ExerciseAccess]` attribute, AutocompleteController implements its own `ValidateExerciseAccessAsync` private method with direct `AppDbContext` queries. This duplicates authorization logic and may diverge from the standard authorization handler's behavior.
- **Description:** The authorization infrastructure exists and works. This controller bypasses it with a custom implementation, creating a maintenance and consistency risk.
- **Recommendation:** Replace custom `ValidateExerciseAccessAsync` with `[AuthorizeExerciseAccess]` attribute on the controller or action methods.
- **Affected files:** `src/Cadence.WebApi/Controllers/AutocompleteController.cs`

---

### AC-M07: `CapabilitiesController` write endpoints have no role gate

- **File(s):** `src/Cadence.WebApi/Controllers/CapabilitiesController.cs`
- **Category:** Stability
- **Impact:** Any authenticated organization member can create, update, or delete capabilities. The capability library is an organization-level resource that should be managed by OrgAdmin or OrgManager roles only.
- **Description:** Write endpoints (POST, PUT, DELETE) only require `[Authorize]` (authentication) without any role-based restriction. Read endpoints are correctly open to all members.
- **Recommendation:** Add `[AuthorizeOrgRole("OrgAdmin", "OrgManager")]` to write endpoints. Verify similar patterns in other org-level management controllers.
- **Affected files:** `src/Cadence.WebApi/Controllers/CapabilitiesController.cs`

---

### AC-M08: `NotificationsController` repeats user ID extraction 4 times

- **File(s):** `src/Cadence.WebApi/Controllers/NotificationsController.cs`
- **Category:** DRY
- **Impact:** Four action methods each contain the same user ID extraction logic instead of extracting once in a shared method or base class.
- **Description:** Related to AC-M01 but localized to a single controller. Each action method duplicates the claims extraction.
- **Recommendation:** Extract to a private helper or use the shared extension method from AC-M01's fix.
- **Affected files:** `src/Cadence.WebApi/Controllers/NotificationsController.cs`

---

### AC-M09: `UserPreferencesController` uses manual string-comparison enum validation

- **File(s):** `src/Cadence.WebApi/Controllers/UserPreferencesController.cs`
- **Category:** Readability
- **Impact:** Uses manual string comparison and `Enum.TryParse` to validate preference values instead of using strongly-typed DTOs with enum properties. This bypasses ASP.NET Core's built-in model binding and validation.
- **Description:** Preference values are received as strings and manually parsed. If the enum values change, the string comparisons may silently fail.
- **Recommendation:** Use strongly-typed DTOs with enum properties. ASP.NET Core model binding will automatically deserialize and validate enum values, returning 400 for invalid values.
- **Affected files:** `src/Cadence.WebApi/Controllers/UserPreferencesController.cs`

---

### AC-M10: Global exception handler does not guard `context.Response.HasStarted`

- **File(s):** `src/Cadence.WebApi/Program.cs`
- **Category:** Stability
- **Impact:** If an exception occurs after response headers have been sent (e.g., during streaming), attempting to write a ProblemDetails response will throw a secondary exception, masking the original error.
- **Description:** The inline exception handler middleware does not check `context.Response.HasStarted` before attempting to write the error response. ASP.NET Core's built-in `UseExceptionHandler` handles this case, but the custom inline handler does not.
- **Recommendation:** Add `if (context.Response.HasStarted) { logger.LogWarning("Response already started, cannot write error"); throw; }` before setting the response status code.
- **Affected files:** `src/Cadence.WebApi/Program.cs`

---

## Minor Issues

### AC-N01: Duplicate `GetCurrentUserId` method in `ExerciseClockController`

- **File(s):** `src/Cadence.WebApi/Controllers/ExerciseClockController.cs`
- **Category:** DRY
- **Description:** Contains two identical copies of `GetCurrentUserId()` — likely a merge artifact.
- **Recommendation:** Remove the duplicate. Will be resolved by AC-M01's shared extension method.

---

### AC-N02: `VersionInfo` record defined inline in controller file

- **File(s):** `src/Cadence.WebApi/Controllers/VersionController.cs`
- **Category:** Convention
- **Description:** The `VersionInfo` record is defined in the same file as the controller instead of in a DTOs directory.
- **Recommendation:** Move to `src/Cadence.Core/Features/System/Models/DTOs/VersionInfoDto.cs` or similar.

---

### AC-N03: `StatusCode(500)` returned directly in `BulkParticipantImportController`

- **File(s):** `src/Cadence.WebApi/Controllers/BulkParticipantImportController.cs`
- **Category:** Convention
- **Description:** Returns `StatusCode(500, ...)` directly instead of using the ProblemDetails pattern consistent with other controllers.
- **Recommendation:** Replace with `Problem(detail: ..., statusCode: 500)` or let the global exception handler manage it.

---

### AC-N04: Export route path inconsistency in `ExcelExportController`

- **File(s):** `src/Cadence.WebApi/Controllers/ExcelExportController.cs`
- **Category:** Convention
- **Description:** Route uses `/api/exercises/{exerciseId}/export/...` while other exercise-scoped controllers use `/api/exercises/{exerciseId}/...` directly. The `/export/` prefix is inconsistent.
- **Recommendation:** Minor — consider aligning with the standard route pattern if it doesn't break frontend URLs.

---

### AC-N05: Missing `[Authorize]` attribute on `ExerciseHub`

- **File(s):** `src/Cadence.WebApi/Hubs/ExerciseHub.cs`
- **Category:** Stability
- **Description:** The SignalR hub class does not have an `[Authorize]` attribute. While the hub route may be protected by middleware, explicit authorization on the hub class makes the security intent clear and provides defense-in-depth.
- **Recommendation:** Add `[Authorize]` to the `ExerciseHub` class.

---

### AC-N06: Regex-based body sanitization fragility

- **File(s):** `src/Cadence.WebApi/Middleware/RequestResponseLoggingMiddleware.cs`
- **Category:** Stability
- **Description:** The request/response logging middleware uses regex patterns to sanitize sensitive fields from request bodies before logging. Regex-based sanitization is fragile and can be bypassed with whitespace, encoding, or nested JSON.
- **Recommendation:** Parse the body as JSON, remove sensitive keys programmatically, then serialize back. This is more robust than regex replacement.

---

## Positive Patterns

### AC-P01: Authorization policy infrastructure is well-designed

- **Where:** `src/Cadence.WebApi/Authorization/`
- **What:** The `ExerciseAccessRequirement`, `ExerciseRoleRequirement`, and their handlers provide a clean, reusable authorization pattern. The custom `[AuthorizeExerciseAccess]` and `[AuthorizeExerciseRole]` attributes make intent clear at the controller level. The pattern correctly separates policy evaluation from business logic.
- **Replicate in:** Apply to all controllers that currently lack exercise-scoped authorization (see AC-C02).

---

### AC-P02: Rate limiting on authentication endpoints

- **Where:** `src/Cadence.WebApi/Program.cs` rate limiter configuration
- **What:** Authentication endpoints (login, register, token refresh) have specific rate limit policies that prevent brute-force attacks. The rate limiter is registered as middleware and applied via `[EnableRateLimiting]` attributes.
- **Replicate in:** Consider applying rate limits to other sensitive endpoints (password change, email verification).

---

### AC-P03: Structured Serilog context middleware

- **Where:** `src/Cadence.WebApi/Program.cs` Serilog enrichment
- **What:** Request logging includes user ID, organization ID, and correlation ID as structured properties. This enables powerful log queries in production for debugging user-specific issues.
- **Replicate in:** Ensure all controllers and services use `ILogger` consistently so these enriched properties propagate.

---

### AC-P04: `AuthController` pattern is exemplary

- **Where:** `src/Cadence.WebApi/Controllers/AuthController.cs`
- **What:** AuthController correctly delegates all business logic to `IAuthService`, uses proper HTTP status codes (201 for registration, 200 for login, 401 for failures), returns structured error responses, and has complete authorization attribute coverage. This is the gold standard for controller implementation in this project.
- **Replicate in:** All other controllers should follow this delegation pattern.

---

### AC-P05: `RequestResponseLoggingMiddleware` captures audit trail

- **Where:** `src/Cadence.WebApi/Middleware/RequestResponseLoggingMiddleware.cs`
- **What:** Despite the regex fragility noted in AC-N06, the middleware provides valuable audit logging by capturing request/response pairs with timing information. This is essential for debugging production issues.
- **Replicate in:** Maintain this middleware but improve the sanitization approach.

---

### AC-P06: Clean hub group management pattern

- **Where:** `src/Cadence.WebApi/Hubs/ExerciseHub.cs`
- **What:** The hub correctly manages SignalR group membership with `JoinExerciseGroup` and `LeaveExerciseGroup` methods that map to exercise IDs. The `ExerciseHubContext` in Core provides a clean abstraction for broadcasting to exercise groups from services.
- **Replicate in:** Any future hub implementations should follow this group management pattern (fix the user group issue in AC-C06 first).
