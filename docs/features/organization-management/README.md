# Feature: Organization Management

**Parent Epic:** Multi-Tenancy & Access Control

## Overview

Organizations are the primary security boundary in Cadence. All exercise data, users, and configurations are scoped to an organization. Users can belong to multiple organizations with different roles in each, but they work within one organization context at a time.

## Business Value

- **Data Isolation:** Ensures CISA data never leaks to commercial clients
- **Scalability:** Supports multiple independent customers on shared infrastructure
- **Flexibility:** Consultants and contractors can work across organizations
- **Compliance:** Clear audit trails of who did what in which organization

## User Personas

| Persona | Description | Key Needs |
|---------|-------------|-----------|
| SysAdmin | Anthropic/platform operator | Manage all orgs, assign users, system health |
| OrgAdmin | Organization administrator | Manage their org's users, settings, exercises |
| OrgManager | Exercise coordinator | Create exercises, manage participants |
| OrgUser | Exercise participant | Access assigned exercises within their org |
| Pending User | Newly registered | Waiting for org assignment |

## Role Hierarchy

```
System Level
  └── SysAdmin (global access, manages all organizations)

Organization Level (per-org)
  ├── OrgAdmin (manages org users, settings, full exercise access)
  ├── OrgManager (creates/manages exercises, assigns participants)
  └── OrgUser (participates in assigned exercises)

Exercise Level (per-exercise, HSEEP roles)
  ├── Exercise Director
  ├── Controller
  ├── Evaluator
  └── Observer
```

## User Stories

### P0 - MVP Priority

| ID | Story | Description | Status |
|----|-------|-------------|--------|
| OM-01 | [Organization List](./stories/OM-01-organization-list.md) | SysAdmin views all organizations | 🔲 |
| OM-02 | [Create Organization](./stories/OM-02-create-organization.md) | SysAdmin creates org with first OrgAdmin | 🔲 |
| OM-03 | [Edit Organization](./stories/OM-03-edit-organization.md) | SysAdmin/OrgAdmin edits org details | 🔲 |
| OM-04 | [Organization Lifecycle](./stories/OM-04-organization-lifecycle.md) | Archive, soft delete, restore operations | 🔲 |
| OM-05 | [User-Organization Assignment](./stories/OM-05-user-org-assignment.md) | Assign pending users to organizations | 🔲 |
| OM-06 | [Organization Switcher](./stories/OM-06-organization-switcher.md) | User switches between their organizations | 🔲 |

### P1 - Standard Priority

| ID | Story | Description | Status |
|----|-------|-------------|--------|
| OM-07 | [Invite User to Organization](./stories/OM-07-invite-user.md) | OrgAdmin generates invite link | 🔲 |
| OM-08 | [Join via Organization Code](./stories/OM-08-org-code-join.md) | User joins org using code | 🔲 |
| OM-09 | [Agency List Management](./stories/OM-09-agency-list.md) | OrgAdmin manages participating agencies | 🔲 |
| OM-10 | [Agency Assignment](./stories/OM-10-agency-assignment.md) | Assign agencies to exercises/participants | 🔲 |

### P2 - Future Enhancement

| ID | Story | Description | Status |
|----|-------|-------------|--------|
| OM-11 | [Capability Library Selection](./stories/OM-11-capability-libraries.md) | Select frameworks during org setup | 🔲 |
| OM-12 | [Organization Settings](./stories/OM-12-organization-settings.md) | Timezone, defaults, branding | 🔲 |

## Data Model

