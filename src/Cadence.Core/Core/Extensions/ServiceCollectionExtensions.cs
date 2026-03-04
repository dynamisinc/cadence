using Cadence.Core.Data.Interceptors;
using Cadence.Core.Features.Assignments.Services;
using Cadence.Core.Features.Autocomplete.Services;
using Cadence.Core.Features.BulkParticipantImport.Services;
using Cadence.Core.Features.Capabilities.Services;
using Cadence.Core.Features.DeliveryMethods.Services;
using Cadence.Core.Features.ExcelExport.Services;
using Cadence.Core.Features.Feedback.Services;
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
using Cadence.Core.Features.Eeg.Services;
using Cadence.Core.Features.Email.Services;
using Cadence.Core.Features.Photos.Services;
using Cadence.Core.Features.SystemSettings.Services;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;

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
        services.AddScoped<IPhotoService, PhotoService>();
        services.AddScoped<IInjectService, InjectService>();
        services.AddScoped<IInjectReadinessService, InjectReadinessService>();
        services.AddScoped<IExerciseClockService, ExerciseClockService>();
        services.AddScoped<IExerciseStatusService, ExerciseStatusService>();
        services.AddScoped<IExerciseDeleteService, ExerciseDeleteService>();
        services.AddScoped<IExerciseParticipantService, ExerciseParticipantService>();
        services.AddScoped<IExerciseApprovalSettingsService, ExerciseApprovalSettingsService>();
        services.AddScoped<IExerciseApprovalQueueService, ExerciseApprovalQueueService>();
        services.AddScoped<IApprovalPermissionService, ApprovalPermissionService>();
        services.AddScoped<IMselService, MselService>();
        services.AddScoped<ISetupProgressService, SetupProgressService>();
        services.AddScoped<IExpectedOutcomeService, ExpectedOutcomeService>();
        services.AddScoped<IDeliveryMethodService, DeliveryMethodService>();
        services.AddScoped<IAutocompleteService, AutocompleteService>();
        services.AddScoped<IOrganizationSuggestionService, OrganizationSuggestionService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        LegacyExcelReader.EnsureEncodingRegistered(); // Required for .xls (BIFF) file support
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IApprovalNotificationService, ApprovalNotificationService>();
        services.AddScoped<IExerciseMetricsService, ExerciseMetricsService>();
        services.AddScoped<IExerciseCapabilityService, ExerciseCapabilityService>();
        services.AddScoped<ICapabilityService, CapabilityService>();
        services.AddSingleton<IPredefinedLibraryProvider, PredefinedLibraryProvider>();
        services.AddScoped<ICapabilityImportService, CapabilityImportService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IOrganizationInvitationService, OrganizationInvitationService>();

        // Bulk Participant Import Services
        services.AddScoped<IParticipantFileParser, ParticipantFileParser>();
        services.AddScoped<IParticipantClassificationService, ParticipantClassificationService>();
        services.AddScoped<IBulkParticipantImportService, BulkParticipantImportService>();

        // EEG (Exercise Evaluation Guide) Services
        services.AddScoped<ICapabilityTargetService, CapabilityTargetService>();
        services.AddScoped<ICriticalTaskService, CriticalTaskService>();
        services.AddScoped<IEegEntryService, EegEntryService>();
        services.AddScoped<IEegExportService, EegExportService>();
        services.AddScoped<IEegDocumentService, EegDocumentService>();

        // Feedback Services
        services.AddScoped<IGitHubIssueService, GitHubIssueService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        // Email Services
        services.AddMemoryCache();
        services.AddSingleton<InMemoryEmailTemplateStore>(sp =>
        {
            var store = new InMemoryEmailTemplateStore();
            EmailTemplateRegistrar.RegisterAll(store);
            return store;
        });
        services.AddSingleton<IEmailTemplateStore>(sp => sp.GetRequiredService<InMemoryEmailTemplateStore>());
        services.AddScoped<IEmailTemplateRenderer, PlaceholderEmailTemplateRenderer>();
        services.AddScoped<IEmailLogService, EmailLogService>();
        services.AddScoped<IEmailPreferenceService, EmailPreferenceService>();
        services.AddScoped<AuthenticationEmailService>();
        services.AddScoped<Cadence.Core.Features.Authentication.Services.IEmailService>(sp =>
            sp.GetRequiredService<AuthenticationEmailService>());

        // System Settings Services
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IEmailConfigurationProvider, EmailConfigurationProvider>();

        return services;
    }

    /// <summary>
    /// Adds database context with SQL Server configuration.
    /// Includes organization validation interceptor for write-side data isolation.
    /// </summary>
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException(
                "Database connection string not found. Set 'ConnectionStrings:DefaultConnection' or 'SqlConnectionString'.");

        // Register the organization validation interceptor as a singleton
        // (interceptors must be singletons, but it resolves ICurrentOrganizationContext from a scope internally)
        services.AddSingleton<OrganizationValidationInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);

                sqlOptions.CommandTimeout(30);
            });

            // Add organization validation interceptor for write-side protection
            var orgValidationInterceptor = serviceProvider.GetRequiredService<OrganizationValidationInterceptor>();
            options.AddInterceptors(orgValidationInterceptor);

            // Enable detailed errors in development
#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
        });

        return services;
    }
}
