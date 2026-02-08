using Cadence.Core.Data;
using Cadence.Core.Exceptions;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Organizations.Services;

/// <summary>
/// Service for managing user-organization memberships.
/// Handles assignment, role changes, and validation of memberships.
/// </summary>
public class MembershipService : IMembershipService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MembershipService> _logger;

    public MembershipService(AppDbContext context, ILogger<MembershipService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MembershipDto>> GetUserMembershipsAsync(string userId, CancellationToken ct = default)
    {
        // IgnoreQueryFilters: users must see ALL their memberships across orgs
        // (not just the current org) so they can switch organizations.
        var memberships = await _context.OrganizationMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active && !m.IsDeleted && !m.Organization.IsDeleted)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new MembershipDto(
                m.Id,
                m.UserId,
                m.OrganizationId,
                m.Organization.Name,
                m.Organization.Slug,
                m.Role.ToString(),
                m.JoinedAt,
                false // IsCurrent will be set by caller based on CurrentOrganizationId
            ))
            .ToListAsync(ct);

        _logger.LogDebug("Retrieved {Count} memberships for user {UserId}", memberships.Count, userId);

        return memberships;
    }

    /// <inheritdoc />
    public async Task<MembershipDto> AssignUserToOrganizationAsync(
        string userId,
        AssignUserRequest request,
        string assignedByUserId,
        CancellationToken ct = default)
    {
        // Validate user exists
        var user = await _context.Set<ApplicationUser>().FindAsync(new object[] { userId }, ct);
        if (user == null)
        {
            throw new NotFoundException($"User with ID '{userId}' not found.");
        }

        // Validate organization exists
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, ct);

        if (organization == null)
        {
            throw new NotFoundException($"Organization with ID '{request.OrganizationId}' not found.");
        }

        // Check for existing membership
        var existingMembership = await _context.OrganizationMemberships
            .FirstOrDefaultAsync(
                m => m.UserId == userId && m.OrganizationId == request.OrganizationId,
                ct);

        if (existingMembership != null)
        {
            if (existingMembership.Status == MembershipStatus.Active)
            {
                throw new ConflictException(
                    $"User already has active membership in organization '{organization.Name}'.");
            }

            // Reactivate inactive membership
            existingMembership.Status = MembershipStatus.Active;
            existingMembership.Role = request.Role;
            existingMembership.InvitedById = assignedByUserId;

            _logger.LogInformation(
                "Reactivated membership {MembershipId} for user {UserId} in organization {OrgId} with role {Role}",
                existingMembership.Id,
                userId,
                request.OrganizationId,
                request.Role);
        }
        else
        {
            // Create new membership
            existingMembership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = request.OrganizationId,
                Role = request.Role,
                Status = MembershipStatus.Active,
                JoinedAt = DateTime.UtcNow,
                InvitedById = assignedByUserId
            };

            _context.OrganizationMemberships.Add(existingMembership);

            _logger.LogInformation(
                "Created membership {MembershipId} for user {UserId} in organization {OrgId} with role {Role}",
                existingMembership.Id,
                userId,
                request.OrganizationId,
                request.Role);
        }

        // If this is the user's first organization, change status from Pending to Active
        var hasOtherMemberships = await _context.OrganizationMemberships
            .AnyAsync(
                m => m.UserId == userId
                    && m.Id != existingMembership.Id
                    && m.Status == MembershipStatus.Active,
                ct);

        if (!hasOtherMemberships && user.Status == UserStatus.Pending)
        {
            user.Status = UserStatus.Active;
            user.CurrentOrganizationId = request.OrganizationId;

            _logger.LogInformation(
                "User {UserId} status changed from Pending to Active (first organization assignment)",
                userId);
        }

        await _context.SaveChangesAsync(ct);

        return new MembershipDto(
            existingMembership.Id,
            userId,
            request.OrganizationId,
            organization.Name,
            organization.Slug,
            request.Role.ToString(),
            existingMembership.JoinedAt,
            false
        );
    }

    /// <inheritdoc />
    public async Task<MembershipDto> UpdateMembershipRoleAsync(
        Guid membershipId,
        UpdateMembershipRequest request,
        CancellationToken ct = default)
    {
        var membership = await _context.OrganizationMemberships
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership == null)
        {
            throw new NotFoundException($"Membership with ID '{membershipId}' not found.");
        }

        // Validate role change
        await ValidateCanChangeRoleAsync(membershipId, request.Role, ct);

        membership.Role = request.Role;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated membership {MembershipId} role to {Role}",
            membershipId,
            request.Role);

        return new MembershipDto(
            membership.Id,
            membership.UserId,
            membership.OrganizationId,
            membership.Organization.Name,
            membership.Organization.Slug,
            membership.Role.ToString(),
            membership.JoinedAt,
            false
        );
    }

    /// <inheritdoc />
    public async Task<RemoveMembershipResponse> RemoveMembershipAsync(Guid membershipId, string deletedBy, CancellationToken ct = default)
    {
        var membership = await _context.OrganizationMemberships
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership == null)
        {
            throw new NotFoundException($"Membership with ID '{membershipId}' not found.");
        }

        // Check if removing last admin
        if (membership.Role == OrgRole.OrgAdmin)
        {
            var adminCount = await _context.OrganizationMemberships
                .CountAsync(
                    m => m.OrganizationId == membership.OrganizationId
                        && m.Role == OrgRole.OrgAdmin
                        && m.Status == MembershipStatus.Active,
                    ct);

            if (adminCount <= 1)
            {
                throw new BusinessRuleException(
                    "Cannot remove the last administrator from an organization. " +
                    "Assign another administrator first or deactivate the organization.");
            }
        }

        // Soft delete the membership
        membership.IsDeleted = true;
        membership.DeletedAt = DateTime.UtcNow;
        membership.DeletedBy = deletedBy;

        _logger.LogInformation(
            "Removed membership {MembershipId} for user {UserId} from organization {OrgId}",
            membershipId,
            membership.UserId,
            membership.OrganizationId);

        // Check if user has any other active memberships
        var hasOtherMemberships = await _context.OrganizationMemberships
            .AnyAsync(
                m => m.UserId == membership.UserId
                    && m.Id != membershipId
                    && m.Status == MembershipStatus.Active,
                ct);

        bool userStatusChanged = false;
        string newUserStatus = UserStatus.Active.ToString();

        // If this was the user's last organization, change status to Pending
        if (!hasOtherMemberships)
        {
            var user = await _context.Set<ApplicationUser>().FindAsync(new object[] { membership.UserId }, ct);
            if (user != null && user.Status == UserStatus.Active)
            {
                user.Status = UserStatus.Pending;
                user.CurrentOrganizationId = null;
                userStatusChanged = true;
                newUserStatus = UserStatus.Pending.ToString();

                _logger.LogInformation(
                    "User {UserId} status changed from Active to Pending (last organization removed)",
                    membership.UserId);
            }
        }

        await _context.SaveChangesAsync(ct);

        return new RemoveMembershipResponse(
            true,
            userStatusChanged,
            newUserStatus
        );
    }

    /// <inheritdoc />
    public async Task<bool> HasMembershipAsync(string userId, Guid organizationId, CancellationToken ct = default)
    {
        return await _context.OrganizationMemberships
            .AnyAsync(
                m => m.UserId == userId
                    && m.OrganizationId == organizationId
                    && m.Status == MembershipStatus.Active,
                ct);
    }

    /// <inheritdoc />
    public async Task<OrgRole?> GetUserRoleInOrganizationAsync(
        string userId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var membership = await _context.OrganizationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.UserId == userId
                    && m.OrganizationId == organizationId
                    && m.Status == MembershipStatus.Active,
                ct);

        return membership?.Role;
    }

    /// <inheritdoc />
    public async Task ValidateCanChangeRoleAsync(Guid membershipId, OrgRole newRole, CancellationToken ct = default)
    {
        var membership = await _context.OrganizationMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == membershipId, ct);

        if (membership == null)
        {
            throw new NotFoundException($"Membership with ID '{membershipId}' not found.");
        }

        // If demoting from OrgAdmin, ensure there's at least one other admin
        if (membership.Role == OrgRole.OrgAdmin && newRole != OrgRole.OrgAdmin)
        {
            var adminCount = await _context.OrganizationMemberships
                .CountAsync(
                    m => m.OrganizationId == membership.OrganizationId
                        && m.Role == OrgRole.OrgAdmin
                        && m.Status == MembershipStatus.Active,
                    ct);

            if (adminCount <= 1)
            {
                throw new BusinessRuleException(
                    "Cannot change role of the last administrator. " +
                    "Organization must have at least one administrator.");
            }
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrgMemberDto>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default)
    {
        var members = await _context.OrganizationMemberships
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId && m.Status == MembershipStatus.Active)
            .OrderBy(m => m.User.DisplayName)
            .Select(m => new OrgMemberDto(
                m.Id,
                m.UserId,
                m.User.Email ?? "",
                m.User.DisplayName ?? "",
                m.Role.ToString(),
                m.JoinedAt
            ))
            .ToListAsync(ct);

        _logger.LogDebug("Retrieved {Count} members for organization {OrgId}", members.Count, organizationId);

        return members;
    }

    /// <inheritdoc />
    public async Task<OrgMemberDto> AddMemberByEmailAsync(
        Guid organizationId,
        AddMemberRequest request,
        string addedByUserId,
        CancellationToken ct = default)
    {
        // Validate organization exists
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, ct);

        if (organization == null)
        {
            throw new NotFoundException($"Organization with ID '{organizationId}' not found.");
        }

        // Find user by email
        var user = await _context.Set<ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user == null)
        {
            throw new NotFoundException($"User with email '{request.Email}' not found. User must be registered first.");
        }

        // Check for existing active membership
        var existingMembership = await _context.OrganizationMemberships
            .FirstOrDefaultAsync(
                m => m.UserId == user.Id && m.OrganizationId == organizationId && m.Status == MembershipStatus.Active,
                ct);

        if (existingMembership != null)
        {
            throw new ConflictException($"User '{request.Email}' is already a member of this organization.");
        }

        // Check for existing inactive membership to reactivate
        var inactiveMembership = await _context.OrganizationMemberships
            .FirstOrDefaultAsync(
                m => m.UserId == user.Id && m.OrganizationId == organizationId && m.Status != MembershipStatus.Active,
                ct);

        OrganizationMembership membership;
        if (inactiveMembership != null)
        {
            // Reactivate
            inactiveMembership.Status = MembershipStatus.Active;
            inactiveMembership.Role = request.Role;
            inactiveMembership.InvitedById = addedByUserId;
            inactiveMembership.IsDeleted = false;
            inactiveMembership.DeletedAt = null;
            membership = inactiveMembership;

            _logger.LogInformation(
                "Reactivated membership {MembershipId} for user {Email} in organization {OrgId}",
                membership.Id, request.Email, organizationId);
        }
        else
        {
            // Create new membership
            membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organizationId,
                Role = request.Role,
                Status = MembershipStatus.Active,
                JoinedAt = DateTime.UtcNow,
                InvitedById = addedByUserId
            };
            _context.OrganizationMemberships.Add(membership);

            _logger.LogInformation(
                "Created membership {MembershipId} for user {Email} in organization {OrgId}",
                membership.Id, request.Email, organizationId);
        }

        // If this is the user's first organization, activate them
        var hasOtherMemberships = await _context.OrganizationMemberships
            .AnyAsync(
                m => m.UserId == user.Id
                    && m.Id != membership.Id
                    && m.Status == MembershipStatus.Active,
                ct);

        if (!hasOtherMemberships && user.Status == UserStatus.Pending)
        {
            user.Status = UserStatus.Active;
            user.CurrentOrganizationId = organizationId;

            _logger.LogInformation(
                "User {Email} status changed from Pending to Active (first organization assignment)",
                request.Email);
        }

        await _context.SaveChangesAsync(ct);

        return new OrgMemberDto(
            membership.Id,
            user.Id,
            user.Email ?? "",
            user.DisplayName ?? "",
            request.Role.ToString(),
            membership.JoinedAt
        );
    }
}
