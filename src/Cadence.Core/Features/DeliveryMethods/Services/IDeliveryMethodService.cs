using Cadence.Core.Features.DeliveryMethods.Models.DTOs;

namespace Cadence.Core.Features.DeliveryMethods.Services;

/// <summary>
/// Service interface for delivery method lookup operations.
/// Delivery methods are system-level reference data (read-only for users).
/// </summary>
public interface IDeliveryMethodService
{
    /// <summary>
    /// Gets all active delivery methods, ordered by SortOrder.
    /// </summary>
    Task<List<DeliveryMethodDto>> GetAllAsync();

    /// <summary>
    /// Gets a single delivery method by ID.
    /// </summary>
    Task<DeliveryMethodDto?> GetByIdAsync(Guid id);
}
