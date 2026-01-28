# Story: User Time Format Setting

**Feature**: Settings  
**Story ID**: S02  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As a** Cadence user (any role),  
**I want** to choose between 12-hour and 24-hour time formats,  
**So that** I can view times in the format I'm accustomed to (military time is standard in many EM operations).

---

## Context

Emergency management professionals often use 24-hour (military) time for clarity and to avoid AM/PM confusion during extended operations that span midnight. However, some users—particularly those from civilian organizations or local government—prefer 12-hour format. This setting affects all time displays throughout the application.

HSEEP exercises frequently reference specific times for inject delivery. Misreading "1400" as "4:00 PM" vs "2:00 PM" can cause coordination failures. Users should see times in their preferred format.

---

## Acceptance Criteria

- [ ] **Given** I am in user settings, **when** I view time format options, **then** I can select 12-hour (AM/PM) or 24-hour (military)
- [ ] **Given** I select 24-hour format, **when** viewing any time in the app, **then** times display as HH:MM (e.g., "14:30", "09:15")
- [ ] **Given** I select 12-hour format, **when** viewing any time in the app, **then** times display as h:MM AM/PM (e.g., "2:30 PM", "9:15 AM")
- [ ] **Given** I change time format, **when** viewing the MSEL, **then** inject scheduled times update immediately
- [ ] **Given** I change time format, **when** viewing the exercise clock, **then** the clock display updates immediately
- [ ] **Given** 24-hour is selected, **when** entering times in forms, **then** time pickers accept 24-hour input
- [ ] **Given** 12-hour is selected, **when** entering times in forms, **then** time pickers show AM/PM selector
- [ ] **Given** I am a new user, **when** I first log in, **then** the default time format is 24-hour (EM standard)

---

## Out of Scope

- Date format preferences (separate story if needed)
- Timezone selection (exercises operate in local time)
- Seconds display (times shown to minute precision)

---

## Dependencies

- User authentication system
- User preferences storage
- S01: User Display Preferences (establishes settings panel pattern)

---

## Open Questions

- [ ] Should midnight display as "00:00" or "24:00" in 24-hour mode?
- [ ] For time input, should we support typing "1430" without colon and auto-format?
- [ ] Should we show timezone indicator anywhere (e.g., "14:30 EST")?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Military Time | 24-hour time format commonly used in emergency management and military operations |
| Inject Time | The scheduled time for an inject to be delivered during exercise conduct |
| Exercise Clock | The unified time display showing current exercise time |

---

## UI/UX Notes

### Time Format Setting

```
┌─────────────────────────────────────────────────────────────┐
│  User Settings                                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Time & Date                                                │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Time Format                                                │
│  ┌────────────────────────┐ ┌────────────────────────┐     │
│  │   24-hour (14:30)      │ │   12-hour (2:30 PM)    │     │
│  │   Military / Default   │ │   AM/PM                │     │
│  └────────────────────────┘ └────────────────────────┘     │
│             ●                          ○                    │
│                                                             │
│  Preview: Current time is 14:30                            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Time Display Examples

| Context | 24-hour | 12-hour |
|---------|---------|---------|
| Inject scheduled time | 14:30 | 2:30 PM |
| Exercise clock | 09:15 | 9:15 AM |
| Observation timestamp | 16:45 | 4:45 PM |
| Inject fired time | 14:32 | 2:32 PM |

### Time Picker Behavior

- 24-hour mode: Single time input, accepts "14:30" format
- 12-hour mode: Time input + AM/PM toggle

---

## Technical Notes

- Create a centralized `formatTime(date, userPreference)` utility
- All time displays should use this utility (no hardcoded formatting)
- Store preference as enum: `TimeFormat.TwentyFourHour | TimeFormat.TwelveHour`
- Time picker component should adapt based on preference
- Consider using date-fns or dayjs for consistent formatting

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 2
