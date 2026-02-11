using System.Diagnostics;
using System.Net;
using GalleryLib.model.album;
using GalleryLib.model.configuration;
using GalleryLib.repository;
using GalleryLib.repository.auth;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GalleryLib.service.album;


public record VirtualAlbumYml
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public string AlbumType { get { return string.IsNullOrEmpty(Expression) ? "folder" : "expression"; } }  
    public string Feature { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public bool Persistent { get; set; } = false;
    public string Parent { get; set; } = string.Empty;
    public string Role { get; set; } = "public";
}


/// <summary>
/// Rad yaml file and load all albums 
/// </summary>
public class VirtualAlbumLoaderService : IHostedService
{
    public VirtualAlbumLoaderService(IHostApplicationLifetime lifetime, PicturesDataConfiguration configuration, DatabaseConfiguration dbConfig,FileInfo yamlFile)        
    {
        _lifetime = lifetime;
        _yamlFile = yamlFile;        
        albumRepository = new AlbumRepository(configuration, dbConfig);
        _authRepository = new AuthRepository(dbConfig);
    }

    private readonly IHostApplicationLifetime _lifetime;
    private readonly FileInfo _yamlFile;
    private readonly AlbumRepository albumRepository;
    private readonly AuthRepository _authRepository;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var yaml = await File.ReadAllTextAsync(_yamlFile.FullName, cancellationToken);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();            
            var virtualAlbums = deserializer.Deserialize<Dictionary<string, VirtualAlbumYml>>(yaml);
            var roles = await _authRepository.GetAllRolesAsync();
            
            foreach (var name in virtualAlbums.Keys)        
            {
                var role = roles.FirstOrDefault(r => r.Name.Equals(virtualAlbums[name].Role, StringComparison.OrdinalIgnoreCase)) ?? roles.FirstOrDefault(r => r.Name.Equals("public", StringComparison.OrdinalIgnoreCase));
                var yalbum = virtualAlbums[name];
                var album = VirtualAlbum.CreateFromYaml(name, yalbum, role?.Id ?? 1);
                if (album.HasParentAlbum)
                {
                    var parent = await albumRepository.GetVirtualAlbumByNameAsync(yalbum.Parent);
                    if (parent == null)
                    {
                        parent = new VirtualAlbum()
                        {
                            AlbumName = yalbum.Parent,
                            AlbumDescription = "",
                            AlbumExpression = "",
                            IsPublic = true,
                            LastUpdatedUtc = DateTimeOffset.UtcNow
                        };
                        parent = await albumRepository.UpsertVirtualAlbumAsync(parent);
                    }
                    album.ParentAlbumId = parent.Id;
                }
                await albumRepository.UpsertVirtualAlbumAsync(album);
                Console.WriteLine($"Loaded virtual album: {album.AlbumName}");
            }
            
            Console.WriteLine("Virtual album loading completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load virtual albums: {ex.Message}");
        }
        finally
        {
            _lifetime.StopApplication();
        }                                
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
