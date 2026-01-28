# Story: Import Predefined Capability Libraries

**Feature:** Exercise Capabilities  
**Story ID:** S03  
**Priority:** P0 (MVP)  
**Phase:** Standard Implementation

---

## User Story

**As an** Administrator,  
**I want** to import predefined capability libraries with one click,  
**So that** I can quickly set up industry-standard capability tracking without manual data entry.

---

## Context

Different organizations use different capability frameworks depending on their sector, geographic location, and regulatory requirements. Rather than forcing all users to adopt FEMA's 32 Core Capabilities, Cadence provides multiple predefined libraries that can be imported on demand:

| Library | Capabilities | Primary Audience |
|---------|--------------|------------------|
| **FEMA Core Capabilities** | 32 | US Emergency Management, CISA, TSA |
| **NATO Baseline Requirements** | 7 | International/Allied civil preparedness |
| **NIST Cybersecurity Framework** | 6 | Cybersecurity exercises |
| **ISO 22301 Process Areas** | 10 | Business continuity exercises |

Organizations can import multiple libraries if needed (e.g., FEMA + custom capabilities) or create a fully custom library. The import process checks for duplicates and skips capabilities that already exist with the same name.

---

## Acceptance Criteria

- [ ] **Given** the Capability Library page, **when** I click "Import", **then** I see a dropdown/menu with predefined library options
- [ ] **Given** the import menu, **when** I select "FEMA Core Capabilities", **then** a confirmation dialog shows library details (32 capabilities, 5 mission areas)
- [ ] **Given** the import menu, **when** I select "NATO Baseline Requirements", **then** a confirmation dialog shows library details (7 capabilities)
- [ ] **Given** the import menu, **when** I select "NIST Cybersecurity Framework", **then** a confirmation dialog shows library details (6 functions)
- [ ] **Given** the import menu, **when** I select "ISO 22301 Process Areas", **then** a confirmation dialog shows library details (10 process areas)
- [ ] **Given** the confirmation dialog, **when** I click "Import", **then** capabilities are created with correct names, descriptions, and categories
- [ ] **Given** the import process, **when** a capability name already exists in my library, **then** it is skipped (not duplicated)
- [ ] **Given** the import completes, **when** some capabilities were skipped, **then** toast shows "Imported X capabilities (Y skipped as duplicates)"
- [ ] **Given** the import completes, **when** all capabilities are new, **then** toast shows "Imported X capabilities"
- [ ] **Given** imported capabilities, **when** viewing the list, **then** SourceLibrary field shows origin (FEMA, NATO, NIST, ISO)
- [ ] **Given** imported capabilities, **when** editing, **then** all fields are editable (user can customize)
- [ ] **Given** the API, **when** `POST /api/organizations/{orgId}/capabilities/import` is called with library name, **then** appropriate capabilities are created

---

## Out of Scope

- CSV import for custom libraries
- Automatic updates when predefined libraries change
- Undo/rollback of import
- Library version tracking

---

## Dependencies

- S01: Capability Entity and API
- S02: Capability Library Admin UI
- Predefined library data file (see predefined-capability-libraries.md)

---

## Open Questions

- [x] Should duplicate detection be case-sensitive? **No, case-insensitive**
- [x] Can users import the same library multiple times? **Yes, but duplicates are skipped**
- [ ] Should we provide a "preview" of what will be imported? **Defer - show count in confirmation**

---

## Domain Terms

| Term | Definition |
|------|------------|
| Predefined Library | A curated set of capabilities from a recognized framework (FEMA, NATO, NIST, ISO) |
| Source Library | Tag indicating which predefined library a capability originated from |
| Duplicate | A capability with the same name (case-insensitive) already exists in the organization |

---

## UI/UX Notes

### Import Menu

```
┌─────────────────────────────────────────┐
│  Import ▼                               │
├─────────────────────────────────────────┤
│  📋 FEMA Core Capabilities              │
│     32 capabilities • US Emergency Mgmt │
├─────────────────────────────────────────┤
│  🌐 NATO Baseline Requirements          │
│     7 capabilities • Allied Resilience  │
├─────────────────────────────────────────┤
│  🔒 NIST Cybersecurity Framework        │
│     6 capabilities • Cyber Exercises    │
├─────────────────────────────────────────┤
│  📊 ISO 22301 Process Areas             │
│     10 capabilities • Business Cont.    │
└─────────────────────────────────────────┘
```

