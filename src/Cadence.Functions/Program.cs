using Cadence.Core.Data;
using Cadence.Core.Extensions;
using Cadence.Functions.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// =============================================================================
// Cadence - Azure Functions Entry Point
// =============================================================================

// Bootstrap logger for capturing startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = FunctionsApplication.CreateBuilder(args);

    // Configure Azure Functions web application
    builder.ConfigureFunctionsWebApplication();

    // Get configuration
    var configuration = builder.Configuration;

    // =============================================================================
    // Logging Configuration (Serilog)
    // =============================================================================

    var serilogConfig = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "Cadence.Functions")
        .WriteTo.Console();

    // Application Insights (optional - controlled by configuration)
    var appInsightsEnabled = configuration.GetValue<bool>("ApplicationInsights:Enabled");
    var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (appInsightsEnabled || !string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            serilogConfig.WriteTo.ApplicationInsights(
                appInsightsConnectionString,
                new Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter());
        }
    }

    Log.Logger = serilogConfig.CreateLogger();

    // Register Serilog as the logging provider
    builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddSerilog(dispose: true);
    });

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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Azure Functions host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
