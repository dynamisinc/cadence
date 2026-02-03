using System.Text.RegularExpressions;
using Cadence.Core.Data;
using Cadence.Core.Features.Organizations.Mappers;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Organizations.Services;

/// <summary>
/// Service for organization management operations.
/// Handles organization CRUD, slug validation, and lifecycle management.
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(AppDbContext context, ILogger<OrganizationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<OrganizationListItemDto> Items, int TotalCount)> GetOrganizationsAsync(
        string? search = null,
        OrgStatus? status = null,
        string sortBy = "name",
        string sortDir = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = _context.Organizations
            .AsNoTracking()
            .Where(o => !o.IsDeleted);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(searchLower) ||
                o.Slug.ToLower().Contains(searchLower));
        }

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "name" => sortDir.ToLower() == "desc"
                ? query.OrderByDescending(o => o.Name)
                : query.OrderBy(o => o.Name),
            "slug" => sortDir.ToLower() == "desc"
                ? query.OrderByDescending(o => o.Slug)
                : query.OrderBy(o => o.Slug),
            "status" => sortDir.ToLower() == "desc"
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            "createdat" => sortDir.ToLower() == "desc"
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt),
            _ => query.OrderBy(o => o.Name)
        };

        // Project to list item DTOs with counts
        var items = await query
            .Select(o => new
            {
                Organization = o,
                UserCount = o.Memberships.Count(m => m.Status == MembershipStatus.Active && !m.IsDeleted),
                ExerciseCount = o.Exercises.Count(e => !e.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => x.Organization.ToListItemDto(x.UserCount, x.ExerciseCount));

        return (dtos, totalCount);
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        return organization?.ToDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto> CreateAsync(
        CreateOrganizationRequest request,
        string createdByUserId,
        CancellationToken cancellationToken = default)
    {
        // Validate slug uniqueness
        var slugExists = await _context.Organizations
            .AnyAsync(o => o.Slug.ToLower() == request.Slug.ToLower(), cancellationToken);

        if (slugExists)
        {
            throw new InvalidOperationException($"An organization with slug '{request.Slug}' already exists.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Create organization
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description,
                ContactEmail = request.ContactEmail,
                Status = OrgStatus.Active,
                CreatedBy = createdByUserId,
                ModifiedBy = createdByUserId
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync(cancellationToken);

            // Find or create user
            var user = await _context.Set<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Email!.ToLower() == request.FirstAdminEmail.ToLower(), cancellationToken);

            if (user == null)
            {
                // Create new pending user
                // Extract display name from email - use part before @ or full email if no @
                var atIndex = request.FirstAdminEmail.IndexOf('@');
                var displayName = atIndex > 0 ? request.FirstAdminEmail[..atIndex] : request.FirstAdminEmail;

                user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(), // Identity uses string IDs
                    Email = request.FirstAdminEmail,
                    UserName = request.FirstAdminEmail, // Identity requires UserName
                    DisplayName = displayName, // Default display name from email
                    OrganizationId = organization.Id,
                    CurrentOrganizationId = organization.Id,
                    Status = UserStatus.Pending,
                    SystemRole = SystemRole.User,
                    CreatedById = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<ApplicationUser>().Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Created pending user {UserId} with email {Email} for organization {OrgId}",
                    user.Id, user.Email, organization.Id);
            }
            else if (user.Status == UserStatus.Pending)
            {
                // Activate pending user
                user.Status = UserStatus.Active;
                user.CurrentOrganizationId = organization.Id;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Create organization membership for first admin
            var membership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                UserId = user.Id,
                Role = OrgRole.OrgAdmin,
                Status = MembershipStatus.Active,
                JoinedAt = DateTime.UtcNow,
                InvitedById = createdByUserId
            };

            _context.OrganizationMemberships.Add(membership);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Created organization {OrgId} '{Name}' with slug '{Slug}' and first admin {UserId}",
                organization.Id, organization.Name, organization.Slug, user.Id);

            return organization.ToDto();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> UpdateAsync(
        Guid id,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization == null)
        {
            return null;
        }

        organization.Name = request.Name;
        organization.Description = request.Description;
        organization.ContactEmail = request.ContactEmail;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated organization {OrgId}", id);

        return organization.ToDto();
    }

    /// <inheritdoc />
    public async Task<SlugCheckResponse> CheckSlugAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var slugLower = slug.ToLower();

        var query = _context.Organizations
            .Where(o => o.Slug.ToLower() == slugLower && !o.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);

        if (!exists)
        {
            return new SlugCheckResponse(true, null);
        }

        // Generate suggestion by appending number
        var baseSlugsPattern = $"{slugLower}-%";
        var existingSlugs = await _context.Organizations
            .Where(o => EF.Functions.Like(o.Slug, baseSlugsPattern) && !o.IsDeleted)
            .Select(o => o.Slug)
            .ToListAsync(cancellationToken);

        var maxNumber = 1;
        foreach (var existingSlug in existingSlugs)
        {
            var match = Regex.Match(existingSlug, $@"^{Regex.Escape(slugLower)}-(\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var num))
            {
                maxNumber = Math.Max(maxNumber, num);
            }
        }

        var suggestion = $"{slugLower}-{maxNumber + 1}";

        return new SlugCheckResponse(false, suggestion);
    }

    /// <inheritdoc />
    public string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        // Convert to lowercase
        var slug = name.ToLower();

        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");

        // Remove special characters (keep only alphanumeric and hyphens)
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Collapse multiple hyphens to single
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> ArchiveAsync(
        Guid id,
        string archivedByUserId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization == null)
        {
            return null;
        }

        organization.Status = OrgStatus.Archived;
        organization.ModifiedBy = archivedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Archived organization {OrgId} by user {UserId}",
            id, archivedByUserId);

        return organization.ToDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> DeactivateAsync(
        Guid id,
        string deactivatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization == null)
        {
            return null;
        }

        organization.Status = OrgStatus.Inactive;
        organization.ModifiedBy = deactivatedByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deactivated organization {OrgId} by user {UserId}",
            id, deactivatedByUserId);

        return organization.ToDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> RestoreAsync(
        Guid id,
        string restoredByUserId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization == null)
        {
            return null;
        }

        organization.Status = OrgStatus.Active;
        organization.ModifiedBy = restoredByUserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Restored organization {OrgId} to Active by user {UserId}",
            id, restoredByUserId);

        return organization.ToDto();
    }

    /// <inheritdoc />
    public async Task<OrganizationDto?> UpdateApprovalPolicyAsync(
        Guid id,
        ApprovalPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization == null)
        {
            return null;
        }

        var oldPolicy = organization.InjectApprovalPolicy;
        organization.InjectApprovalPolicy = policy;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated organization {OrgId} inject approval policy from {OldPolicy} to {NewPolicy}",
            id, oldPolicy, policy);

        return organization.ToDto();
    }
}
