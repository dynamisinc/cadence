# Feature: Authentication & Authorization

**Phase:** MVP
**Status:** In Progress

## Overview

Cadence users can securely authenticate using local credentials (MVP) or their organization's Azure Entra identity (future), with role-based access control enforced at both the application and exercise level. The system implements a hybrid authentication architecture with JWT token management, user registration, and comprehensive role-based permissions.

## Problem Statement

Emergency management exercises require secure access control to protect sensitive scenario data and evaluations. Users need different permissions for different exercises - a person might be a Controller for one exercise but an Evaluator for another. The system must support both local authentication for standalone deployments and future enterprise SSO integration, while ensuring the first deployment can be bootstrapped without manual configuration.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-registration-form.md) | Registration Form | P0 | 📋 Ready |
| [S02](./S02-validate-save-user.md) | Validate and Save User | P0 | 📋 Ready |
| [S03](./S03-first-user-admin.md) | First User Becomes Admin | P0 | 📋 Ready |
| [S04](./S04-login-form.md) | Login Form | P0 | 📋 Ready |
| [S05](./S05-jwt-issuance.md) | JWT Token Issuance | P0 | 📋 Ready |
| [S06](./S06-failed-login-handling.md) | Failed Login Handling | P0 | 📋 Ready |
| [S07](./S07-token-refresh.md) | Automatic Token Refresh | P0 | 📋 Ready |
| [S08](./S08-expiration-handling.md) | Token Expiration Handling | P0 | 📋 Ready |
| [S09](./S09-logout.md) | Secure Logout | P0 | 📋 Ready |
| [S10](./S10-user-list.md) | View User List | P0 | 📋 Ready |
| [S11](./S11-edit-user.md) | Edit User Details | P0 | 📋 Ready |
| [S12](./S12-deactivate-user.md) | Deactivate User | P1 | 📋 Ready |
| [S13](./S13-global-role-assignment.md) | Global Role Assignment | P0 | 📋 Ready |
| [S14](./S14-exercise-role-assignment.md) | Exercise Role Assignment | P0 | 📋 Ready |
| [S15](./S15-role-inheritance.md) | Role Inheritance | P0 | 📋 Ready |
| [S16](./S16-auth-service-interface.md) | Auth Service Interface | P1 | 📋 Ready |
| [S17](./S17-identity-provider.md) | Identity Provider Implementation | P0 | 📋 Ready |
| [S18](./S18-entra-provider.md) | Entra Provider Implementation | P2 | 📋 Ready |
| [S19](./S19-user-account-linking.md) | User Account Linking | P2 | 📋 Ready |
| [S20](./S20-initiate-external-login.md) | Initiate External Login | P2 | 📋 Ready |
| [S21](./S21-oauth-callback-handling.md) | OAuth Callback Handling | P2 | 📋 Ready |
| [S22](./S22-entra-admin-configuration.md) | Entra Admin Configuration | P2 | 📋 Ready |
| [S23](./S23-external-auth-error-handling.md) | External Auth Error Handling | P2 | 📋 Ready |
| [S24](./S24-password-reset.md) | Password Reset | P2 | 📋 Ready |
| [S25](./S25-inline-user-creation.md) | Inline User Creation | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Administrator** | Registers first account (becomes Admin), manages all users and system settings, has full access to all exercises |
| **Exercise Director** | Logs in to plan exercises, assigns participants with appropriate roles, can create Observer-level users inline during setup |
| **Controller** | Authenticates to deliver injects during exercise conduct, may have different roles across multiple exercises |
| **Evaluator** | Signs in to record observations during exercise, has exercise-specific write access for observations only |
| **Observer** | Logs in with read-only access to watch exercise without interfering, may be created inline by Directors |

## Key Concepts

| Term | Definition |
|------|------------|
| **Access Token** | Short-lived JWT (15 minutes) used to authenticate API requests, stored in memory |
| **Refresh Token** | Long-lived token (4 hours) stored in HttpOnly cookie, used to obtain new access tokens |
| **Global Role** | User's default role across the platform (Admin, Director, Controller, Evaluator, Observer) |
| **Exercise Role** | Role assigned to a user for a specific exercise, can override global role |
| **Role Inheritance** | Exercise roles default to user's global role if not explicitly overridden |
| **First User Bootstrap** | First registered user automatically becomes Administrator, eliminating deployment configuration |
| **Hybrid Authentication** | Architecture supporting multiple providers (local + Azure Entra) simultaneously |
| **Identity Provider** | Local ASP.NET Core Identity implementation (MVP) |
| **Entra Provider** | Azure Active Directory SSO integration (future) |
| **User Linking** | Automatic association of external auth accounts with local users by email |

## Dependencies

- **Depends on**: Phase 0 infrastructure (database, Entity Framework, API structure)
- **Blocks**: All user-facing features - exercises, injects, observations, exercise clock
- **Related**: Organization management (multi-tenancy context)

## Acceptance Criteria (Feature-Level)

- [ ] Users can register with email and password, first user becomes Administrator
- [ ] Users can log in and receive JWT access and refresh tokens
- [ ] Access tokens automatically refresh without user intervention
- [ ] Token expiration results in graceful logout and redirect to login
- [ ] Administrators can view, create, edit, and deactivate users
- [ ] Global roles can be assigned to users (Admin, Director, Controller, Evaluator, Observer)
- [ ] Exercise-specific roles can be assigned, overriding global roles
- [ ] Users without explicit exercise role inherit their global role
- [ ] Failed login attempts are tracked and handled appropriately
- [ ] Logout invalidates tokens and clears authentication state
- [ ] System architecture supports future Azure Entra SSO integration via provider abstraction

## Notes

### Architecture Highlights

**Hybrid Authentication Architecture**: The `IAuthenticationService` orchestrator enables multiple authentication providers to work simultaneously. Users can sign in with local credentials (MVP) or Azure Entra (future), with accounts linked by email.

**Token Strategy**: 15-minute access tokens limit exposure, 4-hour refresh tokens align with typical exercise duration. Tokens stored in memory (not localStorage) to prevent XSS attacks. Refresh tokens in HttpOnly cookies.

**Exercise-Scoped Roles**: Users can have different roles per exercise. Example: A user with global role "Controller" might be assigned as "Evaluator" for Exercise A but remain "Controller" for Exercise B.

### Security Considerations

- Access tokens in memory prevent XSS token theft
- HttpOnly refresh cookies prevent JavaScript access
- Server-side role validation on every request prevents escalation
- Database-level unique constraints prevent first-user race conditions

### Out of Scope (MVP)

- Azure Entra SSO integration (designed for, not implemented until P2 stories)
- Multi-factor authentication
- Social login providers (Google, GitHub, etc.)
- Complex password policies beyond basic requirements
- Email-based account recovery (manual admin reset for MVP)
- Detailed audit logging of authentication events

### Success Metrics

- Time to first login (new deployment): < 2 minutes
- Token refresh success rate: > 99.9%
- Failed login lockout: After 5 attempts
- Session timeout (idle): 4 hours
