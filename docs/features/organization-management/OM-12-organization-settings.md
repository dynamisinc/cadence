# Story OM-12: Organization Settings

**Priority:** P2 (Future Enhancement)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As an** Organization Administrator,  
**I want** to configure organization-wide settings and defaults,  
**So that** exercises are consistent and users have a streamlined experience.

---

## Context

Organization settings provide centralized configuration that applies across all exercises and users within the organization. These settings reduce repetitive configuration and ensure consistency.

**Setting Categories:**
1. **Display Settings** - Timezone, date/time formats
2. **Exercise Defaults** - Default exercise type, duration, templates
3. **Observation Settings** - Rating scale, required fields
4. **Branding** - Logo, colors (optional)
5. **Notification Preferences** - Email settings

---

## Acceptance Criteria

### Settings Access

- [ ] **Given** I am an OrgAdmin, **when** I navigate to Organization Settings, **then** I see all configurable settings organized by category
- [ ] **Given** I am an OrgManager or OrgUser, **when** I try to access Organization Settings, **then** I see only read-only information about my organization
- [ ] **Given** I am a SysAdmin, **when** viewing any organization, **then** I can access and modify its settings

### Timezone Settings

- [ ] **Given** I am configuring timezone, **when** I view the dropdown, **then** I see a searchable list of IANA timezones
- [ ] **Given** I set a timezone, **when** exercises display times, **then** they use this timezone by default
- [ ] **Given** I set a timezone, **when** viewing exercise clocks, **then** the exercise time is displayed in the organization's timezone
- [ ] **Given** a timezone is set, **when** creating an exercise, **then** the timezone is pre-selected (but can be overridden)

### Date/Time Format Settings

- [ ] **Given** I am configuring date format, **when** I view options, **then** I can choose: MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD
- [ ] **Given** I am configuring time format, **when** I view options, **then** I can choose: 12-hour (AM/PM) or 24-hour
- [ ] **Given** formats are set, **when** dates/times are displayed, **then** they use the organization's format

### Exercise Default Settings

- [ ] **Given** I am configuring defaults, **when** I set a default exercise type, **then** new exercises pre-select this type
- [ ] **Given** I am configuring defaults, **when** I set a default duration, **then** new exercises pre-fill this duration
- [ ] **Given** defaults are set, **when** creating an exercise, **then** the defaults are applied but can be changed
- [ ] **Given** I want to enable exercise templates, **when** I toggle the setting, **then** template features become available

### Observation Rating Scale

- [ ] **Given** I am configuring observations, **when** I view rating scale options, **then** I can choose: P/S/M/U (HSEEP), 1-5 Numeric, Pass/Fail, Custom
- [ ] **Given** I select P/S/M/U, **when** evaluators rate observations, **then** they use Performed/Partially/Marginally/Unable
- [ ] **Given** I select Custom, **when** configuring, **then** I can define my own rating labels (2-5 levels)
- [ ] **Given** a rating scale is set, **when** viewing exercise observations, **then** ratings use the configured scale

### Observation Required Fields

- [ ] **Given** I am configuring observations, **when** I view field requirements, **then** I can toggle which fields are mandatory
- [ ] **Given** I make "Capability" required, **when** evaluators create observations, **then** they must select a capability
- [ ] **Given** I make "Agency" required, **when** evaluators create observations, **then** they must select an observed agency

### Branding Settings (Optional)

- [ ] **Given** I am configuring branding, **when** I upload a logo, **then** it appears in the organization header
- [ ] **Given** I upload a logo, **when** viewing size requirements, **then** it must be max 500KB, recommended 200x50px
- [ ] **Given** I am configuring branding, **when** I select a primary color, **then** it's used for accent elements
- [ ] **Given** branding is set, **when** users view the app, **then** they see the organization's branding

### Notification Settings

- [ ] **Given** I am configuring notifications, **when** I view options, **then** I can enable/disable notification types
- [ ] **Given** I enable "Exercise Start Reminder", **when** an exercise is about to start, **then** participants receive email notifications
- [ ] **Given** I enable "Daily Digest", **when** configured, **then** users receive a summary of their pending tasks

### Settings Audit

- [ ] **Given** any setting is changed, **when** the change is saved, **then** it is logged with who changed it and when
- [ ] **Given** I am an OrgAdmin, **when** I view settings history, **then** I can see recent changes (future enhancement)

---

## Out of Scope

- Per-user setting overrides (users cannot override org settings)
- Setting inheritance from parent organizations (no hierarchy)
- Setting templates across organizations
- Custom field definitions
- API rate limiting configuration
- Data retention policies

---

## Dependencies

