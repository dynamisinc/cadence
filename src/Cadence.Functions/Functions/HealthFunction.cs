using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Cadence.Core.Data;

namespace Cadence.Functions.Functions;

/// <summary>
/// Health check endpoint for monitoring and deployment validation.
/// </summary>
public class HealthFunction
{
    private readonly AppDbContext _context;
    private readonly ILogger<HealthFunction> _logger;
    private readonly IConfiguration _configuration;

    public HealthFunction(
        AppDbContext context,
        ILogger<HealthFunction> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Returns the health status of the API.
    /// </summary>
    [Function("Health")]
    public async Task<IActionResult> GetHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        _logger.LogInformation("Health check requested");

        var response = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = GetVersion(),
            Environment = _configuration["AZURE_FUNCTIONS_ENVIRONMENT"] ?? "Development"
        };

        // Check database connectivity
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            response.Database = new ComponentHealth
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                Message = canConnect ? "Connected" : "Cannot connect to database"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            response.Database = new ComponentHealth
            {
                Status = "Unhealthy",
                Message = $"Error: {ex.Message}"
            };
            response.Status = "Degraded";
        }

        // Check SignalR (placeholder)
        response.SignalR = new ComponentHealth
        {
            Status = "Healthy",
            Message = "SignalR configured"
        };

        // Determine overall status
        if (response.Database.Status == "Unhealthy")
        {
            response.Status = "Unhealthy";
            return new ObjectResult(response) { StatusCode = 503 };
        }

        return new OkObjectResult(response);
    }

    /// <summary>
    /// Simple ping endpoint for basic availability checks.
    /// </summary>
    [Function("Ping")]
    public IActionResult Ping(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req)
    {
        return new OkObjectResult(new { message = "pong", timestamp = DateTime.UtcNow });
    }

    private static string GetVersion()
    {
        try
        {
            var versionFile = Path.Combine(AppContext.BaseDirectory, "VERSION");
            if (File.Exists(versionFile))
            {
                return File.ReadAllText(versionFile).Trim();
            }
        }
        catch
        {
            // Ignore errors reading version file
        }

        return "unknown";
    }
}

/// <summary>
/// Health check response model.
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "Unknown";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "unknown";
    public string Environment { get; set; } = "unknown";
    public ComponentHealth Database { get; set; } = new();
    public ComponentHealth SignalR { get; set; } = new();
}

/// <summary>
/// Health status for a component.
/// </summary>
public class ComponentHealth
{
    public string Status { get; set; } = "Unknown";
    public string Message { get; set; } = string.Empty;
}
