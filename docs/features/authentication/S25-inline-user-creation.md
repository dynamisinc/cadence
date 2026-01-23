# S25: Inline User Creation from Exercise Participants

## Story

**As an** Exercise Director,
**I want** to create new system users directly while adding exercise participants,
**So that** I can build my exercise team without leaving the exercise context.

## Context

When planning exercises, Directors often need to add users who don't exist in the system yet. Currently, this requires:
1. Leaving the exercise context
2. Navigating to user management (if they have Administrator access)
3. Waiting for an Administrator to create the user (if they don't)
4. Returning to the exercise to assign the participant

This workflow disrupts the planning process and creates unnecessary friction. Inline user creation allows Directors to stay focused on exercise setup while building their team.

## Acceptance Criteria

- [ ] **Given** I am an Exercise Director or Administrator, **when** I open the Add Participant dialog, **then** I see a "Create New User" option
- [ ] **Given** I click "Create New User", **when** the modal opens, **then** I see fields for Display Name, Email, and Password
- [ ] **Given** I submit valid user details, **when** the user is created, **then** they are automatically selected in the Add Participant dialog
- [ ] **Given** I create a user inline, **when** they are created, **then** their global role is set to Observer (enforced server-side)
- [ ] **Given** I try to create a user with an existing email, **when** I submit, **then** I see error "A user with this email already exists"
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view the Add Participant dialog, **then** I do NOT see the "Create New User" option
- [ ] **Given** I successfully create a user, **when** the modal closes, **then** I can immediately assign them an exercise role
- [ ] **Given** I create a user inline, **when** the user is created, **then** the CreatedBy field is set to my user ID

## Out of Scope

- Email invitation workflow (see Future Enhancements)
- One-time password (OTP) workflow (see Future Enhancements)
- Custom global role selection (always Observer)
- Bulk user import
- Password strength indicator (use existing validation from S01)

## Future Enhancements

> **Security Note:** The current approach requires Directors to manually share passwords with new users. This is acceptable for MVP but presents security concerns:
> - Passwords may be shared insecurely (email, chat, etc.)
> - No guarantee user changes password after first login
> - No audit trail of password communication
>
> **Recommended future improvements (prioritized):**
> 1. **Email Invitation Flow** - Send email with secure link to set password
> 2. **One-Time Password (OTP)** - Generate temporary password that must be changed on first login
> 3. **Magic Link Authentication** - Passwordless login via email link
>
> These should be addressed before production deployment with external users.

## Dependencies

- S01 (Registration Form) - Reuse validation logic
- S02 (Validate Save User) - Reuse backend creation logic
- S14 (Exercise Role Assignment) - Integration point

## Domain Terms

| Term | Definition |
|------|------------|
| Inline Creation | Creating a user directly from within another workflow (participant assignment) |
| Observer Role | Lowest-privilege global role; safe default for inline-created users |
| CreatedBy | Audit field tracking which user created the account |

## API Contract

**Endpoint:** `POST /api/users` (existing endpoint, enhanced)

**Request:**
```json
{
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response (201 Created):**
```json
{
  "id": "user-guid",
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "role": "Observer",
  "isActive": true,
  "createdAt": "2025-01-23T12:00:00Z",
  "createdBy": "director-guid"
}
```

**Error Response (409 Conflict):**
```json
{
  "error": "DuplicateEmail",
  "message": "A user with this email already exists."
}
```

**Authorization:**
- Administrators: Full access
- Exercise Directors: Can create users (Observer role only)
- Controllers/Evaluators/Observers: 403 Forbidden

## Technical Notes

### Backend Changes

Add `CreatedBy` tracking to User entity:

```csharp
public class User : BaseEntity
{
    // ... existing properties

    /// <summary>
    /// The user who created this account. Null for self-registered users.
    /// </summary>
    public Guid? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
}
```

Enforce Observer role for non-Administrators:

```csharp
public async Task<UserDto> CreateUser(CreateUserRequest request, Guid? createdById)
{
    // If creator is not Administrator, force Observer role
    if (!await IsAdministrator(createdById))
    {
        request.Role = "Observer";
    }

    var user = new User
    {
        // ... other properties
        CreatedById = createdById
    };

    // ... save and return
}
```

### Frontend Components

**New Component:** `CreateUserModal.tsx`
- Display Name field (required)
- Email field (required, validated)
- Password field (required, show/hide toggle)
- Reuse validation from RegisterPage
- On success: call `onUserCreated(user)` callback

**Modified Component:** `AddParticipantDialog.tsx`
- Add "Create New User" button (conditional on role)
- State for showing CreateUserModal
- Handle `onUserCreated` to auto-select new user

### Permission Check

```typescript
const { currentUser } = useAuth();
const canCreateUsers = currentUser?.role === 'Administrator' ||
                       currentUser?.role === 'ExerciseDirector';
```

## UI/UX Notes

### Add Participant Dialog Enhancement

```
+--------------------------------------------------+
|  Add Participant                            [X]  |
+--------------------------------------------------+
|                                                  |
|  Select User                                     |
|  +--------------------------------------------+  |
|  | Search users...                          v |  |
|  +--------------------------------------------+  |
|                                                  |
|  Can't find the person you're looking for?       |
|  [+ Create New User]                             |
|                                                  |
|  Exercise Role                                   |
|  +--------------------------------------------+  |
|  | Select role...                           v |  |
|  +--------------------------------------------+  |
|                                                  |
|                      [Cancel]  [Add Participant] |
+--------------------------------------------------+
```

### Create User Modal

```
+--------------------------------------------------+
|  Create New User                            [X]  |
+--------------------------------------------------+
|                                                  |
|  Display Name *                                  |
|  +--------------------------------------------+  |
|  |                                            |  |
|  +--------------------------------------------+  |
|                                                  |
|  Email Address *                                 |
|  +--------------------------------------------+  |
|  |                                            |  |
|  +--------------------------------------------+  |
|                                                  |
|  Password *                                      |
|  +--------------------------------------------+  |
|  |                                         [] |  |
|  +--------------------------------------------+  |
|  Min 8 characters                                |
|                                                  |
|  This user will be created with Observer role.   |
|  You can assign their exercise role after        |
|  creation.                                       |
|                                                  |
|                       [Cancel]  [Create User]    |
+--------------------------------------------------+
```

### Success Confirmation

After user creation, show brief confirmation:

```
+--------------------------------------------------+
|  User Created Successfully                       |
+--------------------------------------------------+
|                                                  |
|  Jane Smith has been created.                    |
|                                                  |
|  Please share their login credentials securely:  |
|  Email: jane@example.com                         |
|  Password: ••••••••••••  [Show] [Copy]           |
|                                                  |
|  [Done]                                          |
+--------------------------------------------------+
```

## Testing Requirements

### Frontend Tests (CreateUserModal.test.tsx)

- Renders all required fields
- Validates email format
- Validates password requirements
- Shows/hides password on toggle
- Disables submit when form invalid
- Calls onUserCreated with new user data
- Shows error message for duplicate email
- Shows loading state during submission

### Frontend Tests (AddParticipantDialog.test.tsx additions)

- Shows "Create New User" button for Administrators
- Shows "Create New User" button for Exercise Directors
- Hides "Create New User" button for Controllers
- Hides "Create New User" button for Evaluators
- Hides "Create New User" button for Observers
- Auto-selects newly created user in dropdown
- Refreshes user list after creation

### Backend Tests (UserServiceTests.cs additions)

- CreateUser_AsDirector_AssignsObserverRole
- CreateUser_AsAdmin_AllowsAnyRole
- CreateUser_DuplicateEmail_Returns409
- CreateUser_SetsCreatedByField
- CreateUser_AsController_Returns403

---

*Story created: 2025-01-23*