- OM-03: Edit Organization (basic org info)
- Exercise feature (uses defaults)
- Observation feature (uses rating scale)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Organization Settings | Configuration that applies to all exercises and users in an org |
| Rating Scale | The system used to evaluate observations (P/S/M/U is HSEEP standard) |
| Exercise Defaults | Pre-configured values applied to new exercises |

---

## UI/UX Notes

### Settings Page Layout
```
┌─────────────────────────────────────────────────────────────────┐
│ Organization Settings                                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ ┌─────────────┐                                                 │
│ │ General     │ Display & Regional Settings                     │
│ │ Exercises   │ ─────────────────────────────────────────────── │
│ │ Observations│                                                 │
│ │ Branding    │ Timezone                                        │
│ │ Notifications│ ┌───────────────────────────────────────────┐  │
│ └─────────────┘ │ America/New_York (EST/EDT)              ▼ │  │
│                  └───────────────────────────────────────────┘  │
│                  Used for exercise schedules and time displays  │
│                                                                  │
│                  Date Format                                     │
│                  ○ MM/DD/YYYY (01/29/2025)                      │
│                  ● DD/MM/YYYY (29/01/2025)                      │
│                  ○ YYYY-MM-DD (2025-01-29)                      │
│                                                                  │
│                  Time Format                                     │
│                  ● 12-hour (2:30 PM)                            │
│                  ○ 24-hour (14:30)                              │
│                                                                  │
│                                              [Save Changes]      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Exercise Defaults Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Exercise Defaults                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Default Exercise Type                                           │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Tabletop Exercise (TTX)                                ▼  │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ Default Duration                                                 │
│ ┌────────────┐                                                  │
│ │ 4          │ hours                                            │
│ └────────────┘                                                  │
│                                                                  │
│ Default Participant Notification                                │
│ ☑ Send email to participants when added to exercise            │
│ ☑ Send reminder 24 hours before exercise                       │
│ ☐ Send reminder 1 hour before exercise                         │
│                                                                  │
│                                              [Save Changes]      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Observation Settings Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Observation Settings                                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Rating Scale                                                     │
│ ● P/S/M/U (HSEEP Standard)                                      │
│   Performed | Performed with Some Gaps | Major Gaps | Unable    │
│                                                                  │
│ ○ Numeric (1-5)                                                 │
│   1 (Poor) to 5 (Excellent)                                     │
│                                                                  │
│ ○ Pass/Fail                                                     │
│   Pass | Fail                                                   │
│                                                                  │
│ ○ Custom                                                        │
│   Define your own rating levels                                 │
│                                                                  │
│ ─────────────────────────────────────────────────────────────── │
│                                                                  │
│ Required Observation Fields                                      │
│ ☑ Rating (always required)                                      │
│ ☑ Observation Text (always required)                            │
│ ☐ Related Inject                                                │
│ ☐ Capability                                                    │
│ ☐ Agency                                                        │
│ ☐ Recommended Actions                                           │
│                                                                  │
│                                              [Save Changes]      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Custom Rating Scale Configuration
```
┌─────────────────────────────────────────────────────────────────┐
│ Custom Rating Scale                                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Define your rating levels (2-5 levels, best to worst):          │
│                                                                  │
│ Level 1 (Best)                                                  │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Excellent                                                  │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ Level 2                                                         │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Good                                                       │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ Level 3                                                         │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Needs Improvement                                          │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ Level 4 (Worst)                                                 │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Unsatisfactory                                             │   │
│ └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│ [+ Add Level]                                                   │
│                                                                  │
│                              [Cancel]  [Save Rating Scale]       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Branding Section
```
┌─────────────────────────────────────────────────────────────────┐
│ Branding                                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│ Organization Logo                                               │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │                                                              │ │
│ │    [Current Logo Preview]                                   │ │
│ │                                                              │ │
│ │    [Upload New Logo]  [Remove Logo]                         │ │
│ │                                                              │ │
│ │    Max 500KB, PNG or SVG recommended                        │ │
│ │    Recommended size: 200x50 pixels                          │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│ Primary Accent Color                                            │
│ ┌────────────────────────────────────────┐                     │
│ │ ■ #1976D2                      [Pick] │                     │
│ └────────────────────────────────────────┘                     │
│ Used for buttons and highlights                                │
│                                                                  │
│                                              [Save Changes]      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### Data Model

**OrganizationSettings Entity:**
```csharp
public class OrganizationSettings
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; }
    
    // Display Settings
    public string Timezone { get; set; }  // IANA timezone, default "America/New_York"
    public DateFormat DateFormat { get; set; }  // default MDY
    public TimeFormat TimeFormat { get; set; }  // default TwelveHour
    
    // Exercise Defaults
    public ExerciseType DefaultExerciseType { get; set; }  // default TTX
    public int DefaultDurationHours { get; set; }  // default 4
    public bool NotifyOnParticipantAdd { get; set; }  // default true
    public bool Reminder24Hours { get; set; }  // default true
    public bool Reminder1Hour { get; set; }  // default false
    
    // Observation Settings
    public RatingScaleType RatingScale { get; set; }  // default PSMU
    public string? CustomRatingLevels { get; set; }  // JSON array if custom
    public bool RequireInject { get; set; }  // default false
    public bool RequireCapability { get; set; }  // default false
    public bool RequireAgency { get; set; }  // default false
    public bool RequireRecommendedActions { get; set; }  // default false
    
    // Branding
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }  // hex color
    
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
}

