# Organization Invitations Frontend Implementation

## Overview

This document summarizes the frontend implementation for organization invitation features (EM-02-S01 and EM-02-S02).

## Stories Implemented

- **EM-02-S01**: Send invitation - OrgAdmins can invite users to join the organization via email
- **EM-02-S02**: Resend invitation - OrgAdmins can resend pending invitations

## Files Created

### 1. Types (`src/frontend/src/features/organizations/types/index.ts`)

Added the following TypeScript interfaces:

```typescript
export type InvitationStatus = 'Pending' | 'Used' | 'Expired' | 'Cancelled'

export interface Invitation {
  id: string;
  email: string;
  code?: string;
  role: OrgRole;
  status: InvitationStatus;
  createdAt: string;
  expiresAt: string;
  usedAt?: string;
  createdByUserName: string;
}

export interface CreateInvitationRequest {
  email: string;
  role?: OrgRole;
}

export interface InvitationSentResponse {
  invitationId: string;
  email: string;
  message: string;
}
```

### 2. API Service (`src/frontend/src/features/organizations/services/organizationService.ts`)

Added four invitation API methods:

- `createInvitation(request)` - POST /api/organizations/current/invitations
- `getInvitations(status?)` - GET /api/organizations/current/invitations
- `resendInvitation(invitationId)` - POST /api/organizations/current/invitations/{id}/resend
- `cancelInvitation(invitationId)` - DELETE /api/organizations/current/invitations/{id}

### 3. Custom Hooks (`src/frontend/src/features/organizations/hooks/useInvitations.ts`)

Created React Query hooks for invitation management:

- `useInvitations(status?)` - Query hook to fetch invitations with optional status filter
- `useCreateInvitation()` - Mutation hook to create new invitation
- `useResendInvitation()` - Mutation hook to resend an invitation email
- `useCancelInvitation()` - Mutation hook to cancel a pending invitation

All hooks properly invalidate queries on success to keep the UI in sync.

### 4. InviteMemberDialog Component (`src/frontend/src/features/organizations/components/InviteMemberDialog.tsx`)

A dialog component that allows OrgAdmins to:
- Enter an email address
- Select an initial organization role (OrgAdmin, OrgManager, OrgUser)
- Send the invitation
- See confirmation that invitation was sent
- Handle validation and error states

Key features:
- Uses COBRA styled components (CobraPrimaryButton, CobraSecondaryButton, CobraTextField)
- FontAwesome icons (faEnvelope)
- Form validation for email
- Info alert explaining the invitation process
- Loading states during submission

### 5. InvitationsTable Component (`src/frontend/src/features/organizations/components/InvitationsTable.tsx`)

A table component that displays pending invitations with:

**Columns:**
- Email
- Role
- Status (with color-coded chips)
- Created date/time
- Expires date/time
- Created by (username)
- Actions

**Actions:**
- Resend button (envelope icon) - for pending invitations
- Cancel button (X icon) - for pending invitations
- Actions are disabled when loading

**Empty State:**
- Shows a friendly message when no invitations exist
- Uses FontAwesome envelope icon

**Date Formatting:**
- Uses date-fns to format timestamps as "MMM d, yyyy h:mm a"

### 6. Updated OrganizationMembersPage (`src/frontend/src/features/organizations/pages/OrganizationMembersPage.tsx`)

Integrated invitation features into the existing members page:

**New UI Sections:**
- "Pending Invitations" section below the members table
- "Send Invitation" button with envelope icon
- InvitationsTable showing all pending invitations

**New Dialogs:**
- InviteMemberDialog for sending email invitations (NEW)
- AddMemberDialog for adding existing users (EXISTING)

**Event Handlers:**
- `handleInviteMember()` - Creates invitation and shows success toast
- `handleResendInvitation()` - Resends invitation email
- `handleCancelInvitation()` - Cancels pending invitation

**State Management:**
- Uses React Query hooks for data fetching and mutations
- Proper loading states during operations
- Toast notifications for all actions (success/error)
- Automatic query invalidation to refresh data

## Component Export Updates

Updated `src/frontend/src/features/organizations/components/index.ts` to export:
- InviteMemberDialog
- InvitationsTable

## UI/UX Features

### COBRA Styling Compliance
- All buttons use COBRA components (CobraPrimaryButton, CobraSecondaryButton)
- All text inputs use CobraTextField
- No raw MUI components for styled elements

### FontAwesome Icons
- All icons use FontAwesome (NO MUI icons)
- faEnvelope - Invitation/email actions
- faXmark - Cancel/close actions
- faUsers - Members section
- faBuilding - Organization context

### Accessibility
- All interactive elements have aria-labels
- Icon buttons include descriptive tooltips
- Form fields have proper labels and helper text

### User Feedback
- Toast notifications for all operations (success/error)
- Loading states on buttons during async operations
- Error messages displayed in dialogs
- Empty states with helpful messaging

### Responsive Design
- Tables are responsive with proper overflow handling
- Dialogs use maxWidth="sm" fullWidth for consistent sizing
- Proper spacing using COBRA spacing system

## Integration with Backend

All API calls use the existing `apiClient` from `@/core/services/api` which:
- Automatically includes JWT token with org_id claim
- Handles organization context via CurrentOrganizationContext
- Returns properly typed responses

## Type Safety

- All components are fully typed with TypeScript
- Interface definitions match backend DTOs exactly
- No type errors (verified with `npm run type-check`)
- Proper type guards for error handling

## Testing Readiness

The implementation is ready for testing with:
- Clear component boundaries for unit testing
- Proper error handling that can be mocked
- React Query hooks that support test utilities
- Accessibility features for integration testing

## Next Steps

1. **Backend Testing**: Verify API endpoints work as expected
2. **Frontend Testing**: Write component tests using Vitest + React Testing Library
3. **Integration Testing**: Test the full invitation flow end-to-end
4. **Email Template Testing**: Verify invitation emails are sent correctly
5. **User Acceptance Testing**: Have OrgAdmins test the invitation workflow

## Files Modified/Created Summary

**Created:**
- `src/frontend/src/features/organizations/hooks/useInvitations.ts`
- `src/frontend/src/features/organizations/components/InviteMemberDialog.tsx`
- `src/frontend/src/features/organizations/components/InvitationsTable.tsx`

**Modified:**
- `src/frontend/src/features/organizations/types/index.ts` - Added invitation types
- `src/frontend/src/features/organizations/services/organizationService.ts` - Added API methods
- `src/frontend/src/features/organizations/components/index.ts` - Added exports
- `src/frontend/src/features/organizations/pages/OrganizationMembersPage.tsx` - Integrated invitation UI

## Conventions Followed

✅ COBRA styled components used throughout
✅ FontAwesome icons only (no MUI icons)
✅ TypeScript strict mode compliance
✅ React Query for server state management
✅ Proper error handling with try/catch
✅ Toast notifications for user feedback
✅ Loading states for async operations
✅ Accessibility best practices
✅ Co-located with existing organization features
✅ Consistent naming conventions

## Known Limitations

1. No offline support yet (planned for future)
2. No pagination for invitations list (acceptable for MVP)
3. No bulk operations (send multiple invitations at once)
4. No invitation preview before sending

These limitations can be addressed in future iterations based on user feedback.
