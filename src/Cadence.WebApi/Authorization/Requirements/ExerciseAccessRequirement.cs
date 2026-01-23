using Microsoft.AspNetCore.Authorization;

namespace Cadence.WebApi.Authorization.Requirements;

/// <summary>
/// Requirement: User must be able to access a specific exercise.
/// System Admins can access all exercises.
/// Other users must be assigned as participants.
/// </summary>
public class ExerciseAccessRequirement : IAuthorizationRequirement
{
}
