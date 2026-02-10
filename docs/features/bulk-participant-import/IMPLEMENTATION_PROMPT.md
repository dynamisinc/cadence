# Bulk Participant Import - Implementation Prompt

> Copy everything below this line into a new Claude Code session.

---

## Task

Implement the **Bulk Participant Import** feature defined in `docs/features/bulk-participant-import/`. This feature allows Exercise Directors to upload CSV/XLSX files to bulk-add participants to an exercise. Read all story files S01-S06 and the FEATURE.md before starting.

## Architecture Decision: Mandatory

Participants **must** become organization members before receiving exercise assignments. The upload flow bridges org invitation + exercise assignment into a single user action. Read `docs/features/bulk-participant-import/S04-invite-non-members.md` carefully — this introduces the `PendingExerciseAssignment` entity concept.

## Implementation Strategy

Use the **orchestrator** agent to coordinate parallel work across backend, frontend, database, and testing agents. The implementation order is:

### Phase 1: Database & Contracts (database-agent + infrastructure-agent, parallel)

**database-agent:**
- Create `PendingExerciseAssignment` entity: `{ Id, OrganizationInviteId, ExerciseId, ExerciseRole, Status (Pending/Activated/Expired/Cancelled), CreatedAt }`
- Create `BulkImportRecord` entity: `{ Id, ExerciseId, ImportedById, ImportedAt, FileName, TotalRows, AssignedCount, UpdatedCount, InvitedCount, ErrorCount }`
- Create `BulkImportRowResult` entity: `{ Id, BulkImportRecordId, RowNumber, Email, ExerciseRole, Classification (Assign/Update/Invite/Error), Status, ErrorMessage }`
- All entities follow `BaseEntity` pattern with soft delete
- `PendingExerciseAssignment` is org-scoped via its relationship to `OrganizationInvite`
- Create EF migration

**infrastructure-agent:**
- Define shared DTOs in `Cadence.Core/Features/BulkParticipantImport/Models/DTOs/`
- Define `IBulkParticipantImportService` interface
- Define `IParticipantFileParser` interface for file parsing abstraction
- Define classification enum: `ParticipantClassification { Assign, Update, Invite, Error }`

### Phase 2: Backend Services (backend-agent + testing-agent, parallel TDD)

**CRITICAL: TDD is mandatory. The testing-agent writes tests FIRST, then the backend-agent implements to pass them.**

**testing-agent writes these test classes first:**

1. `ParticipantFileParserTests` — column detection and row parsing:
   - Parses CSV with exact headers: `Email, Exercise Role, Display Name, Organization Role`
   - Parses XLSX with exact headers
   - Handles synonym headers case-insensitively: `E-mail`, `Email Address`, `e_mail`, `EMAIL`
   - Handles role synonyms: `Exercise Role`, `HSEEP Role`, `Role`, `ExRole`, `Participant Role`
   - Handles display name synonyms: `Display Name`, `Name`, `Full Name`, `DisplayName`, `Participant Name`
   - Handles org role synonyms: `Organization Role`, `Org Role`, `OrgRole`
   - Handles whitespace in headers (leading/trailing spaces, tabs)
   - Handles headers with different casing: `EMAIL`, `email`, `Email`, `eMail`
   - Skips empty rows (all-blank cells)
   - Rejects files missing required `Email` column after synonym matching
   - Rejects files missing required `Exercise Role` column after synonym matching
   - Rejects files exceeding 500 rows
   - Validates email format per row
   - Validates exercise role values per row (accepts synonyms: `Exercise Director`, `ExerciseDirector`, `Director`, `Controller`, `Evaluator`, `Observer`)
   - Flags duplicate emails within the same file
   - Returns structured `ParsedParticipantRow` results with row numbers

2. `ParticipantClassificationServiceTests` — classifying parsed rows:
   - Classifies existing org member not in exercise as `Assign`
   - Classifies existing org member already in exercise (same role) as `Update` with no-change flag
   - Classifies existing org member already in exercise (different role) as `Update` with old/new roles
   - Classifies existing Cadence user not in org as `Invite`
   - Classifies unknown email as `Invite` with new-account flag
   - Classifies email with pending org invite as `Invite` with existing-invite flag
   - Classifies row with invalid email as `Error`
   - Classifies Exercise Director assignment for User-role system user as `Error`
   - Uses batch queries (WHERE email IN) not per-row lookups

