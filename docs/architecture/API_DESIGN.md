# API Design

> Last Updated: 2026-03-06 | Version: 2.0

This document is the authoritative catalog of all REST API endpoints for the Cadence backend, hosted on Azure App Service (B1). It is generated from the 35 controllers in `src/Cadence.WebApi/Controllers/`.

---

## Table of Contents

1. [API Conventions](#api-conventions)
2. [Authentication](#authentication)
3. [Rate Limiting](#rate-limiting)
4. [Endpoint Catalog](#endpoint-catalog)
   - [Authentication](#1-authentication--authcontroller)
   - [Organizations (OrgAdmin)](#2-organizations-orgadmin--organizationscontroller)
   - [Organizations (Admin)](#3-organizations-admin--adminorganizationscontroller)
   - [Invitations (Public)](#4-invitations-public--organizationscontroller-overflow)
   - [Exercises](#5-exercises--exercisescontroller)
   - [Exercise Status](#6-exercise-status--exercisestatuscontroller)
   - [Exercise Participants](#7-exercise-participants--exerciseparticipantscontroller)
   - [Exercise Capabilities](#8-exercise-capabilities--exercisecapabilitiescontroller)
   - [Exercise Metrics](#9-exercise-metrics--exercisemetricscontroller)
   - [Injects](#10-injects--injectscontroller)
   - [Exercise Clock](#11-exercise-clock--exerciseclockcontroller)
   - [Observations](#12-observations--observationscontroller)
   - [Objectives](#13-objectives--objectivescontroller)
   - [Expected Outcomes](#14-expected-outcomes--expectedoutcomescontroller)
   - [Phases](#15-phases--phasescontroller)
   - [Excel Import](#16-excel-import--excelimportcontroller)
   - [Excel Export](#17-excel-export--excelexportcontroller)
   - [Bulk Participant Import](#18-bulk-participant-import--bulkparticipantimportcontroller)
   - [Capability Targets (EEG)](#19-capability-targets-eeg--capabilitytargetscontroller)
   - [Critical Tasks (EEG)](#20-critical-tasks-eeg--criticaltaskscontroller)
   - [EEG Entries](#21-eeg-entries--eegentriescontroller)
   - [Capabilities](#22-capabilities--capabilitiescontroller)
   - [Photos](#23-photos--photoscontroller)
   - [Notifications](#24-notifications--notificationscontroller)
   - [Users](#25-users--userscontroller)
   - [User Preferences](#26-user-preferences--userpreferencescontroller)
   - [Email Preferences](#27-email-preferences--emailpreferencescontroller)
   - [Autocomplete](#28-autocomplete--autocompletecontroller)
   - [Organization Suggestions](#29-organization-suggestions--organizationsuggestionscontroller)
   - [Delivery Methods](#30-delivery-methods--deliverymethodscontroller)
   - [System Settings](#31-system-settings--systemsettingscontroller)
   - [EULA](#32-eula--eulacontroller)
   - [Feedback](#33-feedback--feedbackcontroller)
   - [Health](#34-health--healthcontroller)
   - [Version](#35-version--versioncontroller)
   - [Assignments](#36-assignments--assignmentscontroller)
5. [Error Response Format](#error-response-format)
6. [Related Documentation](#related-documentation)

---

## API Conventions

### Base URL

| Environment | Base URL |
|-------------|----------|
| Local | `http://localhost:5071/api` |
| Production | `https://{app-service-name}.azurewebsites.net/api` |

### HTTP Methods

| Method | Purpose | Idempotent | Body |
|--------|---------|------------|------|
| `GET` | Retrieve resource(s) | Yes | No |
| `POST` | Create resource or trigger action | No | Yes |
| `PUT` | Replace resource (full update) | Yes | Yes |
| `PATCH` | Partial update | Yes | Yes |
| `DELETE` | Remove resource | Yes | No |

### Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| `200 OK` | Success | GET, PUT, PATCH, POST actions |
| `201 Created` | Resource created | POST (new resource) |
| `204 No Content` | Success, no body | DELETE, POST actions |
| `400 Bad Request` | Validation error | Invalid request body or parameters |
| `401 Unauthorized` | Not authenticated | Missing or invalid JWT |
| `403 Forbidden` | Not authorized | Insufficient role |
| `404 Not Found` | Resource missing | ID not found |
| `409 Conflict` | Duplicate resource | Unique constraint violation |
| `429 Too Many Requests` | Rate limit exceeded | Auth endpoints |
| `503 Service Unavailable` | Unhealthy | Database unreachable |

### JSON Serialization

All request and response bodies use JSON with the following conventions:

| Convention | Detail |
|------------|--------|
| Property names | `camelCase` |
| Enum values | Serialized as strings (e.g., `"Draft"`, `"Active"`) |
| Exception: `ApprovalRoles` | Serialized as integer bitmask |
| DateTime format | ISO 8601 UTC (`2026-03-06T14:30:00Z`) |
| Null fields | Included as `null` (not omitted) |
| GUIDs | Standard hyphenated string (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`) |

---

## Authentication

All endpoints require a JWT Bearer token unless explicitly marked as `[AllowAnonymous]`.

```
Authorization: Bearer <access_token>
```

### JWT Claims Structure

```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "name": "Display Name",
  "role": "User",
  "SystemRole": "Admin",
  "org_id": "org-guid",
  "org_role": "OrgAdmin",
  "org_name": "Organization Name"
}
```

### Token Lifecycle

| Token | Storage | Expiry |
|-------|---------|--------|
| Access token | Memory / response body | Short-lived (minutes) |
| Refresh token | HttpOnly cookie (`refreshToken`) | Configurable (days) |

Refresh tokens are rotated on each use. Logout revokes the current refresh token and clears the cookie.

### Authorization Attributes

| Attribute | Required Role |
|-----------|---------------|
| `[Authorize]` | Any authenticated user |
| `[AuthorizeAdmin]` | SystemRole = Admin |
| `[AuthorizeManager]` | SystemRole = Admin or Manager |
| `[AuthorizeOrgAdmin]` | OrgRole = OrgAdmin |
| `[AuthorizeExerciseAccess]` | Any exercise participant |
| `[AuthorizeExerciseController]` | ExerciseRole = Controller, ExerciseDirector, or Admin |
| `[AuthorizeExerciseDirector]` | ExerciseRole = ExerciseDirector or Admin |
| `[AuthorizeExerciseEvaluator]` | ExerciseRole = Evaluator, ExerciseDirector, or Admin |

---

## Rate Limiting

| Policy | Endpoint(s) | Limit |
|--------|------------|-------|
| `auth` | `POST /api/auth/register`, `POST /api/auth/login` | 10 requests per minute per IP |
| `password-reset` | `POST /api/auth/password-reset/request` | 3 requests per 15 minutes per IP |

Exceeding a rate limit returns `429 Too Many Requests`.

---

## Endpoint Catalog

### 1. Authentication — `AuthController`

**Route prefix:** `api/auth` | **Auth:** `[AllowAnonymous]` (all endpoints)

| Method | Route | Description | Rate Limit |
|--------|-------|-------------|------------|
| `POST` | `/register` | Register a new user. First user becomes Admin. Returns access token + sets refresh cookie. | `auth` |
| `POST` | `/login` | Authenticate with email and password. Returns access token + sets refresh cookie. | `auth` |
| `POST` | `/refresh` | Rotate refresh token (from `refreshToken` cookie). Returns new access token. | None |
| `POST` | `/logout` | Revoke current session. Clears refresh token cookie. | None |
| `GET` | `/methods` | List available authentication methods (password, SSO, etc.). | None |
| `POST` | `/password-reset/request` | Send password reset email. Always returns 200 to prevent email enumeration. | `password-reset` |
| `POST` | `/password-reset/complete` | Complete reset with token + new password. Auto-authenticates on success. | None |

---

### 2. Organizations (OrgAdmin) — `OrganizationsController`

**Route prefix:** `api/organizations` | **Auth:** `[Authorize]`

#### Current Organization

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/current` | Get current organization details. | OrgAdmin |
| `PUT` | `/current` | Update current organization (name, settings). | OrgAdmin |

#### Current Organization Members

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/current/members` | List all members of current organization. | OrgAdmin |
| `POST` | `/current/members` | Add a user to current organization by email. | OrgAdmin |
| `PUT` | `/current/members/{membershipId}` | Update a member's org role. | OrgAdmin |
| `DELETE` | `/current/members/{membershipId}` | Remove a member from current organization. | OrgAdmin |

#### Current Organization Invitations

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/current/invitations` | List invitations. Optional `?status=` filter. | OrgAdmin |
| `POST` | `/current/invitations` | Create and send an invitation. | OrgAdmin |
| `POST` | `/current/invitations/{invitationId}/resend` | Resend a pending or expired invitation. | OrgAdmin |
| `DELETE` | `/current/invitations/{invitationId}` | Cancel a pending invitation. | OrgAdmin |

#### Current Organization Settings

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/current/settings/approval-permissions` | Get approval permission settings (roles, self-approval policy). | OrgAdmin |
| `PUT` | `/current/settings/approval-permissions` | Update approval permission settings. | OrgAdmin |
| `PUT` | `/current/settings/approval-policy` | Update inject approval policy (Disabled, Optional, Required). | OrgAdmin |

---

### 3. Organizations (Admin) — `AdminOrganizationsController`

**Route prefix:** `api/admin/organizations` | **Auth:** `[AuthorizeAdmin]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List all organizations. Query params: `search`, `status`, `sortBy`, `sortDir`. |
| `GET` | `/{id}` | Get a single organization by ID. |
| `POST` | `/` | Create a new organization. |
| `PUT` | `/{id}` | Update an organization. |
| `GET` | `/check-slug` | Check slug availability. Query params: `slug`, `excludeId`. |
| `POST` | `/{id}/archive` | Archive an organization (read-only). |
| `POST` | `/{id}/deactivate` | Deactivate an organization (soft delete). |
| `POST` | `/{id}/restore` | Restore archived or inactive organization to active. |
| `GET` | `/{id}/members` | List all members of an organization. |
| `POST` | `/{id}/members` | Add a user to an organization by email. |
| `PUT` | `/{id}/members/{membershipId}` | Update a member's org role. |
| `DELETE` | `/{id}/members/{membershipId}` | Remove a member from an organization. |
| `PUT` | `/{id}/settings/approval-policy` | Update organization approval policy. |
| `GET` | `/{id}/settings/approval-permissions` | Get approval permission settings. |
| `PUT` | `/{id}/settings/approval-permissions` | Update approval permission settings. |

---

### 4. Invitations (Public) — `OrganizationsController` overflow

**Note:** These endpoints use an absolute route override (`/api/invitations/...`) and are served from `OrganizationsController`.

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/invitations/validate/{code}` | Validate an invitation code. Public. | `[AllowAnonymous]` |
| `POST` | `/api/invitations/accept/{code}` | Accept invitation and join organization. | `[Authorize]` |

---

### 5. Exercises — `ExercisesController`

**Route prefix:** `api/exercises` | **Auth:** `[Authorize]`

#### CRUD

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List exercises. Query params: `includeArchived`, `archivedOnly`. Scoped to org context. |
| `GET` | `/{id}` | Get a single exercise by ID. |
| `POST` | `/` | Create a new exercise (requires org context). |
| `PUT` | `/{id}` | Update exercise name, description, type, schedule, timing config. |
| `DELETE` | `/{id}` | Permanently delete exercise and all related data. |
| `POST` | `/{id}/duplicate` | Duplicate exercise with MSEL, phases, and objectives. Body: optional `{ name, scheduledDate }`. |

#### MSEL

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/{id}/msel/summary` | Get active MSEL summary (progress, counts, last modified). |
| `GET` | `/{id}/msels` | List all MSEL versions for an exercise. |
| `GET` | `/msels/{mselId}/summary` | Get a specific MSEL summary by ID. |

#### Setup and Deletion

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/{id}/setup-progress` | Get setup completion status for configuration areas. |
| `GET` | `/{id}/delete-summary` | Preview what will be deleted before permanent delete. |

#### Settings

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/{id}/settings` | Get exercise settings (clock mode, auto-fire, confirmations). | ExerciseAccess |
| `PUT` | `/{id}/settings` | Update exercise settings. Clock multiplier requires paused state. | ExerciseDirector |

#### Approval

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/{id}/approval-settings` | Get exercise approval settings and org policy context. | ExerciseAccess |
| `PUT` | `/{id}/approval-settings` | Update exercise approval settings. | ExerciseDirector |
| `GET` | `/{id}/approval-status` | Get approval status summary (counts by Draft/Submitted/Approved). | ExerciseAccess |

---

### 6. Exercise Status — `ExerciseStatusController`

**Route prefix:** `api/exercises/{exerciseId}` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/{exerciseId}/activate` | Transition Draft → Active. Requires at least one inject. |
| `POST` | `/{exerciseId}/pause` | Transition Active → Paused. Preserves elapsed clock time. |
| `POST` | `/{exerciseId}/resume` | Transition Paused → Active. |
| `POST` | `/{exerciseId}/complete` | Transition Active/Paused → Completed. Stops clock permanently. |
| `POST` | `/{exerciseId}/archive` | Transition Completed → Archived. Makes exercise fully read-only. |
| `POST` | `/{exerciseId}/unarchive` | Transition Archived → Completed. |
| `POST` | `/{exerciseId}/revert-to-draft` | Transition Paused → Draft. Clears all conduct data. |
| `GET` | `/{exerciseId}/available-transitions` | List valid next statuses from current status. |
| `GET` | `/{exerciseId}/publish-validation` | Validate exercise can go live (checks unapproved inject counts). |

---

### 7. Exercise Participants — `ExerciseParticipantsController`

**Route prefix:** `api/exercises/{exerciseId}/participants` | **Auth:** `[Authorize]`

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/` | List all participants with exercise roles. | ExerciseAccess |
| `GET` | `/{userId}` | Get a specific participant. | ExerciseAccess |
| `POST` | `/` | Add participant with optional exercise role. | ExerciseDirector |
| `PUT` | `/{userId}/role` | Update participant's exercise role. | ExerciseDirector |
| `DELETE` | `/{userId}` | Remove participant from exercise. | ExerciseDirector |
| `PUT` | `/` | Bulk update participants (replace or add). | ExerciseDirector |

---

### 8. Exercise Capabilities — `ExerciseCapabilitiesController`

**Route prefix:** `api/exercises/{exerciseId}/capabilities` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List target capabilities for an exercise (active only). |
| `PUT` | `/` | Set target capabilities (replaces all). Pass empty array to clear. |
| `GET` | `/summary` | Get capability coverage summary (target count, evaluated count, percentage). |

---

### 9. Exercise Metrics — `ExerciseMetricsController`

**Route prefix:** `api/exercises/{exerciseId}` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/{exerciseId}/progress` | Real-time progress for conduct view (inject counts, observations, clock status). |
| `GET` | `/{exerciseId}/metrics/injects` | Inject delivery statistics for AAR. Query param: `onTimeToleranceMinutes` (default: 5). |
| `GET` | `/{exerciseId}/metrics/observations` | Observation statistics (P/S/M/U distribution, coverage). |
| `GET` | `/{exerciseId}/metrics/timeline` | Timeline and duration analysis (pause history, phase timing). |
| `GET` | `/{exerciseId}/metrics/controllers` | Controller activity metrics. Query param: `onTimeToleranceMinutes`. |
| `GET` | `/{exerciseId}/metrics/evaluators` | Evaluator coverage metrics (observation distribution, objective coverage). |
| `GET` | `/{exerciseId}/metrics/capabilities` | Capability performance metrics (P/S/M/U by capability). |

---

### 10. Injects — `InjectsController`

**Route prefix:** `api/exercises/{exerciseId}/injects` | **Auth:** `[Authorize]`

#### CRUD

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/` | List all injects for exercise active MSEL. Query params: `status`, `mySubmissionsOnly`. | ExerciseAccess |
| `GET` | `/{id}` | Get a single inject with all fields. | ExerciseAccess |
| `GET` | `/{id}/history` | Get status change audit trail for an inject. | ExerciseAccess |
| `POST` | `/` | Create a new inject (auto-creates MSEL if none exists). | ExerciseController |
| `PUT` | `/{id}` | Update inject. Editing Approved/Submitted injects reverts to Draft if approval enabled. | ExerciseController |

#### Conduct Actions

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `POST` | `/{id}/fire` | Fire (deliver) an inject. Broadcasts `InjectFired` SignalR event. | ExerciseController |
| `POST` | `/{id}/skip` | Skip an inject with mandatory reason. | ExerciseController |
| `POST` | `/{id}/reset` | Reset a fired or skipped inject to Draft. | ExerciseController |
| `POST` | `/reorder` | Reorder injects by providing new sequence of inject IDs. | ExerciseController |

#### Approval Workflow

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `POST` | `/{id}/submit` | Submit inject for approval. | ExerciseController |
| `POST` | `/{id}/approve` | Approve a submitted inject. Body: optional `{ notes, confirmSelfApproval }`. | ExerciseDirector |
| `POST` | `/{id}/reject` | Reject a submitted inject (reason required, min 10 chars). | ExerciseDirector |
| `POST` | `/{id}/revert` | Revert approved inject to Submitted for re-review (reason required). | ExerciseDirector |
| `POST` | `/batch/approve` | Batch approve multiple submitted injects. Self-submissions skipped. | ExerciseDirector |
| `POST` | `/batch/reject` | Batch reject multiple submitted injects (reason required). | ExerciseDirector |
| `GET` | `/{id}/can-approve` | Check if current user can approve a specific inject (includes self-approval check). | ExerciseAccess |
| `GET` | `/can-approve` | Check if current user can approve any inject in this exercise. | ExerciseAccess |

#### Critical Task Linking

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/{id}/critical-tasks` | Get linked critical task IDs for an inject. | ExerciseAccess |
| `PUT` | `/{id}/critical-tasks` | Set linked critical tasks (replaces all). | ExerciseController |

---

### 11. Exercise Clock — `ExerciseClockController`

**Route prefix:** `api/exercises/{exerciseId}/clock` | **Auth:** `[Authorize]`

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/` | Get current clock state (elapsed time, multiplier, state). | ExerciseAccess |
| `POST` | `/start` | Start the clock (transitions Draft → Active). | ExerciseController |
| `POST` | `/pause` | Pause clock, preserving elapsed time. | ExerciseController |
| `POST` | `/stop` | Stop clock and complete the exercise. | ExerciseController |
| `POST` | `/reset` | Reset clock to zero (Draft or Stopped only). | ExerciseController |
| `POST` | `/set-time` | Manually set elapsed time (clock must be paused). | ExerciseDirector |

---

### 12. Observations — `ObservationsController`

**Route prefix:** `api` | **Auth:** `[Authorize]`

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/exercises/{exerciseId}/observations` | List all observations for an exercise. | ExerciseAccess |
| `POST` | `/exercises/{exerciseId}/observations` | Create observation. Content required (max 4000 chars). | ExerciseEvaluator |
| `GET` | `/injects/{injectId}/observations` | List all observations for a specific inject. | Authenticated |
| `GET` | `/observations/{id}` | Get a single observation. | Authenticated |
| `PUT` | `/observations/{id}` | Update observation. Evaluators can only edit own observations. | ExerciseEvaluator |
| `DELETE` | `/observations/{id}` | Soft-delete an observation. | ExerciseDirector |

---

### 13. Objectives — `ObjectivesController`

**Route prefix:** `api/exercises/{exerciseId}/objectives` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List all objectives for an exercise. |
| `GET` | `/summaries` | Lightweight objective summaries for dropdowns. |
| `GET` | `/{id}` | Get a single objective. |
| `POST` | `/` | Create an objective (name required, min 3 chars). |
| `PUT` | `/{id}` | Update an objective. |
| `DELETE` | `/{id}` | Soft-delete. Only allowed if no injects are linked. |
| `GET` | `/check-number` | Check if objective number is available. Query params: `number`, `excludeId`. |

---

### 14. Expected Outcomes — `ExpectedOutcomesController`

**Route prefix:** `api/injects/{injectId}/outcomes` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List all expected outcomes for an inject. |
| `GET` | `/{id}` | Get a single expected outcome. |
| `POST` | `/` | Create expected outcome. Blocked on archived exercises. |
| `PUT` | `/{id}` | Update outcome description. Blocked on archived exercises. |
| `POST` | `/{id}/evaluate` | Record evaluation (WasAchieved + evaluator notes) for AAR. |
| `POST` | `/reorder` | Reorder expected outcomes for an inject. |
| `DELETE` | `/{id}` | Delete an expected outcome. Blocked on archived exercises. |

---

### 15. Phases — `PhasesController`

**Route prefix:** `api/exercises/{exerciseId}/phases` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List all phases ordered by sequence, with inject counts. |
| `GET` | `/{id}` | Get a single phase with inject count. |
| `POST` | `/` | Create a phase (name required, min 3 chars, max 100 chars). |
| `PUT` | `/{id}` | Update a phase. Blocked on archived exercises. |
| `DELETE` | `/{id}` | Hard delete. Blocked if phase has injects assigned. |
| `PUT` | `/reorder` | Reorder phases by providing new sequence of phase IDs. |

---

### 16. Excel Import — `ExcelImportController`

**Route prefix:** `api/import` | **Auth:** `[Authorize]`

Multi-step wizard for importing MSEL data from Excel (.xlsx, .xls) or CSV files. Max file size: 10 MB.

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/upload` | Upload file and analyze worksheets. Returns session ID. |
| `GET` | `/sessions/{sessionId}` | Get current session state. |
| `POST` | `/select-worksheet` | Select a worksheet and get column information. |
| `GET` | `/sessions/{sessionId}/mappings` | Get suggested column-to-field mappings. |
| `POST` | `/validate` | Validate data with configured mappings. Returns error list. |
| `PATCH` | `/sessions/{sessionId}/rows` | Update row values and re-validate (for inline corrections). |
| `POST` | `/execute` | Execute the import with configured mappings. |
| `DELETE` | `/sessions/{sessionId}` | Cancel session and clean up temporary files. |

---

### 17. Excel Export — `ExcelExportController`

**Route prefix:** `api/export` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/msel` | Export MSEL to Excel/CSV with options in request body. |
| `GET` | `/exercises/{exerciseId}/msel` | Export MSEL. Query params: `format`, `includeFormatting`, `includeObjectives`, `includePhases`, `includeConductData`, `filename`. |
| `GET` | `/template` | Download blank MSEL template. Query param: `includeFormatting`. |
| `GET` | `/exercises/{exerciseId}/observations` | Export observations to Excel. Query params: `includeFormatting`, `filename`. |
| `GET` | `/exercises/{exerciseId}/full` | Export full exercise package as ZIP (MSEL + Observations + Summary). |

**Response headers** on file downloads:
- `X-Inject-Count`, `X-Phase-Count`, `X-Objective-Count` (MSEL exports)
- `X-Observation-Count` (observation exports)

---

### 18. Bulk Participant Import — `BulkParticipantImportController`

**Route prefix:** `api/exercises/{exerciseId}/participants/bulk-import` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/upload` | Upload and parse CSV or XLSX participant file. Returns session ID. |
| `GET` | `/{sessionId}/preview` | Preview import: shows row classifications (Assign, Update, Invite, Error). |
| `POST` | `/{sessionId}/confirm` | Execute import. Processes assigns, updates, and invitations. |
| `GET` | `/history` | List past bulk imports with summary counts. |
| `GET` | `/records/{importRecordId}` | Get details for a specific import record. |
| `GET` | `/records/{importRecordId}/rows` | Get row-level results for a specific import. |
| `GET` | `/pending` | List participants awaiting invitation acceptance. |
| `GET` | `/template` | Download participant import template. Query param: `format` (csv or xlsx). `[AllowAnonymous]`. |

---

### 19. Capability Targets (EEG) — `CapabilityTargetsController`

**Route prefix:** `api` | **Auth:** `[Authorize]`

Capability targets are exercise-specific measurable performance thresholds linked to capabilities.

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/exercises/{exerciseId}/capability-targets` | List all capability targets for an exercise. | ExerciseAccess |
| `GET` | `/capability-targets/{id}` | Get a single capability target. | Authenticated |
| `POST` | `/exercises/{exerciseId}/capability-targets` | Create a capability target. | ExerciseDirector |
| `PUT` | `/exercises/{exerciseId}/capability-targets/{id}` | Update a capability target. | ExerciseDirector |
| `DELETE` | `/exercises/{exerciseId}/capability-targets/{id}` | Delete (cascades to critical tasks). | ExerciseDirector |
| `PUT` | `/exercises/{exerciseId}/capability-targets/reorder` | Reorder capability targets (body: array of IDs). | ExerciseDirector |

---

### 20. Critical Tasks (EEG) — `CriticalTasksController`

**Route prefix:** `api` | **Auth:** `[Authorize]`

Critical tasks are specific actions required to achieve a capability target.

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/capability-targets/{targetId}/critical-tasks` | List critical tasks for a capability target. | Authenticated |
| `GET` | `/exercises/{exerciseId}/critical-tasks` | List all critical tasks for an exercise. Query params: `hasInjects`, `hasEegEntries`. | ExerciseAccess |
| `GET` | `/critical-tasks/{id}` | Get a single critical task. | Authenticated |
| `POST` | `/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks` | Create a critical task. | ExerciseDirector |
| `PUT` | `/exercises/{exerciseId}/critical-tasks/{id}` | Update a critical task. | ExerciseDirector |
| `DELETE` | `/exercises/{exerciseId}/critical-tasks/{id}` | Delete (cascades to EEG entries and inject links). | ExerciseDirector |
| `PUT` | `/exercises/{exerciseId}/capability-targets/{targetId}/critical-tasks/reorder` | Reorder critical tasks within a target. | ExerciseDirector |
| `PUT` | `/exercises/{exerciseId}/critical-tasks/{id}/injects` | Set linked injects for a critical task. | ExerciseDirector |
| `GET` | `/critical-tasks/{id}/injects` | Get linked inject IDs for a critical task. | Authenticated |

---

### 21. EEG Entries — `EegEntriesController`

**Route prefix:** `api` | **Auth:** `[Authorize]`

EEG entries are structured HSEEP observations recorded against critical tasks.

#### Entries

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/exercises/{exerciseId}/eeg-entries` | List EEG entries with filtering and pagination. Query params: `page`, `pageSize`, `rating`, `evaluatorId`, `capabilityTargetId`, `criticalTaskId`, `fromDate`, `toDate`, `sortBy`, `sortOrder`, `search`. | ExerciseAccess |
| `GET` | `/critical-tasks/{taskId}/eeg-entries` | List EEG entries for a specific critical task. | Authenticated |
| `GET` | `/eeg-entries/{id}` | Get a single EEG entry. | Authenticated |
| `POST` | `/exercises/{exerciseId}/critical-tasks/{taskId}/eeg-entries` | Create EEG entry (observation text required). | ExerciseEvaluator |
| `PUT` | `/exercises/{exerciseId}/eeg-entries/{id}` | Update EEG entry. | ExerciseEvaluator |
| `DELETE` | `/exercises/{exerciseId}/eeg-entries/{id}` | Soft-delete EEG entry. | ExerciseDirector |
| `GET` | `/exercises/{exerciseId}/eeg-coverage` | Get task coverage and rating distribution statistics. | ExerciseAccess |

#### EEG Export and Documents

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/exercises/{exerciseId}/eeg-export` | Export EEG data to Excel or JSON. Query params: `format`, `includeSummary`, `includeByCapability`, `includeAllEntries`, `includeCoverageGaps`, `includeEvaluatorNames`, `includeFormatting`, `filename`. | ExerciseDirector |
| `POST` | `/exercises/{exerciseId}/eeg-export` | Export EEG data with options in request body. | ExerciseDirector |
| `POST` | `/exercises/{exerciseId}/eeg-document` | Generate HSEEP-compliant EEG Word document (blank or completed). | ExerciseAccess |

---

### 22. Capabilities — `CapabilitiesController`

**Route prefix:** `api/organizations/{organizationId}/capabilities` | **Auth:** `[Authorize]`

Organizational capability library (FEMA Core Capabilities, NATO, NIST CSF, ISO 22301, custom).

#### Capabilities CRUD

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List capabilities. Query param: `includeInactive` (default: false). |
| `GET` | `/{id}` | Get a single capability. |
| `POST` | `/` | Create a capability (name required, min 2 chars). |
| `PUT` | `/{id}` | Update a capability. |
| `DELETE` | `/{id}` | Soft-delete (deactivate) a capability. |
| `POST` | `/{id}/reactivate` | Reactivate a previously deactivated capability. |
| `GET` | `/check-name` | Check name availability. Query params: `name`, `excludeId`. |

#### Predefined Libraries

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/libraries` | List available predefined capability libraries. |
| `POST` | `/import` | Import a predefined library. Body: `{ libraryName }`. Skips duplicates by name. |

---

### 23. Photos — `PhotosController`

**Route prefix:** `api` | **Auth:** `[Authorize]`

Photo management for visual documentation during exercise conduct. Max file size: 10 MB.

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `POST` | `/exercises/{exerciseId}/photos` | Upload photo (multipart form: `photo`, optional `thumbnail`, `metadata`). Supports `X-Idempotency-Key` header. | ExerciseAccess |
| `GET` | `/exercises/{exerciseId}/photos` | List photos with filtering and pagination. | ExerciseAccess |
| `GET` | `/exercises/{exerciseId}/photos/{photoId}` | Get a single photo. | ExerciseAccess |
| `PUT` | `/exercises/{exerciseId}/photos/{photoId}` | Update photo metadata (observation link, display order). | ExerciseAccess |
| `DELETE` | `/exercises/{exerciseId}/photos/{photoId}` | Soft-delete a photo. | ExerciseAccess |
| `POST` | `/exercises/{exerciseId}/photos/quick` | Quick capture: upload photo and auto-generate draft observation. | ExerciseAccess |
| `GET` | `/exercises/{exerciseId}/photos/deleted` | List soft-deleted photos (trash view). | ExerciseDirector |
| `POST` | `/exercises/{exerciseId}/photos/{photoId}/restore` | Restore soft-deleted photo to gallery. | ExerciseDirector |
| `DELETE` | `/exercises/{exerciseId}/photos/{photoId}/permanent` | Permanently delete photo and remove from blob storage. Irreversible. | ExerciseDirector |

---

### 24. Notifications — `NotificationsController`

**Route prefix:** `api/notifications` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | Get notifications for current user. Query params: `limit` (default: 10, max: 100), `offset` (default: 0). |
| `GET` | `/unread-count` | Get unread notification count for current user. |
| `POST` | `/{id}/read` | Mark a specific notification as read. |
| `POST` | `/read-all` | Mark all notifications as read. Returns `{ markedCount }`. |

---

### 25. Users — `UsersController`

**Route prefix:** `api/users` | **Auth:** `[Authorize]`

#### User Management (Admin/Manager)

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/` | Paginated user list. Query params: `page`, `pageSize`, `search`, `role`, `status`, `organizationId`. | Manager |
| `GET` | `/{id}` | Get a single user by ID. | Admin |
| `POST` | `/` | Create a user account (non-admins limited to Observer role). | Manager |
| `PUT` | `/{id}` | Update user display name and email. | Admin |
| `PATCH` | `/{id}/role` | Change user's system role (protects last-administrator). | Admin |
| `POST` | `/{id}/deactivate` | Deactivate user account. Body: optional `{ reason }`. | Admin |
| `POST` | `/{id}/reactivate` | Reactivate a deactivated user account. | Admin |
| `GET` | `/{userId}/exercise-assignments` | Get exercise assignments for a user. Users can get own, Admins any. | Authenticated |
| `GET` | `/{userId}/memberships` | Get organization memberships for a user. | Authenticated |

#### Current User

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/me` | Get current user profile including contact info. |
| `PATCH` | `/me/contact` | Update current user's phone number. |
| `GET` | `/me/organizations` | Get current user's organization memberships with `isCurrent` flag. |
| `POST` | `/current-organization` | Switch organization context. Returns new JWT with updated org claims. |

---

### 26. User Preferences — `UserPreferencesController`

**Route prefix:** `api/users/me/preferences` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | Get current user's preferences (creates defaults if none exist). |
| `PUT` | `/` | Update preferences (theme, display density, time format). |
| `DELETE` | `/` | Reset preferences to defaults. |

**Valid values:**
- `theme`: `Light`, `Dark`, `System`
- `displayDensity`: `Comfortable`, `Compact`
- `timeFormat`: `TwentyFourHour`, `TwelveHour`

---

### 27. Email Preferences — `EmailPreferencesController`

**Route prefix:** `api/users/me/email-preferences` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | Get current user's email notification preferences (all 7 categories). |
| `PUT` | `/` | Toggle a single email category. Body: `{ category, isEnabled }`. Cannot disable mandatory categories. |

**Email categories:** `Security` (mandatory), `Invitations` (mandatory), `Assignments`, `Workflow`, `Reminders`, `DailyDigest`, `WeeklyDigest`

---

### 28. Autocomplete — `AutocompleteController`

**Route prefix:** `api/autocomplete` | **Auth:** `[Authorize]`

Provides organization-scoped suggestions based on previously used values in inject fields.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/exercises/{exerciseId}/tracks` | Track suggestions. Query params: `filter`, `limit` (default: 20). |
| `GET` | `/exercises/{exerciseId}/targets` | Target suggestions. |
| `GET` | `/exercises/{exerciseId}/sources` | Source suggestions. |
| `GET` | `/exercises/{exerciseId}/location-names` | Location name suggestions. |
| `GET` | `/exercises/{exerciseId}/location-types` | Location type suggestions. |
| `GET` | `/exercises/{exerciseId}/responsible-controllers` | Responsible controller suggestions. |

---

### 29. Organization Suggestions — `OrganizationSuggestionsController`

**Route prefix:** `api/organizations/current/suggestions` | **Auth:** `[Authorize]` + `[AuthorizeOrgAdmin]`

OrgAdmins curate autocomplete values per inject field for their organization.

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | List suggestions for a field. Query params: `fieldName`, `includeInactive`. |
| `GET` | `/{id}` | Get a single suggestion. |
| `POST` | `/` | Create a managed suggestion. |
| `PUT` | `/{id}` | Update a suggestion. |
| `DELETE` | `/{id}` | Soft-delete a suggestion. |
| `POST` | `/bulk` | Bulk-create suggestions from a list of values (paste support). |
| `PUT` | `/reorder` | Reorder suggestions within a field. Query param: `fieldName`. Body: ordered ID array. |
| `GET` | `/historical` | Get historical values for a field (excludes curated and blocked). Query params: `fieldName`, `limit`. |
| `POST` | `/block` | Block a historical value from autocomplete. |
| `DELETE` | `/block/{id}` | Unblock a previously blocked value. |

---

### 30. Delivery Methods — `DeliveryMethodsController`

**Route prefix:** `api/delivery-methods` | **Auth:** `[Authorize]`

Lookup table for inject delivery method options.

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/` | List all active delivery methods. | Authenticated |
| `GET` | `/all` | List all delivery methods including inactive. | Admin |
| `GET` | `/{id}` | Get a single delivery method. | Authenticated |
| `POST` | `/` | Create a delivery method. | Admin |
| `PUT` | `/{id}` | Update a delivery method. | Admin |
| `DELETE` | `/{id}` | Soft-delete a delivery method. | Admin |
| `PUT` | `/reorder` | Reorder delivery methods. Body: ordered ID array. | Admin |

---

### 31. System Settings — `SystemSettingsController`

**Route prefix:** `api/system-settings` | **Auth:** `[Authorize(Roles = "Admin")]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | Get current system settings. |
| `PUT` | `/` | Update system settings. |
| `POST` | `/test-github` | Test GitHub integration connectivity. |

---

### 32. EULA — `EulaController`

**Route prefix:** `api/eula` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/status` | Get current user's EULA acceptance status. Returns whether acceptance is required and content. |
| `POST` | `/accept` | Accept current EULA version. Body: `{ version }`. |

---

### 33. Feedback — `FeedbackController`

**Route prefix:** `api/feedback` | **Auth:** `[Authorize]`

| Method | Route | Description | Authorization |
|--------|-------|-------------|---------------|
| `GET` | `/` | Paginated feedback reports. Query params: `page`, `pageSize`, `search`, `type`, `status`, `sortBy`, `sortDesc`. | Admin |
| `PATCH` | `/{id}/status` | Update feedback status and admin notes. Body: `{ status, adminNotes }`. | Admin |
| `DELETE` | `/{id}` | Soft-delete a feedback report. | Admin |
| `POST` | `/bug-report` | Submit a bug report. Sends email to support team + acknowledgment to user. | Authenticated |
| `POST` | `/feature-request` | Submit a feature request. | Authenticated |
| `POST` | `/general` | Submit general feedback. | Authenticated |
| `POST` | `/error-report` | Submit automated error report from frontend ErrorBoundary. | Authenticated |

---

### 34. Health — `HealthController`

**Route prefix:** `api/health` | **Auth:** None (no `[Authorize]`)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/live` | Liveness check. Returns `{ status: "Alive", timestamp }`. Always 200. |
| `GET` | `/` | Health check including database connectivity. Returns 200 if healthy, 503 if database unreachable. |

---

### 35. Version — `VersionController`

**Route prefix:** `api/version` | **Auth:** `[AllowAnonymous]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/` | Returns `{ version, commitSha, buildDate, environment }`. |

---

### 36. Assignments — `AssignmentsController`

**Route prefix:** `api/assignments` | **Auth:** `[Authorize]`

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/my` | Get current user's exercise assignments grouped by status (Active, Upcoming, Completed). |
| `GET` | `/my/{exerciseId}` | Get assignment details for a specific exercise. |

---

## Error Response Format

All error responses use a consistent JSON format:

```json
{
  "message": "Human-readable error description"
}
```

Validation errors may include additional context:

```json
{
  "error": "validation_error",
  "message": "Name must be 200 characters or less",
  "field": "name"
}
```

Business rule violations:

```json
{
  "error": "business_rule_violation",
  "message": "Cannot remove the last OrgAdmin from an organization"
}
```

Auth errors follow the `AuthError` format:

```json
{
  "code": "invalid_credentials",
  "message": "Email or password is incorrect"
}
```

Account locked (returned as `429`):

```json
{
  "code": "account_locked",
  "message": "Account is temporarily locked due to multiple failed login attempts"
}
```

---

## Related Documentation

| Document | Location |
|----------|----------|
| Data model and entities | `docs/architecture/DATA_MODEL.md` |
| Role architecture | `docs/architecture/ROLE_ARCHITECTURE.md` |
| SignalR real-time events | `docs/architecture/SIGNALR_EVENTS.md` |
| Architecture overview | `docs/architecture/OVERVIEW.md` |
| Coding standards | `docs/CODING_STANDARDS.md` |
| Domain glossary (HSEEP terms) | `docs/DOMAIN_GLOSSARY.md` |
