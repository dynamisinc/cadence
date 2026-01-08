using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace DynamisReferenceApp.Api.Shared.OpenApi;

/// <summary>
/// Provides OpenAPI specification and Scalar API documentation UI.
/// </summary>
public class OpenApiFunction
{
    private readonly ILogger<OpenApiFunction> _logger;
    private static string? _openApiJson;
    private static readonly object _lock = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OpenApiFunction(ILogger<OpenApiFunction> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the OpenAPI specification document.
    /// </summary>
    [Function("OpenApiSpec")]
    public IActionResult GetOpenApiSpec(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "openapi.json")] HttpRequest req)
    {
        _logger.LogInformation("OpenAPI specification requested");

        var json = GetOpenApiJson();

        return new ContentResult
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = 200
        };
    }

    /// <summary>
    /// Returns the Scalar API documentation UI.
    /// </summary>
    [Function("ScalarDocs")]
    public IActionResult GetScalarDocs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "docs")] HttpRequest req)
    {
        _logger.LogInformation("Scalar documentation UI requested");

        var baseUrl = GetBaseUrl(req);
        var html = GenerateScalarHtml(baseUrl);

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    private static string GetOpenApiJson()
    {
        if (_openApiJson != null)
            return _openApiJson;

        lock (_lock)
        {
            if (_openApiJson != null)
                return _openApiJson;

            _openApiJson = BuildOpenApiJson();
            return _openApiJson;
        }
    }

    private static string BuildOpenApiJson()
    {
        var document = new OpenApiDocumentModel
        {
            Openapi = "3.0.3",
            Info = new OpenApiInfo
            {
                Title = "Dynamis Reference App API",
                Version = "1.0.0",
                Description = "REST API for the Dynamis Reference Application - a template for building modern web applications with Azure Functions.",
                Contact = new OpenApiContact
                {
                    Name = "Dynamis Inc.",
                    Url = "https://github.com/dynamisinc/dynamis-reference-app"
                }
            },
            Servers = new List<OpenApiServer>
            {
                new() { Url = "/api", Description = "API Base URL" }
            },
            Tags = new List<OpenApiTag>
            {
                new() { Name = "Health", Description = "Health check endpoints" },
                new() { Name = "Notes", Description = "Notes CRUD operations" }
            },
            Paths = BuildPaths(),
            Components = BuildComponents()
        };

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    private static Dictionary<string, PathItem> BuildPaths()
    {
        var userIdHeader = new Parameter
        {
            Name = "X-User-Id",
            In = "header",
            Required = false,
            Schema = new Schema { Type = "string" },
            Description = "User ID (for development/testing). Falls back to default dev user if not provided."
        };

        return new Dictionary<string, PathItem>
        {
            // Health endpoints
            ["/health"] = new PathItem
            {
                Get = new Operation
                {
                    Tags = new[] { "Health" },
                    Summary = "Get API health status",
                    Description = "Returns the health status of the API and its dependencies (database, SignalR).",
                    OperationId = "Health",
                    Responses = new Dictionary<string, Response>
                    {
                        ["200"] = new Response
                        {
                            Description = "API is healthy",
                            Content = JsonContent("HealthResponse")
                        },
                        ["503"] = new Response
                        {
                            Description = "API is unhealthy",
                            Content = JsonContent("HealthResponse")
                        }
                    }
                }
            },
            ["/ping"] = new PathItem
            {
                Get = new Operation
                {
                    Tags = new[] { "Health" },
                    Summary = "Simple ping endpoint",
                    Description = "Returns a simple pong response for basic availability checks.",
                    OperationId = "Ping",
                    Responses = new Dictionary<string, Response>
                    {
                        ["200"] = new Response
                        {
                            Description = "Success",
                            Content = new Dictionary<string, MediaType>
                            {
                                ["application/json"] = new MediaType
                                {
                                    Schema = new Schema
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, Schema>
                                        {
                                            ["message"] = new Schema { Type = "string" },
                                            ["timestamp"] = new Schema { Type = "string", Format = "date-time" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },

            // Notes endpoints
            ["/notes"] = new PathItem
            {
                Get = new Operation
                {
                    Tags = new[] { "Notes" },
                    Summary = "Get all notes",
                    Description = "Returns all notes for the current user.",
                    OperationId = "GetNotes",
                    Parameters = new[] { userIdHeader },
                    Responses = new Dictionary<string, Response>
                    {
                        ["200"] = new Response
                        {
                            Description = "Success",
                            Content = new Dictionary<string, MediaType>
                            {
                                ["application/json"] = new MediaType
                                {
                                    Schema = new Schema
                                    {
                                        Type = "array",
                                        Items = new Schema { Ref = "#/components/schemas/NoteDto" }
                                    }
                                }
                            }
                        }
                    }
                },
                Post = new Operation
                {
                    Tags = new[] { "Notes" },
                    Summary = "Create a note",
                    Description = "Creates a new note for the current user.",
                    OperationId = "CreateNote",
                    Parameters = new[] { userIdHeader },
                    RequestBody = new RequestBody
                    {
                        Required = true,
                        Content = JsonContent("CreateNoteRequest")
                    },
                    Responses = new Dictionary<string, Response>
                    {
                        ["201"] = new Response
                        {
                            Description = "Note created",
                            Content = JsonContent("NoteDto")
                        },
                        ["400"] = new Response
                        {
                            Description = "Invalid request",
                            Content = JsonContent("ErrorResponse")
                        }
                    }
                }
            },
            ["/notes/{id}"] = new PathItem
            {
                Parameters = new[]
                {
                    new Parameter
                    {
                        Name = "id",
                        In = "path",
                        Required = true,
                        Schema = new Schema { Type = "string", Format = "uuid" },
                        Description = "Note ID"
                    },
                    userIdHeader
                },
                Get = new Operation
                {
                    Tags = new[] { "Notes" },
                    Summary = "Get a note by ID",
                    Description = "Returns a single note by its ID.",
                    OperationId = "GetNote",
                    Responses = new Dictionary<string, Response>
                    {
                        ["200"] = new Response
                        {
                            Description = "Success",
                            Content = JsonContent("NoteDto")
                        },
                        ["404"] = new Response
                        {
                            Description = "Note not found",
                            Content = JsonContent("ErrorResponse")
                        }
                    }
                },
                Put = new Operation
                {
                    Tags = new[] { "Notes" },
                    Summary = "Update a note",
                    Description = "Updates an existing note.",
                    OperationId = "UpdateNote",
                    RequestBody = new RequestBody
                    {
                        Required = true,
                        Content = JsonContent("UpdateNoteRequest")
                    },
                    Responses = new Dictionary<string, Response>
                    {
                        ["200"] = new Response
                        {
                            Description = "Note updated",
                            Content = JsonContent("NoteDto")
                        },
                        ["400"] = new Response
                        {
                            Description = "Invalid request",
                            Content = JsonContent("ErrorResponse")
                        },
                        ["404"] = new Response
                        {
                            Description = "Note not found",
                            Content = JsonContent("ErrorResponse")
                        }
                    }
                },
                Delete = new Operation
                {
                    Tags = new[] { "Notes" },
                    Summary = "Delete a note",
                    Description = "Soft-deletes a note. The note can be restored later.",
                    OperationId = "DeleteNote",
                    Responses = new Dictionary<string, Response>
                    {
                        ["204"] = new Response { Description = "Note deleted" },
                        ["404"] = new Response
                        {
                            Description = "Note not found",
                            Content = JsonContent("ErrorResponse")
                        }
                    }
                }
            },
            ["/notes/{id}/restore"] = new PathItem
            {
                Parameters = new[]
                {
                    new Parameter
                    {
                        Name = "id",
                        In = "path",
                        Required = true,
                        Schema = new Schema { Type = "string", Format = "uuid" },
                        Description = "Note ID"
                    },
                    userIdHeader
                },
                Post = new Operation
                {
                    Tags = new[] { "Notes" },
                    Summary = "Restore a deleted note",
                    Description = "Restores a soft-deleted note.",
                    OperationId = "RestoreNote",
                    Responses = new Dictionary<string, Response>
                    {
                        ["200"] = new Response
                        {
                            Description = "Note restored",
                            Content = JsonContent("NoteDto")
                        },
                        ["404"] = new Response
                        {
                            Description = "Note not found or not deleted",
                            Content = JsonContent("ErrorResponse")
                        }
                    }
                }
            }
        };
    }

    private static Dictionary<string, MediaType> JsonContent(string schemaRef)
    {
        return new Dictionary<string, MediaType>
        {
            ["application/json"] = new MediaType
            {
                Schema = new Schema { Ref = $"#/components/schemas/{schemaRef}" }
            }
        };
    }

    private static Components BuildComponents()
    {
        return new Components
        {
            Schemas = new Dictionary<string, Schema>
            {
                ["NoteDto"] = new Schema
                {
                    Type = "object",
                    Required = new[] { "id", "title", "createdAt", "updatedAt" },
                    Properties = new Dictionary<string, Schema>
                    {
                        ["id"] = new Schema { Type = "string", Format = "uuid", Description = "Unique identifier" },
                        ["title"] = new Schema { Type = "string", Description = "Note title", MaxLength = 100 },
                        ["content"] = new Schema { Type = "string", Nullable = true, Description = "Note content", MaxLength = 10000 },
                        ["createdAt"] = new Schema { Type = "string", Format = "date-time", Description = "Creation timestamp" },
                        ["updatedAt"] = new Schema { Type = "string", Format = "date-time", Description = "Last update timestamp" }
                    }
                },
                ["CreateNoteRequest"] = new Schema
                {
                    Type = "object",
                    Required = new[] { "title" },
                    Properties = new Dictionary<string, Schema>
                    {
                        ["title"] = new Schema { Type = "string", Description = "Note title (required)", MaxLength = 100 },
                        ["content"] = new Schema { Type = "string", Nullable = true, Description = "Note content", MaxLength = 10000 }
                    }
                },
                ["UpdateNoteRequest"] = new Schema
                {
                    Type = "object",
                    Required = new[] { "title" },
                    Properties = new Dictionary<string, Schema>
                    {
                        ["title"] = new Schema { Type = "string", Description = "Note title (required)", MaxLength = 100 },
                        ["content"] = new Schema { Type = "string", Nullable = true, Description = "Note content", MaxLength = 10000 }
                    }
                },
                ["HealthResponse"] = new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema>
                    {
                        ["status"] = new Schema { Type = "string", Description = "Overall health status" },
                        ["timestamp"] = new Schema { Type = "string", Format = "date-time" },
                        ["version"] = new Schema { Type = "string" },
                        ["environment"] = new Schema { Type = "string" },
                        ["database"] = new Schema { Ref = "#/components/schemas/ComponentHealth" },
                        ["signalR"] = new Schema { Ref = "#/components/schemas/ComponentHealth" }
                    }
                },
                ["ComponentHealth"] = new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema>
                    {
                        ["status"] = new Schema { Type = "string" },
                        ["message"] = new Schema { Type = "string" }
                    }
                },
                ["ErrorResponse"] = new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, Schema>
                    {
                        ["message"] = new Schema { Type = "string", Description = "Error message" }
                    }
                }
            }
        };
    }

    private static string GetBaseUrl(HttpRequest req)
    {
        var scheme = req.Scheme;
        var host = req.Host.Value;
        return $"{scheme}://{host}";
    }

    private static string GenerateScalarHtml(string baseUrl)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Dynamis Reference App - API Documentation</title>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <style>
                    body {
                        margin: 0;
                        padding: 0;
                    }
                </style>
            </head>
            <body>
                <script id="api-reference" data-url="{{baseUrl}}/api/openapi.json"></script>
                <script>
                    var configuration = {
                        theme: 'purple',
                        layout: 'modern',
                        showSidebar: true,
                        hideModels: false,
                        hideDownloadButton: false,
                        darkMode: true,
                        metaData: {
                            title: 'Dynamis Reference App API'
                        }
                    };
                    document.getElementById('api-reference').dataset.configuration = JSON.stringify(configuration);
                </script>
                <script src="https://cdn.jsdelivr.net/npm/@scalar/api-reference"></script>
            </body>
            </html>
            """;
    }

    #region OpenAPI Model Classes

    private class OpenApiDocumentModel
    {
        public string Openapi { get; set; } = "3.0.3";
        public OpenApiInfo Info { get; set; } = new();
        public List<OpenApiServer> Servers { get; set; } = new();
        public List<OpenApiTag> Tags { get; set; } = new();
        public Dictionary<string, PathItem> Paths { get; set; } = new();
        public Components Components { get; set; } = new();
    }

    private class OpenApiInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string? Description { get; set; }
        public OpenApiContact? Contact { get; set; }
    }

    private class OpenApiContact
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
    }

    private class OpenApiServer
    {
        public string Url { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    private class OpenApiTag
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    private class PathItem
    {
        public Parameter[]? Parameters { get; set; }
        public Operation? Get { get; set; }
        public Operation? Post { get; set; }
        public Operation? Put { get; set; }
        public Operation? Delete { get; set; }
    }

    private class Operation
    {
        public string[]? Tags { get; set; }
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? OperationId { get; set; }
        public Parameter[]? Parameters { get; set; }
        public RequestBody? RequestBody { get; set; }
        public Dictionary<string, Response> Responses { get; set; } = new();
    }

    private class Parameter
    {
        public string Name { get; set; } = string.Empty;
        public string In { get; set; } = string.Empty;
        public bool? Required { get; set; }
        public Schema? Schema { get; set; }
        public string? Description { get; set; }
    }

    private class RequestBody
    {
        public bool? Required { get; set; }
        public Dictionary<string, MediaType>? Content { get; set; }
    }

    private class Response
    {
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, MediaType>? Content { get; set; }
    }

    private class MediaType
    {
        public Schema? Schema { get; set; }
    }

    private class Schema
    {
        public string? Type { get; set; }
        public string? Format { get; set; }
        public string? Description { get; set; }
        public bool? Nullable { get; set; }
        public int? MaxLength { get; set; }
        public string[]? Required { get; set; }
        public Dictionary<string, Schema>? Properties { get; set; }
        public Schema? Items { get; set; }

        [JsonPropertyName("$ref")]
        public string? Ref { get; set; }
    }

    private class Components
    {
        public Dictionary<string, Schema> Schemas { get; set; } = new();
    }

    #endregion
}
