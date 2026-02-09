# Story: EM-04-S04 - New Device Login Alert

**Feature:** email-communications
**Status:** ⏳ Not Started
**Priority:** P1

**As a** User,
**I want** to be notified when my account is accessed from a new device,
**So that** I can detect unauthorized access early.

## Context

New device alerts help users identify if someone else has gained access to their account. The alert includes device/location information and provides quick action to secure the account if needed.

## Implementation Status

**Current Status:** ⏳ Not Started

### Existing Infrastructure (Available for Reuse)

The following components already exist and can be leveraged:

1. **Auth Flow Captures Device Info**
   - `AuthController` captures `HttpContext.Request.Headers.UserAgent` and passes as `deviceInfo` to `AuthenticationService`
   - `RefreshToken` entity stores `DeviceInfo` (raw User-Agent string) and `CreatedByIp` per session

2. **Email Infrastructure** (from EM-01)
   - Azure Communication Services (ACS) email service configured
   - Template-based email system with parameterization
   - `AuthenticationEmailService` pattern for auth-related emails
   - Email preferences with category system

3. **Token Management**
   - `RefreshTokenStore.RevokeAllForUserAsync()` exists for "sign out all devices"
   - Password change/reset flows already implemented

### What Does NOT Exist Yet

All acceptance criteria below are incomplete because:

- **No `UserDevice` entity or database table** - there is no device registry to track known vs. new devices
- **No device fingerprint calculation** - only raw User-Agent string is currently stored
- **No known vs. new device comparison logic** - no code to determine if a device is "new"
- **No trusted device marking** - no way to flag devices as trusted
- **No IP geolocation service** - no integration to convert IP addresses to approximate location
- **No new-device-alert email template** - template needs to be created in ACS
- **No "Wasn't you?" / "This was me" endpoints** - no API or frontend pages for security response actions
- **No "Security" category email preference wiring** - category exists, but this specific email type not mapped

### Key Implementation Decisions Needed

1. **IP Geolocation Approach**
   - Option A: External API (e.g., ip-api.com, ipstack.com) - simpler, no local data
   - Option B: Local database (MaxMind GeoLite2) - more control, no external dependency
   - **Recommendation:** Option A for MVP (external API), migrate to Option B if volume/cost becomes issue

2. **User-Agent Parsing**
   - Use NuGet package like `UAParser` to extract browser, OS, device type from User-Agent string
   - Calculate device fingerprint as hash of parsed characteristics

3. **Device Trust Model**
   - Initial sign-in from new device: Send alert
   - "This was me" button: Mark device fingerprint as trusted in `UserDevice` table
   - Subsequent sign-ins from same fingerprint: Skip alert

### Estimated Effort

**3-4 days**
- 3 days without IP geolocation (show IP address only)
- 4 days with IP geolocation integration

**Breakdown:**
- Day 1: `UserDevice` entity, migration, device fingerprinting logic
- Day 2: New-device detection in auth flow, email template, service integration
- Day 3: "This was me" / "Wasn't you?" endpoints and frontend pages
- Day 4 (optional): IP geolocation service integration

## Acceptance Criteria

### Detection

- [ ] **AC-01**: Given user signs in, when device fingerprint is new, then send alert email
  - Test: `AuthenticationServiceTests.cs::SignIn_NewDeviceFingerprint_SendsAlertEmail`

- [ ] **AC-02**: Given device fingerprint, when calculated, then includes browser, OS, and device type
  - Test: `DeviceFingerprintServiceTests.cs::CalculateFingerprint_ValidUserAgent_IncludesBrowserOsDeviceType`

- [ ] **AC-03**: Given known device, when signing in again, then NO alert sent
  - Test: `AuthenticationServiceTests.cs::SignIn_KnownDevice_NoAlertSent`

- [ ] **AC-04**: Given same device different browser, when signing in, then alert IS sent
  - Test: `AuthenticationServiceTests.cs::SignIn_SameDeviceDifferentBrowser_SendsAlert`

### Email Content

- [ ] **AC-05**: Given new device alert, when received, then shows approximate location (city/country from IP)
  - Test: `NewDeviceAlertEmailTests.cs::GenerateEmail_ValidData_ShowsApproximateLocation`

- [ ] **AC-06**: Given alert, when received, then shows browser and operating system
  - Test: `NewDeviceAlertEmailTests.cs::GenerateEmail_ValidData_ShowsBrowserAndOS`

- [ ] **AC-07**: Given alert, when received, then shows date and time of sign-in
  - Test: `NewDeviceAlertEmailTests.cs::GenerateEmail_ValidData_ShowsSignInDateTime`

- [ ] **AC-08**: Given alert, when received, then includes "Wasn't you?" button to secure account
  - Test: `NewDeviceAlertEmailTests.cs::GenerateEmail_ValidData_IncludesSecureAccountButton`

- [ ] **AC-09**: Given alert, when received, then includes "This was me" button to dismiss
  - Test: `NewDeviceAlertEmailTests.cs::GenerateEmail_ValidData_IncludesThisWasMeButton`

### Security Response

- [ ] **AC-10**: Given "Wasn't you?" clicked, when loaded, then user can sign out all devices
  - Test: `SecurityResponsePageTests.tsx::renders sign out all devices option`

