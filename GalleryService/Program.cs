using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using GalleryLib.service;
using GalleryLib.model;
using System.Threading;
using GalleryLib.model.configuration;
using GalleryLib.service.thumbnail;
using GalleryLib.service.album;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
var basePath = AppContext.BaseDirectory;

var configuration = new ConfigurationBuilder()
    //.SetBasePath(Directory.GetCurrentDirectory())  // use current directory when running in development
    .SetBasePath(basePath)   // use assembly base path when running as published app
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup Dependency injection to enable future testability  
var serviceProvider = new ServiceCollection()
    .Configure<PicturesDataConfiguration>(configuration.GetSection(PicturesDataConfiguration.SectionName))
    .Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.SectionName))
    .BuildServiceProvider();

var picturesConfig = serviceProvider.GetRequiredService<IOptions<PicturesDataConfiguration>>().Value;
var dbConfig = serviceProvider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
// Console.WriteLine($"Base path: {basePath}");
// Console.WriteLine($"Environment: {environment}");
// Console.WriteLine($"appsettings.json exists: {File.Exists(Path.Combine(basePath, "appsettings.json"))}");
// Console.WriteLine($"appsettings.{environment}.json exists: {File.Exists(Path.Combine(basePath, $"appsettings.{environment}.json"))}");
// Console.WriteLine($"Database:Username from config: '{configuration["Database:Username"]}'");
// Console.WriteLine($"Database:Password from config: '{configuration["Database:Password"]}'");
// Console.WriteLine($"Database:Database from config: '{configuration["Database:Database"]}'");

// Console.WriteLine("Press Enter to continue...");
// Console.ReadLine(); // Press Enter to continue...

var rootCommand = new RootCommand("Pictures background services console application");
var folderOption = new Option<string>(new[] {"--folder", "-f"}, () => picturesConfig.Folder, "Pictures folder path");
var yamlFileOption = new Option<string?>(new[] {"--yaml", "-y"}, () => null, "YAML file path");
var heightsOption = new Option<int[]>(new[] {"--height", "-h"}, () => new[] { 400, 1080, 1440 }, "Thumbnail heights in pixels (can specify multiple)")
{
    AllowMultipleArgumentsPerToken = true
};
var heightOption = new Option<int>(new[] {"--height", "-h"}, () => 400, "Thumbnail height in pixels");
var nonParallelOption = new Option<bool>(new[] {"--nonparallel", "-np"}, "Run thumbnail processing in non-parallel mode");
var parallelDegreeOption = new Option<int>(new[] {"--parallel", "-p"}, () => Environment.ProcessorCount, "Degree of parallelism for thumbnail processing");

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// thumb   400
/// HD      1080
/// UHD     1440
/// 4K      2160
/// 5K      2880 
/// 8K      4320
var thumbCommand = new Command("thumbnails", "Run the thumbnail building processor as a background service");
thumbCommand.AddOption(folderOption);
thumbCommand.AddOption(heightsOption);
thumbCommand.AddOption(nonParallelOption);
thumbCommand.AddOption(parallelDegreeOption);
thumbCommand.SetHandler(async (string folder, int[] heights, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                if (heights.Length == 1) { services.AddSingleton<IHostedService>(sp => ThumbnailProcessor.CreateProcessorNotParallel(picturesConfig, heights[0])); }  
                else {services.AddSingleton<IHostedService>(sp => MultipleThumbnailsProcessor.CreateProcessorNotParallel(picturesConfig, heights));}
            }
            else
            {
                if (heights.Length == 1) { services.AddSingleton<IHostedService>(sp => ThumbnailProcessor.CreateProcessor(picturesConfig, heights[0], parallelDegree)); }  
                else {services.AddSingleton<IHostedService>(sp => MultipleThumbnailsProcessor.CreateProcessor(picturesConfig, heights, parallelDegree));}                
            }
        })
        .Build();

    Console.WriteLine($"Starting thumbnail processor on '{picturesConfig.Folder}' with heights=[{string.Join(", ", heights)}]. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, heightsOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(thumbCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the thumbnail cleanup background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var cleanupCommand = new Command("cleanup", "Run the thumbnail cleanup processor as a background service");
cleanupCommand.AddOption(folderOption);
cleanupCommand.AddOption(heightOption);
cleanupCommand.AddOption(nonParallelOption);
cleanupCommand.AddOption(parallelDegreeOption);
cleanupCommand.SetHandler(async (string folder, int height, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailCleanup.CreateProcessorNotParallel(picturesConfig, height));
            }
            else
            {
                services.AddSingleton<IHostedService>(sp => ThumbnailCleanup.CreateProcessor(picturesConfig, height, parallelDegree));
            }
            
        })
        .Build();

    Console.WriteLine($"Starting thumbnail cleanup processor for pictures on '{picturesConfig.Folder}' with height={height}. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, heightOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(cleanupCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the album building background service and keep the app running
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var albumCommand = new Command("album", "Run the album building processor as a background service");
albumCommand.AddOption(folderOption);
albumCommand.AddOption(nonParallelOption);
albumCommand.AddOption(parallelDegreeOption);
albumCommand.SetHandler(async (string folder, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                services.AddSingleton<IHostedService>(sp => AlbumProcessor.CreateProcessorNotParallel(picturesConfig, dbConfig));
            }
            else
            {
                services.AddSingleton<IHostedService>(sp => AlbumProcessor.CreateProcessor(picturesConfig, dbConfig, parallelDegree));
            }            
        })
        .Build();

    Console.WriteLine($"Starting album buiding processor on '{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(albumCommand);


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run the album building background service along with image exif extraction functionality 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var imageExifCommand = new Command("db", "Run the db sync processor as a background service");
imageExifCommand.AddOption(folderOption);
imageExifCommand.AddOption(nonParallelOption);
imageExifCommand.AddOption(parallelDegreeOption);
imageExifCommand.SetHandler(async (string folder, bool nonParallel, int parallelDegree) =>
{
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            if (nonParallel)
            {
                services.AddSingleton<IHostedService>(sp => DbSyncProcessor.CreateProcessorNotParallel(picturesConfig, dbConfig));
            }
            else
            {
                services.AddSingleton<IHostedService>(sp => DbSyncProcessor.CreateProcessor(picturesConfig, dbConfig, parallelDegree));
            }            
        })
        .Build();

    Console.WriteLine($"Starting db sync processor on '{picturesConfig.Folder}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, nonParallelOption, parallelDegreeOption);
rootCommand.AddCommand(imageExifCommand);



//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Command to run load virtual albums from a yaml file  
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
var valbumCommand = new Command("valbum", "Load virtual albums from a YAML file");
valbumCommand.AddOption(folderOption);
valbumCommand.AddOption(yamlFileOption);
valbumCommand.SetHandler(async (string folder, string? yamlFilePath) =>
{
    if (string.IsNullOrWhiteSpace(yamlFilePath) || !File.Exists(yamlFilePath))
    {
        Console.WriteLine("YAML file path is required and must exist.");
        return;
    }
    picturesConfig.Folder = folder;
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

    var host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<IHostedService>(sp => new VirtualAlbumLoaderService(
                sp.GetRequiredService<IHostApplicationLifetime>(),
                picturesConfig,
                dbConfig,
                new FileInfo(yamlFilePath!)
            ));                        
        })
        .Build();

    Console.WriteLine($"Starting virtual album loader on '{yamlFilePath}'. Press Ctrl+C to stop.");
    await host.RunAsync(cts.Token);
}, folderOption, yamlFileOption);
rootCommand.AddCommand(valbumCommand);



return await rootCommand.InvokeAsync(args);
