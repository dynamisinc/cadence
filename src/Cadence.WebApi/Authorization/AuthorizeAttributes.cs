using Microsoft.AspNetCore.Authorization;

namespace Cadence.WebApi.Authorization;

/// <summary>
/// Requires the user to have Admin system role.
/// </summary>
public class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute() => Policy = PolicyNames.RequireAdmin;
}

/// <summary>
/// Requires the user to have Admin or Manager system role.
/// </summary>
public class AuthorizeManagerAttribute : AuthorizeAttribute
{
    public AuthorizeManagerAttribute() => Policy = PolicyNames.RequireManager;
}

/// <summary>
/// Requires the user to have any role assignment for the exercise.
/// The exercise ID must be provided via route parameter named "exerciseId".
/// </summary>
public class AuthorizeExerciseAccessAttribute : AuthorizeAttribute
{
    public AuthorizeExerciseAccessAttribute() => Policy = PolicyNames.ExerciseAccess;
}

/// <summary>
/// Requires the user to have the Controller role for the exercise.
/// The exercise ID must be provided via route parameter named "exerciseId".
/// </summary>
public class AuthorizeExerciseControllerAttribute : AuthorizeAttribute
{
    public AuthorizeExerciseControllerAttribute() => Policy = PolicyNames.ExerciseController;
}

/// <summary>
/// Requires the user to have the Exercise Director role for the exercise.
/// The exercise ID must be provided via route parameter named "exerciseId".
/// </summary>
public class AuthorizeExerciseDirectorAttribute : AuthorizeAttribute
{
    public AuthorizeExerciseDirectorAttribute() => Policy = PolicyNames.ExerciseDirector;
}

/// <summary>
/// Requires the user to have the Evaluator role for the exercise.
/// The exercise ID must be provided via route parameter named "exerciseId".
/// </summary>
public class AuthorizeExerciseEvaluatorAttribute : AuthorizeAttribute
{
    public AuthorizeExerciseEvaluatorAttribute() => Policy = PolicyNames.ExerciseEvaluator;
}
