# S04: Login Form

## Story

**As a** registered user,
**I want** to sign in using my preferred authentication method,
**So that** I can access my Cadence account conveniently.

## Context

The login form is the gateway to Cadence. It should support multiple authentication methods: local email/password for users who registered directly, and Microsoft SSO for organizations using Azure Entra. The form dynamically shows available methods based on system configuration.

## Acceptance Criteria

### Core Login
- [ ] **Given** I navigate to Cadence, **when** I am not authenticated, **then** I see the login form
- [ ] **Given** I am on the login form, **when** I view it, **then** I see fields for Email and Password
- [ ] **Given** I enter credentials, **when** I press Enter, **then** the form submits (keyboard accessible)
- [ ] **Given** I click "Sign In", **when** the form submits, **then** I see a loading indicator
- [ ] **Given** I click the password visibility toggle, **when** I view the field, **then** the password is shown/hidden
- [ ] **Given** I check "Remember me", **when** I login successfully, **then** my session persists longer

### External Authentication (Hybrid)
- [ ] **Given** Entra is enabled, **when** I view the login form, **then** I see "Sign in with Microsoft" button
- [ ] **Given** Entra is disabled, **when** I view the login form, **then** I do NOT see the Microsoft button
- [ ] **Given** I click "Sign in with Microsoft", **when** the redirect completes, **then** I am authenticated via Entra
- [ ] **Given** I sign in via Entra, **when** my account is linked, **then** I access my existing Cadence account
- [ ] **Given** I sign in via Entra, **when** no linked account exists, **then** a new account is created for me

### Offline Behavior
- [ ] **Given** I am offline, **when** I view the login page, **then** I see an indicator that I'm offline
- [ ] **Given** I was previously logged in and am offline, **when** I access Cadence, **then** I can continue with cached session
- [ ] **Given** I am offline, **when** I click "Sign in with Microsoft", **then** I see "External sign-in requires internet connection"

## Out of Scope

- "Forgot password" flow (manual admin reset for MVP)
- Additional social login providers (Google, etc.)
- MFA/2FA challenge

## Dependencies

- MUI component library ✅
- S16 (Auth Service Interface) - for `GetAvailableMethods()`
- S18 (Entra Provider) - for Microsoft SSO

## Domain Terms

| Term | Definition |
|------|------------|
| Remember Me | Option to extend session duration beyond default 4 hours |
| Cached Session | Previously authenticated session stored locally for offline access |
| External Login | Authentication via third-party provider (Entra, etc.) |
| Account Linking | Connecting external identity to local Cadence account |

## UI/UX Notes

```
┌─────────────────────────────────────────┐
│              CADENCE                     │
│                                         │
│     ┌─────────────────────────────┐     │
│     │         Sign In             │     │
│     ├─────────────────────────────┤     │
│     │                             │     │
│     │  Email Address              │     │
│     │  ┌───────────────────────┐  │     │
│     │  │ jane@example.com      │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  Password                   │     │
│     │  ┌───────────────────────┐  │     │
│     │  │ ••••••••          👁  │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  ☐ Remember me              │     │
│     │                             │     │
│     │  ┌───────────────────────┐  │     │
│     │  │       Sign In        │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  ─────────  OR  ─────────   │     │
│     │                             │     │
│     │  ┌───────────────────────┐  │     │
│     │  │ 🔷 Sign in with       │  │     │
│     │  │    Microsoft          │  │     │
│     │  └───────────────────────┘  │     │
│     │                             │     │
│     │  Don't have an account?     │     │
│     │  Create one                 │     │
│     └─────────────────────────────┘     │
│                                         │
│   ⚠️ Offline - Using cached session    │
└─────────────────────────────────────────┘
```

- Autofocus on email field
- Tab order: Email → Password → Remember Me → Sign In → Microsoft
- Error messages appear above form, not inline (for login)
- "OR" divider only shown when external providers enabled
- Microsoft button uses official branding guidelines
- Link to registration below form
- Offline indicator uses subtle warning banner

## Technical Notes

```typescript
// Fetch available auth methods on mount
const [authMethods, setAuthMethods] = useState<AuthMethod[]>([]);

useEffect(() => {
  const methods = await authService.getAvailableMethods();
  setAuthMethods(methods);
}, []);

// Handle external login
const handleExternalLogin = async (provider: string) => {
  if (!navigator.onLine) {
    showError("External sign-in requires internet connection");
    return;
  }
  
  // Redirect to OAuth flow
  window.location.href = `/api/auth/external/${provider}`;
};

// Render external buttons dynamically
{authMethods
  .filter(m => m.provider !== 'Identity')
  .map(method => (
    <Button 
      key={method.provider}
      onClick={() => handleExternalLogin(method.provider)}
      startIcon={<ProviderIcon provider={method.provider} />}
    >
      {method.displayName}
    </Button>
  ))
}
```

- Use controlled inputs with React state
- Disable submit while loading
- Store credentials temporarily for retry on network failure
- Clear password field on failed login attempt
- Microsoft button disabled when offline

---

*Story created: 2025-01-21*
*Updated: 2025-01-21 - Added hybrid authentication support*
