# S23: External Auth Error Handling

## Story

**As a** user experiencing issues with Microsoft sign-in,
**I want** to see clear error messages and recovery options,
**So that** I understand what went wrong and how to fix it.

## Context

External authentication can fail for many reasons: user cancellation, permission denial, domain restrictions, network issues, or configuration problems. Users need clear, actionable error messages - not cryptic OAuth error codes. This story covers all error scenarios in the external auth flow.

## Acceptance Criteria

### User-Initiated Errors
- [ ] **Given** I'm at Microsoft login, **when** I click Cancel, **then** I see "Sign in was cancelled. You can try again or use email/password."
- [ ] **Given** consent is required, **when** I deny permission, **then** I see "Permission was not granted. Cadence needs access to your profile."

### Domain Restriction Errors
- [ ] **Given** my domain isn't allowed, **when** auth completes, **then** I see "Your organization (domain.com) is not authorized to use this application."
- [ ] **Given** domain error, **when** I view the message, **then** I see option to "Sign in with email instead"

### Account State Errors
- [ ] **Given** my linked account is deactivated, **when** I sign in via Microsoft, **then** I see "Account deactivated. Contact administrator."
- [ ] **Given** account error, **when** I view the message, **then** I see administrator contact option

### Technical Errors
- [ ] **Given** network fails during token exchange, **when** error occurs, **then** I see "Connection error. Please check your internet and try again."
- [ ] **Given** Microsoft service is down, **when** error occurs, **then** I see "Microsoft sign-in is temporarily unavailable. Please try again later or use email/password."
- [ ] **Given** state validation fails, **when** error occurs, **then** I see "Session expired. Please try again."

### Configuration Errors (Admin Visibility)
- [ ] **Given** Entra is misconfigured, **when** auth fails, **then** user sees generic error
- [ ] **Given** Entra is misconfigured, **when** auth fails, **then** detailed error is logged for admins

### Recovery Options
- [ ] **Given** any external auth error, **when** I view the error page, **then** I see "Try again" button
- [ ] **Given** any external auth error, **when** I view the error page, **then** I see "Sign in with email" option
- [ ] **Given** I click "Try again", **when** redirect happens, **then** I'm sent to Microsoft login again

## Out of Scope

- Automatic retry logic
- Error reporting to external service
- Admin notification of errors

## Dependencies

- S20 (Initiate External Login)
- S21 (OAuth Callback Handling)
- S04 (Login Form - error display)

## Domain Terms

| Term | Definition |
|------|------------|
| OAuth Error | Error returned by Microsoft during authentication |
| Domain Restriction | Policy limiting which email domains can use SSO |
| State Mismatch | Security error when callback state doesn't match request |

## Error Codes Reference

| Code | User Message | Cause |
|------|--------------|-------|
| `access_denied` | Sign in was cancelled | User clicked Cancel at Microsoft |
| `consent_required` | Permission was not granted | User denied consent |
| `domain_not_allowed` | Your organization is not authorized | Email domain not in AllowedDomains |
| `account_deactivated` | Account deactivated | Linked account is disabled |
| `invalid_state` | Session expired | State mismatch or expired |
| `token_exchange_failed` | Authentication failed | Code exchange with Microsoft failed |
| `profile_fetch_failed` | Could not retrieve profile | Microsoft Graph API error |
| `network_error` | Connection error | Network timeout or DNS failure |
| `server_error` | An unexpected error occurred | Unhandled exception |

## Error Page Design