3. `BulkParticipantImportServiceTests` — processing confirmed imports:
   - Creates ExerciseParticipant for `Assign` rows
   - Updates ExerciseParticipant role for `Update` rows with role change
   - Skips `Update` rows with no change
   - Reactivates soft-deleted ExerciseParticipant
   - Creates OrganizationInvite + PendingExerciseAssignment for `Invite` rows
   - Reuses existing pending OrganizationInvite when one exists
   - Sets AssignedById audit field to importing user
   - Creates BulkImportRecord with correct counts
   - Continues processing when individual rows fail (no full rollback)
   - Validates exercise is in Draft or Active status

4. `PendingExerciseAssignmentTests` — auto-activation on invite acceptance:
   - When org invite is accepted, pending exercise assignment activates
   - ExerciseParticipant is created with the stored role
   - Pending assignment status changes to Activated
   - When invite expires, pending assignment status changes to Expired
   - When invite is cancelled, pending assignment status changes to Cancelled
   - Exercise Director pending assignment validates system role on activation

**backend-agent implements to pass these tests:**

- `ParticipantFileParser` in `Core/Features/BulkParticipantImport/Services/` — use the synonym mapping pattern from the existing `ExcelImportService` (read it first: `Core/Features/ExcelImport/Services/ExcelImportService.cs`)
- `ParticipantClassificationService` — batch-query based classification
- `BulkParticipantImportService` — orchestrates parse → classify → process
- Hook `PendingExerciseAssignment` activation into existing `OrganizationInvitationService.AcceptInvitationAsync`
- `BulkParticipantImportController` with endpoints:
  - `POST /api/exercises/{exerciseId}/participants/bulk-import/upload` — upload and parse file
  - `GET /api/exercises/{exerciseId}/participants/bulk-import/{sessionId}/preview` — get classification preview
  - `POST /api/exercises/{exerciseId}/participants/bulk-import/{sessionId}/confirm` — execute import
  - `GET /api/exercises/{exerciseId}/participants/bulk-import/history` — list past imports
  - `GET /api/exercises/{exerciseId}/participants/bulk-import/template?format=csv|xlsx` — download template

### Phase 3: Frontend (frontend-agent + testing-agent, parallel TDD)

**testing-agent writes frontend tests first using Vitest + React Testing Library:**

1. `BulkImportDialog.test.tsx`:
   - Renders upload area with file input accepting .csv and .xlsx
   - Shows error for files > 10MB
   - Shows error for unsupported file types
   - Calls upload API on file selection
   - Shows loading state during upload
   - Navigates to preview on successful parse

2. `ImportPreview.test.tsx`:
   - Renders summary counts (assign, update, invite, error)
   - Renders classification rows with correct indicators (green/yellow/blue/red)
   - Filters rows by classification when filter chips clicked
   - Shows role change details for Update rows
   - Shows warnings for Exercise Director validation issues
   - Disables confirm button when all rows are errors
   - Calls confirm API on button click
   - Navigates to results on successful confirm

3. `ImportResults.test.tsx`:
   - Renders result summary counts
   - Renders assigned participants list
   - Renders pending invitations with Resend/Cancel actions
   - Renders error rows with reasons
   - Calls resend API when Resend clicked
   - Calls cancel API when Cancel clicked

4. `useParticipantImport.test.ts`:
   - Hook manages upload → preview → confirm flow state
   - Handles API errors at each stage
   - Invalidates participant queries on successful import

**frontend-agent implements:**

- All components use COBRA styled components (`CobraPrimaryButton`, `CobraTextField`, etc.) — NOT raw MUI
- All icons use FontAwesome — NOT MUI icons
- File structure: `src/frontend/src/features/exercises/components/bulk-import/`
  - `BulkImportDialog.tsx`
  - `ImportPreview.tsx`
  - `ImportResults.tsx`
  - `ParticipantTemplateDownload.tsx`
