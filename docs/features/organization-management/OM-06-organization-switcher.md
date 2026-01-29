# Story OM-06: Organization Switcher

**Priority:** P0 (MVP)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As a** user who belongs to multiple organizations,  
**I want** to switch between my organizations,  
**So that** I can work in different organizational contexts without logging out.

---

## Context

Users may belong to multiple organizations (e.g., a consultant working with CISA and State EMA, or a FEMA employee supporting multiple regions). The organization switcher allows them to change their current working context.

**Key behaviors:**
- All data displayed is scoped to the current organization
- Switching organizations refreshes the JWT with new `org_id` claim
- User's last-used organization is remembered across sessions
- Single-org users don't need to see the switcher prominently

---

## Acceptance Criteria

### Organization Indicator

- [ ] **Given** I am logged in, **when** I view the header/navigation, **then** I see my current organization name displayed
- [ ] **Given** I belong to one organization, **when** viewing the org indicator, **then** it shows the org name without a dropdown arrow
- [ ] **Given** I belong to multiple organizations, **when** viewing the org indicator, **then** it shows a dropdown arrow indicating I can switch

### Organization Switching

- [ ] **Given** I belong to multiple organizations, **when** I click the org indicator, **then** I see a dropdown with all my organizations
- [ ] **Given** I see the org dropdown, **when** viewing the list, **then** my current organization is visually highlighted
- [ ] **Given** I see the org dropdown, **when** I click a different organization, **then** I switch to that organization
- [ ] **Given** I switch organizations, **when** the switch completes, **then** the page refreshes to show data from the new organization
- [ ] **Given** I switch organizations, **when** the switch completes, **then** my JWT is refreshed with the new org_id claim

### Organization Context Persistence

- [ ] **Given** I switch to an organization, **when** I log out and log back in, **then** I am returned to my last-used organization
- [ ] **Given** I switch to an organization, **when** I open a new browser tab, **then** the new tab shows the same organization
- [ ] **Given** my last-used organization becomes Inactive, **when** I log in, **then** I am placed in my next available organization

### Pending User Experience

- [ ] **Given** I am a pending user (no organization assigned), **when** I log in, **then** I see a "Pending Assignment" page instead of the dashboard
- [ ] **Given** I am on the pending page, **when** viewing it, **then** I see a message explaining I'm waiting for organization assignment
- [ ] **Given** I am on the pending page, **when** viewing it, **then** I see an option to enter an organization code
- [ ] **Given** I enter a valid org code on the pending page, **when** I submit, **then** I am added to that organization and redirected to the dashboard

### Navigation After Switch

