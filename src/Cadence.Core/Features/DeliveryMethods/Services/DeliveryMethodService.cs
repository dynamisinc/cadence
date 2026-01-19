using Cadence.Core.Data;
using Cadence.Core.Features.DeliveryMethods.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.DeliveryMethods.Services;

/// <summary>
/// Service for delivery method lookup operations.
/// </summary>
public class DeliveryMethodService : IDeliveryMethodService
{
    private readonly AppDbContext _context;

    public DeliveryMethodService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<DeliveryMethodDto>> GetAllAsync()
    {
        var methods = await _context.DeliveryMethods
            .Where(dm => dm.IsActive)
            .OrderBy(dm => dm.SortOrder)
            .ToListAsync();

        return methods.Select(dm => dm.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<DeliveryMethodDto?> GetByIdAsync(Guid id)
    {
        var method = await _context.DeliveryMethods.FindAsync(id);
        return method?.ToDto();
    }
}
