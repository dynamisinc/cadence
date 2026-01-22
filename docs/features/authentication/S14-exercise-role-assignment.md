# S14: Exercise-Specific Role Assignment

## Story

**As an** Exercise Director,
**I want** to assign roles to participants for my exercise,
**So that** I can build the right team with appropriate permissions.

## Context

Different exercises need different team compositions. A user who is normally a Controller might observe a training exercise, or an Evaluator might take a Controller role during a smaller exercise. Exercise-specific roles enable this flexibility.

## Acceptance Criteria

- [ ] **Given** I am an Exercise Director for an exercise, **when** I view exercise settings, **then** I see a "Participants" section
- [ ] **Given** I am in the Participants section, **when** I click "Add Participant", **then** I can search for users
- [ ] **Given** I am adding a participant, **when** I select a user, **then** I can choose their role for this exercise
- [ ] **Given** I add a participant with role "Controller", **when** they access this exercise, **then** they have Controller permissions
- [ ] **Given** a user has global role "Observer" and exercise role "Controller", **when** they access this exercise, **then** Controller permissions apply
- [ ] **Given** I am a participant, **when** I view my exercise role, **then** I see my effective role clearly
- [ ] **Given** I am an Administrator, **when** I view any exercise, **then** I can manage its participants
- [ ] **Given** I am not a Director or Admin for an exercise, **when** I view it, **then** I cannot manage participants

## Out of Scope

- Self-service exercise joining
- Invitation workflow (email invites)
- Role templates per exercise type
- Bulk participant import

## Dependencies

- S13 (Global Role Assignment)
- Exercise CRUD (Phase B) ✅

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Participant | A user assigned to a specific exercise with a specific role |
| Effective Role | The role that applies after considering exercise override vs global |
| Exercise Director | User with Director role for a specific exercise (can manage that exercise) |

## API Contract

**Endpoint:** `POST /api/exercises/{exerciseId}/participants`

**Request:**
```json
{
  "userId": "user-guid",
  "role": "Evaluator"
}
```

**Success Response (201 Created):**
```json
{
  "participantId": "guid",
  "exerciseId": "exercise-guid",
  "userId": "user-guid",
  "displayName": "Jane Smith",
  "role": "Evaluator",
  "assignedAt": "2025-01-21T12:00:00Z",
  "assignedBy": "admin-guid"
}
```

**Endpoint:** `GET /api/exercises/{exerciseId}/participants`

**Response:**
```json
{
  "participants": [
    {
      "participantId": "guid",
      "userId": "user-guid",
      "displayName": "Jane Smith",
      "email": "jane@example.com",
      "exerciseRole": "Evaluator",
      "globalRole": "Observer",
      "effectiveRole": "Evaluator"
    }
  ]
}
```

## Technical Notes

- ExerciseParticipant entity links User to Exercise with Role
- Unique constraint on (ExerciseId, UserId)
- Consider soft-delete for participant removal (audit trail)

```csharp
public class ExerciseParticipant
{
    public Guid Id { get; set; }
    public Guid ExerciseId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid AssignedById { get; set; }
    
    public Exercise Exercise { get; set; }
    public User User { get; set; }
    public User AssignedBy { get; set; }
}
```

## UI/UX Notes

- User search with autocomplete
- Show user's global role when selecting (for context)
- Participant list shows both exercise and global role
- Quick role change via inline dropdown
- Remove participant with confirmation

---

*Story created: 2025-01-21*
