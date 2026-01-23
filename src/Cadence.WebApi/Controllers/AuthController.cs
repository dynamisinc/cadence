using Cadence.Core.Features.Authentication.Models.DTOs;
using Cadence.Core.Features.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login, and token management.
/// Implements HSEEP-compliant authentication with JWT tokens and HttpOnly refresh tokens.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthenticationService authService,
        ILogger<AuthController> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _authService = authService;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>
    /// Register a new user account.
    /// First user becomes Administrator, subsequent users become Observer.
    /// </summary>
    /// <param name="request">Registration details (email, password, display name).</param>
    /// <returns>Authentication response with access token and user information.</returns>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AuthError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.RegisterAsync(request, ipAddress, deviceInfo);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        // Set refresh token in HttpOnly cookie
        SetRefreshTokenCookie(result.RefreshToken!);

        _logger.LogInformation(
            "User registered successfully: {UserId}, Email: {Email}, IsFirstUser: {IsFirstUser}",
            result.UserId, result.Email, result.IsFirstUser);

        // Return response without refresh token in body (it's in cookie)
        return CreatedAtAction(nameof(Register), new AuthResponse
        {
            IsSuccess = true,
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Role = result.Role,
            AccessToken = result.AccessToken,
            ExpiresIn = result.ExpiresIn,
            Status = result.Status,
            IsFirstUser = result.IsFirstUser,
            IsNewAccount = true
        });
    }

    /// <summary>
    /// Authenticate with email and password.
    /// Returns JWT access token and sets HttpOnly refresh token cookie.
    /// </summary>
    /// <param name="request">Login credentials and "remember me" preference.</param>
    /// <returns>Authentication response with access token and user information.</returns>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AuthError), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.AuthenticateWithPasswordAsync(request, ipAddress, deviceInfo);

        if (!result.IsSuccess)
        {
            // Return 429 for account locked
            if (result.Error?.Code == "account_locked")
            {
                _logger.LogWarning(
                    "Login failed - account locked: {Email}",
                    request.Email);
                return StatusCode(StatusCodes.Status429TooManyRequests, result.Error);
            }

            _logger.LogWarning(
                "Login failed: {Email}, Reason: {ErrorCode}",
                request.Email, result.Error?.Code);

            return Unauthorized(result.Error);
        }

        SetRefreshTokenCookie(result.RefreshToken!);

        _logger.LogInformation(
            "User logged in successfully: {UserId}, Email: {Email}",
            result.UserId, result.Email);

        // Return response without refresh token in body
        return Ok(new AuthResponse
        {
            IsSuccess = true,
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Role = result.Role,
            AccessToken = result.AccessToken,
            ExpiresIn = result.ExpiresIn,
            Status = result.Status
        });
    }

    /// <summary>
    /// Refresh access token using refresh token from HttpOnly cookie.
    /// Issues new access token and rotates refresh token for security.
    /// </summary>
    /// <returns>New access token.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        // Get refresh token from HttpOnly cookie
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Refresh attempt without token cookie");
            return Unauthorized(AuthError.InvalidToken);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

        if (!result.IsSuccess)
        {
            // Clear invalid cookie
            ClearRefreshTokenCookie();

            _logger.LogWarning("Token refresh failed: {ErrorCode}", result.Error?.Code);
            return Unauthorized(result.Error);
        }

        SetRefreshTokenCookie(result.RefreshToken!);

        _logger.LogInformation("Token refreshed successfully: {UserId}", result.UserId);

        return Ok(new AuthResponse
        {
            IsSuccess = true,
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Role = result.Role,
            AccessToken = result.AccessToken,
            ExpiresIn = result.ExpiresIn,
            Status = result.Status
        });
    }

    /// <summary>
    /// Logout current session by revoking refresh token.
    /// Clears HttpOnly refresh token cookie.
    /// </summary>
    /// <returns>No content on success.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeTokenAsync(refreshToken);
            _logger.LogInformation("User logged out successfully");
        }

        ClearRefreshTokenCookie();

        return NoContent();
    }

    /// <summary>
    /// Get available authentication methods.
    /// Used by login UI to display enabled sign-in options.
    /// </summary>
    /// <returns>List of available authentication methods.</returns>
    [HttpGet("methods")]
    [ProducesResponseType(typeof(IReadOnlyList<AuthMethod>), StatusCodes.Status200OK)]
    public IActionResult GetMethods()
    {
        var methods = _authService.GetAvailableMethods();
        return Ok(methods);
    }

    /// <summary>
    /// Request password reset email.
    /// Always returns success to prevent email enumeration.
    /// </summary>
    /// <param name="request">Email address for password reset.</param>
    /// <returns>Success message (always, even if email doesn't exist).</returns>
    [HttpPost("password-reset/request")]
    [EnableRateLimiting("password-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Always returns success to prevent email enumeration
        await _authService.RequestPasswordResetAsync(request.Email, ipAddress);

        _logger.LogInformation("Password reset requested for: {Email}", request.Email);

        return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
    }

    /// <summary>
    /// Complete password reset with token and new password.
    /// Auto-authenticates user on success.
    /// </summary>
    /// <param name="request">Reset token and new password.</param>
    /// <returns>Authentication response with tokens on success, or error details on failure.</returns>
    [HttpPost("password-reset/complete")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompletePasswordReset([FromBody] PasswordResetCompleteRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            ipAddress,
            deviceInfo);

        if (!result.IsSuccess)
        {
            // Return 401 for invalid token errors
            if (result.Error?.Code == "invalid_token")
            {
                _logger.LogWarning("Password reset failed: {ErrorCode}", result.Error.Code);
                return Unauthorized(result.Error);
            }

            // Return 400 for validation errors
            _logger.LogWarning("Password reset failed: {ErrorCode}", result.Error?.Code);
            return BadRequest(result.Error);
        }

        // Set refresh token in HttpOnly cookie
        SetRefreshTokenCookie(result.RefreshToken!);

        _logger.LogInformation("Password reset completed for user: {UserId}", result.UserId);

        // Return response without refresh token in body (it's in cookie)
        return Ok(new AuthResponse
        {
            IsSuccess = true,
            UserId = result.UserId,
            DisplayName = result.DisplayName,
            Email = result.Email,
            Role = result.Role,
            AccessToken = result.AccessToken,
            ExpiresIn = result.ExpiresIn,
            Status = result.Status
        });
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Set refresh token in HttpOnly cookie.
    /// Cookie settings are environment-aware:
    /// - Development: Secure=false, SameSite=Lax (works with HTTP localhost)
    /// - Production: Secure=true, SameSite configured via appsettings (Strict, Lax, or None for cross-origin)
    /// </summary>
    /// <param name="refreshToken">The refresh token to store.</param>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var isDevelopment = _environment.IsDevelopment();

        // For cross-origin production deployments (SWA + App Service on different domains),
        // set Authentication:Cookie:SameSite to "None" in appsettings.Production.json
        // SameSite=None requires Secure=true and works for cross-origin requests
        var sameSiteSetting = _configuration.GetValue<string>("Authentication:Cookie:SameSite");
        var sameSiteMode = sameSiteSetting?.ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "lax" => SameSiteMode.Lax,
            "strict" => SameSiteMode.Strict,
            _ => isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict
        };

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment || sameSiteMode == SameSiteMode.None, // SameSite=None requires Secure
            SameSite = sameSiteMode,
            Expires = DateTimeOffset.UtcNow.AddDays(30) // Max possible expiration
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Clear refresh token cookie (on logout or invalid token).
    /// </summary>
    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken");
    }
}
