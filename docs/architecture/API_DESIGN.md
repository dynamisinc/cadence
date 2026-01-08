# API Design

This document describes the REST API design patterns used in the reference app.

---

## API Conventions

### Base URL

| Environment | Base URL |
|-------------|----------|
| Local | `http://localhost:5071/api` |
| Production | `https://{function-app}.azurewebsites.net/api` |

### HTTP Methods

| Method | Purpose | Idempotent | Body |
|--------|---------|------------|------|
| `GET` | Retrieve resource(s) | Yes | No |
| `POST` | Create resource | No | Yes |
| `PUT` | Update resource (full) | Yes | Yes |
| `PATCH` | Update resource (partial) | Yes | Yes |
| `DELETE` | Remove resource | Yes | No |

### Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| `200 OK` | Success | GET, PUT, PATCH |
| `201 Created` | Resource created | POST |
| `204 No Content` | Success, no body | DELETE |
| `400 Bad Request` | Invalid input | Validation errors |
| `401 Unauthorized` | Not authenticated | Missing/invalid auth |
| `403 Forbidden` | Not authorized | Insufficient permissions |
| `404 Not Found` | Resource not found | GET/PUT/DELETE on missing |
| `429 Too Many Requests` | Rate limited | Exceeded rate limit |
| `500 Internal Server Error` | Server error | Unhandled exceptions |

---

## Endpoints

### Notes

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/notes` | List all notes for user |
| `GET` | `/notes/{id}` | Get single note |
| `POST` | `/notes` | Create note |
| `PUT` | `/notes/{id}` | Update note |
| `DELETE` | `/notes/{id}` | Soft-delete note |
| `POST` | `/notes/{id}/restore` | Restore deleted note |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/ping` | Simple availability check |
| `GET` | `/health` | Detailed health check |

### SignalR

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/negotiate` | SignalR connection negotiation |

---

## Request/Response Examples

### List Notes

```http
GET /api/notes HTTP/1.1
Host: localhost:5071
X-User-Id: user@example.com
X-Correlation-Id: abc123
```

```json
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Meeting Notes",
    "content": "Discussed project timeline...",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T14:45:00Z"
  }
]
```

### Get Single Note

```http
GET /api/notes/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5071
X-User-Id: user@example.com
```

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Meeting Notes",
  "content": "Discussed project timeline...",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T14:45:00Z"
}
```

### Create Note

```http
POST /api/notes HTTP/1.1
Host: localhost:5071
Content-Type: application/json
X-User-Id: user@example.com

{
  "title": "New Note",
  "content": "This is the content"
}
```

```json
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/notes/660e8400-e29b-41d4-a716-446655440001

{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "title": "New Note",
  "content": "This is the content",
  "createdAt": "2024-01-16T09:00:00Z",
  "updatedAt": "2024-01-16T09:00:00Z"
}
```

### Update Note

```http
PUT /api/notes/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5071
Content-Type: application/json
X-User-Id: user@example.com

{
  "title": "Updated Title",
  "content": "Updated content"
}
```

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Updated Title",
  "content": "Updated content",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-16T11:00:00Z"
}
```

### Delete Note

```http
DELETE /api/notes/550e8400-e29b-41d4-a716-446655440000 HTTP/1.1
Host: localhost:5071
X-User-Id: user@example.com
```

```
HTTP/1.1 204 No Content
```

### Restore Note

```http
POST /api/notes/550e8400-e29b-41d4-a716-446655440000/restore HTTP/1.1
Host: localhost:5071
X-User-Id: user@example.com
```

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Meeting Notes",
  "content": "Discussed project timeline...",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-16T12:00:00Z"
}
```

---

## Error Responses

### Standard Error Format

```json
{
  "message": "Human-readable error message",
  "errors": [
    {
      "field": "title",
      "message": "Title is required",
      "code": "TITLE_REQUIRED"
    }
  ]
}
```

### 400 Bad Request (Validation)

```json
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "message": "Validation failed",
  "errors": [
    {
      "field": "title",
      "message": "Title is required",
      "code": "TITLE_REQUIRED"
    },
    {
      "field": "content",
      "message": "Content cannot exceed 10,000 characters",
      "code": "CONTENT_TOO_LONG"
    }
  ]
}
```

### 404 Not Found

```json
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "message": "Note not found"
}
```

### 500 Internal Server Error

```json
HTTP/1.1 500 Internal Server Error
Content-Type: application/json

{
  "message": "An unexpected error occurred",
  "correlationId": "abc123-def456"
}
```

---

## Headers

### Request Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Content-Type` | Yes (POST/PUT) | `application/json` |
| `X-User-Id` | Dev only | User identifier (dev auth) |
| `Authorization` | Production | `Bearer {token}` |
| `X-Correlation-Id` | No | Request tracking ID |

### Response Headers

| Header | Description |
|--------|-------------|
| `X-Correlation-Id` | Request tracking ID (echoed or generated) |
| `X-RateLimit-Limit` | Rate limit ceiling |
| `X-RateLimit-Remaining` | Requests remaining |
| `X-RateLimit-Reset` | When limit resets (ISO 8601) |

---

## Function Implementation Pattern

### HTTP Trigger Function

```csharp
[Function("GetNotes")]
public async Task<IActionResult> GetNotes(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notes")]
    HttpRequest req)
{
    var userId = GetUserId(req);
    _logger.LogInformation("GetNotes called by user {UserId}", userId);

    var notes = await _notesService.GetNotesAsync(userId);
    return new OkObjectResult(notes);
}
```

### Route Parameters

```csharp
[Function("GetNoteById")]
public async Task<IActionResult> GetNoteById(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "notes/{id:guid}")]
    HttpRequest req,
    Guid id)  // Route parameter bound automatically
{
    // ...
}
```

### Request Body Parsing

```csharp
private async Task<T?> ParseRequestBody<T>(HttpRequest req) where T : class
{
    try
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (JsonException)
    {
        return null;
    }
}
```

---

## OpenAPI Documentation

The API includes OpenAPI 3.0 documentation accessible via Scalar UI:

```
GET /api/docs           → Scalar UI (interactive docs)
GET /api/openapi.json   → Raw OpenAPI spec
```

### Adding Endpoint Documentation

Update `src/api/Shared/Docs/OpenApiFunction.cs`:

```yaml
paths:
  /notes:
    get:
      summary: Get all notes
      operationId: getNotes
      tags:
        - Notes
      responses:
        '200':
          description: List of notes
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/NoteDto'
```

---

## Best Practices

1. **Use proper HTTP methods** - GET for reads, POST for creates, etc.
2. **Return appropriate status codes** - 201 for creates, 204 for deletes
3. **Include Location header** - For POST responses, include resource URL
4. **Use consistent error format** - Same structure for all errors
5. **Log with correlation IDs** - Track requests across services
6. **Validate early** - Check input before processing
7. **Don't expose internal errors** - Return generic message to clients

---

## Related Documentation

- [OpenAPI Spec](../../src/api/Shared/Docs/) - Full API specification
- [guides/API_VERSIONING.md](../guides/API_VERSIONING.md) - Versioning strategy
- [guides/INPUT_VALIDATION.md](../guides/INPUT_VALIDATION.md) - Validation patterns
- [guides/RATE_LIMITING.md](../guides/RATE_LIMITING.md) - Rate limiting
