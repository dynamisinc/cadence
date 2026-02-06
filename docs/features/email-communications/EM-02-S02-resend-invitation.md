# Story: EM-02-S02 - Resend Invitation

**As an** OrgAdmin,  
**I want** to resend a pending invitation,  
**So that** recipients who missed or lost the original email can still join.

## Context

Invitation emails can be lost to spam filters, overlooked, or accidentally deleted. Resending generates a new email with an extended expiration date, giving the recipient another opportunity to join.

## Acceptance Criteria

- [ ] **Given** a pending invitation exists, **when** I click "Resend", **then** a new email is sent
- [ ] **Given** invitation resent, **when** email sent, **then** expiration is reset to 7 days from now
- [ ] **Given** invitation resent, **when** successful, **then** I see "Invitation resent to [email]"
- [ ] **Given** invitation was expired, **when** I resend, **then** status changes back to Pending
- [ ] **Given** invitation already accepted, **when** I try to resend, **then** error "Invitation already accepted"
- [ ] **Given** invitation cancelled, **when** I try to resend, **then** error "Create a new invitation instead"
- [ ] **Given** resend action, **when** logged, **then** audit entry shows "Resent by [admin] at [time]"

## Out of Scope

- Automatic resend reminders
- Resend rate limiting (trust admins)

## Dependencies

- EM-02-S01: Send Organization Invitation

## Domain Terms

| Term | Definition |
|------|------------|
| Resend | Send the invitation email again with refreshed expiration |

## Effort Estimate

**2 story points** - Extends existing invitation flow

---

*Feature: EM-02 Organization Invitations*  
*Priority: P0*
