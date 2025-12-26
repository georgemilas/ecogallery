
using GalleryLib.Model.Auth;
using Microsoft.AspNetCore.Mvc;
using GalleryApi.model;
using GalleryApi.service.auth;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserAuthService _authService;
    private readonly IConfiguration _config;
    
    public AuthController(UserAuthService authService, IConfiguration config)
    {
        _config = config;
        _authService = authService;        
    }
    
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        
        var response = await _authService.LoginAsync(request, ipAddress, userAgent);
        
        if (response.Success)
        {
            // Set session token in cookie
            // Secure should be false for HTTP dev, true for HTTPS. Use request scheme to decide.
            var isHttps = HttpContext.Request.IsHttps;

            Response.Cookies.Append("session_token", response.SessionToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });            
            return Ok(new { success = true, message = response.Message, user = response.User, sessionToken = response.SessionToken });
        }        
        return Unauthorized(new { success = false, message = response.Message});
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionToken = Request.Cookies["session_token"];
        
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Ok(new { success = true, message = "Already logged out" });
        }        
        await _authService.LogoutAsync(sessionToken);        
        Response.Cookies.Delete("session_token");        
        return Ok(new { success = true, message = "Logged out successfully" });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> ValidateSession()
    {
        var sessionToken = Request.Cookies["session_token"];

        // Fallback to Authorization: Bearer header (used by frontend when cookie not yet set/available)
        if (string.IsNullOrEmpty(sessionToken))
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                sessionToken = authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        if (string.IsNullOrEmpty(sessionToken))
        {
            return Unauthorized(new { success = false, message = "No session found" });
        }

        var user = await _authService.ValidateSessionAsync(sessionToken);        
        if (user == null)
        {
            Response.Cookies.Delete("session_token");
            return Unauthorized(new { success = false, message = "Invalid or expired session" });
        }
        
        return Ok(new { success = true, user});
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var success = await _authService.CreateUserAsync(request.Username, request.Email, request.Password, request.FullName);
        
        if (success)
        {
            return Ok(new { success = true, message = "User registered successfully" });
        }
        
        return BadRequest(new { success = false, message = "Registration failed. Username or email may already exist." });
    }


    [HttpPost("reset-password")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        try
        {
            await _authService.SetPasswordResetRequest(request);
            return Ok(new { success = true });
        }
        catch (InvalidInputException)
        {
            // Ignore errors to avoid leaking user existence
            return Ok(new { success = true });
        }
        catch (Exception)
        {
            return BadRequest(new { success = false, message = "Failed to process request" });
        }                
    }



    [HttpPost("set-password")]
    public async Task<IActionResult> SetNewPassword([FromBody] SetPasswordRequest request)
    {
        try 
        {
            await _authService.UpdateUserPasswordAsync(request);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }



}


