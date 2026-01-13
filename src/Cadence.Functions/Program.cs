using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cadence.Core.Data;
using Cadence.Core.Extensions;
using Cadence.Functions.Middleware;

// =============================================================================
// Cadence - Azure Functions Entry Point
// =============================================================================

var builder = FunctionsApplication.CreateBuilder(args);

// Configure Azure Functions web application
builder.ConfigureFunctionsWebApplication();

// Get configuration
var configuration = builder.Configuration;

// =============================================================================
// Logging Configuration
// =============================================================================

builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Information);
    logging.AddConsole();

    // Filter out noisy logs
    logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
});

// Application Insights (optional - controlled by configuration)
var appInsightsEnabled = configuration.GetValue<bool>("ApplicationInsights:Enabled");
if (appInsightsEnabled || !string.IsNullOrEmpty(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services
        .AddApplicationInsightsTelemetryWorkerService()
        .ConfigureFunctionsApplicationInsights();
}

// =============================================================================
// Middleware
// =============================================================================

builder.UseMiddleware<CorrelationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();
builder.UseMiddleware<ValidationMiddleware>();

// =============================================================================
// Database
// =============================================================================

builder.Services.AddDatabase(configuration);

// =============================================================================
// Application Services
// =============================================================================

builder.Services.AddApplicationServices();

// =============================================================================
// Build and Run
// =============================================================================

var app = builder.Build();

// Apply migrations on startup (development only - disable in production)
var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate");
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        // Don't throw - let the app start even if migrations fail
        // This allows health checks to report the issue
    }
}

app.Run();
