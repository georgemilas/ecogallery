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
    private readonly AlbumRepository _albumRepository;
    private readonly VirtualAlbumsService _albumsService;

    public VirtualAlbumsController(AlbumRepository albumRepository, VirtualAlbumsService albumsService)
    {
        _albumRepository = albumRepository;
        _albumsService = albumsService;
    }

    // GET: /api/v1/albums
    [HttpGet]
    public async Task<ActionResult<VirtualAlbumContent>> GetRoot()
    {

        try
        {
            var albumContent = await _albumsService.GetRootVirtualAlbumsContent();
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
            var albumContent = await _albumsService.GetVirtualAlbumContentById(albumId);
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
