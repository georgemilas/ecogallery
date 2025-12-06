using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PicturesLib.model.album;
using PicturesLib.model.configuration;
using PicturesLib.repository;

namespace WeatherApi.Controllers;

[ApiController]
[Route("api/v1/albums")]
public class AlbumsController : ControllerBase
{
    private readonly AlbumRepository _albumRepository;
    private readonly PicturesDataConfiguration _picturesConfig;

    public AlbumsController(IOptions<PicturesDataConfiguration> picturesOptions)
    {
        _picturesConfig = picturesOptions.Value;
        _albumRepository = new AlbumRepository(_picturesConfig);
    }

    // GET: /api/v1/albums
    [HttpGet]
    public async Task<ActionResult<List<AlbumContentHierarchical>>> GetRoot()
    {
        var albumContent = await _albumRepository.GetRootAlbumContentHierarchical();
        return Ok(albumContent);
    }

    // GET: /api/v1/albums/root/hierarchy
    [HttpGet("root/hierarchy")]
    public async Task<ActionResult<List<AlbumContentHierarchical>>> GetRootHierarchy()
    {
        var albumContent = await _albumRepository.GetRootAlbumContentHierarchical();
        return Ok(albumContent);
    }

    // GET: /api/v1/albums/{albumName}
    [HttpGet("{albumName}")]
    public async Task<ActionResult<List<AlbumContentHierarchical>>> GetAlbumContentHierarchicalDefault(string albumName)
    {
        var albumContent = await _albumRepository.GetAlbumContentHierarchicalByName(albumName);
        return Ok(albumContent);
    }
    // GET: /api/v1/albums/{albumName}/hierarchy
    [HttpGet("{albumName}/hierarchy")]
    public async Task<ActionResult<List<AlbumContentHierarchical>>> GetAlbumContentHierarchicalByName(string albumName)
    {
        var albumContent = await _albumRepository.GetAlbumContentHierarchicalByName(albumName);
        return Ok(albumContent);
    }
    // GET: /api/v1/albums/{albumId}/hierarchy
    [HttpGet("{albumId:long}/hierarchy")]
    public async Task<ActionResult<List<AlbumContentHierarchical>>> GetAlbumContentHierarchicalById(long albumId)
    {
        var albumContent = await _albumRepository.GetAlbumContentHierarchicalById(albumId);
        return Ok(albumContent);
    }


    // GET: /api/v1/albums/{albumName}/flatten
    [HttpGet("{albumName}/flatten")]
    public async Task<ActionResult<List<AlbumContentFlatten>>> GetAlbumContentFlattenByName(string albumName)
    {
        var albumContent = await _albumRepository.GetAlbumContentFlattenByName(albumName);
        return Ok(albumContent);
    }
    // GET: /api/v1/albums/{albumId}/flatten
    [HttpGet("{albumId:long}/flatten")]
    public async Task<ActionResult<List<AlbumContentFlatten>>> GetAlbumContentFlattenById(long albumId)
    {
        var albumContent = await _albumRepository.GetAlbumContentFlattenById(albumId);
        return Ok(albumContent);
    }


    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _albumRepository?.Dispose();
        }
    }
}