- [ ] **Given** I am on the Exercise List page, **when** I switch organizations, **then** I see the Exercise List for the new organization
- [ ] **Given** I am on an Exercise Detail page, **when** I switch organizations, **then** I am redirected to the Exercise List (the exercise doesn't exist in new org)
- [ ] **Given** I am on a settings page, **when** I switch organizations, **then** I stay on the settings page (org-agnostic)

### Role Display

- [ ] **Given** I see the org dropdown, **when** viewing each org entry, **then** I see my role in that organization (Admin, Manager, User)
- [ ] **Given** I have different roles in different orgs, **when** I switch, **then** my permissions update to match the new org's role

### Loading State

- [ ] **Given** I click to switch organizations, **when** the switch is in progress, **then** I see a loading indicator
- [ ] **Given** the switch is in progress, **when** waiting, **then** I cannot interact with the page (prevent stale data actions)

---

## Out of Scope

- Viewing data from multiple organizations simultaneously
- Cross-organization search
- Organization favorites/pinning
- Recent organizations list
- Keyboard shortcuts for switching

---

## Dependencies

- OM-02: Create Organization (organizations must exist)
- OM-05: User-Organization Assignment (user must have memberships)
- JWT authentication with org_id claim
- Organization-scoped API endpoints

---

## Domain Terms

| Term | Definition |
|------|------------|
| Current Organization | The organization context for all data operations |
| Organization Context | The scoped view of data based on selected organization |
| Organization Membership | User's association with an organization and their role |

---

## UI/UX Notes

### Organization Indicator (Header)

**Single Organization User:**
```
┌─────────────────────────────────────────────────────────────────┐
│ 🏢 CISA Region 4                           [🔔] [👤 John Smith] │
└─────────────────────────────────────────────────────────────────┘
No dropdown, just displays the org name
```

**Multi-Organization User:**
```
┌─────────────────────────────────────────────────────────────────┐
│ [🏢 CISA Region 4 ▼]                       [🔔] [👤 John Smith] │
└─────────────────────────────────────────────────────────────────┘
Clickable dropdown
```

### Organization Dropdown
```
┌───────────────────────────────────┐
│ 🏢 CISA Region 4 ▼               │
├───────────────────────────────────┤
│ ✓ CISA Region 4                  │
│   Admin                          │
├───────────────────────────────────┤
│   State Emergency Management     │
│   Manager                        │
├───────────────────────────────────┤
│   FEMA Region 3                  │
│   User                           │
└───────────────────────────────────┘
```

### Organization Dropdown (with icons)
```
┌───────────────────────────────────┐
│ Your Organizations               │
├───────────────────────────────────┤
│ ✓ 🏛️ CISA Region 4              │
│     Administrator                │
├───────────────────────────────────┤
│   🏛️ State Emergency Mgmt       │
│     Manager                      │
├───────────────────────────────────┤
│   🏢 FEMA Region 3              │
│     User                         │
├───────────────────────────────────┤
│ ──────────────────────────────── │
│ ⚙️ Organization Settings         │ (if OrgAdmin)
└───────────────────────────────────┘
```

### Pending User Page
```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│     ┌─────────────────────────────────────────────────┐         │
│     │              ⏳                                  │         │
│     │     Waiting for Organization Assignment         │         │
│     │                                                 │         │
│     │  Your account has been created, but you        │         │
│     │  haven't been assigned to an organization yet. │         │
│     │                                                 │         │
│     │  Have an organization code?                    │         │
│     │  ┌─────────────────────────────────────────┐  │         │
│     │  │ Enter code...                           │  │         │
│     │  └─────────────────────────────────────────┘  │         │
│     │                              [Join Organization]│         │
│     │                                                 │         │
│     │  Or contact your administrator for access.    │         │
│     │                                                 │         │
│     └─────────────────────────────────────────────────┘         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Mobile Layout

On mobile, the org switcher moves into the navigation drawer:
```
┌───────────────────────────┐
│ ☰  Cadence         [👤]  │
├───────────────────────────┤
│                           │
│ Current Organization      │
│ ┌───────────────────────┐ │
│ │ 🏢 CISA Region 4  ▼  │ │
│ └───────────────────────┘ │
│                           │
│ ─────────────────────────│
│ 📋 Exercises             │
│ ⏱️ Active Exercise       │
│ ⚙️ Settings              │
│ ─────────────────────────│
│ 🚪 Sign Out              │
└───────────────────────────┘
```

### Switching Animation

1. User clicks new organization
2. Dropdown closes
3. Full-screen overlay with spinner: "Switching to [Org Name]..."
4. Page reloads with new context
5. Toast: "Now working in [Org Name]"

---

## Technical Notes

### API Endpoints

**Switch Organization:**
```
POST /api/users/current-organization
Authorization: Bearer {token}

Request:
{
  "organizationId": "guid"
}

Response (200 OK):
{
  "organizationId": "guid",
  "organizationName": "State EMA",
  "role": "OrgManager",
  "newToken": "eyJ..."  // New JWT with updated org_id claim
}
```

**Get User's Organizations:**
```
GET /api/users/me/organizations
Authorization: Bearer {token}

Response:
{
  "currentOrganizationId": "guid",
  "memberships": [
    {
      "organizationId": "guid",
      "organizationName": "CISA Region 4",
      "organizationSlug": "cisa-r4",
      "role": "OrgAdmin",
      "isCurrent": true
    },
    {
      "organizationId": "guid",
      "organizationName": "State EMA",
      "organizationSlug": "state-ema",
      "role": "OrgManager",
      "isCurrent": false
    }
  ]
}
```

### JWT Claims

```json
{
  "sub": "user-guid",
  "email": "john@example.com",
  "org_id": "org-guid",
  "org_role": "OrgAdmin",
  "system_role": "None",
  "exp": 1706540000
}
```

### Client-Side Context

```typescript
// OrganizationContext.tsx
interface OrganizationContextValue {
  currentOrg: Organization | null;
  memberships: OrganizationMembership[];
  isLoading: boolean;
  isPending: boolean;  // No org assigned
  switchOrganization: (orgId: string) => Promise<void>;
}

const OrganizationContext = createContext<OrganizationContextValue>(null);

export function useOrganization() {
  const context = useContext(OrganizationContext);
  if (!context) throw new Error('useOrganization must be used within OrganizationProvider');
  return context;
}
```

### Route Protection

```typescript
// ProtectedRoute.tsx
function ProtectedRoute({ children }) {
  const { isPending } = useOrganization();
  
  if (isPending) {
    return <Navigate to="/pending" />;
  }
  
  return children;
}
```

### Switch Flow Sequence

```
1. User clicks new org in dropdown
2. Client calls POST /api/users/current-organization
3. Server validates user has membership in target org
4. Server updates User.CurrentOrganizationId
5. Server generates new JWT with updated org_id claim
6. Server returns new token
7. Client stores new token
8. Client clears cached data (React Query cache, etc.)
9. Client redirects to safe route (e.g., /exercises)
10. New API calls use new org context
```

### Handling Organization Removal

If a user's membership is removed while they're active:
1. Next API call returns 403 with `OrganizationAccessRevoked` error
2. Client catches this error globally
3. Client calls GET /api/users/me/organizations to refresh list
4. If user has other orgs, switch to first available
5. If user has no orgs, redirect to /pending

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Single-org user sees org name (no dropdown) | Component | P0 |
| Multi-org user sees dropdown | Component | P0 |
| Switch organization updates JWT | Integration | P0 |
| Switch organization refreshes data | E2E | P0 |
| Last-used org persisted across sessions | Integration | P0 |
| Pending user sees pending page | Component | P0 |
| Pending user can enter org code | Integration | P0 |
| Cannot switch to org without membership | Integration | P0 |
| Switching shows loading state | Component | P1 |
| Page redirects appropriately after switch | E2E | P1 |

---

## Implementation Checklist

### Backend
- [ ] Create `POST /api/users/current-organization` endpoint
- [ ] Create `GET /api/users/me/organizations` endpoint
- [ ] Add org_id and org_role claims to JWT generation
- [ ] Add CurrentOrganizationId field to User entity
- [ ] Validate user membership before allowing switch
- [ ] Return new JWT on successful switch
- [ ] Handle invalid last-used org on login
- [ ] Unit tests for switch validation
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `OrganizationContext` provider
- [ ] Create `OrganizationSwitcher` component
- [ ] Create `OrganizationDropdown` component
- [ ] Create `PendingUserPage` component
- [ ] Add org indicator to header/navigation
- [ ] Implement switch flow with loading state
- [ ] Clear React Query cache on switch
- [ ] Add protected route wrapper for org-required pages
- [ ] Handle org removal while active (403 handler)
- [ ] Add mobile drawer layout
- [ ] Component tests

### Auth Updates
- [ ] Update JWT generation to include org claims
- [ ] Update JWT refresh to preserve org context
- [ ] Update auth context to expose org info
- [ ] Add global 403 handler for org access revocation

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
