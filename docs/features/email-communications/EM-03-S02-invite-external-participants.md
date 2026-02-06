# Story: EM-03-S02 - Invite External Participants

**As an** Exercise Director,  
**I want** to invite people outside my organization to participate in an exercise,  
**So that** partner agencies and external evaluators can join without full org membership.

## Context

Multi-agency exercises often include participants from external organizations who need exercise access without joining the host organization. This combines organization invitation with exercise invitation in a streamlined flow.

## Acceptance Criteria

### External Invitation

- [ ] **Given** I'm inviting to exercise, **when** email not in organization, **then** I see "Invite External Participant" option
- [ ] **Given** external invite form, **when** filling out, **then** I provide email, name, organization name, exercise role
- [ ] **Given** external invite submitted, **when** successful, **then** creates org invite + exercise participation simultaneously
- [ ] **Given** external recipient, **when** accepting, **then** they join org with "External" flag and exercise

### Email Content

- [ ] **Given** external invitation email, **when** received, **then** explains they're invited to specific exercise
- [ ] **Given** email, **when** received, **then** mentions host organization name
- [ ] **Given** email, **when** received, **then** includes exercise details (same as internal invite)
- [ ] **Given** email, **when** received, **then** explains account creation is required

### External User Handling

- [ ] **Given** external user created, **when** viewing members, **then** they appear with "External" badge
- [ ] **Given** external user, **when** exercise completes, **then** they retain access to exercise data for AAR
- [ ] **Given** external user, **when** no active exercises, **then** they can still access past exercise data

## Out of Scope

- Automatic external user cleanup/archival
- External user access to other exercises (must be explicitly invited)
- Partner organization management

## Dependencies

- EM-02-S01: Send Organization Invitation
- EM-03-S01: Invite Existing Members

## Technical Notes

### Combined Invitation Flow

```csharp
public class ExternalExerciseInvitation
{
    public string Email { get; set; }
    public string Name { get; set; }
    public string ExternalOrganization { get; set; }  // Their home org
    public Guid ExerciseId { get; set; }
    public ExerciseRole Role { get; set; }
    
    // Creates both:
    // 1. OrganizationInvitation with IsExternal = true
    // 2. ExerciseParticipant with status = Invited
}
```

## Domain Terms

| Term | Definition |
|------|------------|
| External Participant | User from outside the host organization invited to specific exercise |
| Partner Agency | External organization participating in joint exercise |

## Effort Estimate

**5 story points** - Combined invitation flow, external user handling

---

*Feature: EM-03 Exercise Invitations*  
*Priority: P0*
