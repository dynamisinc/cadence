using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Organizations.Services;

/// <summary>
/// Service interface for organization management operations.
/// Handles organization CRUD, slug validation, and lifecycle management.
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Gets a paginated, filtered, and sorted list of organizations.
    /// </summary>
    /// <param name="search">Optional search term (filters by name or slug)</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="sortBy">Sort column (name, slug, status, userCount, exerciseCount, createdAt)</param>
    /// <param name="sortDir">Sort direction (asc or desc)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of organization list items and total count</returns>
    Task<(IEnumerable<OrganizationListItemDto> Items, int TotalCount)> GetOrganizationsAsync(
        string? search = null,
        OrgStatus? status = null,
        string sortBy = "name",
        string sortDir = "asc",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single organization by ID.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Organization DTO or null if not found</returns>
    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new organization with its first administrator.
    /// Creates organization and membership atomically.
    /// If the email doesn't exist, creates a pending user and sends invitation.
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <param name="createdByUserId">ID of the SysAdmin creating the org</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created organization DTO</returns>
    Task<OrganizationDto> CreateAsync(
        CreateOrganizationRequest request,
        string createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing organization.
    /// Slug cannot be changed after creation.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated organization DTO or null if not found</returns>
    Task<OrganizationDto?> UpdateAsync(
        Guid id,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a slug is available.
    /// </summary>
    /// <param name="slug">Slug to check</param>
    /// <param name="excludeId">Optional organization ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Slug availability response with optional suggestion</returns>
    Task<SlugCheckResponse> CheckSlugAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a URL-safe slug from a name.
    /// Rules: lowercase, spaces to hyphens, remove special chars, collapse multiple hyphens.
    /// </summary>
    /// <param name="name">Organization name</param>
    /// <returns>Generated slug</returns>
    string GenerateSlug(string name);

    /// <summary>
    /// Archives an organization (Active/Inactive → Archived).
    /// Organization becomes read-only for members.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="archivedByUserId">ID of the SysAdmin archiving the org</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated organization DTO</returns>
    Task<OrganizationDto?> ArchiveAsync(
        Guid id,
        string archivedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates an organization (Active/Archived → Inactive).
    /// Organization becomes hidden from non-SysAdmin users.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="deactivatedByUserId">ID of the SysAdmin deactivating the org</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated organization DTO</returns>
    Task<OrganizationDto?> DeactivateAsync(
        Guid id,
        string deactivatedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an organization (Archived/Inactive → Active).
    /// Organization becomes fully operational again.
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="restoredByUserId">ID of the SysAdmin restoring the org</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated organization DTO</returns>
    Task<OrganizationDto?> RestoreAsync(
        Guid id,
        string restoredByUserId,
        CancellationToken cancellationToken = default);
}
