# S10: Configurable Status Workflow

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P2  
**Points:** 8  
**Dependencies:** S00 (HSEEP Status Enum), S01 (Organization Approval Configuration)

## User Story

**As an** Administrator,  
**I want** to configure inject statuses, categories, and other dropdowns to match my organization's framework,  
**So that** the system uses terminology familiar to our team whether we follow HSEEP, DoD, NATO, or other standards.

## Context

Cross-domain research identified significant terminology variation across five major framework ecosystems:

| Framework | Status Workflow | Inject Categories | Role Types |
|-----------|-----------------|-------------------|------------|
| **HSEEP (Default)** | 8-state (Draft→Complete) | Inject, Contingency, Expected Action, Other | Controller, Evaluator, Simulator |
| **DoD/JTS** | JELC phases | Key, Enabling, Supporting | Exercise Control, Player, Observer |
| **NATO** | STARTEX/ENDEX phases | MEL entries | EXCON, DISTAFF, Players |
| **Cybersecurity** | Simplified 3-4 states | Technical, Management | Red, Blue, White, Purple Team |
| **Healthcare** | HSEEP + surge phases | HICS-aligned | ICS roles + medical |
| **Financial** | BC/DR focused | Process, Technical, Communication | Crisis Management Team |

**Key Insight:** HSEEP is the U.S. civilian standard, but organizations serving DoD, international, cybersecurity, healthcare, and financial sectors need different terminology. Making these configurable positions Cadence for broader market adoption.

## Acceptance Criteria

### Framework Template Selection
- [ ] **Given** I am an Admin setting up a new organization, **when** I view setup wizard, **then** I can select from pre-built framework templates
- [ ] **Given** template options, **when** displayed, **then** I see: HSEEP (default), DoD/JTS, NATO, UK Cabinet Office, Australian AIIMS, Cybersecurity, Healthcare, Financial, ISO 22301
- [ ] **Given** I select a template, **when** applied, **then** all configurable dropdowns are populated with that framework's terminology
- [ ] **Given** template is applied, **when** I view configured values, **then** I can still customize individual items

### Status Workflow Configuration
- [ ] **Given** I am an Admin, **when** I access Settings → Status Workflow, **then** I can view and edit inject status values
- [ ] **Given** status editor, **when** editing, **then** I can rename, reorder, add, or deactivate status values
- [ ] **Given** I deactivate a status, **when** it has existing injects, **then** those injects retain the status but it's unavailable for new items
- [ ] **Given** custom statuses, **when** editing, **then** I must maintain minimum workflow: Draft → (approval states) → Released/Complete
- [ ] **Given** I change status names, **when** users view injects, **then** they see the custom names in chips and dropdowns

### Inject Category Configuration
- [ ] **Given** I access Settings → Inject Categories, **when** viewing, **then** I see current categories with counts of injects using each
- [ ] **Given** category editor, **when** editing, **then** I can add/rename/reorder/deactivate categories
- [ ] **Given** categories, **when** configured, **then** they appear in inject create/edit forms as dropdown options
- [ ] **Given** HSEEP template, **when** applied, **then** categories are: Inject, Contingency Inject, Expected Action, Other
- [ ] **Given** DoD template, **when** applied, **then** categories are: Key Event, Enabling Event, Supporting Event

### Delivery Method Configuration
- [ ] **Given** I access Settings → Delivery Methods, **when** viewing, **then** I see available delivery methods
- [ ] **Given** HSEEP template, **when** applied, **then** methods are: Email, Phone, Radio, Fax, In-Person, SIMCELL
- [ ] **Given** Cybersecurity template, **when** applied, **then** methods include: Dashboard Alert, SIEM Notification, Chat/Slack, Email

