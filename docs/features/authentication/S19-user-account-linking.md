# S19: User Account Linking

## Story

**As a** user signing in with an external provider,
**I want** my external identity linked to my Cadence account,
**So that** I can use either login method and access the same account.

## Context

When users authenticate via external providers (like Azure Entra), we need to connect their external identity to a local Cadence account. This enables hybrid authentication where:
- Existing users can add Microsoft SSO to their account
- New SSO users get a Cadence account created automatically
- Users can have both a local password AND SSO linked

## Acceptance Criteria

### Auto-Linking by Email
- [ ] **Given** I sign in via Entra, **when** my email matches an existing account, **then** my Entra identity is linked to that account
- [ ] **Given** my accounts are linked, **when** I sign in via either method, **then** I access the same Cadence account
- [ ] **Given** `AutoLinkByEmail` is disabled, **when** no explicit link exists, **then** a new account is created

### New User Creation
- [ ] **Given** I sign in via Entra, **when** no matching account exists, **then** a new account is created with my Entra profile info
- [ ] **Given** a new account is created via Entra, **when** I view my profile, **then** I see my display name from Microsoft
- [ ] **Given** a new account is created via Entra, **when** I check my role, **then** I have the configured default role (Observer)

### Multiple Links
- [ ] **Given** I have a local account, **when** I sign in via Entra with matching email, **then** both methods are linked
- [ ] **Given** I have linked accounts, **when** I view my profile, **then** I see all linked authentication methods
- [ ] **Given** I have linked accounts, **when** I sign in with either method, **then** I see the same exercises and data

### Edge Cases
- [ ] **Given** I sign in via Entra, **when** my email matches a DEACTIVATED account, **then** I see "Account deactivated"
- [ ] **Given** two users try to link same Entra identity, **when** it's already linked, **then** second attempt fails
- [ ] **Given** I sign in via Entra, **when** domain restrictions apply and my domain isn't allowed, **then** I'm rejected

## Out of Scope

- Manual account linking UI (automatic only for MVP)
- Unlinking external accounts
- Merging two existing accounts
- Multiple Entra identities per account

## Dependencies

- S16 (Auth Service Interface)
- S18 (Entra Provider)

## Domain Terms

| Term | Definition |
|------|------------|
| External Login | Record linking external provider identity to local account |
| Provider Key | Unique ID from external provider (Object ID for Entra) |
| Auto-Link | Automatically connecting external identity to existing account by email |
| Linked Providers | List of authentication methods available for an account |

## Data Model

```csharp
/// <summary>
/// Links external provider identity to local Cadence user.
/// </summary>
public class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Provider identifier (e.g., "Entra", "Google")
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique identifier from the provider (e.g., Azure AD Object ID)
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for this login method
    /// </summary>
    public string? ProviderDisplayName { get; set; }
    
    public DateTime LinkedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    
    // Navigation
    public ApplicationUser User { get; set; } = null!;
}

// Indexes:
// - UNIQUE (Provider, ProviderKey) - Each external identity links to ONE account
// - INDEX (UserId) - Find all logins for a user
```

## Service Implementation

