using Cadence.Core.Features.DeliveryMethods.Models.DTOs;
using Cadence.Core.Features.DeliveryMethods.Services;
using Cadence.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// API endpoints for delivery method lookup data.
/// Read endpoints available to all authenticated users.
/// Write endpoints restricted to system administrators.
/// </summary>
[ApiController]
[Route("api/delivery-methods")]
[Authorize]
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
    /// Get all delivery methods including inactive (admin only).
    /// </summary>
    [HttpGet("all")]
    [AuthorizeAdmin]
    public async Task<ActionResult<IEnumerable<DeliveryMethodDto>>> GetAllIncludingInactive()
    {
        var methods = await _service.GetAllIncludingInactiveAsync();
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

    /// <summary>
    /// Create a new delivery method (admin only).
    /// </summary>
    [HttpPost]
    [AuthorizeAdmin]
    public async Task<ActionResult<DeliveryMethodDto>> Create(
        [FromBody] CreateDeliveryMethodRequest request)
    {
        try
        {
            var method = await _service.CreateAsync(request);
            _logger.LogInformation("Delivery method '{Name}' created by admin", method.Name);
            return CreatedAtAction(nameof(GetById), new { id = method.Id }, method);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a delivery method (admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [AuthorizeAdmin]
    public async Task<ActionResult<DeliveryMethodDto>> Update(
        Guid id, [FromBody] UpdateDeliveryMethodRequest request)
    {
        try
        {
            var method = await _service.UpdateAsync(id, request);
            if (method == null)
                return NotFound(new { message = "Delivery method not found" });

            _logger.LogInformation("Delivery method '{Name}' updated by admin", method.Name);
            return Ok(method);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Soft-delete a delivery method (admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [AuthorizeAdmin]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Delivery method not found" });

        _logger.LogInformation("Delivery method {Id} deleted by admin", id);
        return NoContent();
    }

    /// <summary>
    /// Reorder delivery methods (admin only).
    /// </summary>
    [HttpPut("reorder")]
    [AuthorizeAdmin]
    public async Task<IActionResult> Reorder([FromBody] List<Guid> orderedIds)
    {
        await _service.ReorderAsync(orderedIds);
        _logger.LogInformation("Delivery methods reordered by admin");
        return NoContent();
    }
}