```
┌─────────────────────────────────────────────────────────────────┐
│                         Organization                             │
├─────────────────────────────────────────────────────────────────┤
│ Id (GUID)                                                        │
│ Name (string, required, max 200)                                 │
│ Slug (string, required, unique, max 50, URL-safe)               │
│ Description (string, optional, max 1000)                         │
│ ContactEmail (string, optional)                                  │
│ Status (enum: Active, Archived, Inactive)                        │
│ CreatedAt (datetime)                                             │
│ UpdatedAt (datetime)                                             │
│ CreatedById (GUID, FK to User)                                   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ 1:many
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OrganizationMembership                        │
├─────────────────────────────────────────────────────────────────┤
│ Id (GUID)                                                        │
│ UserId (GUID, FK to User)                                        │
│ OrganizationId (GUID, FK to Organization)                        │
│ OrgRole (enum: OrgAdmin, OrgManager, OrgUser)                   │
│ JoinedAt (datetime)                                              │
│ InvitedById (GUID, FK to User, nullable)                        │
│ Status (enum: Active, Inactive)                                  │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ unique constraint: (UserId, OrganizationId)
                                │
┌─────────────────────────────────────────────────────────────────┐
│                            Agency                                │
├─────────────────────────────────────────────────────────────────┤
│ Id (GUID)                                                        │
│ OrganizationId (GUID, FK to Organization)                        │
│ Name (string, required, max 200)                                 │
│ Abbreviation (string, optional, max 20)                          │
│ Description (string, optional, max 500)                          │
│ IsActive (bool, default true)                                    │
│ SortOrder (int, default 0)                                       │
│ CreatedAt (datetime)                                             │
│ UpdatedAt (datetime)                                             │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ unique constraint: (OrganizationId, Name)
                                │
┌─────────────────────────────────────────────────────────────────┐
│                      OrganizationInvite                          │
├─────────────────────────────────────────────────────────────────┤
│ Id (GUID)                                                        │
│ OrganizationId (GUID, FK to Organization)                        │
│ Email (string, optional - null for code-based)                   │
│ Code (string, unique, 8-char alphanumeric)                       │
│ OrgRole (enum: OrgAdmin, OrgManager, OrgUser)                   │
│ ExpiresAt (datetime)                                             │
│ UsedAt (datetime, nullable)                                      │
│ UsedById (GUID, FK to User, nullable)                           │
│ CreatedById (GUID, FK to User)                                   │
│ CreatedAt (datetime)                                             │
│ MaxUses (int, default 1)                                         │
│ UseCount (int, default 0)                                        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        User (updated)                            │
├─────────────────────────────────────────────────────────────────┤
│ Id (GUID)                                                        │
│ Email (string, required, unique)                                 │
│ ... (existing auth fields)                                       │
│ SystemRole (enum: None, SysAdmin)                                │
│ Status (enum: Pending, Active, Disabled)                         │
│ CurrentOrganizationId (GUID, FK to Organization, nullable)      │
└─────────────────────────────────────────────────────────────────┘
```

## Authorization Matrix

| Action | SysAdmin | OrgAdmin | OrgManager | OrgUser |
|--------|----------|----------|------------|---------|
| List all organizations | ✅ | ❌ | ❌ | ❌ |
| Create organization | ✅ | ❌ | ❌ | ❌ |
| Edit any organization | ✅ | ❌ | ❌ | ❌ |
| Edit own organization | ✅ | ✅ | ❌ | ❌ |
| Archive/Delete organization | ✅ | ❌ | ❌ | ❌ |
| Assign users to any org | ✅ | ❌ | ❌ | ❌ |
| Invite users to own org | ✅ | ✅ | ❌ | ❌ |
| Manage agencies in own org | ✅ | ✅ | ❌ | ❌ |
| Switch own organizations | ✅ | ✅ | ✅ | ✅ |
| View own organization | ✅ | ✅ | ✅ | ✅ |

## Technical Notes

### Organization Context

The current organization context is determined by:
1. `User.CurrentOrganizationId` - persisted preference
2. JWT claim `org_id` - included in token for API authorization
3. React context `OrganizationContext` - client-side state

All API endpoints (except auth and org-switching) require organization context and automatically filter data.

### EF Core Global Query Filters

```csharp
// Applied to all org-scoped entities
modelBuilder.Entity<Exercise>()
    .HasQueryFilter(e => e.OrganizationId == _currentOrgId);
```

### Token Refresh on Org Switch

When user switches organizations:
1. API call to `/api/users/current-organization`
2. Backend updates `User.CurrentOrganizationId`
3. New JWT issued with updated `org_id` claim
4. Client refreshes auth context

## UI/UX Patterns

### Organization Switcher Location
- Desktop: User menu dropdown in header
- Mobile: User menu in navigation drawer
- Shows current org name + icon, click to expand list

### Pending User Experience
- Can log in, sees limited dashboard
- Message: "Your account is pending organization assignment"
- Option to enter organization code
- Contact info for support

### Visual Org Indicator
- Current org name always visible in header
- Subtle background color or accent per org (optional future enhancement)
- Clear separation when viewing org admin vs exercise content

## Dependencies

- Authentication system (JWT with claims)
- User registration flow
- Exercise model updates (OrganizationId FK)

## Open Questions

- [ ] Should org codes expire? (Recommend: Yes, 7 days default, configurable)
- [ ] Can OrgAdmin demote themselves? (Recommend: No, must have another OrgAdmin)
- [ ] Audit log for org changes? (Recommend: Yes, P1 feature)

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial feature specification |
