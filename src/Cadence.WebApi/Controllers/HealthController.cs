using Cadence.Core.Data;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// Health check endpoint for monitoring and deployment validation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Simple liveness check - confirms the app is running.
    /// Use this for deployment validation.
    /// </summary>
    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Returns the full health status of the API including database connectivity.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var canConnect = false;
        try
        {
            canConnect = await _context.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
        }

        var response = new
        {
            status = canConnect ? "Healthy" : "Unhealthy",
            timestamp = DateTime.UtcNow,
            database = canConnect ? "Connected" : "Disconnected"
        };

        return canConnect ? Ok(response) : StatusCode(503, response);
    }
}
