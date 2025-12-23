using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GalleryApi.model;
using GalleryApi.service;
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

    // GET: /api/v1/albums
    [HttpGet]
    public async Task<ActionResult<VirtualAlbumContent>> GetRoot()
    {

        try
        {
            var albumContent = await _albumsService.GetRootVirtualAlbumsContentAsync();
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
   

    // GET: /api/v1/valbums/{albumId}
    [HttpGet("{albumId:long}")]
    public async Task<ActionResult<VirtualAlbumContent>> GetVirtualAlbumContentById(long albumId)
    {
        try
        {
            var albumContent = await _albumsService.GetVirtualAlbumContentByIdAsync(albumId);
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
