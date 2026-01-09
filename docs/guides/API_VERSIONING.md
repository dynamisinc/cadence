# API Versioning Guide

> **Status:** Reference Guide - Not Yet Implemented in Template
> **Priority:** High for Production

This guide explains how to implement API versioning for the Cadence.

---

## Table of Contents

1. [Overview](#overview)
2. [Versioning Strategies](#versioning-strategies)
3. [Implementation](#implementation)
4. [Frontend Integration](#frontend-integration)
5. [Deprecation Policy](#deprecation-policy)
6. [Testing](#testing)

---

## Overview

### Why Version Your API?

- **Breaking changes** - Evolve API without breaking existing clients
- **Multiple clients** - Support mobile apps, web apps, third-party integrations
- **Gradual migration** - Allow clients to upgrade on their schedule
- **A/B testing** - Test new API behavior with subset of users

### Current State

All endpoints are unversioned:
```
GET /api/notes
POST /api/notes
```

### Target State

Versioned endpoints:
```
GET /api/v1/notes
POST /api/v1/notes
GET /api/v2/notes  (future)
```

---

## Versioning Strategies

### Option 1: URL Path Versioning (Recommended)

```
/api/v1/notes
/api/v2/notes
```

**Pros:**
- Most explicit and visible
- Easy to test and debug
- Clear in logs and documentation
- Cache-friendly

**Cons:**
- Changes the URL structure
- Harder to do gradual rollouts

### Option 2: Header Versioning

```
GET /api/notes
X-API-Version: 1
```

**Pros:**
- Clean URLs
- Easy to change version

**Cons:**
- Hidden from URL
- Harder to test in browser
- Often forgotten by clients

### Option 3: Query Parameter Versioning

```
GET /api/notes?version=1
```

**Pros:**
- Easy to implement
- Visible in URL

**Cons:**
- Pollutes query string
- Optional parameter issues

### Recommendation

**Use URL path versioning** (`/api/v1/...`) for clarity and simplicity.

---

## Implementation

### Step 1: Create Version Constants

Create `src/Cadence.Core/Versioning/ApiVersions.cs`:

```csharp
namespace Cadence.Core.Versioning;

/// <summary>
/// API version constants for use in route definitions.
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Current stable API version.
    /// </summary>
    public const string V1 = "v1";

    /// <summary>
    /// Preview/beta API version (when applicable).
    /// </summary>
    public const string V2 = "v2";

    /// <summary>
    /// Default version for clients that don't specify.
    /// </summary>
    public const string Default = V1;
}

/// <summary>
/// Route prefixes for versioned endpoints.
/// </summary>
public static class ApiRoutes
{
    public const string V1Prefix = "v1";
    public const string V2Prefix = "v2";

    public static class Notes
    {
        public const string Base = "notes";
        public const string V1 = $"{V1Prefix}/{Base}";
        public const string V2 = $"{V2Prefix}/{Base}";
    }

    public static class Health
    {
        public const string Ping = "ping";
        public const string Check = "health";
    }
}
```

### Step 2: Update Function Routes

Update `src/Cadence.Functions/Functions/NotesFunction.cs`:

```csharp
using Cadence.Core.Versioning;

namespace Cadence.Functions.Functions;

/// <summary>
/// V1 Notes API endpoints.
/// </summary>
public class NotesFunctionV1
{
    private readonly INotesService _notesService;
    private readonly ILogger<NotesFunctionV1> _logger;

    public NotesFunctionV1(INotesService notesService, ILogger<NotesFunctionV1> logger)
    {
        _notesService = notesService;
        _logger = logger;
    }

    [Function("GetNotes_V1")]
    public async Task<IActionResult> GetNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Notes.V1)]
        HttpRequest req)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("[V1] GetNotes called by user {UserId}", userId);

        var notes = await _notesService.GetNotesAsync(userId);
        return new OkObjectResult(notes);
    }

    [Function("GetNoteById_V1")]
    public async Task<IActionResult> GetNoteById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Notes.V1 + "/{id:guid}")]
        HttpRequest req,
        Guid id)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("[V1] GetNoteById called for {NoteId} by user {UserId}", id, userId);

        var note = await _notesService.GetNoteByIdAsync(id, userId);

        if (note == null)
        {
            return new NotFoundObjectResult(new { message = "Note not found" });
        }

        return new OkObjectResult(note);
    }

    [Function("CreateNote_V1")]
    public async Task<NoteWithSignalROutput> CreateNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Notes.V1)]
        HttpRequest req,
        FunctionContext context)
    {
        // ... existing implementation
    }

    [Function("UpdateNote_V1")]
    public async Task<NoteWithSignalROutput> UpdateNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.Notes.V1 + "/{id:guid}")]
        HttpRequest req,
        Guid id,
        FunctionContext context)
    {
        // ... existing implementation
    }

    [Function("DeleteNote_V1")]
    public async Task<NoteWithSignalROutput> DeleteNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.Notes.V1 + "/{id:guid}")]
        HttpRequest req,
        Guid id,
        FunctionContext context)
    {
        // ... existing implementation
    }

    private static string GetUserId(HttpRequest req) =>
        req.Headers["X-User-Id"].FirstOrDefault() ?? "dev-user@example.com";
}
```

### Step 3: Create V2 Endpoints (When Needed)

When you need breaking changes, create a new version:

```csharp
/// <summary>
/// V2 Notes API endpoints with enhanced response format.
/// </summary>
public class NotesFunctionV2
{
    private readonly INotesService _notesService;
    private readonly ILogger<NotesFunctionV2> _logger;

    public NotesFunctionV2(INotesService notesService, ILogger<NotesFunctionV2> logger)
    {
        _notesService = notesService;
        _logger = logger;
    }

    [Function("GetNotes_V2")]
    public async Task<IActionResult> GetNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Notes.V2)]
        HttpRequest req)
    {
        var userId = GetUserId(req);
        _logger.LogInformation("[V2] GetNotes called by user {UserId}", userId);

        var notes = await _notesService.GetNotesAsync(userId);

        // V2 includes pagination metadata
        return new OkObjectResult(new
        {
            data = notes,
            meta = new
            {
                total = notes.Count,
                page = 1,
                pageSize = notes.Count,
                hasMore = false
            }
        });
    }

    // V2 might have different request/response shapes
    [Function("CreateNote_V2")]
    public async Task<NoteWithSignalROutput> CreateNote(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Notes.V2)]
        HttpRequest req,
        FunctionContext context)
    {
        // V2 might accept additional fields, return different format, etc.
    }
}
```

### Step 4: Add Version Redirect (Optional)

Create a redirect from unversioned to default version:

```csharp
/// <summary>
/// Redirects unversioned API calls to the default version.
/// </summary>
public class VersionRedirectFunction
{
    [Function("RedirectUnversionedNotes")]
    public IActionResult RedirectNotes(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "notes/{*path}")]
        HttpRequest req,
        string? path)
    {
        var newPath = $"/api/{ApiRoutes.Notes.V1}";
        if (!string.IsNullOrEmpty(path))
        {
            newPath += $"/{path}";
        }

        // Preserve query string
        if (req.QueryString.HasValue)
        {
            newPath += req.QueryString.Value;
        }

        return new RedirectResult(newPath, permanent: false);
    }
}
```

### Step 5: Update OpenAPI Documentation

Update your OpenAPI spec to document versions:

```yaml
openapi: 3.0.3
info:
  title: Cadence API
  version: "1.0"
  description: |
    ## API Versioning

    This API uses URL path versioning. All endpoints are prefixed with the version:

    - **V1 (Current)**: `/api/v1/...`
    - **V2 (Preview)**: `/api/v2/...` (when available)

    ### Version Lifecycle

    | Version | Status | Sunset Date |
    |---------|--------|-------------|
    | V1 | Current | N/A |

servers:
  - url: http://localhost:5071/api/v1
    description: Local Development (V1)
  - url: https://your-app.azurewebsites.net/api/v1
    description: Production (V1)

paths:
  /notes:
    get:
      summary: Get all notes
      operationId: getNotes
      tags:
        - Notes
      # ... rest of spec
```

---

## Frontend Integration

### Step 1: Create API Version Configuration

Create `src/frontend/src/core/config/apiConfig.ts`:

```typescript
/**
 * API version configuration
 */
export const API_CONFIG = {
  /** Current API version to use */
  version: "v1",

  /** Base URL from environment */
  baseUrl: import.meta.env.VITE_API_URL ?? "",

  /** Get full API URL with version prefix */
  getApiUrl(): string {
    return `${this.baseUrl}/api/${this.version}`;
  },
};

/**
 * API endpoints with version prefix
 */
export const API_ENDPOINTS = {
  notes: {
    list: () => `${API_CONFIG.getApiUrl()}/notes`,
    get: (id: string) => `${API_CONFIG.getApiUrl()}/notes/${id}`,
    create: () => `${API_CONFIG.getApiUrl()}/notes`,
    update: (id: string) => `${API_CONFIG.getApiUrl()}/notes/${id}`,
    delete: (id: string) => `${API_CONFIG.getApiUrl()}/notes/${id}`,
    restore: (id: string) => `${API_CONFIG.getApiUrl()}/notes/${id}/restore`,
  },
  health: {
    ping: () => `${API_CONFIG.baseUrl}/api/ping`,
    check: () => `${API_CONFIG.baseUrl}/api/health`,
  },
};
```

### Step 2: Update API Client

Update `src/frontend/src/core/services/api.ts`:

```typescript
import axios from "axios";
import { API_CONFIG } from "../config/apiConfig";

const apiClient = axios.create({
  baseURL: API_CONFIG.getApiUrl(),
  headers: {
    "Content-Type": "application/json",
  },
});

// Add version header for logging/debugging
apiClient.interceptors.request.use((config) => {
  config.headers["X-API-Version"] = API_CONFIG.version;
  config.headers["X-Correlation-Id"] = crypto.randomUUID();
  return config;
});

export { apiClient };
```

### Step 3: Update Services

Update `src/frontend/src/tools/notes/services/notesService.ts`:

```typescript
import { apiClient } from "@/core/services/api";
import type { NoteDto, CreateNoteRequest, UpdateNoteRequest } from "../types";

// Note: baseURL already includes version prefix from apiClient

export const notesService = {
  getNotes: async (): Promise<NoteDto[]> => {
    const response = await apiClient.get<NoteDto[]>("/notes");
    return response.data;
  },

  getNoteById: async (id: string): Promise<NoteDto> => {
    const response = await apiClient.get<NoteDto>(`/notes/${id}`);
    return response.data;
  },

  createNote: async (request: CreateNoteRequest): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>("/notes", request);
    return response.data;
  },

  updateNote: async (id: string, request: UpdateNoteRequest): Promise<NoteDto> => {
    const response = await apiClient.put<NoteDto>(`/notes/${id}`, request);
    return response.data;
  },

  deleteNote: async (id: string): Promise<void> => {
    await apiClient.delete(`/notes/${id}`);
  },

  restoreNote: async (id: string): Promise<NoteDto> => {
    const response = await apiClient.post<NoteDto>(`/notes/${id}/restore`);
    return response.data;
  },
};
```

---

## Deprecation Policy

### Version Lifecycle

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Preview   │────▶│   Current   │────▶│ Deprecated  │────▶│   Sunset    │
│   (beta)    │     │  (stable)   │     │ (6 months)  │     │  (removed)  │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
```

### Timeline

| Phase | Duration | Description |
|-------|----------|-------------|
| Preview | Variable | New version available for testing |
| Current | Until next version | Fully supported, recommended |
| Deprecated | 6 months | Still works, migration warnings |
| Sunset | Immediate | Returns 410 Gone |

### Deprecation Headers

Add headers to deprecated versions:

```csharp
public class DeprecationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly Dictionary<string, (string SunsetDate, string Replacement)> _deprecatedVersions = new()
    {
        // Example: V1 deprecated, sunset on 2025-06-01
        // { "v1", ("2025-06-01", "v2") }
    };

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        await next(context);

        var httpContext = context.GetHttpContext();
        if (httpContext == null) return;

        var path = httpContext.Request.Path.Value ?? "";

        foreach (var (version, (sunsetDate, replacement)) in _deprecatedVersions)
        {
            if (path.Contains($"/api/{version}/"))
            {
                httpContext.Response.Headers.Add("Deprecation", "true");
                httpContext.Response.Headers.Add("Sunset", sunsetDate);
                httpContext.Response.Headers.Add("Link", $"</api/{replacement}>; rel=\"successor-version\"");
                break;
            }
        }
    }
}
```

### Sunset Response

When a version is fully sunset:

```csharp
[Function("SunsetV0")]
public IActionResult HandleSunsetV0(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", Route = "v0/{*path}")]
    HttpRequest req)
{
    return new ObjectResult(new
    {
        error = "Gone",
        message = "API version v0 has been sunset. Please migrate to v1.",
        migrationGuide = "https://docs.example.com/api/migration/v0-to-v1",
        currentVersion = "v1"
    })
    {
        StatusCode = StatusCodes.Status410Gone
    };
}
```

---

## Testing

### Test Multiple Versions

```csharp
public class NotesApiVersioningTests
{
    [Fact]
    public async Task V1_GetNotes_Returns_Array()
    {
        // V1 returns plain array
        var response = await _client.GetAsync("/api/v1/notes");
        var notes = await response.Content.ReadFromJsonAsync<NoteDto[]>();

        notes.Should().NotBeNull();
    }

    [Fact]
    public async Task V2_GetNotes_Returns_Paginated_Response()
    {
        // V2 returns wrapped response with metadata
        var response = await _client.GetAsync("/api/v2/notes");
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<NoteDto>>();

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Meta.Should().NotBeNull();
    }

    [Fact]
    public async Task Unversioned_Redirects_To_Default()
    {
        var response = await _client.GetAsync("/api/notes");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/api/v1/notes");
    }
}
```

### Frontend Version Testing

```typescript
import { describe, it, expect, vi } from "vitest";
import { API_CONFIG, API_ENDPOINTS } from "./apiConfig";

describe("API Configuration", () => {
  it("uses correct version prefix", () => {
    expect(API_CONFIG.version).toBe("v1");
  });

  it("builds correct endpoint URLs", () => {
    const url = API_ENDPOINTS.notes.list();
    expect(url).toContain("/api/v1/notes");
  });

  it("includes ID in parameterized endpoints", () => {
    const url = API_ENDPOINTS.notes.get("123");
    expect(url).toContain("/api/v1/notes/123");
  });
});
```

---

## Migration Checklist

When releasing a new API version:

1. [ ] Create new version folder/files if needed
2. [ ] Update route constants in `ApiRoutes`
3. [ ] Implement new endpoints with `_V{N}` suffix
4. [ ] Update OpenAPI documentation
5. [ ] Add deprecation headers to old version
6. [ ] Update frontend `API_CONFIG.version`
7. [ ] Write migration guide
8. [ ] Communicate timeline to API consumers
9. [ ] Monitor usage of deprecated version
10. [ ] Sunset old version after grace period

---

## Best Practices

1. **Version early** - Easier to add versions before you need them
2. **Never break V1** - Always create V2 for breaking changes
3. **Document everything** - Clear migration guides are essential
4. **Long deprecation periods** - Give clients time to migrate
5. **Monitor version usage** - Know when it's safe to sunset
6. **Test both versions** - Ensure old versions keep working
7. **Version the contract, not implementation** - Same backend can serve multiple versions

---

## Related Documentation

- [Microsoft API Versioning Best Practices](https://docs.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning-a-restful-web-api)
- [Semantic Versioning](https://semver.org/)
