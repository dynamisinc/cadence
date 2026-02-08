# Story: S04 - Invite Non-Members via Bulk Upload

## User Story

**As an** Exercise Director with OrgManager permissions,
**I want** people who are not yet organization members to automatically receive organization invitations with exercise pre-assignment,
**So that** new participants can join the organization and be assigned to the exercise in a single flow.

## Context

This is the most architecturally significant story in the bulk import feature. When a participant email does not belong to a current organization member, the system must bridge two sequential processes: organization invitation and exercise assignment. The key design decision is that **participants must always be organization members** before receiving exercise assignments, preserving the platform's multi-tenancy security model.

To make this seamless for the Exercise Director, the system creates an organization invitation and records a "pending exercise assignment" that activates automatically when the invitation is accepted. This covers two scenarios:
- **Existing Cadence user, different org**: User has an account but is not a member of this organization
- **No Cadence account**: User needs to register first, then accept the org invitation

## Acceptance Criteria

### Organization Invitation Creation

- [ ] **Given** the import is confirmed, **when** processing a row classified as "Invite", **then** an `OrganizationInvite` is created for the email with the specified Organization Role (default: OrgUser)
- [ ] **Given** an invitation is created, **when** the email already has a pending invitation for this org, **then** the existing invitation is reused (no duplicate invitation created)
- [ ] **Given** an invitation is created, **when** the email already has a pending invitation, **then** the pending exercise assignment is added to the existing invitation
- [ ] **Given** an invitation is created, **when** the system sends the invitation email, **then** the email body mentions both the organization name and the specific exercise name and role

### Pending Exercise Assignment

- [ ] **Given** an invitation with a pending exercise assignment exists, **when** the user accepts the org invitation, **then** the exercise assignment is automatically created with the specified Exercise Role
- [ ] **Given** a pending exercise assignment specifies Exercise Director, **when** the user accepts and their System Role is User (not Admin/Manager), **then** the pending assignment fails gracefully and the user is assigned as Observer instead with a notification
- [ ] **Given** a pending exercise assignment exists, **when** the invitation expires without being accepted, **then** the pending exercise assignment is also marked as expired
- [ ] **Given** a pending exercise assignment exists, **when** the invitation is cancelled, **then** the pending exercise assignment is also cancelled

### Permission Enforcement

- [ ] **Given** I am an Exercise Director with OrgManager or OrgAdmin org role, **when** processing "Invite" rows, **then** invitations are created and sent
- [ ] **Given** I am an Exercise Director with only OrgUser org role, **when** processing "Invite" rows, **then** the rows are flagged as "Requires OrgManager approval" and queued for approval
- [ ] **Given** "Invite" rows are queued for approval, **when** an OrgManager reviews them, **then** they can approve or reject individual invitations

### Error Handling

- [ ] **Given** an invitation email fails to send, **when** processing the row, **then** the invitation is still created but marked with `EmailSent = false` and an error note
- [ ] **Given** the organization has reached a member limit (if applicable), **when** processing "Invite" rows, **then** the rows fail with "Organization member limit reached"
- [ ] **Given** the email domain is in the reserved/blocked list, **when** processing the row, **then** it fails with "Email domain not allowed"

### Tracking

- [ ] **Given** invitations are created via bulk import, **when** viewing the import results (S05), **then** I can see the status of each invitation (Pending, Accepted, Expired)
- [ ] **Given** a bulk import created invitations, **when** viewing the organization's invitation list, **then** bulk-import invitations appear alongside individually-created invitations

## Out of Scope

- Customizing invitation email content per participant
- Scheduling invitation delivery (invitations are sent immediately)
- Bulk invitation approval workflow UI (OrgManager sees individual pending items)
- Cross-organization bulk invitations (only the current org)

## Dependencies

- bulk-participant-import/S03: Process Existing Members (processes "Assign" and "Update" rows first)
- organization-management/OM-07: Organization Invitations (invitation creation and acceptance flow)
- email-communications/EM-02: Invitation Emails (email templates and delivery)

## Open Questions

- [ ] Should pending exercise assignments be stored as a new entity (`PendingExerciseAssignment`) or as JSON metadata on `OrganizationInvite`?
- [ ] If the exercise is completed or archived before the invitation is accepted, should the pending assignment be auto-cancelled?
- [ ] Should there be a limit on how many invitations can be sent in a single bulk import to prevent abuse?
- [ ] What happens if the same email appears in multiple bulk imports for different exercises? Should both pending assignments activate on acceptance?

## Domain Terms

| Term | Definition |
|------|------------|
| **Pending Exercise Assignment** | A deferred exercise role that activates when the participant accepts their organization invitation |
| **Organization Invitation** | An email-based invitation for a user to join an organization, with a unique code and expiration |
| **Invitation Acceptance** | The act of a user redeeming an invitation code, creating their organization membership |
| **Approval Queue** | A list of pending actions requiring OrgManager review (when the importer lacks invitation permissions) |

## UI/UX Notes

### Invitation Email Content

The invitation email for bulk-imported participants should include:

```
Subject: You've been invited to join [Org Name] on Cadence

Hi [Display Name],

You've been invited to join [Org Name] on Cadence for the following exercise:

  Exercise: [Exercise Name]
  Your Role: [Exercise Role]
  Exercise Date: [Exercise Date, if set]

To accept this invitation, click the link below:
[Accept Invitation Link]

This invitation expires on [Expiration Date].

If you don't have a Cadence account, you'll be prompted to create one.
```

## Technical Notes

- The `PendingExerciseAssignment` concept requires either a new entity or extension to `OrganizationInvite`:
  - **Option A (New Entity)**: `PendingExerciseAssignment { InviteId, ExerciseId, ExerciseRole, Status }` - cleaner separation, supports multiple exercises per invite
  - **Option B (Metadata on Invite)**: Add `ExerciseId` and `ExerciseRole` columns to `OrganizationInvite` - simpler but limits to one exercise per invite
  - **Recommendation**: Option A, since a user could be bulk-imported to multiple exercises simultaneously
- Hook into the existing `AcceptInvitationAsync` method to trigger pending exercise assignments
- Email sending should be fire-and-forget (don't block import on email delivery)
- Consider rate limiting invitation emails (e.g., max 50 per minute) to avoid spam filter issues
