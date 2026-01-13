
using GalleryLib.Model.Auth;
using Microsoft.AspNetCore.Mvc;
using GalleryApi.model;
using GalleryApi.service.auth;
using YamlDotNet.Core.Tokens;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/pictures")]
public class PicturesController : ControllerBase
{
    private readonly UserAuthService _authService;
    private readonly IConfiguration _config;
    
    public PicturesController(UserAuthService authService, IConfiguration config)
    {
        _config = config;
        _authService = authService;        
    }
    
    [HttpGet("{*filePath}")]
    public IActionResult GetPicture(string filePath)
    {
        // AppAuthMiddleware and SessionAuthMiddleware already run so we should be good to go.        
        // Return the X-Accel-Redirect header for nginx to serve
        
        //validate filePath to avoid path traversal attacks?
        if (filePath.Contains(".."))
        {
            return BadRequest(new { success = false, message = "Invalid request" });
        }
        
        //TODO: validate user is supposed to have access to this picture ?
        //user is authenticated but can only see pictures in albums they have access to

        Response.Headers.Append("X-Accel-Redirect", $"/pictures/internal/{filePath}");
        Response.ContentLength = 0;
        return Ok();
    } 

}