- [ ] **AC-11**: Given "Wasn't you?" clicked, when loaded, then user can change password
  - Test: `SecurityResponsePageTests.tsx::renders change password option`

- [ ] **AC-12**: Given "This was me" clicked, when processed, then device is marked as trusted
  - Test: `UserDeviceServiceTests.cs::MarkDeviceTrusted_ValidFingerprint_SetsIsTrustedTrue`

### Preferences

- [ ] **AC-13**: Given this email type, when checking preferences, then it's in "Security" category (mandatory)
  - Test: `EmailPreferencesTests.cs::NewDeviceAlert_IsInSecurityCategory_AndMandatory`

## Out of Scope

- Real-time push notifications
- Automatic account lockout
- VPN detection
- Precise geolocation (GPS coordinates)
- Device fingerprinting beyond User-Agent parsing (canvas fingerprinting, WebGL, etc.)

## Dependencies

- EM-01-S01: ACS Email Configuration ✅ Complete
- Device fingerprinting implementation (UAParser NuGet package)
- IP geolocation service (ip-api.com or MaxMind GeoLite2)

## Technical Notes

### Device Tracking

```csharp
public class UserDevice : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Fingerprint { get; set; } = string.Empty;      // SHA256 hash of device characteristics
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;       // Desktop, Mobile, Tablet
    public string LastIpAddress { get; set; } = string.Empty;
    public string? ApproximateLocation { get; set; }             // "Richmond, Virginia, US"
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsTrusted { get; set; }
}
```

### Fingerprint Calculation

```csharp
// Using UAParser to extract device characteristics
var uaParser = Parser.GetDefault();
ClientInfo clientInfo = uaParser.Parse(userAgent);

var fingerprintData = $"{clientInfo.UA.Family}|{clientInfo.UA.Major}|{clientInfo.OS.Family}|{clientInfo.Device.Family}";
var fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintData));
```

### IP Geolocation (Option A - External API)

```csharp
public interface IGeolocationService
{
    Task<ApproximateLocation?> GetLocationAsync(string ipAddress);
}

public class ApproximateLocation
{
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    public override string ToString() => $"{City}, {Region}, {CountryCode}";
}
```

## UI/UX Notes

### Email Preview

```
Subject: New sign-in to your Cadence account

---

[Cadence Logo]

New Sign-In Detected

Hi Jane,

We noticed a sign-in to your Cadence account from
a new device.

SIGN-IN DETAILS
━━━━━━━━━━━━━━━━
📍 Location: Richmond, Virginia, US
💻 Device: Chrome on Windows
🕐 Time: February 6, 2026 at 4:22 PM EST

If this was you, you can ignore this email or mark
this device as trusted.

        [This Was Me]

If this wasn't you, secure your account immediately:

        [Secure My Account]

---

This is an automated security email from Cadence.
```

### Security Response Page (Wasn't You?)

User clicks "Secure My Account" button and lands on dedicated page:

```
┌─────────────────────────────────────┐
│ Secure Your Account                 │
├─────────────────────────────────────┤
│                                     │
│ We'll help you secure your account. │
│                                     │
│ [✓] Sign out all devices            │
│ [✓] Change my password              │
│                                     │
│     [Secure Account]                │
│                                     │
└─────────────────────────────────────┘
```

### Trust Device Page (This Was Me)

User clicks "This Was Me" button and lands on confirmation page:

```
┌─────────────────────────────────────┐
│ Device Trusted                      │
├─────────────────────────────────────┤
│                                     │
│ ✓ This device has been marked as   │
│   trusted. You won't receive alerts│
│   for sign-ins from this device.   │
│                                     │
│     [Back to Cadence]               │
│                                     │
└─────────────────────────────────────┘
```

## Domain Terms

| Term | Definition |
|------|------------|
| Device Fingerprint | Unique identifier (SHA256 hash) based on device characteristics (browser, OS, device type) |
| Trusted Device | Device user has confirmed as legitimate via "This was me" action |
| Approximate Location | City/region/country derived from IP address geolocation |
| User-Agent | HTTP header containing browser/OS information |

## Test Coverage

**Backend:**
- `src/Cadence.Core.Tests/Features/Authentication/DeviceFingerprintServiceTests.cs`
- `src/Cadence.Core.Tests/Features/Authentication/UserDeviceServiceTests.cs`
- `src/Cadence.Core.Tests/Features/Authentication/AuthenticationServiceTests.cs` (new device detection)
- `src/Cadence.Core.Tests/Features/Email/AuthenticationEmailServiceTests.cs` (alert email)

**Frontend:**
- `src/frontend/src/features/authentication/pages/SecurityResponsePage.test.tsx`
- `src/frontend/src/features/authentication/pages/TrustDevicePage.test.tsx`

## Effort Estimate

**3 story points** - Device fingerprinting, geolocation integration, template

**Detailed Breakdown:**
- Backend: 2 days (entity, fingerprinting, detection logic, email template)
- Frontend: 1 day (security response page, trust device page)
- Geolocation: +1 day if using external API integration

---

*Feature: EM-04 Authentication Emails*
*Priority: P1*
