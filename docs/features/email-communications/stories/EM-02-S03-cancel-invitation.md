# Story: EM-02-S03 - Cancel Invitation

**As an** OrgAdmin,  
**I want** to cancel a pending invitation,  
**So that** I can revoke access if sent to the wrong person or circumstances change.

## Context

Invitations may need to be cancelled if sent in error, the person is no longer joining, or security concerns arise. Cancelled invitations cannot be accepted even if the link is accessed.

## Acceptance Criteria

- [ ] **Given** a pending invitation, **when** I click "Cancel", **then** I see confirmation dialog
- [ ] **Given** confirmation dialog, **when** I confirm cancellation, **then** invitation status becomes "Cancelled"
- [ ] **Given** cancelled invitation, **when** recipient clicks link, **then** they see "This invitation has been cancelled"
- [ ] **Given** cancelled invitation, **when** viewing invitation list, **then** it shows with "Cancelled" status
- [ ] **Given** cancellation, **when** logged, **then** audit entry includes cancellation reason if provided
- [ ] **Given** invitation already accepted, **when** trying to cancel, **then** error "Cannot cancel accepted invitation"

## Out of Scope

- Notify recipient of cancellation (they just see invalid link)
- Bulk cancellation

## Dependencies

- EM-02-S01: Send Organization Invitation

## Domain Terms

| Term | Definition |
|------|------------|
| Cancel | Permanently invalidate an invitation so it cannot be accepted |

## Effort Estimate

**2 story points** - Status update, validation, confirmation UI

---

*Feature: EM-02 Organization Invitations*  
*Priority: P0*