### Role Type Configuration (Exercise-Scoped)
- [ ] **Given** I access Settings → Exercise Roles, **when** viewing, **then** I see configurable role types
- [ ] **Given** HSEEP template, **when** applied, **then** roles are: Controller, Evaluator, Simulator, Observer, Player
- [ ] **Given** NATO template, **when** applied, **then** roles are: EXCON, DISTAFF, Player, Observer
- [ ] **Given** Cybersecurity template, **when** applied, **then** roles include: Red Team, Blue Team, White Team, Purple Team

### Framework-Specific Fields (Future)
- [ ] **Given** Healthcare template, **when** applied, **then** inject form includes: HICS Function, Surge Level fields
- [ ] **Given** Cybersecurity template, **when** applied, **then** inject form includes: ATT&CK Tactic, ATT&CK Technique fields
- [ ] **Given** Financial template, **when** applied, **then** inject form includes: RTO Impact, MTD Category fields
- [ ] **Given** custom fields, **when** configured, **then** they appear in reports and exports

### Cross-Framework Mapping
- [ ] **Given** organization operates under multiple frameworks, **when** configured, **then** I can map custom statuses to standard export formats
- [ ] **Given** HSEEP export requested, **when** org uses custom statuses, **then** export maps to nearest HSEEP equivalent

## UI Design

### Framework Template Selection (Setup Wizard)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Organization Setup                                              Step 2/4   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Select your exercise framework                                             │
│  ─────────────────────────────────────────────────────────────────────────  │
│  This determines default terminology for statuses, categories, and roles.   │
│  You can customize these settings later.                                    │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ○ FEMA/HSEEP (Recommended for U.S. civilian)                        │   │
│  │   Standard for emergency management, required for FEMA grants       │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ Department of Defense (DoD/JTS)                                   │   │
│  │   Joint Training System terminology for military exercises          │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ NATO/Allied                                                       │   │
│  │   STANAG-aligned terminology for allied exercises                   │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ Cybersecurity (NIST/MITRE)                                        │   │
│  │   Includes ATT&CK mapping, Red/Blue team roles                      │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ Healthcare (CMS/Joint Commission)                                 │   │
│  │   HICS roles, HVA integration, surge level tracking                 │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ Financial Services (FFIEC/FINRA)                                  │   │
│  │   BC/DR focused with RTO/RPO tracking                               │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ ISO 22301 / BCI                                                   │   │
│  │   International business continuity standard                        │   │
│  ├─────────────────────────────────────────────────────────────────────┤   │
│  │ ○ Custom                                                            │   │
│  │   Start from scratch with blank configuration                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│                                           [Back]  [Continue]                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Status Workflow Configuration

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Settings > Status Workflow                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Framework: HSEEP  [Change Framework ▼]                                     │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Inject Statuses                               [+ Add Status]         │   │
│  ├──────┬───────────────┬─────────┬─────────┬───────────┬─────────────┤   │
│  │ ⋮⋮   │ Name          │ Phase   │ Color   │ In Use    │ Actions     │   │
│  ├──────┼───────────────┼─────────┼─────────┼───────────┼─────────────┤   │
│  │ ⋮⋮   │ Draft         │ Design  │ Gray    │ 24 injects│ [Edit]      │   │
│  │ ⋮⋮   │ Submitted     │ Review  │ Yellow  │ 8 injects │ [Edit]      │   │
│  │ ⋮⋮   │ Approved      │ Review  │ Green   │ 45 injects│ [Edit]      │   │
│  │ ⋮⋮   │ Synchronized  │ Ready   │ Blue    │ 32 injects│ [Edit]      │   │
│  │ ⋮⋮   │ Released      │ Conduct │ Purple  │ 18 injects│ [Edit]      │   │
│  │ ⋮⋮   │ Complete      │ Done    │ DkGreen │ 120 inject│ [Edit]      │   │
│  │ ⋮⋮   │ Deferred      │ Cancel  │ Orange  │ 5 injects │ [Edit]      │   │
│  │ ⋮⋮   │ Obsolete      │ Archive │ LtGray  │ 3 injects │ [Edit]      │   │
│  └──────┴───────────────┴─────────┴─────────┴───────────┴─────────────┘   │
│                                                                             │
│  ⋮⋮ = drag to reorder                                                       │
│                                                                             │
│  Workflow Visualization:                                                    │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ Draft → Submitted → Approved → Synchronized → Released → Complete  │    │
│  │                                     ↓                              │    │
│  │                                  Deferred                          │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Status Edit Dialog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Edit Status: Approved                                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Display Name*:                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Approved                                                             │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  Description:                                                               │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Director has reviewed and approved for use in exercise conduct       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│  Workflow Phase:          Color:                                            │
│  ┌─────────────────┐     ┌─────────────────┐                               │
│  │ Review       ▼  │     │ 🟢 Green     ▼  │                               │
│  └─────────────────┘     └─────────────────┘                               │
│                                                                             │
│  Icon:                    HSEEP Mapping:                                    │
│  ┌─────────────────┐     ┌─────────────────┐                               │
│  │ ✓ fa-check   ▼  │     │ Approved     ▼  │                               │
│  └─────────────────┘     └─────────────────┘                               │
│                                                                             │
│  ☐ Requires approval before transition                                      │
│  ☑ Available for MSEL export                                                │
│  ☐ Deactivated (hide from new injects)                                      │
│                                                                             │
│                                     [Cancel]  [Save Changes]                │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Framework Template Entity

