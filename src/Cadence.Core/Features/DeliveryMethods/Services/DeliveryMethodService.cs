using Cadence.Core.Data;
using Cadence.Core.Features.DeliveryMethods.Models.DTOs;
using Cadence.Core.Models.Entities;
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
    public async Task<List<DeliveryMethodDto>> GetAllIncludingInactiveAsync()
    {
        var methods = await _context.DeliveryMethods
            .OrderBy(dm => dm.SortOrder)
            .ThenBy(dm => dm.Name)
            .ToListAsync();

        return methods.Select(dm => dm.ToDto()).ToList();
    }

    /// <inheritdoc />
    public async Task<DeliveryMethodDto?> GetByIdAsync(Guid id)
    {
        var method = await _context.DeliveryMethods.FindAsync(id);
        return method?.ToDto();
    }

    /// <inheritdoc />
    public async Task<DeliveryMethodDto> CreateAsync(CreateDeliveryMethodRequest request)
    {
        var trimmedName = request.Name.Trim();
        if (string.IsNullOrEmpty(trimmedName))
            throw new ArgumentException("Name cannot be empty.");

        if (trimmedName.Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters.");

        var trimmedDescription = request.Description?.Trim();
        if (trimmedDescription?.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.");

        // Check for duplicate name (case-insensitive)
        var exists = await _context.DeliveryMethods
            .AnyAsync(dm => dm.Name.ToLower() == trimmedName.ToLower());

        if (exists)
            throw new InvalidOperationException($"A delivery method with name '{trimmedName}' already exists.");

        // If IsOther, verify no other active method has IsOther=true
        if (request.IsOther)
        {
            var otherExists = await _context.DeliveryMethods
                .AnyAsync(dm => dm.IsOther);

            if (otherExists)
                throw new InvalidOperationException("Only one delivery method can be marked as 'Other'.");
        }

        var method = new DeliveryMethodLookup
        {
            Name = trimmedName,
            Description = trimmedDescription,
            SortOrder = request.SortOrder,
            IsOther = request.IsOther,
            IsActive = true,
        };

        _context.DeliveryMethods.Add(method);
        await _context.SaveChangesAsync();

        return method.ToDto();
    }

    /// <inheritdoc />
    public async Task<DeliveryMethodDto?> UpdateAsync(Guid id, UpdateDeliveryMethodRequest request)
    {
        var method = await _context.DeliveryMethods.FindAsync(id);
        if (method == null)
            return null;

        var trimmedName = request.Name.Trim();
        if (string.IsNullOrEmpty(trimmedName))
            throw new ArgumentException("Name cannot be empty.");

        if (trimmedName.Length > 100)
            throw new ArgumentException("Name cannot exceed 100 characters.");

        var trimmedDescription = request.Description?.Trim();
        if (trimmedDescription?.Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.");

        // Check for duplicate name (case-insensitive, excluding self)
        var duplicate = await _context.DeliveryMethods
            .AnyAsync(dm => dm.Id != id && dm.Name.ToLower() == trimmedName.ToLower());

        if (duplicate)
            throw new InvalidOperationException($"A delivery method with name '{trimmedName}' already exists.");

        // If setting IsOther, verify no other method has IsOther=true
        if (request.IsOther && !method.IsOther)
        {
            var otherExists = await _context.DeliveryMethods
                .AnyAsync(dm => dm.Id != id && dm.IsOther);

            if (otherExists)
                throw new InvalidOperationException("Only one delivery method can be marked as 'Other'.");
        }

        method.Name = trimmedName;
        method.Description = trimmedDescription;
        method.SortOrder = request.SortOrder;
        method.IsActive = request.IsActive;
        method.IsOther = request.IsOther;

        await _context.SaveChangesAsync();

        return method.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        var method = await _context.DeliveryMethods.FindAsync(id);
        if (method == null)
            return false;

        method.IsDeleted = true;
        method.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc />
    public async Task ReorderAsync(List<Guid> orderedIds)
    {
        var methods = await _context.DeliveryMethods.ToListAsync();
        var lookup = methods.ToDictionary(dm => dm.Id);

        for (var i = 0; i < orderedIds.Count; i++)
        {
            if (lookup.TryGetValue(orderedIds[i], out var method))
            {
                method.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync();
    }
}