- Hook: `src/frontend/src/features/exercises/hooks/useParticipantImport.ts`
- Service: `src/frontend/src/features/exercises/services/bulkImportService.ts`
- Types: `src/frontend/src/features/exercises/types/bulkImport.ts`

### Phase 4: Integration & Review (code-review agent)

- Run all backend tests: `dotnet test src/Cadence.Core.Tests/`
- Run all frontend tests: `cd src/frontend && npm test`
- Run type check: `cd src/frontend && npm run type-check`
- Use code-review agent to verify:
  - COBRA styling compliance (no raw MUI, no MUI icons)
  - HSEEP terminology (participants, not users; fire, not trigger)
  - Organization scoping (all queries filter by org context)
  - Soft delete patterns on new entities
  - No security vulnerabilities (file upload validation, SQL injection via email)

## Key Design Decisions

### Column Header Flexibility

The parser MUST handle real-world spreadsheet variations. Emergency management agencies don't use standardized column names. Implement a **weighted synonym matching** system:

```
Email synonyms: "Email", "E-mail", "Email Address", "E-Mail Address", "Participant Email", "Contact Email", "email", "EMAIL"
Exercise Role synonyms: "Exercise Role", "HSEEP Role", "Role", "Participant Role", "Assignment", "ExRole"
Display Name synonyms: "Display Name", "Name", "Full Name", "Participant Name", "DisplayName", "Contact Name"
Org Role synonyms: "Organization Role", "Org Role", "OrgRole", "Organization"
```

Matching rules:
1. Exact match first (case-insensitive)
2. Strip whitespace, underscores, hyphens, then re-match
3. Normalize to lowercase for comparison
4. If multiple columns match the same field, take the first match and warn

### Exercise Role Value Flexibility

Accept multiple formats for the same role:
- Exercise Director: `ExerciseDirector`, `Exercise Director`, `Director`, `ED`
- Controller: `Controller`, `CTRL`
- Evaluator: `Evaluator`, `EVAL`
- Observer: `Observer`, `OBS`

### File Format Handling

- CSV: Handle UTF-8 BOM, detect delimiter (comma vs semicolon vs tab), handle quoted fields with embedded commas
- XLSX: Read first sheet only, skip rows before the header row (detect header row by matching known column names), handle merged cells gracefully

## Reference Files to Read First

Before writing any code, these existing files MUST be read to understand patterns:

```
# Feature requirements
docs/features/bulk-participant-import/FEATURE.md
docs/features/bulk-participant-import/S01-upload-participant-file.md
docs/features/bulk-participant-import/S02-preview-validate-import.md
docs/features/bulk-participant-import/S03-process-existing-members.md
docs/features/bulk-participant-import/S04-invite-non-members.md
docs/features/bulk-participant-import/S05-view-upload-results.md
docs/features/bulk-participant-import/S06-download-participant-template.md

# Existing patterns to follow
src/Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs  (file parsing, synonym mapping)
src/Cadence.Core/Features/Exercises/Services/ExerciseParticipantService.cs  (participant assignment)
src/Cadence.Core/Features/Organizations/Services/OrganizationInvitationService.cs  (invitation flow)
src/Cadence.Core/Features/Organizations/Services/MembershipService.cs  (org membership)
src/Cadence.Core/Models/Entities/ExerciseParticipant.cs  (entity pattern)
src/Cadence.Core/Models/Entities/OrganizationInvite.cs  (entity pattern)
src/Cadence.Core/Models/Entities/BaseEntity.cs  (base entity pattern)

# Styling and conventions
docs/COBRA_STYLING.md
docs/CODING_STANDARDS.md
```

## Constraints

- Do NOT modify existing entity classes unless absolutely necessary for relationships
- Do NOT change existing service method signatures
- Exercise must be in Draft or Active status to accept bulk imports
- Maximum 500 rows per upload, 10MB file size limit
- All new entities must inherit from BaseEntity with soft delete
- Backend tests use TestDbContextFactory pattern (see existing tests)
- Frontend tests use Vitest + React Testing Library (see existing tests)
- Use `npm run type-check` to verify frontend compiles — NOT `npm run build`
