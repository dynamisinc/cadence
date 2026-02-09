# _core/S01: Error Reporting

## Story

**As a** user who encounters an application error,
**I want** to send an error report to the support team,
**So that** the development team can diagnose and fix issues I encounter.

## Context

When users encounter unexpected errors in the application, the ErrorBoundary component displays a friendly error page. Basic error reporting was implemented as part of EM-08-S01 (Bug Report Submission): the "Send Error Report" button is enabled and submits error details (error message, stack trace, component stack, URL, browser info) to `POST /api/feedback/error-report`, which emails the support team using the BugReport template and returns a reference number.

This story covers enhancements to that baseline:
1. Adding a modal for user context (what they were doing, contact email)
2. Storing error reports in a dedicated database table
3. Application Insights correlation
4. Rate limiting and offline clipboard fallback

## Acceptance Criteria

### Report Submission

- [ ] **Given** an error occurs and the ErrorBoundary is displayed, **when** I click "Send Error Report", **then** a modal opens allowing me to provide additional context
- [ ] **Given** the report modal is open, **when** I view the form, **then** I see fields for: Description of what I was doing (optional textarea), Email for follow-up (optional, pre-filled if logged in)
- [ ] **Given** the report modal is open, **when** I view the form, **then** I see a preview of the technical details that will be sent (error message, component stack, browser info)
- [ ] **Given** I submit the report, **when** the submission succeeds, **then** I see a success message "Report sent. Thank you for helping us improve!"
- [ ] **Given** I submit the report, **when** the submission fails, **then** I see an error message with option to copy details manually
- [ ] **Given** I click "Send Error Report", **when** I change my mind, **then** I can close the modal without sending

### Report Content

- [ ] **Given** a report is submitted, **when** it is processed, **then** it includes: Error message, Component stack trace, Browser user agent, Current URL/route, Timestamp (UTC), User ID (if authenticated), Organization ID (if in org context), App version
- [ ] **Given** a report is submitted, **when** it is stored, **then** PII is minimized (no passwords, tokens, or sensitive form data)
- [ ] **Given** I provide a description, **when** the report is sent, **then** my description is included with the technical details

### Backend Processing

- [ ] **Given** a report is received, **when** it is processed, **then** it is stored in a dedicated error reports table
- [ ] **Given** a report is received, **when** Application Insights is configured, **then** the report is also sent to App Insights with correlation ID
- [ ] **Given** reports are stored, **when** an admin views the admin panel, **then** they can see a list of recent error reports (future admin feature)

### Offline Handling

- [ ] **Given** I am offline when submitting a report, **when** the submission fails, **then** I am shown an option to copy the report details to clipboard
- [ ] **Given** I am offline, **when** I click "Copy Report", **then** all report details are copied in a formatted text block

### Rate Limiting

- [ ] **Given** I submit an error report, **when** I try to submit another within 1 minute, **then** I am told to wait before submitting again
- [ ] **Given** a user submits many reports, **when** they exceed 10 reports per hour, **then** additional reports are silently dropped (but logged)

## Out of Scope

- Admin dashboard for viewing error reports (separate story)
- Email notifications to support team (can use App Insights alerts)
- Automatic screenshot capture
- Session replay or user action history
- Integration with external ticketing systems (Jira, GitHub Issues)

## Dependencies

- ErrorBoundary component (implemented)
- Application Insights telemetry (implemented)
- Backend API endpoint for error reports (implemented — `POST /api/feedback/error-report` via EM-08-S01)

## API Design

### POST /api/error-reports

**Request Body:**
```json
{
  "errorMessage": "TypeError: Cannot read property 'foo' of undefined",
  "componentStack": "at UserProfile\n    at Dashboard\n    at App",
  "userDescription": "I was trying to update my profile picture",
  "userEmail": "user@example.com",
  "browserInfo": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0",
  "currentUrl": "/exercises/123/msel",
  "appVersion": "1.2.3",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

**Response (201 Created):**
```json
{
  "reportId": "550e8400-e29b-41d4-a716-446655440000",
  "message": "Report submitted successfully"
}
```

**Response (429 Too Many Requests):**
```json
{
  "error": "Rate limit exceeded. Please wait before submitting another report."
}
```

## Data Model

### ErrorReport Entity

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Primary key |
| ErrorMessage | string | The error message/exception |
| ComponentStack | string? | React component stack trace |
| UserDescription | string? | User-provided context |
| UserEmail | string? | Contact email for follow-up |
| UserId | Guid? | Authenticated user ID (if logged in) |
| OrganizationId | Guid? | User's organization (if in context) |
| BrowserInfo | string | User agent string |
| CurrentUrl | string | URL where error occurred |
| AppVersion | string | Frontend app version |
| CreatedAt | DateTime | Timestamp (UTC) |
| CorrelationId | string? | App Insights correlation ID |
| Status | enum | New, Reviewed, Resolved, Duplicate |

## UI/UX Notes

- Modal should be simple and not overwhelming
- Pre-fill email if user is logged in
- Show clear indication that technical details will be shared
- Provide "What were you doing?" prompt to encourage useful descriptions
- Success state should feel appreciative ("Thank you for helping us improve!")
- Consider adding a reference number so users can follow up

## Technical Notes

- Use existing telemetry service for App Insights correlation
- Error reports should not block the user from recovering
- Consider using a dedicated Azure Function for processing (scale to zero)
- Implement client-side rate limiting in addition to server-side
- Strip any sensitive data patterns (tokens, passwords) before submission

## Security Considerations

- Sanitize all user input before storage
- Do not log or store any authentication tokens
- Rate limiting prevents abuse
- Consider CAPTCHA for unauthenticated submissions (future)
- Error reports should not expose internal system details to users

## Open Questions

- [ ] Should we require authentication to submit reports, or allow anonymous?
- [ ] Should reports be automatically linked to App Insights exceptions?
- [ ] Do we need a follow-up workflow (email user when resolved)?
- [ ] Should we expose a public status page for known issues?

## Domain Terms

| Term | Definition |
|------|------------|
| ErrorBoundary | React component that catches JavaScript errors in child components |
| Component Stack | Trace showing the React component hierarchy where error occurred |
| Correlation ID | Unique identifier linking related telemetry events |
| Rate Limiting | Restricting the number of requests a user can make in a time period |
