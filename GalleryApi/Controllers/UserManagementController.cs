using Microsoft.AspNetCore.Mvc;
using GalleryApi.model;
using GalleryApi.service.auth;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UserManagementController : ControllerBase
{
    private readonly UserAuthService _authService;

    public UserManagementController(UserAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        try
        {
            var result = await _authService.GetAllRolesWithEffectiveRolesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { success = false, message = "Failed to retrieve roles." });
        }
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            // Support both single ParentRoleId and multiple ParentRoleIds
            var parentIds = request.ParentRoleIds ?? (request.ParentRoleId.HasValue ? [request.ParentRoleId.Value] : null);
            var roleId = await _authService.CreateRoleAsync(request.Name, request.Description, parentIds);
            return Ok(new { success = true, id = roleId, message = "Role created successfully." });
        }
        catch (InvalidInputException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { success = false, message = "Failed to create role." });
        }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        try
        {
            await _authService.CreateUserInvitationAsync(request);
            return Ok(new { success = true, message = "Invitation sent successfully." });
        }
        catch (InvalidInputException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { success = false, message = "Failed to send invitation." });
        }
    }
}
