namespace Cadence.WebApi.Authorization;

/// <summary>
/// Constants for authorization policy names.
/// Use the corresponding custom attributes instead of these constants directly.
/// </summary>
public static class PolicyNames
{
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireManager = nameof(RequireManager);
    public const string RequireOrgAdmin = nameof(RequireOrgAdmin);
    public const string ExerciseAccess = nameof(ExerciseAccess);
    public const string ExerciseController = nameof(ExerciseController);
    public const string ExerciseDirector = nameof(ExerciseDirector);
    public const string ExerciseEvaluator = nameof(ExerciseEvaluator);
}