public enum DateFormat { MDY, DMY, YMD }
public enum TimeFormat { TwelveHour, TwentyFourHour }
public enum RatingScaleType { PSMU, Numeric, PassFail, Custom }
```

### API Endpoints

**Get Organization Settings:**
```
GET /api/organizations/current/settings
Authorization: Bearer {token}

Response:
{
  "timezone": "America/New_York",
  "dateFormat": "MDY",
  "timeFormat": "TwelveHour",
  "exerciseDefaults": {
    "type": "TTX",
    "durationHours": 4,
    "notifyOnParticipantAdd": true,
    "reminder24Hours": true,
    "reminder1Hour": false
  },
  "observationSettings": {
    "ratingScale": "PSMU",
    "ratingLevels": ["Performed", "Performed with Some Gaps", "Performed with Major Gaps", "Unable to Perform"],
    "requiredFields": {
      "inject": false,
      "capability": false,
      "agency": false,
      "recommendedActions": false
    }
  },
  "branding": {
    "logoUrl": "https://storage.../logo.png",
    "primaryColor": "#1976D2"
  },
  "updatedAt": "2025-01-29T15:30:00Z"
}
```

**Update Organization Settings:**
```
PUT /api/organizations/current/settings
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "timezone": "America/Chicago",
  "dateFormat": "MDY",
  "timeFormat": "TwelveHour",
  // ... other settings
}

Response (200 OK):
{
  // Updated settings object
}
```

**Upload Logo:**
```
POST /api/organizations/current/settings/logo
Authorization: Bearer {token} (OrgAdmin only)
Content-Type: multipart/form-data

Request: File upload (logo image)

Response (200 OK):
{
  "logoUrl": "https://storage.../orgs/{orgId}/logo.png"
}
```

**Delete Logo:**
```
DELETE /api/organizations/current/settings/logo
Authorization: Bearer {token} (OrgAdmin only)

Response (200 OK):
{
  "deleted": true
}
```

### Timezone Support

Use IANA timezone database:
```csharp
// Validate timezone
public bool IsValidTimezone(string timezoneId)
{
    try
    {
        TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        return true;
    }
    catch
    {
        return false;
    }
}

// Convert to org timezone
public DateTime ToOrganizationTime(DateTime utc, OrganizationSettings settings)
{
    var tz = TimeZoneInfo.FindSystemTimeZoneById(settings.Timezone);
    return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| OrgAdmin can update settings | Integration | P0 |
| Timezone affects time display | Integration | P0 |
| Date format affects date display | Integration | P0 |
| Exercise defaults apply to new exercises | Integration | P0 |
| Rating scale options work | Integration | P0 |
| Custom rating scale saves correctly | Integration | P1 |
| Required fields enforced on observations | Integration | P0 |
| Logo upload and display | Integration | P1 |
| OrgManager cannot modify settings | Integration | P0 |
| Settings changes are logged | Integration | P1 |

---

## Implementation Checklist

### Backend
- [ ] Create `OrganizationSettings` entity
- [ ] Create enums for settings options
- [ ] Create database migration
- [ ] Create `GET /api/organizations/current/settings` endpoint
- [ ] Create `PUT /api/organizations/current/settings` endpoint
- [ ] Create `POST /api/organizations/current/settings/logo` endpoint
- [ ] Create `DELETE /api/organizations/current/settings/logo` endpoint
- [ ] Implement timezone conversion utilities
- [ ] Implement logo upload to blob storage
- [ ] Add settings audit logging
- [ ] Unit tests for services
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `OrganizationSettingsPage` component
- [ ] Create `GeneralSettingsSection` component
- [ ] Create `ExerciseDefaultsSection` component
- [ ] Create `ObservationSettingsSection` component
- [ ] Create `BrandingSection` component
- [ ] Create `CustomRatingScaleDialog` component
- [ ] Implement timezone picker (searchable)
- [ ] Implement color picker
- [ ] Implement logo upload with preview
- [ ] Add settings save feedback
- [ ] Component tests

### Integration
- [ ] Update exercise creation to use defaults
- [ ] Update observation creation to use required fields
- [ ] Update time displays to use org timezone
- [ ] Update date displays to use org format
- [ ] Apply branding to header component

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
