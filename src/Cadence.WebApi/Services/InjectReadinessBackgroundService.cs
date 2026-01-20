using Cadence.Core.Features.Injects.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cadence.WebApi.Services;

/// <summary>
/// Background service that periodically evaluates all active exercises for inject readiness.
/// Runs every 5 seconds to check if any pending injects should transition to Ready status
/// based on their delivery time in clock-driven exercises.
/// </summary>
public class InjectReadinessBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InjectReadinessBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);

    public InjectReadinessBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InjectReadinessBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inject readiness background service started (check interval: {Interval}s)", _checkInterval.TotalSeconds);

        // Don't start immediately - wait for the app to finish starting
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a new scope for each evaluation to ensure proper DbContext lifecycle
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IInjectReadinessService>();

                await service.EvaluateAllExercisesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when application is shutting down
                _logger.LogInformation("Inject readiness background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating inject readiness");
            }

            // Wait for next check interval
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when application is shutting down
                break;
            }
        }

        _logger.LogInformation("Inject readiness background service stopped");
    }
}
