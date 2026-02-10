# Feature: Bulk Participant Import

**Phase:** Standard
**Status:** 🚧 In Progress

## Overview

Exercise Directors can upload a CSV or Excel file containing participant information to add multiple participants to an exercise at once. The system handles four scenarios: existing org members, already-assigned participants, existing Cadence users not yet in the org, and completely new users requiring account creation. All participants become organization members before receiving exercise assignments, preserving the platform's multi-tenancy security model.

## Problem Statement

Full-Scale Exercises (FSE) and multi-agency Functional Exercises (FE) often involve 50-500+ participants from multiple agencies. Exercise Directors typically receive participant lists as spreadsheets from partner agencies. Today, each participant must be individually invited to the organization and then separately assigned to the exercise with a specific HSEEP role. For large exercises, this manual one-by-one process is prohibitively slow and error-prone, creating a bottleneck between planning and conduct.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-upload-participant-file.md) | Upload Participant File | P1 | 🚧 In Progress |
| [S02](./S02-preview-validate-import.md) | Preview and Validate Import | P1 | 📋 Ready |
| [S03](./S03-process-existing-members.md) | Process Existing Organization Members | P1 | 📋 Ready |
| [S04](./S04-invite-non-members.md) | Invite Non-Members via Bulk Upload | P1 | 📋 Ready |
| [S05](./S05-view-upload-results.md) | View Upload Results and Status | P2 | 🚧 In Progress |
| [S06](./S06-download-participant-template.md) | Download Participant Template | P2 | 🚧 In Progress |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Exercise Director** | Uploads participant lists, reviews previews, confirms imports, tracks invitation status |
| **OrgAdmin / OrgManager** | May need to approve org invitations if the Exercise Director lacks org invite permissions |
| **External Participant** | Receives invitation email, creates account or accepts org invite, gets auto-assigned to exercise |

## Key Concepts

| Term | Definition |
|------|------------|
| **Bulk Import** | Uploading a file containing multiple participant records for batch processing |
| **Participant Classification** | The system's determination of each row's scenario (existing member, needs invite, etc.) |
| **Pending Exercise Assignment** | An exercise role assignment that activates automatically when the participant accepts their organization invitation |
| **Preview** | A validation step showing the user what will happen before any changes are committed |

## Dependencies

- exercise-config/S02: Assign Participants (existing single-participant assignment)
- organization-management/OM-07: Organization Invitations (email-based invitation system)
- email-communications/EM-02: Invitation Emails (email delivery infrastructure)
- excel-import/S01-S03: Excel Import (existing file parsing and validation patterns)

## Acceptance Criteria (Feature-Level)

- [ ] Exercise Director can upload a CSV or XLSX file with participant information
- [ ] System validates file format, required columns, and row-level data before processing
- [ ] Preview screen shows classification of each row (immediate assignment, needs invite, error)
- [ ] Existing org members are assigned to the exercise immediately
- [ ] Non-members receive organization invitations with pending exercise assignments
- [ ] When a pending invitation is accepted, the exercise assignment activates automatically
- [ ] Results summary shows counts of assigned, invited, and errored rows
- [ ] Downloadable template file available in both CSV and XLSX formats

## Notes

- Bulk invitations were explicitly listed as "Out of Scope" in stories OM-07, EM-02-S01, and S14, confirming this is recognized future work
- The existing `BulkUpdateParticipantsAsync` method in `ExerciseParticipantService` operates on user IDs only; this feature extends it to work with email addresses
- The existing `ExcelImportService` (for injects) provides reusable patterns for file parsing, column detection, and session-based import tracking
- Organization membership is required before exercise assignment to preserve the security architecture (query filters, JWT claims, `OrganizationValidationInterceptor`)
- Exercise Directors who are only OrgUsers cannot create org invitations; the system should handle this permission boundary gracefully

## Implementation Progress (2026-02-09)

### Completed Components
- **BulkImportDialog.tsx** - Main dialog with wizard flow (upload → preview → results)
- **ImportUploadStep.tsx** - Drag-and-drop file upload with validation
- **PendingInvitationsList.tsx** - Display pending invitations with resend functionality
- **usePendingAssignments.ts** - Hook for fetching pending exercise assignments
- **bulkImportService.ts** - Service methods for template URLs and import operations

### Frontend Features Implemented
1. **Drag-and-Drop Upload** (S01)
   - Visual feedback during drag operations
   - File format validation (.csv, .xlsx)
   - File size validation (10 MB max)
   - Template download links in upload dialog

2. **Pending Invitations Display** (S05)
   - Collapsible panel on participants page
   - Status indicators (Pending, Accepted, Expired)
   - Relative time display for expiration
   - Resend invitation functionality
   - Integration with bulk import completion

3. **Template Download Links** (S06)
   - CSV and XLSX format links
   - Integrated into upload dialog
   - Service method for URL generation

### Backend Work Required
1. **File Parsing and Validation** (S01, S02)
   - Endpoint: `POST /api/exercises/{id}/participants/bulk-import/upload`
   - Parse CSV/XLSX files
   - Column detection and synonym mapping
   - Row-level validation
   - Return preview data with classifications

2. **Import Processing** (S03, S04)
   - Endpoint: `POST /api/exercises/{id}/participants/bulk-import/confirm`
   - Process "Assign" rows (existing org members)
   - Process "Update" rows (existing participants)
   - Create organization invitations for "Invite" rows
   - Create pending exercise assignments linked to invitations
   - Return results summary

3. **Pending Assignments** (S05)
   - Endpoint: `GET /api/exercises/{id}/participants/pending-assignments`
   - Return pending exercise assignments with invitation status
   - Endpoint: `POST /api/invitations/{id}/resend`
   - Resend invitation email

4. **Template Generation** (S06)
   - Endpoint: `GET /api/exercises/{id}/participants/bulk-import/template?format={csv|xlsx}`
   - Generate CSV template as text file
   - Generate XLSX template with EPPlus (column validation dropdowns)

### Current Branch
- Branch: `feature/bulk-participants`
- Recent commits:
  - 22b5531: Add pending invitations section
  - 6eb6d16: Add drag-and-drop file upload
  - 0227521: Add implementation prompt