```
┌─────────────────────────────────────────────────────────────────┐
│                           CADENCE                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│                        ⚠️                                       │
│                                                                 │
│              Sign in was cancelled                              │
│                                                                 │
│     You cancelled the Microsoft sign-in process.                │
│     You can try again or sign in with your email               │
│     and password instead.                                       │
│                                                                 │
│     ┌─────────────────────────────────────────┐                │
│     │        Try Microsoft Again              │                │
│     └─────────────────────────────────────────┘                │
│                                                                 │
│     ┌─────────────────────────────────────────┐                │
│     │        Sign in with Email               │                │
│     └─────────────────────────────────────────┘                │
│                                                                 │
│     If you continue to have problems, contact                   │
│     your administrator.                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Implementation

### Error Mapping Service

```csharp
public class ExternalAuthErrorService
{
    /// <summary>
    /// Maps OAuth/internal errors to user-friendly messages.
    /// </summary>
    public AuthErrorInfo GetErrorInfo(string errorCode, string? errorDescription = null)
    {
        return errorCode switch
        {
            "access_denied" => new AuthErrorInfo
            {
                Title = "Sign in was cancelled",
                Message = "You cancelled the Microsoft sign-in process. You can try again or sign in with your email and password instead.",
                ShowRetry = true,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Info
            },
            
            "consent_required" => new AuthErrorInfo
            {
                Title = "Permission was not granted",
                Message = "Cadence needs access to your profile to sign you in. Please try again and accept the permissions.",
                ShowRetry = true,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Warning
            },
            
            "domain_not_allowed" => new AuthErrorInfo
            {
                Title = "Organization not authorized",
                Message = "Your organization is not authorized to use Microsoft sign-in with this application. Please contact your administrator or sign in with email and password.",
                ShowRetry = false,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Warning
            },
            
            "account_deactivated" => new AuthErrorInfo
            {
                Title = "Account deactivated",
                Message = "Your account has been deactivated. Please contact your administrator to restore access.",
                ShowRetry = false,
                ShowEmailLogin = false,
                ShowContactAdmin = true,
                Severity = ErrorSeverity.Error
            },
            
            "invalid_state" or "expired" => new AuthErrorInfo
            {
                Title = "Session expired",
                Message = "Your sign-in session has expired. This can happen if you waited too long. Please try again.",
                ShowRetry = true,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Info
            },
            
            "token_exchange_failed" or "profile_fetch_failed" => new AuthErrorInfo
            {
                Title = "Authentication failed",
                Message = "We couldn't complete your sign-in with Microsoft. Please try again or use email and password.",
                ShowRetry = true,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Error
            },
            
            "network_error" => new AuthErrorInfo
            {
                Title = "Connection error",
                Message = "We couldn't connect to Microsoft. Please check your internet connection and try again.",
                ShowRetry = true,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Warning
            },
            
            _ => new AuthErrorInfo
            {
                Title = "Sign in failed",
                Message = "An unexpected error occurred. Please try again or sign in with email and password.",
                ShowRetry = true,
                ShowEmailLogin = true,
                Severity = ErrorSeverity.Error
            }
        };
    }
}

public class AuthErrorInfo
{
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool ShowRetry { get; init; }
    public bool ShowEmailLogin { get; init; }
    public bool ShowContactAdmin { get; init; }
    public ErrorSeverity Severity { get; init; }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error
}
```

### Frontend Error Display

```typescript
// Login page - error handling
const LoginPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const error = searchParams.get('error');
  const message = searchParams.get('message');

  const errorInfo = useMemo(() => {
    if (!error) return null;
    return getErrorInfo(error, message);
  }, [error, message]);

  return (
    <Box>
      {errorInfo && (
        <AuthErrorAlert 
          error={errorInfo}
          onRetry={() => window.location.href = '/api/auth/external/entra'}
          onEmailLogin={() => setShowEmailForm(true)}
        />
      )}
      
      {/* Rest of login form */}
    </Box>
  );
};

// Error alert component
const AuthErrorAlert: React.FC<{
  error: AuthErrorInfo;
  onRetry: () => void;
  onEmailLogin: () => void;
}> = ({ error, onRetry, onEmailLogin }) => {
  const severity = error.severity === 'info' ? 'info' 
    : error.severity === 'warning' ? 'warning' 
    : 'error';

  return (
    <Alert severity={severity} sx={{ mb: 3 }}>
      <AlertTitle>{error.title}</AlertTitle>
      <Typography variant="body2" sx={{ mb: 2 }}>
        {error.message}
      </Typography>
      
      <Stack direction="row" spacing={2}>
        {error.showRetry && (
          <Button size="small" onClick={onRetry}>
            Try Microsoft Again
          </Button>
        )}
        {error.showEmailLogin && (
          <Button size="small" variant="outlined" onClick={onEmailLogin}>
            Sign in with Email
          </Button>
        )}
      </Stack>
      
      {error.showContactAdmin && (
        <Typography variant="caption" sx={{ mt: 1, display: 'block' }}>
          If you continue to have problems, contact your administrator.
        </Typography>
      )}
    </Alert>
  );
};
```

### Logging for Admins

```csharp
// In callback handler
catch (Exception ex)
{
    // Log detailed error for admins
    _logger.LogError(ex, 
        "External auth failed. Provider: {Provider}, Error: {Error}, State: {State}, " +
        "UserAgent: {UserAgent}, IP: {IP}",
        provider,
        error ?? "unknown",
        state,
        Request.Headers.UserAgent.ToString(),
        HttpContext.Connection.RemoteIpAddress);
    
    // Return generic error to user
    return Redirect("/login?error=server_error&message=An+unexpected+error+occurred");
}
```

## Technical Notes

- Never expose internal error details to users (security risk)
- Always log full error details server-side for troubleshooting
- Use consistent error code format for frontend handling
- Consider adding error tracking (Application Insights) for monitoring
- Test all error scenarios in development

---

*Story created: 2025-01-21*
