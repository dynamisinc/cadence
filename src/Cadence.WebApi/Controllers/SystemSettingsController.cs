using System.Security.Claims;
using Cadence.Core.Features.SystemSettings.Models.DTOs;
using Cadence.Core.Features.SystemSettings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

[ApiController]
[Route("api/system-settings")]
[Authorize(Roles = "Admin")]
public class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingsService _settingsService;

    public SystemSettingsController(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _settingsService.GetSettingsAsync();
        return Ok(settings);
    }

    [HttpPut]
    [ProducesResponseType(typeof(SystemSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateSystemSettingsRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "unknown";
        var settings = await _settingsService.UpdateSettingsAsync(request, email);
        return Ok(settings);
    }
}
