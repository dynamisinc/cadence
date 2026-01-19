using Cadence.Core.Features.DeliveryMethods.Models.DTOs;
using Cadence.Core.Features.DeliveryMethods.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for delivery method lookup data.
/// Delivery methods are system-level reference data (read-only).
/// </summary>
[ApiController]
[Route("api/delivery-methods")]
public class DeliveryMethodsController : ControllerBase
{
    private readonly IDeliveryMethodService _service;
    private readonly ILogger<DeliveryMethodsController> _logger;

    public DeliveryMethodsController(
        IDeliveryMethodService service,
        ILogger<DeliveryMethodsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all active delivery methods.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryMethodDto>>> GetAll()
    {
        var methods = await _service.GetAllAsync();
        return Ok(methods);
    }

    /// <summary>
    /// Get a single delivery method by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryMethodDto>> GetById(Guid id)
    {
        var method = await _service.GetByIdAsync(id);
        if (method == null)
        {
            return NotFound(new { message = "Delivery method not found" });
        }

        return Ok(method);
    }
}
