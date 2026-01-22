# Epic: Authentication & Authorization

> **Production Blocker** - No user can access the system without this epic complete.

## Vision

Cadence users can securely authenticate using local credentials (MVP) or their organization's Azure Entra identity (future), with role-based access control enforced at both the application and exercise level. The first user to register automatically becomes the system Administrator, enabling bootstrap of the platform without external configuration.

## Business Value

- **Security**: Protect sensitive exercise data with industry-standard authentication
- **Compliance**: Support HSEEP role definitions with proper access control
- **Flexibility**: Exercise-scoped roles allow users to have different permissions per exercise
- **Future-proofing**: Clean abstraction enables Entra SSO without rewriting auth logic
- **Self-service**: First-user-becomes-admin eliminates deployment friction

## Success Metrics

| Metric | Target |
|--------|--------|
| Time to first login (new deployment) | < 2 minutes |
| Token refresh success rate | > 99.9% |
| Failed login lockout | After 5 attempts |
| Session timeout (idle) | 4 hours |

## User Personas

| Persona | Description | Auth Needs |
|---------|-------------|------------|
| **Administrator** | System owner, manages users and global settings | Full access, user management |
| **Exercise Director** | Plans and oversees exercises | Create exercises, assign roles |
| **Controller** | Runs exercise, fires injects | Exercise-specific write access |
| **Evaluator** | Observes and captures observations | Exercise-specific write (observations only) |
| **Observer** | Read-only participant | Exercise-specific read access |

## Architecture Decisions

### AD-01: Hybrid Authentication Architecture

**Decision**: Implement `IAuthenticationService` orchestrator that supports multiple simultaneous providers.

**Rationale**: Enables both local authentication (MVP) and Azure Entra SSO (future) to work together. Users can sign in with either method, and accounts are linked by email address.

```
┌─────────────────────────────────────────────────────────────────┐
│                         LOGIN PAGE                               │
│    ┌──────────────────────────────────────────────────────┐     │
│    │  Email: [___________]  Password: [___________]       │     │
│    │  [ Sign In ]                                         │     │
│    │  ──────────────────  OR  ──────────────────────      │     │
│    │  [🔷 Sign in with Microsoft]                         │     │
│    └──────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                  IAuthenticationService                          │
│              (Orchestrates multiple providers)                   │
│  - AuthenticateWithPasswordAsync(credentials)                   │
│  - AuthenticateWithExternalAsync(provider, token)               │
│  - GetAvailableMethods()                                        │
│  - RefreshTokenAsync(refreshToken)                              │
└─────────────────────────┬───────────────────────────────────────┘
                          │
            ┌─────────────┼─────────────┐
            ▼             ▼             ▼
┌───────────────┐ ┌───────────────┐ ┌───────────────┐
│   Identity    │ │    Entra      │ │ UserLinking   │
│   Provider    │ │   Provider    │ │   Service     │
│    (MVP)      │ │   (Future)    │ │               │
├───────────────┤ ├───────────────┤ ├───────────────┤
│ ASP.NET Core  │ │ MSAL + OAuth  │ │ Link external │
│ Identity      │ │ Azure AD      │ │ to local user │
│ Local DB      │ │ SSO           │ │ by email      │
└───────────────┘ └───────────────┘ └───────────────┘
```

**Hybrid Benefits:**
- Users can have both local password AND Entra SSO linked to same account
- Entra users auto-linked by email to existing accounts
- New Entra users get local account created automatically
- Cadence issues its own JWT (not Entra tokens) for consistent offline behavior

### AD-02: JWT Token Strategy

**Decision**: 15-minute access tokens, 4-hour refresh tokens, memory storage.

**Rationale**: 
- Short access tokens limit exposure if compromised
- 4-hour refresh aligns with typical exercise duration
- Memory storage (not localStorage) prevents XSS token theft

| Token Type | Lifetime | Storage | Refresh |
|------------|----------|---------|---------|
| Access Token | 15 minutes | Memory (React state) | Automatic |
| Refresh Token | 4 hours | HttpOnly cookie | On access token expiry |

### AD-03: First User Bootstrap

**Decision**: First registered user automatically becomes Administrator.

**Rationale**: Eliminates need for database seeding or manual configuration during deployment.

### AD-04: Exercise-Scoped Roles

**Decision**: Users have a global role AND can have different roles per exercise.

**Rationale**: A user might be a Controller for one exercise but an Evaluator for another.

```
User (Global Role: Controller)
  ├── Exercise A: Controller (inherited)
  ├── Exercise B: Evaluator (override)
  └── Exercise C: Observer (override)
```

## Stories

