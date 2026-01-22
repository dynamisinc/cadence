# User Management Feature

Administrative interface for managing users in Cadence.

## Overview

This feature provides a comprehensive user management interface for Administrators to:
- View all users in the system
- Search and filter users
- Edit user details (display name, email)
- Assign global roles
- Deactivate/reactivate accounts

## User Stories

This feature implements the following stories:

- **S10**: View User List - Paginated list with search and filters
- **S11**: Edit User Details - Update display name and email
- **S12**: Deactivate User Account - Soft disable accounts
- **S13**: Global Role Assignment - Change user roles with protection

## Components

### Pages

#### UserListPage
Main administrative page showing all users.

**Features:**
- Paginated table (20 per page default)
- Search by name or email
- Filter by role dropdown
- Inline role editing
- Edit/Deactivate/Reactivate actions

**Usage:**
```tsx
import { UserListPage } from 'features/users';

// In routes
<Route path="/admin/users" element={
  <ProtectedRoute requiredRole="Administrator">
    <UserListPage />
  </ProtectedRoute>
} />
```

### Components

#### EditUserDialog
Modal dialog for editing user details.

**Props:**
- `user: UserDto` - User to edit
- `onClose: () => void` - Called when dialog closes
- `onSave: (updates: UpdateUserRequest) => Promise<void>` - Called when save clicked

#### RoleSelect
Dropdown selector for user roles.

**Props:**
- `value: string` - Current role
- `onChange: (role: string) => void` - Called when role changes
- `disabled?: boolean` - Whether select is disabled

## Services

### userService

API client for user management operations.

**Methods:**
- `getUsers(params)` - Get paginated user list with filters
- `getUser(id)` - Get single user by ID
- `updateUser(id, request)` - Update user details
- `changeRole(id, request)` - Change user's global role
- `deactivateUser(id, reason?)` - Deactivate user account
- `reactivateUser(id)` - Reactivate deactivated account

## Types

### UserDto
```typescript
{
  id: string;
  email: string;
  displayName: string;
  role: string;
  status: string;
  lastLoginAt: string | null;
  createdAt: string;
}
```

### UserListResponse
```typescript
{
  users: UserDto[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}
```

## API Contract

All endpoints require Administrator role.

### GET /api/users
Get paginated user list with optional filters.

**Query Parameters:**
- `page` (int, default 1)
- `pageSize` (int, default 20, max 100)
- `search` (string, optional)
- `role` (string, optional)

### PUT /api/users/{userId}
Update user details.

**Request:**
```json
{
  "displayName": "New Name",
  "email": "new@example.com"
}
```

### PATCH /api/users/{userId}/role
Change user's global role.

**Request:**
```json
{
  "role": "Controller"
}
```

**Error (400 - last admin):**
```json
{
  "error": "last_administrator",
  "message": "Cannot remove the last Administrator. Assign another Administrator first."
}
```

### POST /api/users/{userId}/deactivate
Deactivate user account.

**Request:**
```json
{
  "reason": "Left organization" // optional
}
```

### POST /api/users/{userId}/reactivate
Reactivate deactivated user account.

## Security

- All endpoints require authenticated Administrator role
- Cannot deactivate the last Administrator
- Cannot deactivate yourself
- Role changes are logged for audit

## Styling

This feature follows COBRA styling guidelines:

- Uses `CobraTextField` for search input
- Uses `CobraPrimaryButton` and `CobraSecondaryButton` for actions
- Uses FontAwesome icons (never MUI icons)
- Uses `CobraStyles` constants for spacing
- Follows Material-UI table patterns

## Testing

All components have co-located test files:

```bash
# Run all user management tests
npm test src/features/users

# Run specific test file
npm test UserListPage.test.tsx
```

## Future Enhancements

Potential improvements not in current scope:

- Bulk user operations (import/export)
- User activity logs
- Email verification workflow
- Password reset by admin
- Custom role creation
- Exercise-specific role assignments (see separate feature)

---

**Last Updated:** 2025-01-22
