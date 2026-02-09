# Story: EM-02-S01 - Send Organization Invitation

**As an** OrgAdmin,  
**I want** to invite new users to join my organization via email,  
**So that** team members can access Cadence and participate in exercises.

## Context

Organization invitations are the primary way users join Cadence. An invitation email contains a secure link that allows the recipient to create an account and join the organization. Invitations expire after 7 days and include the inviter's name to establish trust.

## Acceptance Criteria

### Sending Invitation

- [ ] **Given** I'm an OrgAdmin, **when** I click "Invite Member", **then** I see an invitation form
- [ ] **Given** invitation form, **when** I enter a valid email and submit, **then** an invitation email is sent
- [ ] **Given** valid email entered, **when** submitted, **then** invitation record is created with unique token
- [ ] **Given** invitation sent, **when** successful, **then** I see confirmation "Invitation sent to [email]"

### Invitation Token

- [ ] **Given** invitation created, **when** token generated, **then** token is cryptographically secure (256-bit)
- [ ] **Given** invitation token, **when** created, **then** expiration is set to 7 days from now
- [ ] **Given** token, **when** recipient clicks link, **then** token is validated before showing registration

### Email Content

- [ ] **Given** invitation email, **when** received, **then** it shows inviter's name and organization name
- [ ] **Given** invitation email, **when** received, **then** it contains clear call-to-action button
- [ ] **Given** invitation email, **when** received, **then** it shows expiration date
- [ ] **Given** email, **when** link clicked, **then** recipient is taken to registration page with invitation context

### Duplicate Handling

- [ ] **Given** email already has pending invitation, **when** inviting again, **then** warn "Pending invitation exists" with option to resend
- [ ] **Given** email is already a member, **when** inviting, **then** error "This person is already a member"
- [ ] **Given** email belongs to user in different org, **when** inviting, **then** allow (user can be in multiple orgs)

### Role Assignment

- [ ] **Given** invitation form, **when** filling out, **then** I can optionally specify initial role (defaults to Observer)
- [ ] **Given** role specified, **when** user accepts, **then** they receive that role in the organization

## Out of Scope

- Bulk invitation upload (future enhancement)
- Invitation message customization (uses standard template)
- SMS invitation alternative

## Dependencies

- EM-01-S01: ACS Email Configuration
- EM-01-S02: Email Template System
- User registration flow (must handle invitation tokens)

## Technical Notes

### OrganizationInvitation Entity

```csharp
public class OrganizationInvitation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }               // Hashed for storage
    public string TokenHash { get; set; }           // For lookup
    public OrganizationRole InitialRole { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Cancelled
}
```

### Invitation Service

```csharp
public interface IOrganizationInvitationService
{
    Task<Invitation> CreateInvitationAsync(
        Guid organizationId, 
        string email, 
        Guid invitedByUserId,
        OrganizationRole? initialRole = null);
    
    Task<InvitationValidation> ValidateTokenAsync(string token);
    Task AcceptInvitationAsync(string token, Guid newUserId);
    Task CancelInvitationAsync(Guid invitationId);
    Task ResendInvitationAsync(Guid invitationId);
}
```

### Email Template Model

```csharp
public class OrganizationInviteEmailModel
{
    public string RecipientEmail { get; set; }
    public string OrganizationName { get; set; }
    public string InviterName { get; set; }
    public string InviteUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string LogoUrl { get; set; }
    public string PrimaryColor { get; set; }
}
```

### API Endpoint

```csharp
[HttpPost("organizations/{orgId}/invitations")]
[Authorize(Roles = "OrgAdmin")]
public async Task<ActionResult<InvitationDto>> CreateInvitation(
    Guid orgId,
    [FromBody] CreateInvitationRequest request)
{
    // Validate email format
    // Check for existing invitation/membership
    // Create invitation
    // Send email
    // Return invitation details
}
```

## UI/UX Notes

### Invite Member Modal

```
┌─────────────────────────────────────────────────────────────────┐
│ Invite Team Member                                      [Close] │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Email Address *                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ jane.doe@example.com                                        │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Initial Role                                                    │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Observer                                                  ▼ │ │
│ └─────────────────────────────────────────────────────────────┘ │
│ ℹ️ Role can be changed later by organization admins            │
│                                                                 │
│                                                                 │
│                              [Cancel]  [Send Invitation]        │
└─────────────────────────────────────────────────────────────────┘
```

### Email Preview

```
Subject: You're invited to join [Organization Name] on Cadence

---

[Organization Logo]

You're Invited!

Hi,

John Smith has invited you to join Acme Emergency Management 
on Cadence, the exercise management platform.

        [Accept Invitation]

This invitation expires on February 13, 2026.

---

If you weren't expecting this invitation, you can ignore this email.

Cadence - Exercise Management Platform
```

## Domain Terms

| Term | Definition |
|------|------------|
| Organization Invitation | Email-based request for someone to join an organization |
| Invitation Token | Secure, single-use code that validates the invitation |
| Initial Role | Organization role assigned when invitation is accepted |

## Open Questions

- [ ] Should we allow custom invitation messages?
- [ ] Should invitations be editable after sending (change role)?

## Effort Estimate

**5 story points** - Token generation, email sending, duplicate handling, UI

---

*Feature: EM-02 Organization Invitations*  
*Priority: P0*
