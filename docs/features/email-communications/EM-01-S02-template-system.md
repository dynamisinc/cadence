# Story: EM-01-S02 - Email Template System

**As a** System,  
**I want** a template engine for composing branded emails,  
**So that** all Cadence emails have a consistent, professional appearance.

## Context

Email templates ensure consistent branding across all communications while enabling dynamic content insertion. Templates support both HTML (for rich display) and plain text (for accessibility and spam filter compatibility). Organization branding can be injected to personalize emails for each organization.

## Acceptance Criteria

### Template Structure

- [ ] **Given** a template exists, **when** rendered, **then** it produces both HTML and plain text versions
- [ ] **Given** HTML template, **when** rendered, **then** it includes standard header with logo, footer with unsubscribe link
- [ ] **Given** any template, **when** rendered, **then** dynamic content placeholders are replaced with actual values
- [ ] **Given** a placeholder has no value, **when** rendered, **then** it shows empty string (not placeholder text)

### Organization Branding

- [ ] **Given** organization has logo URL configured, **when** rendering template, **then** organization logo appears in header
- [ ] **Given** organization has no logo, **when** rendering template, **then** default Cadence logo is used
- [ ] **Given** organization has primary color configured, **when** rendering template, **then** accent colors reflect organization branding
- [ ] **Given** organization name, **when** rendering template, **then** organization name appears in appropriate locations

### Template Management

- [ ] **Given** a template ID, **when** requested, **then** the correct template is loaded
- [ ] **Given** an invalid template ID, **when** requested, **then** a clear error is thrown
- [ ] **Given** templates exist, **when** app starts, **then** templates are validated for required placeholders

### Responsive Design

- [ ] **Given** HTML email, **when** viewed on mobile device, **then** content is readable without horizontal scrolling
- [ ] **Given** HTML email, **when** viewed in dark mode email client, **then** content remains readable
- [ ] **Given** any email client, **when** images are blocked, **then** alt text conveys essential information

## Out of Scope

- Admin UI for editing templates (code-based for MVP)
- Template versioning/history
- A/B testing variations
- Localization/multi-language templates

## Dependencies

- EM-01-S01: ACS Email Configuration (templates are rendered then sent)

## Technical Notes

### Template Structure

```
/Templates/Email/
  ├── _Layout.html           # Shared wrapper (header, footer)
  ├── _Layout.txt            # Plain text equivalent
  ├── OrganizationInvite.html
  ├── OrganizationInvite.txt
  ├── PasswordReset.html
  ├── PasswordReset.txt
  └── ...
```

### Template Model Example

```csharp
public class OrganizationInviteModel
{
    public string RecipientName { get; set; }
    public string OrganizationName { get; set; }
    public string InviterName { get; set; }
    public string InviteUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    
    // Branding (injected automatically)
    public string LogoUrl { get; set; }
    public string PrimaryColor { get; set; }
}
```

### ITemplateRenderer Interface

```csharp
public interface IEmailTemplateRenderer
{
    Task<RenderedEmail> RenderAsync<TModel>(string templateId, TModel model);
}

public record RenderedEmail(
    string Subject,
    string HtmlBody,
    string PlainTextBody
);
```

### HTML Template Example

```html
<!-- OrganizationInvite.html -->
@model OrganizationInviteModel

@{ Layout = "_Layout.html"; }

<h1 style="color: @Model.PrimaryColor;">You're Invited!</h1>

<p>Hi @Model.RecipientName,</p>

<p>
  <strong>@Model.InviterName</strong> has invited you to join 
  <strong>@Model.OrganizationName</strong> on Cadence.
</p>

<p>
  <a href="@Model.InviteUrl" style="background: @Model.PrimaryColor; 
     color: white; padding: 12px 24px; text-decoration: none; 
     border-radius: 4px; display: inline-block;">
    Accept Invitation
  </a>
</p>

<p style="color: #666; font-size: 14px;">
  This invitation expires on @Model.ExpiresAt.ToString("MMMM d, yyyy").
</p>
```

### Plain Text Template Example

```text
<!-- OrganizationInvite.txt -->
@model OrganizationInviteModel

You're Invited to @Model.OrganizationName!

Hi @Model.RecipientName,

@Model.InviterName has invited you to join @Model.OrganizationName on Cadence.

Accept your invitation: @Model.InviteUrl

This invitation expires on @Model.ExpiresAt.ToString("MMMM d, yyyy").

---
Cadence - Exercise Management Platform
```

## UI/UX Notes

### Email Design Guidelines

- Maximum width: 600px (email client compatibility)
- Primary font: System fonts (Arial, Helvetica, sans-serif)
- Button style: Rounded corners, high contrast, minimum 44px touch target
- Footer: Always include unsubscribe link and organization address
- Images: Always include alt text, use absolute URLs

### Accessibility

- Minimum contrast ratio: 4.5:1 for body text
- Semantic HTML where supported
- Plain text version must convey all essential information
- No information conveyed by color alone

## Domain Terms

| Term | Definition |
|------|------------|
| Template | Reusable email structure with placeholders for dynamic content |
| Branding | Organization-specific visual elements (logo, colors) |
| Plain Text Fallback | Non-HTML version for accessibility and spam filter compliance |

## Open Questions

- [ ] Should templates be embedded resources or file-based?
- [ ] Do we need a preview endpoint for testing templates during development?

## Effort Estimate

**5 story points** - Template engine setup, multiple template types, branding injection

---

*Feature: EM-01 Email Infrastructure*  
*Priority: P0*
