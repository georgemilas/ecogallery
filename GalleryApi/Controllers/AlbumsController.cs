using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GalleryApi.model;
using GalleryApi.service;
using GalleryLib.model.configuration;
using GalleryLib.repository;

namespace GalleryApi.Controllers;

[ApiController]
[Route("api/v1/albums")]
public class AlbumsController : ControllerBase
{
    private readonly AlbumRepository _albumRepository;
    private readonly AlbumsService _albumsService;

    public AlbumsController(AlbumRepository albumRepository, AlbumsService albumsService)
    {
        _albumRepository = albumRepository;
        _albumsService = albumsService;
    }

    // GET: /api/v1/albums
    [HttpGet]
    public async Task<ActionResult<AlbumContentHierarchical>> GetRoot()
    {

        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchical();
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    // GET: /api/v1/albums/root/hierarchy
    [HttpGet("root/hierarchy")]
    public async Task<ActionResult<AlbumContentHierarchical>> GetRootHierarchy()
    {
        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchical();
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
            var albumContent = await _albumsService.GetAlbumContentHierarchical(albumName);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }

    }
    // GET: /api/v1/albums/{albumName}/hierarchy
    [HttpGet("{albumName}/hierarchy")]
    public async Task<ActionResult<AlbumContentHierarchical>> GetAlbumContentHierarchicalByName(string albumName)
    {
        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchical(albumName);
            return Ok(albumContent);
        }
        catch(AlbumNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
    // GET: /api/v1/albums/{albumId}/hierarchy
    [HttpGet("{albumId:long}/hierarchy")]
    public async Task<ActionResult<AlbumContentHierarchical>> GetAlbumContentHierarchicalById(long albumId)
    {
        try
        {
            var albumContent = await _albumsService.GetAlbumContentHierarchical(albumId);
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


    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _albumRepository?.Dispose();
        }
    }
}
