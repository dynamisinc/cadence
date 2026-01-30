using Cadence.Core.Features.Assignments.Services;
using Cadence.Core.Features.Autocomplete.Services;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Features.DeliveryMethods.Services;
using Cadence.Core.Features.ExcelExport.Services;
using Cadence.Core.Features.ExcelImport.Services;
using Cadence.Core.Features.ExerciseClock.Services;
using Cadence.Core.Features.Exercises.Services;
using Cadence.Core.Features.ExpectedOutcomes.Services;
using Cadence.Core.Features.Injects.Services;
using Cadence.Core.Features.Metrics.Services;
using Cadence.Core.Features.Msel.Services;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Features.Objectives.Services;
using Cadence.Core.Features.Observations.Services;
using Cadence.Core.Features.Organizations.Services;
using Cadence.Core.Features.Users.Services;
using FluentValidation;

namespace Cadence.Core.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the DI container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Add Validators from this assembly
        services.AddValidatorsFromAssemblyContaining<AppDbContext>();

        // Add Feature Services
        services.AddScoped<IObjectiveService, ObjectiveService>();
        services.AddScoped<IObservationService, ObservationService>();
        services.AddScoped<IInjectService, InjectService>();
        services.AddScoped<IInjectReadinessService, InjectReadinessService>();
        services.AddScoped<IExerciseClockService, ExerciseClockService>();
        services.AddScoped<IExerciseStatusService, ExerciseStatusService>();
        services.AddScoped<IExerciseDeleteService, ExerciseDeleteService>();
        services.AddScoped<IExerciseParticipantService, ExerciseParticipantService>();
        services.AddScoped<IMselService, MselService>();
        services.AddScoped<ISetupProgressService, SetupProgressService>();
        services.AddScoped<IExpectedOutcomeService, ExpectedOutcomeService>();
        services.AddScoped<IDeliveryMethodService, DeliveryMethodService>();
        services.AddScoped<IAutocompleteService, AutocompleteService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IExerciseMetricsService, ExerciseMetricsService>();
        services.AddScoped<IExerciseCapabilityService, ExerciseCapabilityService>();
        services.AddScoped<ICapabilityService, CapabilityService>();
        services.AddSingleton<IPredefinedLibraryProvider, PredefinedLibraryProvider>();
        services.AddScoped<ICapabilityImportService, CapabilityImportService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IMembershipService, MembershipService>();

        return services;
    }

    /// <summary>
    /// Adds database context with SQL Server configuration.
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException(
                "Database connection string not found. Set 'ConnectionStrings:DefaultConnection' or 'SqlConnectionString'.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);
            });

            // Enable detailed errors in development
#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
        });

        return services;
    }
}