```csharp
// File: src/Cadence.Core/Entities/FrameworkTemplate.cs

/// <summary>
/// Pre-built configuration template for exercise frameworks.
/// </summary>
public class FrameworkTemplate
{
    public string Id { get; set; } = string.Empty;  // "hseep", "dod", "nato", etc.
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    
    public List<StatusDefinition> Statuses { get; set; } = new();
    public List<CategoryDefinition> InjectCategories { get; set; } = new();
    public List<DeliveryMethodDefinition> DeliveryMethods { get; set; } = new();
    public List<RoleTypeDefinition> RoleTypes { get; set; } = new();
    public List<CustomFieldDefinition>? CustomFields { get; set; }
}

/// <summary>
/// Configurable status within a workflow.
/// </summary>
public class StatusDefinition
{
    public string Key { get; set; } = string.Empty;       // Internal identifier
    public string Name { get; set; } = string.Empty;      // Display name
    public string Description { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;     // Design, Review, Conduct, etc.
    public string Color { get; set; } = string.Empty;     // Hex or named color
    public string Icon { get; set; } = string.Empty;      // FontAwesome icon
    public int SortOrder { get; set; }
    public string? HseepMapping { get; set; }             // For exports
    public bool RequiresApproval { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
```

### Backend: Organization Configuration Entity

```csharp
// File: src/Cadence.Core/Entities/OrganizationConfiguration.cs

/// <summary>
/// Organization-specific configuration for dropdowns and workflows.
/// </summary>
public class OrganizationConfiguration : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    /// <summary>Base framework template ID.</summary>
    public string BaseTemplateId { get; set; } = "hseep";
    
    /// <summary>Customized status definitions (JSON).</summary>
    public string StatusesJson { get; set; } = "[]";
    
    /// <summary>Customized inject categories (JSON).</summary>
    public string CategoriesJson { get; set; } = "[]";
    
    /// <summary>Customized delivery methods (JSON).</summary>
    public string DeliveryMethodsJson { get; set; } = "[]";
    
    /// <summary>Customized role types (JSON).</summary>
    public string RoleTypesJson { get; set; } = "[]";
    
    /// <summary>Custom fields for sector-specific needs (JSON).</summary>
    public string? CustomFieldsJson { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Backend: Template Seed Data

```csharp
// File: src/Cadence.Core/Data/Seed/FrameworkTemplates.cs