### Import Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────┐
│  IMPORT FEMA CORE CAPABILITIES                          [X] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  📋 FEMA Core Capabilities                                  │
│                                                             │
│  This will import 32 capabilities organized into            │
│  5 mission areas from the National Preparedness Goal:       │
│                                                             │
│    • Prevention (7 capabilities)                            │
│    • Protection (5 capabilities)                            │
│    • Mitigation (4 capabilities)                            │
│    • Response (15 capabilities)                             │
│    • Recovery (4 capabilities)                              │
│                                                             │
│  Note: Planning, Public Information and Warning, and        │
│  Operational Coordination span multiple mission areas       │
│  but will only be imported once.                            │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  ⚠️ Capabilities with duplicate names will be skipped       │
│                                                             │
│                              [Cancel]  [Import 32 Items]    │
└─────────────────────────────────────────────────────────────┘
```

### Success Toast Examples

```
✓ Imported 32 capabilities from FEMA Core Capabilities

✓ Imported 28 capabilities (4 skipped as duplicates)
```

---

## Technical Notes

### API Endpoint

```csharp
[HttpPost("import")]
public async Task<ActionResult<ImportResult>> ImportLibrary(
    [FromRoute] Guid organizationId,
    [FromBody] ImportLibraryRequest request)
{
    // request.LibraryName = "FEMA" | "NATO" | "NIST" | "ISO"
}

public record ImportLibraryRequest(string LibraryName);

public record ImportResult(
    int TotalInLibrary,
    int Imported,
    int SkippedDuplicates
);
```

### Library Data Location

Library definitions stored as embedded JSON resource:
`src/Cadence.Core/Data/PredefinedCapabilityLibraries.json`

See: [predefined-capability-libraries.md](./predefined-capability-libraries.md)

### Import Service

```csharp
public class CapabilityImportService
{
    private readonly ICapabilityRepository _repository;
    private readonly IPredefinedLibraryProvider _libraryProvider;
    
    public async Task<ImportResult> ImportLibraryAsync(
        Guid organizationId, 
        string libraryName)
    {
        var libraryCapabilities = _libraryProvider.GetLibrary(libraryName);
        var existingNames = await _repository
            .GetCapabilityNamesAsync(organizationId);
        
        var toImport = libraryCapabilities
            .Where(c => !existingNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
        
        foreach (var cap in toImport)
        {
            await _repository.CreateAsync(new Capability
            {
                OrganizationId = organizationId,
                Name = cap.Name,
                Description = cap.Description,
                Category = cap.Category,
                SortOrder = cap.SortOrder,
                SourceLibrary = libraryName,
                IsActive = true
            });
        }
        
        return new ImportResult(
            TotalInLibrary: libraryCapabilities.Count,
            Imported: toImport.Count,
            SkippedDuplicates: libraryCapabilities.Count - toImport.Count
        );
    }
}
```

### Frontend Component

```typescript
// ImportLibraryMenu.tsx
const LIBRARIES = [
  {
    id: 'FEMA',
    name: 'FEMA Core Capabilities',
    description: 'US Emergency Management',
    count: 32,
    icon: '📋'
  },
  {
    id: 'NATO',
    name: 'NATO Baseline Requirements',
    description: 'Allied Resilience',
    count: 7,
    icon: '🌐'
  },
  {
    id: 'NIST',
    name: 'NIST Cybersecurity Framework',
    description: 'Cyber Exercises',
    count: 6,
    icon: '🔒'
  },
  {
    id: 'ISO',
    name: 'ISO 22301 Process Areas',
    description: 'Business Continuity',
    count: 10,
    icon: '📊'
  }
];
```

---

## Estimation

**T-Shirt Size:** M  
**Story Points:** 5

---

## Testing Requirements

### Unit Tests
- [ ] Library provider returns correct capabilities for each library
- [ ] Duplicate detection is case-insensitive
- [ ] Import result counts are accurate

### Integration Tests
- [ ] Import creates capabilities with correct data
- [ ] Duplicates are properly skipped
- [ ] SourceLibrary is set correctly
- [ ] Multiple imports of same library don't create duplicates

### E2E Tests
- [ ] Import FEMA library from empty state
- [ ] Import second library with some duplicates
- [ ] Verify all capabilities appear in list
