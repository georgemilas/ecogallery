
using GalleryLib.Model.Auth;
using GalleryLib.model.configuration;
using Microsoft.AspNetCore.Mvc;
using GalleryApi.service.auth;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/pictures")]
public class PicturesController : ControllerBase
{
    private readonly UserAuthService _authService;
    private readonly IConfiguration _config;
    private readonly PicturesDataConfiguration _picturesConfig;

    public PicturesController(UserAuthService authService, IConfiguration config, PicturesDataConfiguration picturesConfig)
    {
        _config = config;
        _authService = authService;
        _picturesConfig = picturesConfig;
    }

    /// <summary>
    /// Auth validation endpoint for nginx auth_request.
    /// AppAuthMiddleware + SessionAuthMiddleware already run, so reaching here means access is granted.
    /// </summary>
    [HttpGet("_validate")]
    public IActionResult ValidateAccess() => Ok();

    [HttpGet("{*filePath}")]
    public IActionResult GetPicture(string filePath)
    {
        // AppAuthMiddleware and SessionAuthMiddleware already run so we should be good to go.

        if (filePath.Contains(".."))
        {
            return BadRequest(new { success = false, message = "Invalid request" });
        }

        var fullPath = Path.Combine(_picturesConfig.RootFolder.FullName, filePath);

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        // Videos: serve directly with range request support for streaming
        if (_picturesConfig.IsMovieFile(fullPath))
        {
            var contentType = GetVideoContentType(fullPath);
            return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
        }

        // Images: use X-Accel-Redirect for nginx to serve directly
        Response.Headers.Append("X-Accel-Redirect", $"/xaccel/{filePath}");
        return Ok();
    }

    private static string GetVideoContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".mkv" => "video/x-matroska",
            ".webm" => "video/webm",
            ".3gp" => "video/3gpp",
            _ => "application/octet-stream"
        };
    }
}
