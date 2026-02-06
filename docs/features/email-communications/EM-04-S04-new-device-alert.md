# Story: EM-04-S04 - New Device Login Alert

**As a** User,  
**I want** to be notified when my account is accessed from a new device,  
**So that** I can detect unauthorized access early.

## Context

New device alerts help users identify if someone else has gained access to their account. The alert includes device/location information and provides quick action to secure the account if needed.

## Acceptance Criteria

### Detection

- [ ] **Given** user signs in, **when** device fingerprint is new, **then** send alert email
- [ ] **Given** device fingerprint, **when** calculated, **then** includes browser, OS, and device type
- [ ] **Given** known device, **when** signing in again, **then** NO alert sent
- [ ] **Given** same device different browser, **when** signing in, **then** alert IS sent

### Email Content

- [ ] **Given** new device alert, **when** received, **then** shows approximate location (city/country from IP)
- [ ] **Given** alert, **when** received, **then** shows browser and operating system
- [ ] **Given** alert, **when** received, **then** shows date and time of sign-in
- [ ] **Given** alert, **when** received, **then** includes "Wasn't you?" button to secure account
- [ ] **Given** alert, **when** received, **then** includes "This was me" button to dismiss

### Security Response

- [ ] **Given** "Wasn't you?" clicked, **when** loaded, **then** user can sign out all devices
- [ ] **Given** "Wasn't you?" clicked, **when** loaded, **then** user can change password
- [ ] **Given** "This was me" clicked, **when** processed, **then** device is marked as trusted

### Preferences

- [ ] **Given** this email type, **when** checking preferences, **then** it's in "Security" category (mandatory)

## Out of Scope

- Real-time push notifications
- Automatic account lockout
- VPN detection
- Precise geolocation

## Dependencies

- EM-01-S01: ACS Email Configuration
- Device fingerprinting implementation
- IP geolocation service

## Technical Notes

### Device Tracking

```csharp
public class UserDevice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Fingerprint { get; set; }      // Hash of device characteristics
    public string Browser { get; set; }
    public string OperatingSystem { get; set; }
    public string DeviceType { get; set; }       // Desktop, Mobile, Tablet
    public string LastIpAddress { get; set; }
    public string? ApproximateLocation { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsTrusted { get; set; }
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

## Domain Terms

| Term | Definition |
|------|------------|
| Device Fingerprint | Unique identifier based on device characteristics |
| Trusted Device | Device user has confirmed as legitimate |

## Effort Estimate

**3 story points** - Device fingerprinting, geolocation integration, template

---

*Feature: EM-04 Authentication Emails*  
*Priority: P1*
