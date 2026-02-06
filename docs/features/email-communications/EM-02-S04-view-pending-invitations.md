# Story: EM-02-S04 - View Pending Invitations

**As an** OrgAdmin,  
**I want** to see all pending invitations for my organization,  
**So that** I can track who has been invited and manage outstanding invitations.

## Context

Organization admins need visibility into invitation status to follow up with recipients, identify issues, and maintain accurate records of who has access to the organization.

## Acceptance Criteria

### List View

- [ ] **Given** I'm an OrgAdmin, **when** I view team management, **then** I see "Pending Invitations" section
- [ ] **Given** pending invitations exist, **when** viewing list, **then** I see email, invited by, sent date, expiration, status
- [ ] **Given** no pending invitations, **when** viewing list, **then** I see "No pending invitations"
- [ ] **Given** invitation list, **when** viewing, **then** expired invitations are visually distinguished

### Filtering & Sorting

- [ ] **Given** invitation list, **when** filtering by status, **then** I can show All, Pending, Expired, Cancelled
- [ ] **Given** invitation list, **when** sorting, **then** I can sort by date sent (newest/oldest)

### Actions

- [ ] **Given** pending invitation, **when** viewing row, **then** I see actions: Resend, Cancel
- [ ] **Given** expired invitation, **when** viewing row, **then** I see actions: Resend, Delete
- [ ] **Given** accepted invitation, **when** viewing, **then** it appears in Members list instead

## Out of Scope

- Invitation history/audit log view
- Export invitation list

## Dependencies

- EM-02-S01: Send Organization Invitation

## UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────┐
│ Team Management                                                 │
├─────────────────────────────────────────────────────────────────┤
│ [Members (12)]  [Pending Invitations (3)]                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Email                │ Invited By    │ Sent      │ Status    │ │
│ ────────────────────┼───────────────┼───────────┼───────────┤ │
│ jane@example.com    │ John Smith    │ 2 days ago│ ⏳ Pending │ │
│                     │               │ Exp: 5 days│  [↻] [✕]  │ │
│ ────────────────────┼───────────────┼───────────┼───────────┤ │
│ bob@old-email.com   │ John Smith    │ 10 days   │ ⚠️ Expired │ │
│                     │               │           │  [↻] [🗑]  │ │
└─────────────────────────────────────────────────────────────────┘
```

## Domain Terms

| Term | Definition |
|------|------------|
| Pending Invitation | Invitation sent but not yet accepted or expired |
| Expired Invitation | Invitation past its expiration date |

## Effort Estimate

**2 story points** - List view, filtering, status display

---

*Feature: EM-02 Organization Invitations*  
*Priority: P0*