public static class FrameworkTemplates
{
    public static FrameworkTemplate Hseep => new()
    {
        Id = "hseep",
        Name = "FEMA/HSEEP",
        Description = "Standard for U.S. civilian emergency management",
        IsDefault = true,
        Statuses = new List<StatusDefinition>
        {
            new() { Key = "draft", Name = "Draft", Phase = "Design", Color = "#9E9E9E", Icon = "fa-pencil", SortOrder = 1 },
            new() { Key = "submitted", Name = "Submitted", Phase = "Review", Color = "#FFC107", Icon = "fa-clock", SortOrder = 2, RequiresApproval = true },
            new() { Key = "approved", Name = "Approved", Phase = "Review", Color = "#4CAF50", Icon = "fa-check", SortOrder = 3 },
            new() { Key = "synchronized", Name = "Synchronized", Phase = "Ready", Color = "#2196F3", Icon = "fa-calendar-check", SortOrder = 4 },
            new() { Key = "released", Name = "Released", Phase = "Conduct", Color = "#9C27B0", Icon = "fa-paper-plane", SortOrder = 5 },
            new() { Key = "complete", Name = "Complete", Phase = "Done", Color = "#1B5E20", Icon = "fa-circle-check", SortOrder = 6 },
            new() { Key = "deferred", Name = "Deferred", Phase = "Cancel", Color = "#FF9800", Icon = "fa-ban", SortOrder = 7 },
            new() { Key = "obsolete", Name = "Obsolete", Phase = "Archive", Color = "#BDBDBD", Icon = "fa-archive", SortOrder = 8 }
        },
        InjectCategories = new List<CategoryDefinition>
        {
            new() { Key = "inject", Name = "Inject" },
            new() { Key = "contingency", Name = "Contingency Inject" },
            new() { Key = "expected_action", Name = "Expected Action" },
            new() { Key = "other", Name = "Other" }
        },
        DeliveryMethods = new List<DeliveryMethodDefinition>
        {
            new() { Key = "email", Name = "Email" },
            new() { Key = "phone", Name = "Phone" },
            new() { Key = "radio", Name = "Radio" },
            new() { Key = "fax", Name = "Fax" },
            new() { Key = "in_person", Name = "In-Person" },
            new() { Key = "simcell", Name = "SIMCELL" }
        },
        RoleTypes = new List<RoleTypeDefinition>
        {
            new() { Key = "controller", Name = "Controller" },
            new() { Key = "evaluator", Name = "Evaluator" },
            new() { Key = "simulator", Name = "Simulator" },
            new() { Key = "observer", Name = "Observer" },
            new() { Key = "player", Name = "Player" }
        }
    };
    
    public static FrameworkTemplate Cybersecurity => new()
    {
        Id = "cybersecurity",
        Name = "Cybersecurity (NIST/MITRE)",
        Description = "Cyber exercises with ATT&CK mapping and team roles",
        Statuses = new List<StatusDefinition>
        {
            new() { Key = "draft", Name = "Draft", Phase = "Design", Color = "#9E9E9E", SortOrder = 1, HseepMapping = "Draft" },
            new() { Key = "ready", Name = "Ready", Phase = "Ready", Color = "#4CAF50", SortOrder = 2, HseepMapping = "Approved" },
            new() { Key = "injected", Name = "Injected", Phase = "Conduct", Color = "#9C27B0", SortOrder = 3, HseepMapping = "Released" },
            new() { Key = "complete", Name = "Complete", Phase = "Done", Color = "#1B5E20", SortOrder = 4, HseepMapping = "Complete" }
        },
        InjectCategories = new List<CategoryDefinition>
        {
            new() { Key = "technical", Name = "Technical Inject" },
            new() { Key = "management", Name = "Management Inject" },
            new() { Key = "communications", Name = "Communications" }
        },
        DeliveryMethods = new List<DeliveryMethodDefinition>
        {
            new() { Key = "dashboard", Name = "Dashboard Alert" },
            new() { Key = "siem", Name = "SIEM Notification" },
            new() { Key = "email", Name = "Email" },
            new() { Key = "chat", Name = "Chat/Slack" },
            new() { Key = "phone", Name = "Phone" }
        },
        RoleTypes = new List<RoleTypeDefinition>
        {
            new() { Key = "red_team", Name = "Red Team" },
            new() { Key = "blue_team", Name = "Blue Team" },
            new() { Key = "white_team", Name = "White Team" },
            new() { Key = "purple_team", Name = "Purple Team" },
            new() { Key = "observer", Name = "Observer" }
        },
        CustomFields = new List<CustomFieldDefinition>
        {
            new() { Key = "attack_tactic", Name = "ATT&CK Tactic", Type = "select", Options = "Reconnaissance,Resource Development,Initial Access,Execution,Persistence,Privilege Escalation,Defense Evasion,Credential Access,Discovery,Lateral Movement,Collection,Command and Control,Exfiltration,Impact" },
            new() { Key = "attack_technique", Name = "ATT&CK Technique", Type = "text" }
        }
    };
    
