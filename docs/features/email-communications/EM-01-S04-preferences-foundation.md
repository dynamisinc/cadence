# Story: EM-01-S04 - Email Preferences Foundation

**As a** User,  
**I want** to control which types of emails I receive,  
**So that** I only get notifications that are relevant to me.

## Context

Email preferences allow users to customize their notification experience. Some emails (security, invitations) are mandatory and cannot be disabled. Others (reminders, digests) can be opted in or out based on user preference. This foundation establishes the preference system that individual email features will check before sending.

## Acceptance Criteria

### Preference Categories

- [ ] **Given** email categories exist, **when** viewing preferences, **then** I see categories grouped by type (Security, Invitations, Assignments, Reminders, Digests)
- [ ] **Given** a mandatory category (Security, Invitations), **when** viewing, **then** toggle is disabled with explanation "Required for account security"
- [ ] **Given** an optional category, **when** toggling off, **then** preference is saved immediately
- [ ] **Given** preferences saved, **when** system sends email, **then** preference is checked before sending

### Default Preferences

- [ ] **Given** a new user account, **when** created, **then** default preferences are applied (all mandatory on, Assignments on, Reminders on, Digests off)
- [ ] **Given** default preferences, **when** user hasn't changed them, **then** system uses organization defaults if set
- [ ] **Given** organization default differs from system default, **when** user joins, **then** organization default takes precedence

### Preference Enforcement

- [ ] **Given** user has disabled Reminders, **when** reminder email would be sent, **then** email is suppressed and logged as "Suppressed - User Preference"
- [ ] **Given** user has disabled a category, **when** checking preference, **then** check completes in <10ms (cached)
- [ ] **Given** mandatory email (password reset), **when** user preference is checked, **then** preference check is skipped (always sends)

### Preference UI

- [ ] **Given** I'm viewing my profile settings, **when** I click "Email Preferences", **then** I see all categories with current settings
- [ ] **Given** preference UI, **when** I change a setting, **then** change is saved without page reload
- [ ] **Given** preference change saved, **when** complete, **then** I see confirmation "Preferences saved"

## Out of Scope

- Per-exercise notification preferences (all or nothing per category)
- Email frequency settings (digest timing configured separately)
- Unsubscribe from all (must maintain account access)
- Organization-level preference management UI

## Dependencies

- User authentication (need logged-in user)

## Technical Notes

### UserEmailPreference Entity

```csharp
public class UserEmailPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public EmailCategory Category { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum EmailCategory
{
    // Mandatory (cannot disable)
    Security,           // Password reset, login alerts
    Invitations,        // Org and exercise invitations
    
    // Optional (default on)
    Assignments,        // Inject assigned, role changed
    Workflow,           // Inject approved/rejected
    Reminders,          // Exercise starting, deadlines
    
    // Optional (default off)
    DailyDigest,        // Daily activity summary
    WeeklyDigest        // Weekly organization report
}
```

### IEmailPreferenceService Interface

```csharp
public interface IEmailPreferenceService
{
    Task<bool> CanSendAsync(Guid userId, EmailCategory category);
    Task<UserEmailPreferences> GetPreferencesAsync(Guid userId);
    Task UpdatePreferenceAsync(Guid userId, EmailCategory category, bool isEnabled);
}
```

### Preference Check in Email Service

```csharp
public class EmailService : IEmailService
{
    public async Task<EmailSendResult> SendAsync(
        EmailMessage message, 
        EmailCategory category,
        Guid recipientUserId)
    {
        // Check if this category requires preference check
        if (!IsMandatoryCategory(category))
        {
            var canSend = await _preferences.CanSendAsync(recipientUserId, category);
            if (!canSend)
            {
                return new EmailSendResult(
                    MessageId: null,
                    Status: EmailSendStatus.Suppressed,
                    Reason: "User preference"
                );
            }
        }
        
        // Proceed with sending
        return await _innerService.SendAsync(message);
    }
}
```

### Caching Strategy

```csharp
// Preferences cached per user for 5 minutes
public class CachedEmailPreferenceService : IEmailPreferenceService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    
    public async Task<bool> CanSendAsync(Guid userId, EmailCategory category)
    {
        var prefs = await GetPreferencesAsync(userId);
        return prefs.IsEnabled(category);
    }
}
```

## UI/UX Notes

### Email Preferences Settings

```
┌─────────────────────────────────────────────────────────────────┐
│ Email Preferences                                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ SECURITY & ACCOUNT                                              │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ [✓] Security alerts                              Required   │ │
│ │     Password resets, login alerts, account verification     │ │
│ │                                                             │ │
│ │ [✓] Invitations                                  Required   │ │
│ │     Organization and exercise invitations                   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ EXERCISE NOTIFICATIONS                                          │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ [✓] Assignments                                    [Toggle] │ │
│ │     When you're assigned to injects or roles                │ │
│ │                                                             │ │
│ │ [✓] Workflow updates                               [Toggle] │ │
│ │     When injects you submitted are approved/rejected        │ │
│ │                                                             │ │
│ │ [✓] Reminders                                      [Toggle] │ │
│ │     Exercise starting, deadlines approaching                │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ SUMMARIES                                                       │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ [ ] Daily digest                                   [Toggle] │ │
│ │     Daily summary of activity in your exercises             │ │
│ │                                                             │ │
│ │ [ ] Weekly report                                  [Toggle] │ │
│ │     Weekly summary for organization admins                  │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│                                         ✓ Preferences saved     │
└─────────────────────────────────────────────────────────────────┘
```

## Domain Terms

| Term | Definition |
|------|------------|
| Mandatory Email | Emails required for account security that cannot be disabled |
| Email Category | Grouping of related email types for preference management |
| Suppressed | Email not sent due to user preference (logged for audit) |

## Open Questions

- [ ] Should organization admins be able to set default preferences for new members?
- [ ] Should we show "Email suppressed" in email logs or is that confusing?

## Effort Estimate

**3 story points** - Preference model, service, UI component

---

*Feature: EM-01 Email Infrastructure*  
*Priority: P0*
