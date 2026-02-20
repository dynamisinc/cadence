using Cadence.Core.Features.DeliveryMethods.Models.DTOs;

namespace Cadence.Core.Features.DeliveryMethods.Services;

/// <summary>
/// Service interface for delivery method lookup operations.
/// Delivery methods are system-level reference data.
/// Read operations available to all users; write operations for admins only.
/// </summary>
public interface IDeliveryMethodService
{
    /// <summary>
    /// Gets all active delivery methods, ordered by SortOrder.
    /// </summary>
    Task<List<DeliveryMethodDto>> GetAllAsync();

    /// <summary>
    /// Gets all delivery methods including inactive, ordered by SortOrder. Admin only.
    /// </summary>
    Task<List<DeliveryMethodDto>> GetAllIncludingInactiveAsync();

    /// <summary>
    /// Gets a single delivery method by ID.
    /// </summary>
    Task<DeliveryMethodDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new delivery method. Admin only.
    /// </summary>
    Task<DeliveryMethodDto> CreateAsync(CreateDeliveryMethodRequest request);

    /// <summary>
    /// Updates an existing delivery method. Admin only.
    /// </summary>
    Task<DeliveryMethodDto?> UpdateAsync(Guid id, UpdateDeliveryMethodRequest request);

    /// <summary>
    /// Soft-deletes a delivery method. Admin only.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Reorders delivery methods by updating SortOrder. Admin only.
    /// </summary>
    Task ReorderAsync(List<Guid> orderedIds);
}
