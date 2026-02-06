# Story: EM-03-S01 - Invite Existing Members to Exercise

**As an** Exercise Director,  
**I want** to invite organization members to participate in an exercise,  
**So that** they receive notification with exercise details and their assigned role.

## Context

Exercise invitations differ from organization invitations—the recipient is already a Cadence user. This email notifies them of their participation, provides exercise details (dates, location, scenario), and explains their role responsibilities.

## Acceptance Criteria

### Sending Invitations

- [ ] **Given** I'm an Exercise Director, **when** I view exercise participants, **then** I can "Invite Members"
- [ ] **Given** invite dialog, **when** opened, **then** I see organization members not yet in exercise
- [ ] **Given** member selected, **when** inviting, **then** I can assign their exercise role
- [ ] **Given** multiple members selected, **when** inviting, **then** all receive individual emails
- [ ] **Given** invitation sent, **when** successful, **then** member is added to exercise with "Invited" status

### Email Content

- [ ] **Given** exercise invitation email, **when** received, **then** shows exercise name and type
- [ ] **Given** email, **when** received, **then** shows scheduled date/time and location
- [ ] **Given** email, **when** received, **then** shows assigned role and brief description
- [ ] **Given** email, **when** received, **then** includes "View Exercise" button linking to exercise
- [ ] **Given** email, **when** received, **then** shows Exercise Director as contact

### Participation Tracking

- [ ] **Given** member invited, **when** they sign in and view exercise, **then** status updates to "Confirmed"
- [ ] **Given** member receives email, **when** email opened, **then** track (if possible via ACS)

## Out of Scope

- RSVP/decline functionality (future enhancement)
- Calendar invite attachment (.ics file)
- Bulk import from spreadsheet

## Dependencies

- EM-01-S01: ACS Email Configuration
- EM-01-S02: Email Template System
- Exercise participant management

## Technical Notes

### Email Template Model

```csharp
public class ExerciseInviteEmailModel
{
    public string RecipientName { get; set; }
    public string ExerciseName { get; set; }
    public string ExerciseType { get; set; }          // "Full-Scale Exercise"
    public string RoleName { get; set; }              // "Controller"
    public string RoleDescription { get; set; }       // Brief role explanation
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Location { get; set; }
    public string Scenario { get; set; }              // Brief scenario summary
    public string DirectorName { get; set; }
    public string DirectorEmail { get; set; }
    public string ExerciseUrl { get; set; }
    public string OrganizationName { get; set; }
    public string LogoUrl { get; set; }
}
```

## UI/UX Notes

### Email Preview

```
Subject: You're invited to participate in Operation Thunderstorm

---

[Organization Logo]

Exercise Invitation

Hi Jane,

You've been invited to participate in Operation Thunderstorm 
as a Controller.

EXERCISE DETAILS
━━━━━━━━━━━━━━━━
Type: Full-Scale Exercise
Date: March 15, 2026 | 8:00 AM - 4:00 PM
Location: County Emergency Operations Center

YOUR ROLE: CONTROLLER
Controllers deliver injects to players and track exercise 
progress. You'll be assigned specific injects to deliver 
at scheduled times.

        [View Exercise Details]

Questions? Contact Exercise Director John Smith 
at john.smith@example.com

---

Cadence - Exercise Management Platform
```

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Invitation | Notification to existing user about exercise participation |
| Exercise Role | Function the participant will perform (Controller, Evaluator, etc.) |

## Effort Estimate

**5 story points** - Member selection, role assignment, email template, tracking

---

*Feature: EM-03 Exercise Invitations*  
*Priority: P0*
