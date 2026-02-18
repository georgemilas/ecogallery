using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GalleryApi.model;
using GalleryApi.service;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/valbums")]
public class VirtualAlbumsController : ControllerBase
{
    private readonly VirtualAlbumsService _albumsService;

    public VirtualAlbumsController(VirtualAlbumsService albumsService)
    {
        _albumsService = albumsService;
    }

    // GET: /api/v1/valbums
    [HttpGet]
    public async Task<ActionResult<VirtualAlbumContent>> GetRoot()
    {

        try
        {
            var albumContent = await _albumsService.GetRootVirtualAlbumsContentAsync();
            albumContent = await _albumsService.FilterByRole(albumContent);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
   

    // GET: /api/v1/valbums/tree
    [HttpGet("tree")]
    public async Task<ActionResult<List<AlbumTree>>> GetVirtualAlbumsTree()
    {
        if (_albumsService.AuthenticatedUser == null || !_albumsService.AuthenticatedUser.Roles.Contains("album_admin"))
            return StatusCode(403, new { error = "You must have the album_admin role to access this resource" });

        var tree = await _albumsService.GetVirtualAlbumsTreeAsync();
        return Ok(tree);
    }

    // POST: /api/v1/valbums/save
    [HttpPost("save")]
    public async Task<ActionResult<VirtualAlbum>> SaveVirtualAlbum([FromBody] VirtualAlbum album)
    {
        if (_albumsService.AuthenticatedUser == null || !_albumsService.AuthenticatedUser.Roles.Contains("album_admin"))
            return StatusCode(403, new { error = "You must have the album_admin role to manage albums" });

        try
        {
            var saved = await _albumsService.SaveVirtualAlbumAsync(album);
            return Ok(saved);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: /api/v1/valbums/{albumId}
    [HttpDelete("{albumId:long}")]
    public async Task<ActionResult> DeleteVirtualAlbum(long albumId)
    {
        if (_albumsService.AuthenticatedUser == null || !_albumsService.AuthenticatedUser.Roles.Contains("album_admin"))
            return StatusCode(403, new { error = "You must have the album_admin role to manage albums" });

        try
        {
            await _albumsService.DeleteVirtualAlbumAsync(albumId);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: /api/v1/valbums/{albumId}
    [HttpGet("{albumId:long}")]
    public async Task<ActionResult<VirtualAlbumContent>> GetVirtualAlbumContentById(long albumId)
    {
        try
        {
            var albumContent = await _albumsService.GetVirtualAlbumContentByIdAsync(albumId);

            bool isLoggedIn = _albumsService.AuthenticatedUser != null;
            bool isAdmin = _albumsService.AuthenticatedUser != null ? _albumsService.AuthenticatedUser.IsAdmin : false;
            var userRolesIds = await _albumsService.GetRoleIds();
            // Console.WriteLine($"Debug: user role ids {string.Join(", ", userRolesIds)}");
            // Console.WriteLine($"Debug: Checking virtual album '{albumContent.Name}' for access. RoleId={albumContent.RoleId}");
        
            if (!isLoggedIn && albumContent.RoleId != 1) //not logged in can only see public
                return StatusCode(403, new { error = "You must be logged in to access albums" });
            if (!isAdmin && !userRolesIds.Contains(albumContent.RoleId)) //non admin must have role to see
                return StatusCode(403, new { error = "You do not have permission to access this album" });

            albumContent = await _albumsService.FilterByRole(albumContent);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // GET: /api/v1/valbums/random
    [HttpGet("random")]
    public async Task<ActionResult<VirtualAlbumContent>> GetRandomImages()
    {
        try
        {
            var albumContent = await _albumsService.GetRandomImages();
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }

    }

    // GET: /api/v1/valbums/recent
    [HttpGet("recent")]
    public async Task<ActionResult<VirtualAlbumContent>> GetRecentImages()
    {
        try
        {
            var albumContent = await _albumsService.GetRecentImages();
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }




    // POST: /api/v1/valbums/settings
    [HttpPost("settings")]
    public async Task<ActionResult<GalleryLib.model.album.AlbumSettings>> AddOrUpdateAlbumSettings([FromBody] GalleryLib.model.album.AlbumSettings albumSettings)
    {
        try
        {
            albumSettings.IsVirtual = true;
            Console.WriteLine($"VirtualAlbumsController: AddOrUpdateAlbumSettings called for {albumSettings}");
            var updatedSettings = await _albumsService.SaveAlbumSettingsAsync(albumSettings);
            return Ok(updatedSettings);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }

    }


}
