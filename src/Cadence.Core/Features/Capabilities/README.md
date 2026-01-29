# Capabilities Feature

## Overview

The Capabilities feature allows organizations to define and manage the skills, functions, or competencies they want to evaluate during exercises. Capabilities support multiple frameworks including FEMA Core Capabilities, NATO Baseline Requirements, NIST Cybersecurity Framework, ISO 22301, and custom organizational capabilities.

## User Stories

### S01 - Create Capability Entity (COMPLETE)
- **Entity**: `Capability` at `Models/Entities/Capability.cs`
- **Properties**: Name, Description, Category, SortOrder, IsActive, SourceLibrary
- **Tests**: `CapabilityServiceTests.cs`

### S02 - CRUD Operations for Capabilities (COMPLETE)
- **Service**: `ICapabilityService` / `CapabilityService` at `Services/`
- **Controller**: `CapabilitiesController` at `Cadence.WebApi/Controllers/`
- **Endpoints**:
  - `GET /api/organizations/{orgId}/capabilities` - List capabilities
  - `GET /api/organizations/{orgId}/capabilities/{id}` - Get single capability
  - `POST /api/organizations/{orgId}/capabilities` - Create capability
  - `PUT /api/organizations/{orgId}/capabilities/{id}` - Update capability
  - `DELETE /api/organizations/{orgId}/capabilities/{id}` - Deactivate capability
  - `GET /api/organizations/{orgId}/capabilities/check-name` - Check name uniqueness
- **Tests**: `CapabilityServiceTests.cs`

### S03 - Import Predefined Capability Libraries (COMPLETE)
- **Service**: `IPredefinedLibraryProvider` / `PredefinedLibraryProvider` at `Services/`
- **Service**: `ICapabilityImportService` / `CapabilityImportService` at `Services/`
- **Data**: `Data/PredefinedCapabilityLibraries.json` (embedded resource)
- **Endpoints**:
  - `GET /api/organizations/{orgId}/capabilities/libraries` - List available libraries
  - `POST /api/organizations/{orgId}/capabilities/import` - Import library
- **Available Libraries**:
  - **FEMA** - 31 Core Capabilities across 5 mission areas (Prevention, Protection, Mitigation, Response, Recovery)
  - **NATO** - 7 Baseline Requirements for emergency preparedness
  - **NIST** - 6 Cybersecurity Framework 2.0 core functions (Govern, Identify, Protect, Detect, Respond, Recover)
  - **ISO** - 10 ISO 22301:2019 Business Continuity process areas
- **Tests**: `PredefinedLibraryProviderTests.cs`, `CapabilityImportServiceTests.cs`

## Architecture

### Namespace Convention
All code uses `Cadence.Core.Features.Capabilities.*` namespace:
- `Services/` - Business logic (interfaces and implementations)
- `Models/DTOs/` - Request/response types
- `Models/Entities/` - `Capability` entity (in `Cadence.Core.Models.Entities`)
- `Mappers/` - Entity ↔ DTO mapping
- `Data/` - Predefined library JSON data

### DI Registration
Services registered in `Cadence.Core.Extensions.ServiceCollectionExtensions`:
```csharp
services.AddScoped<ICapabilityService, CapabilityService>();
services.AddSingleton<IPredefinedLibraryProvider, PredefinedLibraryProvider>();
services.AddScoped<ICapabilityImportService, CapabilityImportService>();
```

### Data Model
The `Capability` entity:
- Scoped to `OrganizationId`
- Unique names per organization (case-insensitive)
- Optional `SourceLibrary` field tracks import origin
- Soft delete via `IsActive` flag (preserves historical data)

### Import Logic
`CapabilityImportService`:
1. Validates organization exists
2. Loads library from `PredefinedLibraryProvider`
3. Checks for duplicate names (case-insensitive)
4. Creates capabilities with `SourceLibrary` set
5. Returns counts: total, imported, skipped duplicates

## Testing

### Test Coverage
- **CapabilityServiceTests** - CRUD operations, validation, name uniqueness
- **PredefinedLibraryProviderTests** - Library loading, metadata, capability counts
- **CapabilityImportServiceTests** - Import logic, duplicate handling, validation

All tests follow TDD pattern: write tests first, implement to pass.

### Running Tests
```bash
# All capability tests
dotnet test --filter "FullyQualifiedName~Capabilities"

# Specific test class
dotnet test --filter "FullyQualifiedName~PredefinedLibraryProviderTests"
dotnet test --filter "FullyQualifiedName~CapabilityImportServiceTests"
```

## Usage Example

### Import FEMA Core Capabilities
```http
POST /api/organizations/{orgId}/capabilities/import
Content-Type: application/json

{
  "libraryName": "FEMA"
}

Response 200 OK:
{
  "totalInLibrary": 31,
  "imported": 31,
  "skippedDuplicates": 0,
  "importedNames": ["Planning", "Mass Care Services", ...]
}
```

### List Available Libraries
```http
GET /api/organizations/{orgId}/capabilities/libraries

Response 200 OK:
[
  {
    "id": "FEMA",
    "name": "FEMA Core Capabilities",
    "description": "32 FEMA Core Capabilities organized by 5 mission areas (HSEEP 2020)",
    "capabilityCount": 31
  },
  ...
]
```

## Future Enhancements
- S04: Tag observations with capabilities (in progress)
- S05: Set target capabilities for exercises
- S06: Generate capability performance reports
