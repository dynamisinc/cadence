# Feature: Organization Management

**Phase:** MVP
**Status:** In Progress

## Overview

Organizations are the primary security boundary in Cadence. All exercise data, users, and configurations are scoped to an organization. Users can belong to multiple organizations with different roles in each, but they work within one organization context at a time.

## Problem Statement

Emergency management platforms need robust multi-tenancy to ensure data isolation between different organizations (e.g., CISA data never leaks to commercial clients), while supporting flexibility for consultants and contractors who work across multiple organizations. Traditional single-tenant or weak multi-tenant approaches either compromise security or usability.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [OM-01](./OM-01-organization-list.md) | Organization List | P0 (MVP) | Ready |
| [OM-02](./OM-02-create-organization.md) | Create Organization | P0 (MVP) | Ready |
| [OM-03](./OM-03-edit-organization.md) | Edit Organization | P0 (MVP) | Ready |
| [OM-04](./OM-04-organization-lifecycle.md) | Organization Lifecycle | P0 (MVP) | Ready |
| [OM-05](./OM-05-user-org-assignment.md) | User-Organization Assignment | P0 (MVP) | Ready |
| [OM-06](./OM-06-organization-switcher.md) | Organization Switcher | P0 (MVP) | Ready |
| [OM-07](./OM-07-invite-user.md) | Invite User to Organization | P1 (Standard) | Ready |
| [OM-08](./OM-08-org-code-join.md) | Join via Organization Code | P1 (Standard) | Ready |
| [OM-09](./OM-09-agency-list.md) | Agency List Management | P1 (Standard) | Ready |
| [OM-10](./OM-10-agency-assignment.md) | Agency Assignment | P1 (Standard) | Ready |
| [OM-11](./OM-11-capability-libraries.md) | Capability Library Selection | P2 (Future) | Ready |
| [OM-12](./OM-12-organization-settings.md) | Organization Settings | P2 (Future) | Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **SysAdmin** | Manages all organizations, creates new organizations, assigns users globally, monitors platform health |
| **OrgAdmin** | Manages their organization's users, settings, and exercises. Cannot access other organizations |
| **OrgManager** | Creates and manages exercises within their organization, assigns participants to exercises |
| **OrgUser** | Participates in assigned exercises within their organization |
| **Pending User** | Newly registered user waiting for organization assignment or entering an organization code |

## Key Concepts

### Three-Tier Role Hierarchy

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

### Organization Status

| Status | Meaning | User Access | Data Access |
|--------|---------|-------------|-------------|
| **Active** | Normal operation | Full access | Full read/write |
| **Archived** | Historical reference | Read-only for existing members | Read-only |
| **Inactive** | Soft-deleted | No access (hidden from users) | Hidden from queries |

### Domain Terminology

| Term | Definition |
|------|------------|
| **Organization** | Tenant boundary containing users, exercises, and configurations |
| **Current Organization** | The active organization context for all user operations |
| **Organization Membership** | User's association with an organization and their OrgRole |
| **Organization Code** | Shareable alphanumeric code allowing users to join an organization |
| **Slug** | URL-friendly unique identifier (e.g., "cisa-region-4") |
| **Agency** | A responding organization/department participating in exercises (e.g., Fire, EMS, Police) |
| **Capability** | A function or activity evaluated during exercises (FEMA Core Capabilities, custom capabilities) |
| **Pending User** | Registered user not yet assigned to any organization |

## Dependencies

- **Authentication System** (JWT with claims: org_id, org_role)
- **User Registration Flow** (creates pending users)
- **Exercise Model Updates** (OrganizationId FK on all org-scoped entities)
- **EF Core Global Query Filters** (automatic org scoping)

## Acceptance Criteria (Feature-Level)

- [ ] SysAdmin can create and manage all organizations
- [ ] OrgAdmins can manage only their own organization
- [ ] All domain data (exercises, injects, observations) is scoped to organizations with no cross-org leakage
- [ ] Users can belong to multiple organizations and switch between them seamlessly
- [ ] JWT tokens include current organization context (org_id, org_role claims)
- [ ] Organization context persists across sessions (last-used organization remembered)
- [ ] Pending users see a waiting page until assigned to an organization
- [ ] Organization lifecycle supports Active, Archived, and Inactive states
- [ ] OrgAdmins can invite users via email or shareable codes
- [ ] Organizations can customize agencies and capability libraries
- [ ] All org-scoped API endpoints automatically filter data by current organization
- [ ] Organization status changes (archive, deactivate) properly restrict access

## Notes

### Business Value

- **Data Isolation:** Ensures CISA data never leaks to commercial clients
- **Scalability:** Supports multiple independent customers on shared infrastructure
- **Flexibility:** Consultants and contractors can work across organizations
- **Compliance:** Clear audit trails of who did what in which organization

### Authorization Matrix

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

### Technical Architecture

**Backend Organization Context:**
- `ICurrentOrganizationContext` interface (Cadence.Core/Hubs/)
- `CurrentOrganizationContext` implementation (Cadence.WebApi/Services/)
- `OrganizationValidationInterceptor` ensures correct OrganizationId on save
- All org-scoped services inject `ICurrentOrganizationContext`

**Frontend Organization Context:**
- `OrganizationContext` React context (contexts/OrganizationContext.tsx)
- `useOrganization` hook for accessing current org and switching
- `OrganizationSwitcher` component in header/navigation

**JWT Claims Structure:**
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "role": "User",           // SystemRole
  "org_id": "org-guid",     // Current organization (nullable)
  "org_role": "OrgAdmin"    // Role in current org (nullable)
}
```

### Open Questions

- [ ] Should org codes expire? (Recommend: Yes, 7 days default, configurable)
- [ ] Can OrgAdmin demote themselves? (Recommend: No, must have another OrgAdmin)
- [ ] Audit log for org changes? (Recommend: Yes, P1 feature)

## Implementation Phases

### P0 - MVP (OM-01 through OM-06)
Core multi-tenancy infrastructure enabling secure data isolation and user management.

- Organization CRUD (list, create, edit, lifecycle management)
- User-organization assignment (SysAdmin assigns pending users)
- Organization switcher (users with multiple orgs can switch context)
- JWT integration (org_id and org_role claims)
- EF Core global filters (automatic org scoping)

### P1 - Standard (OM-07 through OM-10)
Self-service onboarding and exercise metadata management.

- Email invitations (OrgAdmins invite specific users)
- Organization codes (shareable codes for bulk onboarding)
- Agency list management (define participating agencies like Fire, EMS, Police)
- Agency assignment (link agencies to participants, injects, observations)

### P2 - Future Enhancement (OM-11 through OM-12)
Advanced customization and organization-wide settings.

- Capability libraries (FEMA Core, NATO, NIST CSF, custom capabilities)
- Organization settings (timezone, date formats, branding, defaults)

## Changelog

| Date | Change |
|------|--------|
| 2026-02-02 | Created standardized FEATURE.md from README.md |
| 2025-01-29 | Initial feature specification (README.md) |
