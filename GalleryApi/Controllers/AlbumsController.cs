using Microsoft.AspNetCore.Mvc;
using GalleryApi.model;
using GalleryApi.service;


namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/albums")]
public class AlbumsController : ControllerBase
{
    private readonly AlbumsService _albumsService;

    public AlbumsController(AlbumsService albumsService)
    {
        _albumsService = albumsService;
    }

    // GET: /api/v1/albums
    [HttpGet]
    public async Task<ActionResult<AlbumContentHierarchical>> GetRoot()
    {

        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchicalByName();
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }


    // GET: /api/v1/albums/{albumName}
    [HttpGet("{albumName}")]
    public async Task<ActionResult<AlbumContentHierarchical>> GetAlbumContentHierarchicalDefault(string albumName)
    {
        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchicalByName(albumName);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }

    }
    
    // GET: /api/v1/albums/{albumId}
    [HttpGet("{albumId:long}")]
    public async Task<ActionResult<AlbumContentHierarchical>> GetAlbumContentHierarchicalById(long albumId)
    {
        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchicalById(albumId);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // POST: /api/v1/albums/search
    [HttpPost("search")]
    public async Task<ActionResult<VirtualAlbumContent>> SearchAlbumData([FromBody] AlbumSearch albumSearch)
    {
        try
        {
            var albumContent = await _albumsService.SearchContentByExpression(albumSearch);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }

    }

    // GET: /api/v1/albums/random
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

    // GET: /api/v1/albums/recent
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

    // POST: /api/v1/albums/settings
    [HttpPost("settings")]
    public async Task<ActionResult<GalleryLib.model.album.AlbumSettings>> AddOrUpdateAlbumSettings([FromBody] GalleryLib.model.album.AlbumSettings albumSettings)
    {
        try
        {
            albumSettings.IsVirtual = false;
            Console.WriteLine($"AlbumsController: AddOrUpdateAlbumSettings called for {albumSettings}");
            var updatedSettings = await _albumsService.SaveAlbumSettingsAsync(albumSettings);
            return Ok(updatedSettings);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }

    }

}