```csharp
/// <summary>
/// Handles linking external identities to local Cadence accounts.
/// </summary>
public interface IUserLinkingService
{
    /// <summary>
    /// Get or create local user for external identity.
    /// Returns the user and whether it was newly created.
    /// </summary>
    Task<(ApplicationUser User, bool IsNew)> GetOrCreateLinkedUserAsync(ExternalUserInfo externalUser);
    
    /// <summary>
    /// Get all linked providers for a user.
    /// </summary>
    Task<IReadOnlyList<LinkedProvider>> GetLinkedProvidersAsync(Guid userId);
}

public class LinkedProvider
{
    public string Provider { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime LinkedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}

public class UserLinkingService : IUserLinkingService
{
    private readonly CadenceDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserLinkingOptions _options;
    private readonly ILogger<UserLinkingService> _logger;

    public async Task<(ApplicationUser User, bool IsNew)> GetOrCreateLinkedUserAsync(
        ExternalUserInfo externalUser)
    {
        // 1. Check if this external identity is already linked
        var existingLink = await _db.ExternalLogins
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => 
                l.Provider == externalUser.Provider && 
                l.ProviderKey == externalUser.ProviderUserId);

        if (existingLink != null)
        {
            _logger.LogInformation(
                "External login found for {Provider}:{Key}, user {UserId}",
                externalUser.Provider, externalUser.ProviderUserId, existingLink.UserId);
            
            // Check if user is deactivated
            if (existingLink.User.Status == UserStatus.Deactivated)
            {
                throw new AuthenticationException("Account deactivated. Contact administrator.");
            }
            
            // Update last used
            existingLink.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            
            return (existingLink.User, false);
        }

        // 2. Try to auto-link by email (if enabled)
        if (_options.AutoLinkByEmail)
        {
            var existingUser = await _userManager.FindByEmailAsync(externalUser.Email);
            
            if (existingUser != null)
            {
                if (existingUser.Status == UserStatus.Deactivated)
                {
                    throw new AuthenticationException("Account deactivated. Contact administrator.");
                }
                
                // Link external identity to existing account
                await CreateExternalLoginAsync(existingUser.Id, externalUser);
                
                _logger.LogInformation(
                    "Auto-linked {Provider} to existing user {UserId} by email {Email}",
                    externalUser.Provider, existingUser.Id, externalUser.Email);
                
                return (existingUser, false);
            }
        }

        // 3. Create new user
        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = externalUser.Email,
            UserName = externalUser.Email,
            NormalizedEmail = externalUser.Email.ToUpperInvariant(),
            NormalizedUserName = externalUser.Email.ToUpperInvariant(),
            DisplayName = externalUser.DisplayName,
            Role = _options.DefaultRole,
            Status = UserStatus.Active,
            EmailConfirmed = true,  // External provider verified email
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user for external login: {Errors}", errors);
            throw new AuthenticationException($"Failed to create account: {errors}");
        }

        // Link the external identity
        await CreateExternalLoginAsync(Guid.Parse(newUser.Id), externalUser);
        
        _logger.LogInformation(
            "Created new user {UserId} from {Provider} external login, email {Email}",
            newUser.Id, externalUser.Provider, externalUser.Email);

        return (newUser, true);
    }

    public async Task<IReadOnlyList<LinkedProvider>> GetLinkedProvidersAsync(Guid userId)
    {
        var links = await _db.ExternalLogins
            .Where(l => l.UserId == userId)
            .Select(l => new LinkedProvider
            {
                Provider = l.Provider,
                DisplayName = l.ProviderDisplayName ?? l.Provider,
                LinkedAt = l.LinkedAt,
                LastUsedAt = l.LastUsedAt
            })
            .ToListAsync();

        // Check if user has a password (Identity provider)
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null && await _userManager.HasPasswordAsync(user))
        {
            links.Insert(0, new LinkedProvider
            {
                Provider = "Identity",
                DisplayName = "Email & Password",
                LinkedAt = user.CreatedAt,
                LastUsedAt = user.LastLoginAt
            });
        }

        return links;
    }

    private async Task CreateExternalLoginAsync(Guid userId, ExternalUserInfo externalUser)
    {
        var login = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = externalUser.Provider,
            ProviderKey = externalUser.ProviderUserId,
            ProviderDisplayName = GetProviderDisplayName(externalUser.Provider),
            LinkedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        _db.ExternalLogins.Add(login);
        await _db.SaveChangesAsync();
    }

    private static string GetProviderDisplayName(string provider) => provider switch
    {
        "Entra" => "Microsoft (Entra)",
        "Google" => "Google",
        _ => provider
    };
}

public class UserLinkingOptions
{
    public bool AutoLinkByEmail { get; set; } = true;
    public string DefaultRole { get; set; } = "Observer";
}
```

## API Endpoints

```csharp
// GET /api/users/{userId}/linked-providers
// Returns list of authentication methods linked to user

[HttpGet("{userId}/linked-providers")]
[Authorize(Roles = "Administrator")]
public async Task<ActionResult<IReadOnlyList<LinkedProvider>>> GetLinkedProviders(Guid userId)
{
    var providers = await _linkingService.GetLinkedProvidersAsync(userId);
    return Ok(providers);
}

// Response:
[
  {
    "provider": "Identity",
    "displayName": "Email & Password",
    "linkedAt": "2025-01-01T00:00:00Z",
    "lastUsedAt": "2025-01-20T14:30:00Z"
  },
  {
    "provider": "Entra",
    "displayName": "Microsoft (Entra)",
    "linkedAt": "2025-01-15T10:00:00Z",
    "lastUsedAt": "2025-01-21T09:00:00Z"
  }
]
```

## UI/UX Notes

### Profile Page - Linked Accounts Section
```
┌─────────────────────────────────────────────────────────────┐
│ Linked Accounts                                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ✓ Email & Password                                         │
│    Linked Jan 1, 2025 • Last used Jan 20, 2025             │
│                                                             │
│  ✓ Microsoft (Entra)                                        │
│    Linked Jan 15, 2025 • Last used today                   │
│                                                             │
│  [+ Link another account]  (future feature)                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Admin User Detail - Authentication Methods
```
┌─────────────────────────────────────────────────────────────┐
│ Authentication Methods                                       │
├─────────────────────────────────────────────────────────────┤
│  Provider       │ Linked        │ Last Used                 │
│  ─────────────────────────────────────────────────────────  │
│  Identity       │ Jan 1, 2025   │ Jan 20, 2025             │
│  Entra          │ Jan 15, 2025  │ Jan 21, 2025 9:00 AM     │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- External logins stored in separate table (not ASP.NET Identity's AspNetUserLogins)
- Provider + ProviderKey is unique - one external identity maps to one account
- Auto-linking is email-based and happens transparently
- New accounts created via SSO have no password (can't use local login unless they set one)
- Consider adding "Set Password" feature for SSO-only users who want local fallback

---

*Story created: 2025-01-21*
