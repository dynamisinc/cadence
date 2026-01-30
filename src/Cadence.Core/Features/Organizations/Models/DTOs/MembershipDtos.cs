using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Organizations.Models.DTOs;

/// <summary>
/// DTO representing a user's membership in an organization.
/// </summary>
public record MembershipDto(
    Guid Id,
    string UserId,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    string Role,
    DateTime JoinedAt,
    bool IsCurrent
);

/// <summary>
/// Request to assign a user to an organization.
/// </summary>
public record AssignUserRequest(
    Guid OrganizationId,
    OrgRole Role
);

/// <summary>
/// Request to update a user's role in an organization membership.
/// </summary>
public record UpdateMembershipRequest(
    OrgRole Role
);

/// <summary>
/// Response containing a user's organization memberships.
/// </summary>
public record UserOrganizationsResponse(
    Guid? CurrentOrganizationId,
    IEnumerable<MembershipDto> Memberships
);

/// <summary>
/// Request to switch the user's current organization context.
/// </summary>
public record SwitchOrganizationRequest(
    Guid OrganizationId
);

/// <summary>
/// Response after successfully switching organization context.
/// Contains updated organization info and a new JWT with updated org claims.
/// </summary>
public record SwitchOrganizationResponse(
    Guid OrganizationId,
    string OrganizationName,
    string Role,
    string NewToken
);

/// <summary>
/// DTO for user in admin list view with organization memberships.
/// </summary>
public record AdminUserListItemDto(
    string Id,
    string Email,
    string DisplayName,
    string Status,
    DateTime CreatedAt,
    IEnumerable<MembershipDto> Memberships
);

/// <summary>
/// Response for removing a user from an organization.
/// </summary>
public record RemoveMembershipResponse(
    bool Removed,
    bool UserStatusChanged,
    string NewUserStatus
);

/// <summary>
/// DTO representing a member of an organization (for org members list view).
/// </summary>
public record OrgMemberDto(
    Guid MembershipId,
    string UserId,
    string Email,
    string DisplayName,
    string Role,
    DateTime JoinedAt
);

/// <summary>
/// Request to add a user to an organization by email.
/// </summary>
public record AddMemberRequest(
    string Email,
    OrgRole Role
);