    // Additional templates: DoD, NATO, Healthcare, Financial, etc.
}
```

### Frontend: Framework Selector Component

```tsx
// File: src/frontend/src/components/settings/FrameworkSelector.tsx

interface FrameworkSelectorProps {
  selectedTemplate: string;
  onSelect: (templateId: string) => void;
}

export const FrameworkSelector: React.FC<FrameworkSelectorProps> = ({
  selectedTemplate,
  onSelect,
}) => {
  const { templates } = useFrameworkTemplates();
  
  return (
    <FormControl fullWidth>
      <FormLabel>Exercise Framework</FormLabel>
      <RadioGroup value={selectedTemplate} onChange={(e) => onSelect(e.target.value)}>
        {templates.map((template) => (
          <Paper 
            key={template.id}
            variant="outlined" 
            sx={{ 
              p: 2, 
              mb: 1,
              borderColor: selectedTemplate === template.id ? 'primary.main' : 'divider',
              borderWidth: selectedTemplate === template.id ? 2 : 1
            }}
          >
            <FormControlLabel
              value={template.id}
              control={<Radio />}
              label={
                <Box>
                  <Typography fontWeight={600}>
                    {template.name}
                    {template.isDefault && (
                      <Chip label="Recommended" size="small" sx={{ ml: 1 }} />
                    )}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {template.description}
                  </Typography>
                </Box>
              }
            />
          </Paper>
        ))}
      </RadioGroup>
    </FormControl>
  );
};
```

### API Endpoints

```csharp
// File: src/Cadence.Core/Controllers/ConfigurationController.cs

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfigurationController : ControllerBase
{
    /// <summary>
    /// Gets available framework templates.
    /// </summary>
    [HttpGet("templates")]
    public ActionResult<List<FrameworkTemplate>> GetTemplates()
    {
        return Ok(_templateService.GetAllTemplates());
    }
    
    /// <summary>
    /// Gets organization's current configuration.
    /// </summary>
    [HttpGet("organizations/{orgId}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<OrganizationConfigurationDto>> GetOrgConfig(Guid orgId)
    {
        var config = await _configService.GetOrCreateAsync(orgId);
        return Ok(_mapper.Map<OrganizationConfigurationDto>(config));
    }
    
    /// <summary>
    /// Applies a framework template to organization.
    /// </summary>
    [HttpPost("organizations/{orgId}/apply-template")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<OrganizationConfigurationDto>> ApplyTemplate(
        Guid orgId, 
        [FromBody] ApplyTemplateRequest request)
    {
        var config = await _configService.ApplyTemplateAsync(orgId, request.TemplateId);
        return Ok(_mapper.Map<OrganizationConfigurationDto>(config));
    }
    
    /// <summary>
    /// Updates organization's status workflow configuration.
    /// </summary>
    [HttpPut("organizations/{orgId}/statuses")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateStatuses(
        Guid orgId,
        [FromBody] List<StatusDefinition> statuses)
    {
        await _configService.UpdateStatusesAsync(orgId, statuses);
        return NoContent();
    }
    
    /// <summary>
    /// Updates organization's inject categories.
    /// </summary>
    [HttpPut("organizations/{orgId}/categories")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateCategories(
        Guid orgId,
        [FromBody] List<CategoryDefinition> categories)
    {
        await _configService.UpdateCategoriesAsync(orgId, categories);
        return NoContent();
    }
}
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task ApplyTemplate_Hseep_ConfiguresEightStatuses()
{
    // Arrange
    var orgId = await CreateOrganization();
    
    // Act
    var config = await _service.ApplyTemplateAsync(orgId, "hseep");
    
    // Assert
    var statuses = JsonSerializer.Deserialize<List<StatusDefinition>>(config.StatusesJson);
    Assert.Equal(8, statuses.Count);
    Assert.Contains(statuses, s => s.Key == "draft");
    Assert.Contains(statuses, s => s.Key == "synchronized");
}

[Fact]
public async Task ApplyTemplate_Cybersecurity_ConfiguresFourStatuses()
{
    // Arrange
    var orgId = await CreateOrganization();
    
    // Act
    var config = await _service.ApplyTemplateAsync(orgId, "cybersecurity");
    
    // Assert
    var statuses = JsonSerializer.Deserialize<List<StatusDefinition>>(config.StatusesJson);
    Assert.Equal(4, statuses.Count);
    Assert.Contains(statuses, s => s.Key == "injected"); // Cyber-specific
}

[Fact]
public async Task CustomStatus_MapsToHseepForExport()
{
    // Arrange
    var orgId = await CreateOrganization();
    await _service.ApplyTemplateAsync(orgId, "cybersecurity");
    
    // Act
    var config = await _service.GetOrCreateAsync(orgId);
    var statuses = JsonSerializer.Deserialize<List<StatusDefinition>>(config.StatusesJson);
    var injectedStatus = statuses.First(s => s.Key == "injected");
    
    // Assert
    Assert.Equal("Released", injectedStatus.HseepMapping);
}

[Fact]
public async Task DeactivateStatus_WithExistingInjects_RetainsOnInjects()
{
    // Arrange
    var orgId = await CreateOrganization();
    var inject = await CreateInjectWithStatus(orgId, "submitted");
    
    // Act
    await _service.DeactivateStatusAsync(orgId, "submitted");
    
    // Assert
    var reloadedInject = await _context.Injects.FindAsync(inject.Id);
    Assert.Equal("submitted", reloadedInject.StatusKey); // Retained
    
    var config = await _service.GetOrCreateAsync(orgId);
    var statuses = JsonSerializer.Deserialize<List<StatusDefinition>>(config.StatusesJson);
    var submittedStatus = statuses.First(s => s.Key == "submitted");
    Assert.False(submittedStatus.IsActive); // Deactivated
}
```

## Out of Scope

- Visual workflow builder (drag-and-drop status transitions)
- Per-exercise framework override (org-level only for MVP)
- Import/export of custom configurations
- Marketplace for community-shared templates

## Definition of Done

- [ ] Framework template seed data for all 8+ templates
- [ ] OrganizationConfiguration entity and migration
- [ ] Template selection in org setup wizard
- [ ] Settings UI for status workflow editing
- [ ] Settings UI for category editing
- [ ] Settings UI for delivery method editing
- [ ] Settings UI for role type editing
- [ ] Inject forms respect org configuration
- [ ] Status chips render with configured colors/icons
- [ ] HSEEP mapping for exports maintained
- [ ] API endpoints for all configuration operations
- [ ] Unit tests for template application
- [ ] Frontend component tests
- [ ] Documentation for extending with custom templates

## References

- [Cross-Domain Research: Inject Status Configurability](../../research/inject-status-cross-domain-analysis.md)
- [FEMA PrepToolkit Documentation](https://preptoolkit.fema.gov)
- [NIST SP 800-84: IT Security Testing and Exercises](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-84.pdf)
- [MITRE ATT&CK Framework](https://attack.mitre.org)
