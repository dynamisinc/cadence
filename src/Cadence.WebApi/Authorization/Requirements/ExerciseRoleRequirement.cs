using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Cadence.WebApi.Authorization.Requirements;

/// <summary>
/// Requirement: User must have at least the specified HSEEP role in an exercise.
/// System Admins automatically satisfy all role requirements.
/// </summary>
public class ExerciseRoleRequirement : IAuthorizationRequirement
{
    public ExerciseRole MinimumRole { get; }

    public ExerciseRoleRequirement(ExerciseRole minimumRole)
    {
        MinimumRole = minimumRole;
    }
}