| ID | Story | Priority | Status |
|----|-------|----------|--------|
| S01 | [Registration Form](./S01-registration-form.md) | P0 | 📋 Ready |
| S02 | [Validate and Save User](./S02-validate-save-user.md) | P0 | 📋 Ready |
| S03 | [First User Becomes Admin](./S03-first-user-admin.md) | P0 | 📋 Ready |
| S04 | [Login Form](./S04-login-form.md) | P0 | 📋 Ready |
| S05 | [JWT Token Issuance](./S05-jwt-issuance.md) | P0 | 📋 Ready |
| S06 | [Failed Login Handling](./S06-failed-login-handling.md) | P0 | 📋 Ready |
| S07 | [Automatic Token Refresh](./S07-token-refresh.md) | P0 | 📋 Ready |
| S08 | [Token Expiration Handling](./S08-expiration-handling.md) | P0 | 📋 Ready |
| S09 | [Secure Logout](./S09-logout.md) | P0 | 📋 Ready |
| S10 | [View User List](./S10-user-list.md) | P0 | 📋 Ready |
| S11 | [Edit User Details](./S11-edit-user.md) | P0 | 📋 Ready |
| S12 | [Deactivate User](./S12-deactivate-user.md) | P1 | 📋 Ready |
| S13 | [Global Role Assignment](./S13-global-role-assignment.md) | P0 | 📋 Ready |
| S14 | [Exercise Role Assignment](./S14-exercise-role-assignment.md) | P0 | 📋 Ready |
| S15 | [Role Inheritance](./S15-role-inheritance.md) | P0 | 📋 Ready |
| S16 | [Auth Service Interface](./S16-auth-service-interface.md) | P1 | 📋 Ready |
| S17 | [Identity Provider Implementation](./S17-identity-provider.md) | P0 | 📋 Ready |
| S18 | [Entra Provider Implementation](./S18-entra-provider.md) | P2 | 📋 Ready |
| S19 | [User Account Linking](./S19-user-account-linking.md) | P2 | 📋 Ready |
| S20 | [Initiate External Login](./S20-initiate-external-login.md) | P2 | 📋 Ready |
| S21 | [OAuth Callback Handling](./S21-oauth-callback-handling.md) | P2 | 📋 Ready |
| S22 | [Entra Admin Configuration](./S22-entra-admin-configuration.md) | P2 | 📋 Ready |
| S23 | [External Auth Error Handling](./S23-external-auth-error-handling.md) | P2 | 📋 Ready |

## Out of Scope (MVP)

- Azure Entra integration (designed for, not implemented)
- Multi-factor authentication
- Social login providers
- Password complexity rules beyond basic requirements
- Account recovery via email (manual admin reset for MVP)
- Audit logging of auth events (defer to Standard phase)

## Dependencies

- **Depends on**: Phase 0 infrastructure (complete)
- **Blocks**: All user-facing features (exercises, injects, observations)

## Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Token theft via XSS | High | Low | Memory storage, HttpOnly refresh cookie |
| First-user race condition | Medium | Low | Database-level unique constraint |
| Role escalation | High | Low | Server-side role validation on every request |
| Offline auth failure | Medium | Medium | Cache valid session for offline grace period |

## Role Permissions Matrix

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| **System** |
| Manage users | ✅ | ❌ | ❌ | ❌ | ❌ |
| View all exercises | ✅ | ❌ | ❌ | ❌ | ❌ |
| System settings | ✅ | ❌ | ❌ | ❌ | ❌ |
| **Exercise Setup** |
| Create exercise | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit exercise | ✅ | ✅* | ❌ | ❌ | ❌ |
| Delete exercise | ✅ | ✅* | ❌ | ❌ | ❌ |
| Assign participants | ✅ | ✅* | ❌ | ❌ | ❌ |
| **MSEL Authoring** |
| Create inject | ✅ | ✅* | ✅* | ❌ | ❌ |
| Edit inject | ✅ | ✅* | ✅* | ❌ | ❌ |
| Delete inject | ✅ | ✅* | ❌ | ❌ | ❌ |
| **Exercise Conduct** |
| Start/stop clock | ✅ | ✅* | ✅* | ❌ | ❌ |
| Fire inject | ✅ | ✅* | ✅* | ❌ | ❌ |
| Update inject status | ✅ | ✅* | ✅* | ❌ | ❌ |
| **Observations** |
| Create observation | ✅ | ✅* | ✅* | ✅* | ❌ |
| Edit own observation | ✅ | ✅* | ✅* | ✅* | ❌ |
| View observations | ✅ | ✅* | ✅* | ✅* | ✅* |
| **Viewing** |
| View exercise | ✅ | ✅* | ✅* | ✅* | ✅* |
| View MSEL | ✅ | ✅* | ✅* | ✅* | ✅* |
| Export MSEL | ✅ | ✅* | ✅* | ❌ | ❌ |

*\* Within exercises where user has this role*

---

*Created: 2025-01-21*
